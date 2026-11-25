using System;
using System.IO;
using System.Linq;
using DeadCells.Tools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadCells.Core.Tests
{
    public sealed class GothicDungeonImporterIncrementalTests
    {
        private const string TempRoot = "Assets/_Project/Core/Tests/EditMode/__TempGothicImportTest";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.DeleteAsset(TempRoot);
            if (Directory.Exists(TempRoot))
                Directory.Delete(TempRoot, recursive: true);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Import_WhenNewPaintableSpriteAddedToExistingGroup_AppendsTilePreservingExistingCellsAndPaletteGuid()
        {
            Directory.CreateDirectory(TempRoot + "/Textures");
            WriteSolidPng(TempRoot + "/Textures/TestSheet.png", 64, 32);
            AssetDatabase.Refresh();

            string manifestPath = TempRoot + "/import-manifest.json";
            string initialManifest =
                "{\"sheets\":[{\"file\":\"TestSheet.png\",\"width\":64,\"height\":32,\"pixelsPerUnit\":32,\"sprites\":[" +
                "{\"name\":\"Block_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":0,\"y\":0,\"width\":32,\"height\":32,\"referenceOnly\":false,\"outlines\":[]}," +
                "{\"name\":\"Block_002\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":32,\"y\":0,\"width\":32,\"height\":32,\"referenceOnly\":true,\"outlines\":[]}" +
                "]}]}";
            File.WriteAllText(manifestPath, initialManifest);

            GothicDungeonImporter.Import(manifestPath, TempRoot);

            string palettePath = TempRoot + "/Palettes/GothicDungeon_Terrain.prefab";
            string initialGuid = AssetDatabase.AssetPathToGUID(palettePath);
            Assert.IsFalse(string.IsNullOrEmpty(initialGuid));

            var initialPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            var initialMap = initialPalette.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(1, initialMap.GetUsedTilesCount());

            Vector3Int block1Cell = FindTileCell(initialMap, "Block_001");

            // Flip Block_002 from referenceOnly:true to referenceOnly:false in existing group "Terrain"
            string updatedManifest = initialManifest.Replace("\"referenceOnly\":true", "\"referenceOnly\":false");
            File.WriteAllText(manifestPath, updatedManifest);

            GothicDungeonImporter.Import(manifestPath, TempRoot);

            string updatedGuid = AssetDatabase.AssetPathToGUID(palettePath);
            Assert.AreEqual(initialGuid, updatedGuid, "Palette prefab GUID must not change on incremental update.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GridPalette>(palettePath), "GridPalette sub-asset must be preserved after incremental palette update.");

            var updatedPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            var updatedMap = updatedPalette.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(2, updatedMap.GetUsedTilesCount(), "Existing palette must include newly added tile in group.");
            Assert.AreEqual(block1Cell, FindTileCell(updatedMap, "Block_001"), "Existing tile cell coordinates must not shift.");
            Assert.AreNotEqual(block1Cell, FindTileCell(updatedMap, "Block_002"), "Appended tile must occupy a distinct non-overlapping cell.");

            // Running Import a second time with the updated manifest must be idempotent
            GothicDungeonImporter.Import(manifestPath, TempRoot);
            var rerunPalette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            Assert.AreEqual(2, rerunPalette.GetComponentInChildren<Tilemap>().GetUsedTilesCount());
        }

        [Test]
        public void Import_WhenManifestHasDuplicateSpriteNameOrOutOfBoundsRect_ThrowsBeforeMutatingAssets()
        {
            Directory.CreateDirectory(TempRoot + "/Textures");
            WriteSolidPng(TempRoot + "/Textures/TestSheet.png", 32, 32);
            AssetDatabase.Refresh();

            string manifestPath = TempRoot + "/import-manifest.json";
            string invalidBoundsManifest =
                "{\"sheets\":[{\"file\":\"TestSheet.png\",\"width\":32,\"height\":32,\"pixelsPerUnit\":32,\"sprites\":[" +
                "{\"name\":\"OutBounds_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":16,\"y\":16,\"width\":32,\"height\":32,\"referenceOnly\":false,\"outlines\":[]}" +
                "]}]}";
            File.WriteAllText(manifestPath, invalidBoundsManifest);

            var ex = Assert.Throws<InvalidOperationException>(() => GothicDungeonImporter.Import(manifestPath, TempRoot));
            StringAssert.Contains("OutBounds_001", ex.Message);
            Assert.IsFalse(File.Exists(TempRoot + "/Tiles/Terrain/OutBounds_001.asset"), "Pre-validation failure must not write partial tile assets.");

            string duplicateNameManifest =
                "{\"sheets\":[{\"file\":\"TestSheet.png\",\"width\":32,\"height\":32,\"pixelsPerUnit\":32,\"sprites\":[" +
                "{\"name\":\"Dup_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":0,\"y\":0,\"width\":16,\"height\":16,\"referenceOnly\":false,\"outlines\":[]}," +
                "{\"name\":\"Dup_001\",\"group\":\"Terrain\",\"category\":\"Stone\",\"x\":16,\"y\":0,\"width\":16,\"height\":16,\"referenceOnly\":false,\"outlines\":[]}" +
                "]}]}";
            File.WriteAllText(manifestPath, duplicateNameManifest);

            var dupEx = Assert.Throws<InvalidOperationException>(() => GothicDungeonImporter.Import(manifestPath, TempRoot));
            StringAssert.Contains("Dup_001", dupEx.Message);
            Assert.IsFalse(File.Exists(TempRoot + "/Tiles/Terrain/Dup_001.asset"), "Duplicate sprite name failure must not write partial tile assets.");
        }

        private static Vector3Int FindTileCell(Tilemap map, string tileName)
        {
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var tile = map.GetTile(pos);
                if (tile != null && tile.name == tileName)
                    return pos;
            }
            Assert.Fail($"Tile '{tileName}' not found in palette Tilemap.");
            return default;
        }

        private static void WriteSolidPng(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                var pixels = Enumerable.Repeat(new Color32(180, 180, 200, 255), width * height).ToArray();
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
