using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.HUD
{
    public enum MinimapCellKind : byte
    {
        Empty = 0,
        Solid = 1,
        OneWay = 2,
        Climbable = 3,
    }

    public enum MinimapMarkerKind : byte
    {
        Player = 0,
        Enemy = 1,
        Scroll = 2,
        Exit = 3,
        Teleport = 4,
        Chest = 5,
    }

    public readonly struct MinimapMarker
    {
        public readonly Vector2 WorldPosition;
        public readonly MinimapMarkerKind Kind;

        public MinimapMarker(Vector2 worldPosition, MinimapMarkerKind kind)
        {
            WorldPosition = worldPosition;
            Kind = kind;
        }
    }

    /// <summary>
    /// Persistent Dead Cells-style map grid with chunked terrain caching, flood-filled fog-of-war
    /// exploration (does not leak through solid walls), and crisp silhouette pixel rasterization.
    /// </summary>
    public sealed class MinimapMapData
    {
        public const int ChunkSize = 16;
        private const int ChunkArea = ChunkSize * ChunkSize;

        // Dead Cells signature slate-cyan palette.
        public static readonly Color32 ColorVoid = new Color32(10, 14, 24, 0);
        public static readonly Color32 ColorRoomFill = new Color32(24, 62, 102, 224);
        public static readonly Color32 ColorRoomShade = new Color32(18, 48, 82, 224);
        public static readonly Color32 ColorFloorRim = new Color32(124, 226, 255, 255);
        public static readonly Color32 ColorWallOutline = new Color32(66, 170, 212, 245);
        public static readonly Color32 ColorCornerOutline = new Color32(46, 128, 168, 228);
        public static readonly Color32 ColorOneWay = new Color32(138, 224, 236, 248);
        public static readonly Color32 ColorClimbable = new Color32(242, 196, 78, 255);

        public static readonly Color32 ColorPlayerCore = new Color32(255, 252, 228, 255);
        public static readonly Color32 ColorPlayerPulse = new Color32(255, 212, 84, 255);
        public static readonly Color32 ColorPlayerOutline = new Color32(12, 18, 30, 255);
        public static readonly Color32 ColorEnemy = new Color32(255, 52, 68, 255);
        public static readonly Color32 ColorEnemyRim = new Color32(128, 16, 26, 255);
        public static readonly Color32 ColorScroll = new Color32(224, 120, 255, 255);
        public static readonly Color32 ColorScrollCore = new Color32(255, 244, 184, 255);
        public static readonly Color32 ColorExit = new Color32(58, 242, 136, 255);
        public static readonly Color32 ColorExitCore = new Color32(218, 255, 234, 255);
        public static readonly Color32 ColorTeleport = new Color32(66, 246, 255, 255);
        public static readonly Color32 ColorChest = new Color32(255, 204, 68, 255);

        private sealed class Chunk
        {
            public readonly byte[] Kinds = new byte[ChunkArea];
            public readonly bool[] Sampled = new bool[ChunkArea];
            public readonly bool[] Explored = new bool[ChunkArea];
        }

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly Queue<Vector2Int> _bfsQueue = new(256);
        private readonly HashSet<Vector2Int> _bfsVisited = new();

        public float CellSize { get; }
        public int ExploredOpenCellCount { get; private set; }
        public bool HasExploredBounds { get; private set; }
        public Vector2Int ExploredMin { get; private set; }
        public Vector2Int ExploredMax { get; private set; }

        public MinimapMapData(float cellSize = 0.5f)
        {
            if (cellSize <= 0.01f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            CellSize = cellSize;
        }

        public Vector2Int WorldToCell(Vector2 worldPosition) => new(
            Mathf.FloorToInt(worldPosition.x / CellSize),
            Mathf.FloorToInt(worldPosition.y / CellSize));

        public Vector2 CellToWorldCenter(int cellX, int cellY) => new(
            (cellX + 0.5f) * CellSize,
            (cellY + 0.5f) * CellSize);

        public bool IsSampled(int cellX, int cellY)
        {
            if (!TryGetChunk(cellX, cellY, out Chunk chunk, out int localIndex))
                return false;
            return chunk.Sampled[localIndex];
        }

        public MinimapCellKind GetCell(int cellX, int cellY)
        {
            if (!TryGetChunk(cellX, cellY, out Chunk chunk, out int localIndex))
                return MinimapCellKind.Empty;
            return (MinimapCellKind)chunk.Kinds[localIndex];
        }

        public bool IsExplored(int cellX, int cellY)
        {
            if (!TryGetChunk(cellX, cellY, out Chunk chunk, out int localIndex))
                return false;
            return chunk.Explored[localIndex];
        }

        public void SetCell(int cellX, int cellY, MinimapCellKind kind)
        {
            Chunk chunk = GetOrCreateChunk(cellX, cellY, out int localIndex);
            chunk.Kinds[localIndex] = (byte)kind;
            chunk.Sampled[localIndex] = true;
        }

        /// <summary>
        /// Clears cached samples around a world position (e.g. when a gate opens or destructible wall breaks).
        /// Exploration state is preserved.
        /// </summary>
        public void InvalidateSamples(Vector2 worldCenter, float radiusUnits)
        {
            Vector2Int center = WorldToCell(worldCenter);
            int radiusCells = Mathf.CeilToInt(radiusUnits / CellSize);
            InvalidateSamples(
                center.x - radiusCells,
                center.y - radiusCells,
                center.x + radiusCells,
                center.y + radiusCells);
        }

        /// <summary>
        /// Clears cached samples inside an inclusive cell rectangle. Exploration state is preserved.
        /// </summary>
        public void InvalidateSamples(int minX, int minY, int maxX, int maxY)
        {
            if (minX > maxX)
                (minX, maxX) = (maxX, minX);
            if (minY > maxY)
                (minY, maxY) = (maxY, minY);

            for (int cy = minY; cy <= maxY; cy++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    if (TryGetChunk(cx, cy, out Chunk chunk, out int localIndex))
                        chunk.Sampled[localIndex] = false;
                }
            }
        }

        /// <summary>
        /// Reveals reachable non-solid cells within an elliptical vision window around worldCenter using
        /// line-of-room BFS so unexplored rooms behind solid walls stay hidden under fog-of-war.
        /// Returns the number of newly explored open cells.
        /// </summary>
        public int RevealAround(Vector2 worldCenter, float radiusXUnits, float radiusYUnits,
            Func<int, int, MinimapCellKind> sampler)
        {
            if (sampler == null)
                throw new ArgumentNullException(nameof(sampler));

            Vector2Int origin = WorldToCell(worldCenter);
            int rx = Mathf.Max(1, Mathf.CeilToInt(radiusXUnits / CellSize));
            int ry = Mathf.Max(1, Mathf.CeilToInt(radiusYUnits / CellSize));
            float invRxSq = 1f / (rx * rx);
            float invRySq = 1f / (ry * ry);

            // Find an open seed cell near the player (handles crouching/hugging thin ledges).
            Vector2Int seed = FindOpenSeed(origin, sampler);

            _bfsQueue.Clear();
            _bfsVisited.Clear();
            _bfsQueue.Enqueue(seed);
            _bfsVisited.Add(seed);

            int newlyExplored = 0;
            while (_bfsQueue.Count > 0)
            {
                Vector2Int cell = _bfsQueue.Dequeue();
                MinimapCellKind kind = SampleCell(cell.x, cell.y, sampler);
                if (kind == MinimapCellKind.Solid)
                    continue;

                if (MarkExplored(cell.x, cell.y, isOpen: true))
                    newlyExplored++;

                // Sample 8-neighbors so adjacent walls and corners always outline cleanly.
                for (int ny = -1; ny <= 1; ny++)
                {
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        if (nx == 0 && ny == 0)
                            continue;

                        int tx = cell.x + nx;
                        int ty = cell.y + ny;
                        MinimapCellKind neighborKind = SampleCell(tx, ty, sampler);

                        if (neighborKind == MinimapCellKind.Solid)
                        {
                            ExpandBounds(tx, ty);
                            continue;
                        }

                        // Flood-fill orthogonally (4-connected) so vision cannot squeeze through diagonal wall seams.
                        if (nx != 0 && ny != 0)
                            continue;

                        int dx = tx - origin.x;
                        int dy = ty - origin.y;
                        if (dx * dx * invRxSq + dy * dy * invRySq > 1f)
                            continue;

                        var next = new Vector2Int(tx, ty);
                        if (_bfsVisited.Add(next))
                            _bfsQueue.Enqueue(next);
                    }
                }
            }

            return newlyExplored;
        }

        public bool IsAreaExplored(Vector2 worldPosition)
        {
            Vector2Int cell = WorldToCell(worldPosition);
            if (IsExplored(cell.x, cell.y))
                return true;

            // Allow markers resting on solid floors (scroll pedestals, exit doors) to show once adjacent air is explored.
            return IsExplored(cell.x, cell.y + 1)
                || IsExplored(cell.x - 1, cell.y)
                || IsExplored(cell.x + 1, cell.y)
                || IsExplored(cell.x, cell.y - 1);
        }

        /// <summary>
        /// Renders a rectangular region of cells (originCellX..originCellX+width-1, originCellY..originCellY+height-1)
        /// into a flat Color32 pixel buffer with Dead Cells room silhouettes and pixel markers.
        /// </summary>
        public void RenderWindow(
            Color32[] buffer,
            int width,
            int height,
            int originCellX,
            int originCellY,
            IReadOnlyList<MinimapMarker> markers,
            float pulse01)
        {
            if (buffer == null || buffer.Length < width * height)
                throw new ArgumentException("Pixel buffer is too small for the requested dimensions.", nameof(buffer));

            for (int py = 0; py < height; py++)
            {
                int cy = originCellY + py;
                int rowOffset = py * width;
                for (int px = 0; px < width; px++)
                {
                    int cx = originCellX + px;
                    buffer[rowOffset + px] = EvaluateCellColor(cx, cy);
                }
            }

            if (markers == null || markers.Count == 0)
                return;

            // Draw non-player landmarks first, enemies above landmarks, player always on top.
            for (int i = 0; i < markers.Count; i++)
            {
                MinimapMarker marker = markers[i];
                if (marker.Kind != MinimapMarkerKind.Player && marker.Kind != MinimapMarkerKind.Enemy)
                    DrawMarker(buffer, width, height, originCellX, originCellY, marker, pulse01);
            }

            for (int i = 0; i < markers.Count; i++)
            {
                MinimapMarker marker = markers[i];
                if (marker.Kind == MinimapMarkerKind.Enemy)
                    DrawMarker(buffer, width, height, originCellX, originCellY, marker, pulse01);
            }

            for (int i = 0; i < markers.Count; i++)
            {
                MinimapMarker marker = markers[i];
                if (marker.Kind == MinimapMarkerKind.Player)
                    DrawMarker(buffer, width, height, originCellX, originCellY, marker, pulse01);
            }
        }

        private Color32 EvaluateCellColor(int cx, int cy)
        {
            if (!TryGetChunk(cx, cy, out Chunk chunk, out int localIndex) || !chunk.Sampled[localIndex])
                return ColorVoid;

            var kind = (MinimapCellKind)chunk.Kinds[localIndex];
            if (kind == MinimapCellKind.Solid)
            {
                // Top floor edge gets the brightest cyan rim for immediate platformer silhouette clarity.
                if (IsExploredOpen(cx, cy + 1))
                    return ColorFloorRim;
                if (IsExploredOpen(cx - 1, cy) || IsExploredOpen(cx + 1, cy) || IsExploredOpen(cx, cy - 1))
                    return ColorWallOutline;
                if (IsExploredOpen(cx - 1, cy + 1) || IsExploredOpen(cx + 1, cy + 1)
                    || IsExploredOpen(cx - 1, cy - 1) || IsExploredOpen(cx + 1, cy - 1))
                    return ColorCornerOutline;
                return ColorVoid;
            }

            if (!chunk.Explored[localIndex])
                return ColorVoid;

            if (kind == MinimapCellKind.Climbable)
                return ColorClimbable;
            if (kind == MinimapCellKind.OneWay)
                return ColorOneWay;

            // Subtle ceiling shadow band inside explored corridors gives Dead Cells blueprint depth.
            bool ceilingAbove = GetCell(cx, cy + 1) == MinimapCellKind.Solid;
            return ceilingAbove ? ColorRoomShade : ColorRoomFill;
        }

        private bool IsExploredOpen(int cx, int cy)
        {
            if (!TryGetChunk(cx, cy, out Chunk chunk, out int localIndex))
                return false;
            return chunk.Explored[localIndex] && (MinimapCellKind)chunk.Kinds[localIndex] != MinimapCellKind.Solid;
        }

        private void DrawMarker(
            Color32[] buffer,
            int width,
            int height,
            int originCellX,
            int originCellY,
            MinimapMarker marker,
            float pulse01)
        {
            Vector2Int cell = WorldToCell(marker.WorldPosition);
            if (marker.Kind != MinimapMarkerKind.Player && !IsAreaExplored(marker.WorldPosition))
                return;

            int px = cell.x - originCellX;
            int py = cell.y - originCellY;
            if (px < -3 || py < -3 || px >= width + 3 || py >= height + 3)
                return;

            switch (marker.Kind)
            {
                case MinimapMarkerKind.Player:
                    Color32 playerTint = Color32.Lerp(ColorPlayerPulse, ColorPlayerCore, Mathf.Clamp01(pulse01));
                    PlotPixel(buffer, width, height, px - 1, py - 1, ColorPlayerOutline);
                    PlotPixel(buffer, width, height, px + 1, py - 1, ColorPlayerOutline);
                    PlotPixel(buffer, width, height, px - 1, py + 1, ColorPlayerOutline);
                    PlotPixel(buffer, width, height, px + 1, py + 1, ColorPlayerOutline);
                    PlotPixel(buffer, width, height, px, py - 1, playerTint);
                    PlotPixel(buffer, width, height, px, py + 1, playerTint);
                    PlotPixel(buffer, width, height, px - 1, py, playerTint);
                    PlotPixel(buffer, width, height, px + 1, py, playerTint);
                    PlotPixel(buffer, width, height, px, py, ColorPlayerCore);
                    break;

                case MinimapMarkerKind.Enemy:
                    PlotPixel(buffer, width, height, px - 1, py, ColorEnemyRim);
                    PlotPixel(buffer, width, height, px + 1, py, ColorEnemyRim);
                    PlotPixel(buffer, width, height, px, py - 1, ColorEnemyRim);
                    PlotPixel(buffer, width, height, px, py + 1, ColorEnemy);
                    PlotPixel(buffer, width, height, px, py, ColorEnemy);
                    break;

                case MinimapMarkerKind.Scroll:
                    PlotPixel(buffer, width, height, px, py - 1, ColorScroll);
                    PlotPixel(buffer, width, height, px, py + 1, ColorScroll);
                    PlotPixel(buffer, width, height, px - 1, py, ColorScroll);
                    PlotPixel(buffer, width, height, px + 1, py, ColorScroll);
                    PlotPixel(buffer, width, height, px, py, ColorScrollCore);
                    break;

                case MinimapMarkerKind.Exit:
                    for (int dy = 0; dy <= 3; dy++)
                    {
                        PlotPixel(buffer, width, height, px - 1, py + dy, ColorExit);
                        PlotPixel(buffer, width, height, px + 1, py + dy, ColorExit);
                    }
                    PlotPixel(buffer, width, height, px, py + 3, ColorExit);
                    PlotPixel(buffer, width, height, px, py + 2, ColorExitCore);
                    PlotPixel(buffer, width, height, px, py + 1, ColorExitCore);
                    PlotPixel(buffer, width, height, px, py, ColorExit);
                    break;

                case MinimapMarkerKind.Teleport:
                    PlotPixel(buffer, width, height, px, py - 1, ColorTeleport);
                    PlotPixel(buffer, width, height, px, py + 1, ColorTeleport);
                    PlotPixel(buffer, width, height, px - 1, py, ColorTeleport);
                    PlotPixel(buffer, width, height, px + 1, py, ColorTeleport);
                    PlotPixel(buffer, width, height, px, py, ColorPlayerCore);
                    break;

                case MinimapMarkerKind.Chest:
                    PlotPixel(buffer, width, height, px - 1, py, ColorChest);
                    PlotPixel(buffer, width, height, px, py, ColorChest);
                    PlotPixel(buffer, width, height, px + 1, py, ColorChest);
                    PlotPixel(buffer, width, height, px - 1, py + 1, ColorPlayerCore);
                    PlotPixel(buffer, width, height, px, py + 1, ColorChest);
                    PlotPixel(buffer, width, height, px + 1, py + 1, ColorPlayerCore);
                    break;
            }
        }

        private static void PlotPixel(Color32[] buffer, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            buffer[y * width + x] = color;
        }

        private Vector2Int FindOpenSeed(Vector2Int origin, Func<int, int, MinimapCellKind> sampler)
        {
            if (SampleCell(origin.x, origin.y, sampler) != MinimapCellKind.Solid)
                return origin;

            for (int r = 1; r <= 3; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                            continue;
                        if (SampleCell(origin.x + dx, origin.y + dy, sampler) != MinimapCellKind.Solid)
                            return new Vector2Int(origin.x + dx, origin.y + dy);
                    }
                }
            }

            return origin;
        }

        private MinimapCellKind SampleCell(int cellX, int cellY, Func<int, int, MinimapCellKind> sampler)
        {
            Chunk chunk = GetOrCreateChunk(cellX, cellY, out int localIndex);
            if (!chunk.Sampled[localIndex])
            {
                chunk.Kinds[localIndex] = (byte)sampler(cellX, cellY);
                chunk.Sampled[localIndex] = true;
            }
            return (MinimapCellKind)chunk.Kinds[localIndex];
        }

        private bool MarkExplored(int cellX, int cellY, bool isOpen)
        {
            Chunk chunk = GetOrCreateChunk(cellX, cellY, out int localIndex);
            if (chunk.Explored[localIndex])
                return false;

            chunk.Explored[localIndex] = true;
            if (isOpen)
                ExploredOpenCellCount++;
            ExpandBounds(cellX, cellY);
            return true;
        }

        private void ExpandBounds(int cellX, int cellY)
        {
            if (!HasExploredBounds)
            {
                ExploredMin = new Vector2Int(cellX, cellY);
                ExploredMax = new Vector2Int(cellX, cellY);
                HasExploredBounds = true;
                return;
            }

            ExploredMin = new Vector2Int(Mathf.Min(ExploredMin.x, cellX), Mathf.Min(ExploredMin.y, cellY));
            ExploredMax = new Vector2Int(Mathf.Max(ExploredMax.x, cellX), Mathf.Max(ExploredMax.y, cellY));
        }

        private Chunk GetOrCreateChunk(int cellX, int cellY, out int localIndex)
        {
            int chunkX = FloorDiv(cellX, ChunkSize);
            int chunkY = FloorDiv(cellY, ChunkSize);
            int localX = cellX - chunkX * ChunkSize;
            int localY = cellY - chunkY * ChunkSize;
            localIndex = localY * ChunkSize + localX;

            var key = new Vector2Int(chunkX, chunkY);
            if (!_chunks.TryGetValue(key, out Chunk chunk))
            {
                chunk = new Chunk();
                _chunks.Add(key, chunk);
            }
            return chunk;
        }

        private bool TryGetChunk(int cellX, int cellY, out Chunk chunk, out int localIndex)
        {
            int chunkX = FloorDiv(cellX, ChunkSize);
            int chunkY = FloorDiv(cellY, ChunkSize);
            int localX = cellX - chunkX * ChunkSize;
            int localY = cellY - chunkY * ChunkSize;
            localIndex = localY * ChunkSize + localX;
            return _chunks.TryGetValue(new Vector2Int(chunkX, chunkY), out chunk);
        }

        private static int FloorDiv(int value, int divisor) =>
            value >= 0 ? value / divisor : (value - divisor + 1) / divisor;
    }
}
