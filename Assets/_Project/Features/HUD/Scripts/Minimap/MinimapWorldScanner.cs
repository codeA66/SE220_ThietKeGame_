using System;
using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.HUD
{
    /// <summary>
    /// Samples Physics2D world geometry (Ground, OneWayPlatform, Climbable) into MinimapCellKind values
    /// and gathers live/static level markers (Player, Enemies, Scrolls, Exits, Teleporters, Chests).
    /// </summary>
    public sealed class MinimapWorldScanner
    {
        private const float DynamicScanInterval = 0.5f;
        private const float LandmarkMergeDistanceSq = 2.25f;

        private readonly float _cellSize;
        private readonly Vector2 _probeSize;
        private readonly int _oneWayLayer;
        private readonly ContactFilter2D _terrainFilter;
        private readonly ContactFilter2D _climbableFilter;
        private readonly Collider2D[] _hits = new Collider2D[12];
        private readonly List<MinimapMarker> _staticLandmarks = new();

        private Health[] _healths = Array.Empty<Health>();
        private PowerScrollPickup[] _scrolls = Array.Empty<PowerScrollPickup>();
        private float _nextDynamicScan = -1f;

        public MinimapWorldScanner(float cellSize)
        {
            _cellSize = cellSize;
            _probeSize = Vector2.one * (cellSize * 0.86f);
            _oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");

            int terrainMask = LayerMask.GetMask("Ground", "OneWayPlatform");
            if (terrainMask == 0)
                terrainMask = (1 << 6) | (1 << 9);

            int climbableMask = LayerMask.GetMask("Climbable");
            if (climbableMask == 0)
                climbableMask = 1 << 10;

            _terrainFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = terrainMask,
                useTriggers = false,
            };

            _climbableFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = climbableMask,
                useTriggers = true,
            };
        }

        public MinimapCellKind SampleCell(int cellX, int cellY)
        {
            var center = new Vector2((cellX + 0.5f) * _cellSize, (cellY + 0.5f) * _cellSize);

            int terrainCount = Physics2D.OverlapBox(center, _probeSize, 0f, _terrainFilter, _hits);
            bool hasSolid = false;
            bool hasOneWay = false;
            for (int i = 0; i < terrainCount; i++)
            {
                Collider2D hit = _hits[i];
                if (hit == null)
                    continue;
                if (hit.usedByEffector || (_oneWayLayer >= 0 && hit.gameObject.layer == _oneWayLayer))
                    hasOneWay = true;
                else
                    hasSolid = true;
            }

            if (hasSolid)
                return MinimapCellKind.Solid;

            int climbCount = Physics2D.OverlapBox(center, _probeSize, 0f, _climbableFilter, _hits);
            if (climbCount > 0)
                return MinimapCellKind.Climbable;

            return hasOneWay ? MinimapCellKind.OneWay : MinimapCellKind.Empty;
        }

        /// <summary>
        /// Scans the active scene once for static landmarks (Exit doors, Teleporters, Chests)
        /// without requiring cross-feature assembly references or manual scene wiring.
        /// </summary>
        public void ScanStaticLandmarks()
        {
            _staticLandmarks.Clear();

            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];
                if (t == null || !t.gameObject.activeInHierarchy)
                    continue;

                if (TryClassifyLandmark(t.gameObject, out MinimapMarkerKind kind))
                    AddUniqueLandmark(t.position, kind);
            }
        }

        public void CollectMarkers(PlayerController player, List<MinimapMarker> output, float now)
        {
            output.Clear();

            if (now >= _nextDynamicScan)
            {
                _nextDynamicScan = now + DynamicScanInterval;
                _healths = UnityEngine.Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
                _scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsSortMode.None);
            }

            for (int i = 0; i < _staticLandmarks.Count; i++)
                output.Add(_staticLandmarks[i]);

            for (int i = 0; i < _scrolls.Length; i++)
            {
                PowerScrollPickup scroll = _scrolls[i];
                if (scroll == null || !scroll.IsAvailable || !scroll.gameObject.activeInHierarchy)
                    continue;
                output.Add(new MinimapMarker(scroll.IconPosition, MinimapMarkerKind.Scroll));
            }

            Health playerHealth = player != null ? player.Health : null;
            for (int i = 0; i < _healths.Length; i++)
            {
                Health health = _healths[i];
                if (health == null || !health.IsAlive || health == playerHealth || !health.gameObject.activeInHierarchy)
                    continue;
                output.Add(new MinimapMarker((Vector2)health.transform.position + Vector2.up * 0.6f, MinimapMarkerKind.Enemy));
            }

            if (player != null)
                output.Add(new MinimapMarker((Vector2)player.transform.position + Vector2.up * 0.75f, MinimapMarkerKind.Player));
        }

        private static bool TryClassifyLandmark(GameObject go, out MinimapMarkerKind kind)
        {
            // Check component type names first for future/extended levels (e.g. NhaGiamGiengChuong).
            MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "ExitTrigger")
                {
                    kind = MinimapMarkerKind.Exit;
                    return true;
                }
                if (typeName == "TeleportGate")
                {
                    kind = MinimapMarkerKind.Teleport;
                    return true;
                }
                if (typeName == "HealingChest")
                {
                    kind = MinimapMarkerKind.Chest;
                    return true;
                }
            }

            string name = go.name;
            if (name.IndexOf("Exit Door", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                kind = MinimapMarkerKind.Exit;
                return true;
            }

            kind = default;
            return false;
        }

        private void AddUniqueLandmark(Vector2 position, MinimapMarkerKind kind)
        {
            for (int i = 0; i < _staticLandmarks.Count; i++)
            {
                if (_staticLandmarks[i].Kind == kind
                    && (_staticLandmarks[i].WorldPosition - position).sqrMagnitude <= LandmarkMergeDistanceSq)
                    return;
            }
            _staticLandmarks.Add(new MinimapMarker(position, kind));
        }
    }
}
