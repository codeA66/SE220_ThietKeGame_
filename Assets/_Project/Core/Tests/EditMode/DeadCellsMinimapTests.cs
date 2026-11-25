using System.Collections.Generic;
using DeadCells.HUD;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.Core.Tests
{
    public sealed class DeadCellsMinimapTests
    {
        [Test]
        public void WorldToCell_AndChunkStorage_HandleNegativeCoordinatesCleanly()
        {
            var map = new MinimapMapData(0.5f);

            Assert.AreEqual(new Vector2Int(0, 0), map.WorldToCell(new Vector2(0.1f, 0.1f)));
            Assert.AreEqual(new Vector2Int(-1, -1), map.WorldToCell(new Vector2(-0.1f, -0.1f)));
            Assert.AreEqual(new Vector2Int(-33, -17), map.WorldToCell(new Vector2(-16.1f, -8.1f)));

            map.SetCell(-1, -1, MinimapCellKind.Solid);
            map.SetCell(-17, -16, MinimapCellKind.Climbable);
            map.SetCell(16, 0, MinimapCellKind.OneWay);

            Assert.AreEqual(MinimapCellKind.Solid, map.GetCell(-1, -1));
            Assert.AreEqual(MinimapCellKind.Climbable, map.GetCell(-17, -16));
            Assert.AreEqual(MinimapCellKind.OneWay, map.GetCell(16, 0));
            Assert.AreEqual(MinimapCellKind.Empty, map.GetCell(-2, -1));
        }

        [Test]
        public void RevealAround_PersistsExploration_AndDoesNotLeakThroughSolidWalls()
        {
            var map = new MinimapMapData(1f);

            // Two adjacent rooms separated by a vertical solid wall at x = 0:
            // Left room: x in [-5..-1], y in [0..4]; Right hidden room: x in [1..5], y in [0..4]; surrounded by solid rock.
            MinimapCellKind Sampler(int cx, int cy)
            {
                bool inLeftRoom = cx >= -5 && cx <= -1 && cy >= 0 && cy <= 4;
                bool inRightRoom = cx >= 1 && cx <= 5 && cy >= 0 && cy <= 4;
                return (inLeftRoom || inRightRoom) ? MinimapCellKind.Empty : MinimapCellKind.Solid;
            }

            int revealed = map.RevealAround(new Vector2(-2.5f, 2.5f), 8f, 8f, Sampler);

            Assert.Greater(revealed, 0);
            Assert.IsTrue(map.IsExplored(-3, 2), "Player room should be explored.");
            Assert.IsFalse(map.IsExplored(2, 2), "Hidden room behind solid wall must stay fogged.");
            Assert.IsTrue(map.IsSampled(0, 2), "Wall bordering explored room should be sampled for cyan outline.");

            // Moving elsewhere keeps previously explored room permanently revealed.
            map.RevealAround(new Vector2(2.5f, 2.5f), 8f, 8f, Sampler);
            Assert.IsTrue(map.IsExplored(-3, 2), "Previously explored room must persist.");
            Assert.IsTrue(map.IsExplored(2, 2), "Newly visited room should now be explored.");
        }

        [Test]
        public void RenderWindow_DrawsDeadCellsSilhouetteOutlinePlatformsChainsAndGatedMarkers()
        {
            var map = new MinimapMapData(1f);

            // Room interior x in [2..16], y in [2..8] with solid walls around it,
            // a one-way platform at (7, 5), and a climbable chain at (10, 3..7).
            MinimapCellKind Sampler(int cx, int cy)
            {
                if (cx < 2 || cx > 16 || cy < 2 || cy > 8)
                    return MinimapCellKind.Solid;
                if (cx == 10 && cy >= 3 && cy <= 7)
                    return MinimapCellKind.Climbable;
                if (cx == 7 && cy == 5)
                    return MinimapCellKind.OneWay;
                return MinimapCellKind.Empty;
            }

            map.RevealAround(new Vector2(9.5f, 4.5f), 16f, 10f, Sampler);

            var markers = new List<MinimapMarker>
            {
                new(new Vector2(3.5f, 4.5f), MinimapMarkerKind.Player),
                new(new Vector2(12.5f, 4.5f), MinimapMarkerKind.Enemy),     // inside explored room
                new(new Vector2(22.5f, 4.5f), MinimapMarkerKind.Enemy),     // far unexplored room
                new(new Vector2(7.5f, 2.5f), MinimapMarkerKind.Scroll),     // explored scroll
                new(new Vector2(16.2f, 2.5f), MinimapMarkerKind.Exit),      // explored exit against east wall
            };

            const int width = 26;
            const int height = 12;
            var pixels = new Color32[width * height];
            map.RenderWindow(pixels, width, height, 0, 0, markers, 1f);

            Color32 At(int x, int y) => pixels[y * width + x];

            // Deep solid rock (no explored air neighbor) stays transparent void.
            Assert.AreEqual(MinimapMapData.ColorVoid, At(0, 0), "Deep rock outside room must stay void.");

            // Solid floor directly below explored air gets brightest FloorRim color.
            Assert.AreEqual(MinimapMapData.ColorFloorRim, At(3, 1), "Floor rim below explored room should highlight cyan.");

            // Solid side wall bordering explored air gets WallOutline color.
            Assert.AreEqual(MinimapMapData.ColorWallOutline, At(1, 5), "Side wall bordering explored room should outline cyan.");

            // Explored interior room air gets slate-blue room fill; ceiling-adjacent air gets room shade.
            Assert.AreEqual(MinimapMapData.ColorRoomFill, At(2, 6), "Explored room interior should render Dead Cells slate blue.");
            Assert.AreEqual(MinimapMapData.ColorRoomShade, At(2, 8), "Air directly beneath solid ceiling should use subtle room shade.");

            // One-way platform and climbable chain use their distinct Dead Cells colors.
            Assert.AreEqual(MinimapMapData.ColorOneWay, At(7, 5), "One-way platform should render ice-teal.");
            Assert.AreEqual(MinimapMapData.ColorClimbable, At(10, 5), "Climbable chain should render amber gold.");

            // Markers: player always visible, explored enemy/scroll/exit visible, unexplored enemy hidden.
            Assert.AreEqual(MinimapMapData.ColorPlayerCore, At(3, 4), "Player marker core should render on top.");
            Assert.AreEqual(MinimapMapData.ColorEnemy, At(12, 4), "Enemy in explored room should appear.");
            Assert.AreEqual(MinimapMapData.ColorVoid, At(22, 4), "Enemy in unexplored area must stay hidden.");
            Assert.AreEqual(MinimapMapData.ColorScrollCore, At(7, 2), "Scroll in explored room should appear.");
            Assert.AreEqual(MinimapMapData.ColorExitCore, At(16, 3), "Exit arch in explored room should appear.");
        }

        [Test]
        public void MinimapOverlayView_BuildsCornerAndTogglesFullscreenWithClampedZoom()
        {
            var host = new GameObject("MinimapTestHost");
            var tex = new Texture2D(32, 20, TextureFormat.RGBA32, false);
            try
            {
                var view = new MinimapOverlayView(host.transform, tex, new Vector2(288f, 176f));
                Assert.IsFalse(view.IsFullMapOpen, "Fullscreen overlay should start closed.");

                // All non-interactive graphics must have raycastTarget disabled so gameplay clicks pass through.
                foreach (var graphic in host.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    Assert.IsFalse(graphic.raycastTarget, $"Minimap graphic '{graphic.name}' should not block raycasts.");

                view.SetFullMapOpen(true);
                Assert.IsTrue(view.IsFullMapOpen);

                view.AdjustZoom(10f);
                Assert.AreEqual(5f, view.Zoom, 0.001f, "Zoom should clamp at max 5x.");

                view.AdjustZoom(-20f);
                Assert.AreEqual(1f, view.Zoom, 0.001f, "Zoom should clamp at min 1x.");

                view.SetFullMapOpen(false);
                Assert.IsFalse(view.IsFullMapOpen);
            }
            finally
            {
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(host);
            }
        }
    }
}
