using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadCells.Tools.Editor
{
    public static class GothicDungeonImporter
    {
        private const string Root = "Assets/_Project/Art/Environment/GothicDungeon";
        private const string ManifestPath = "SourceAssets/Tilesets/GothicDungeon/import-manifest.json";
        private const string MaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [Serializable] private class Manifest { public Sheet[] sheets; }
        [Serializable] private class Sheet
        {
            public string file;
            public int width, height, pixelsPerUnit;
            public Entry[] sprites;
        }
        [Serializable] private class Entry
        {
            public string name, group, category;
            public int x, y, width, height;
            public bool referenceOnly;
            public Outline[] outlines;
        }
        [Serializable] private class Outline { public Vector2[] points; }

        [MenuItem("Tools/DeadCells/Tilesets/Import Gothic Dungeon")]
        public static void Import()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            var groups = new Dictionary<string, List<Tile>>();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            foreach (var sheet in manifest.sheets)
            {
                var path = Root + "/Textures/" + sheet.file;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Check(importer != null, "Missing texture: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = sheet.pixelsPerUnit;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 8192;
                importer.npotScale = TextureImporterNPOTScale.None;

                var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var rects = sheet.sprites.Select(entry => new SpriteRect
                {
                    name = entry.name,
                    rect = new Rect(entry.x, entry.y, entry.width, entry.height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    spriteID = new GUID(Hash128.Compute(sheet.file + ":" + entry.name).ToString())
                }).ToArray();
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                var outlineProvider = provider.GetDataProvider<ISpriteOutlineDataProvider>();
                for (int i = 0; i < rects.Length; i++)
                {
                    var entry = sheet.sprites[i];
                    if (entry.outlines.Length > 0)
                        outlineProvider.SetOutlines(rects[i].spriteID, entry.outlines.Select(outline => outline.points).ToList());
                }
                provider.Apply();
                importer.SaveAndReimport();
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
                Check(sprites.Count == sheet.sprites.Length, "Sprite count mismatch: " + path);
                foreach (var entry in sheet.sprites)
                {
                    var sprite = sprites[entry.name];
                    Check(sprite.vertices.Length >= 3, "Empty sprite mesh: " + entry.name);
                    if (entry.referenceOnly)
                        continue;
                    EnsureFolder(Root + "/Tiles/" + entry.group);
                    var tilePath = Root + "/Tiles/" + entry.group + "/" + entry.name + ".asset";
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    if (tile == null)
                    {
                        Check(!File.Exists(tilePath), "Existing file is not a Tile: " + tilePath);
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.name = entry.name;
                        tile.sprite = sprite;
                        tile.colliderType = Tile.ColliderType.None;
                        AssetDatabase.CreateAsset(tile, tilePath);
                    }
                    if (!groups.TryGetValue(entry.group, out var tiles))
                        groups.Add(entry.group, tiles = new List<Tile>());
                    tiles.Add(tile);
                }
                Console.WriteLine("GOTHIC_SHEET_IMPORTED " + sheet.file + " sprites=" + sprites.Count);
            }

            EnsureFolder(Root + "/Palettes");
            foreach (var group in groups)
            {
                var name = "GothicDungeon_" + group.Key;
                var path = Root + "/Palettes/" + name + ".prefab";
                if (File.Exists(path))
                    continue;
                GridPaletteUtility.CreateNewPalette(Root + "/Palettes", name, GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual, new Vector3(1f, 1f, 0f), GridLayout.CellSwizzle.XYZ);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var map = contents.GetComponentInChildren<Tilemap>();
                    map.name = group.Key;
                    map.GetComponent<TilemapRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                    int column = 0, rowY = 0, rowHeight = 0;
                    foreach (var tile in group.Value)
                    {
                        int width = Mathf.CeilToInt(tile.sprite.bounds.size.x) + 2;
                        int height = Mathf.CeilToInt(tile.sprite.bounds.size.y) + 2;
                        if (column + width > 56 && column > 0)
                        {
                            column = 0;
                            rowY -= rowHeight;
                            rowHeight = 0;
                        }
                        map.SetTile(new Vector3Int(column + width / 2, rowY - height / 2, 0), tile);
                        column += width;
                        rowHeight = Mathf.Max(rowHeight, height);
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            int spriteCount = 0, tileCount = 0;
            var expectedGroups = new Dictionary<string, int>();
            foreach (var sheet in manifest.sheets)
            {
                var path = Root + "/Textures/" + sheet.file;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Check(texture.width == sheet.width && texture.height == sheet.height, "Texture was downscaled: " + path);
                Check(importer.spritePixelsPerUnit == sheet.pixelsPerUnit && !importer.mipmapEnabled
                    && importer.filterMode == FilterMode.Point && !importer.isReadable, "Invalid import settings: " + path);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
                Check(sprites.Count == sheet.sprites.Length, "Unexpected sprite count: " + path);
                foreach (var entry in sheet.sprites)
                {
                    var sprite = sprites[entry.name];
                    Check(sprite.rect == new Rect(entry.x, entry.y, entry.width, entry.height)
                        && sprite.vertices.Length >= 3 && sprite.triangles.Length >= 3, "Invalid sprite: " + entry.name);
                    spriteCount++;
                    if (entry.referenceOnly)
                        continue;
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tiles/" + entry.group + "/" + entry.name + ".asset");
                    Check(tile != null && tile.sprite == sprite && tile.colliderType == Tile.ColliderType.None,
                        "Invalid tile reference/collider: " + entry.name);
                    tileCount++;
                    expectedGroups[entry.group] = expectedGroups.TryGetValue(entry.group, out int count) ? count + 1 : 1;
                }
            }
            foreach (var group in expectedGroups)
            {
                var palette = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Palettes/GothicDungeon_" + group.Key + ".prefab");
                Check(palette != null && palette.GetComponentInChildren<Tilemap>().GetUsedTilesCount() == group.Value,
                    "Palette is incomplete: " + group.Key);
            }
            Console.WriteLine("GOTHIC_TILESET_VERIFIED sprites=" + spriteCount + " tiles=" + tileCount + " palettes=" + expectedGroups.Count);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            Check(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))), "Could not create " + path);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
