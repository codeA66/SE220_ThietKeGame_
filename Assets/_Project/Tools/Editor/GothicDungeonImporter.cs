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
        public static void Import() => Import(ManifestPath, Root);

        public static void Import(string manifestPathOverride, string rootOverride = null)
        {
            string activeManifestPath = string.IsNullOrWhiteSpace(manifestPathOverride) ? ManifestPath : manifestPathOverride;
            string activeRoot = string.IsNullOrWhiteSpace(rootOverride) ? Root : rootOverride;

            Check(File.Exists(activeManifestPath), "Missing manifest: " + activeManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(activeManifestPath));
            ValidateManifestBeforeImport(manifest, activeRoot);

            var groups = new Dictionary<string, List<Tile>>(StringComparer.Ordinal);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            foreach (var sheet in manifest.sheets)
            {
                var path = activeRoot + "/Textures/" + sheet.file;
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
                    if (entry.outlines != null && entry.outlines.Length > 0)
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
                    EnsureFolder(activeRoot + "/Tiles/" + entry.group);
                    var tilePath = activeRoot + "/Tiles/" + entry.group + "/" + entry.name + ".asset";
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
                    else if (tile.sprite != sprite || tile.colliderType != Tile.ColliderType.None)
                    {
                        tile.sprite = sprite;
                        tile.colliderType = Tile.ColliderType.None;
                        EditorUtility.SetDirty(tile);
                    }
                    if (!groups.TryGetValue(entry.group, out var tiles))
                        groups.Add(entry.group, tiles = new List<Tile>());
                    tiles.Add(tile);
                }
                Console.WriteLine("GOTHIC_SHEET_IMPORTED " + sheet.file + " sprites=" + sprites.Count);
            }

            EnsureFolder(activeRoot + "/Palettes");
            foreach (var group in groups)
                UpsertGroupPalette(activeRoot, group.Key, group.Value);

            AssetDatabase.SaveAssets();
            Verify(activeManifestPath, activeRoot);
        }

        public static void Verify() => Verify(ManifestPath, Root);

        public static void Verify(string manifestPathOverride, string rootOverride = null)
        {
            string activeManifestPath = string.IsNullOrWhiteSpace(manifestPathOverride) ? ManifestPath : manifestPathOverride;
            string activeRoot = string.IsNullOrWhiteSpace(rootOverride) ? Root : rootOverride;

            Check(File.Exists(activeManifestPath), "Missing manifest: " + activeManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(activeManifestPath));
            Check(manifest != null && manifest.sheets != null, "Invalid manifest: " + activeManifestPath);
            int spriteCount = 0, tileCount = 0;
            var expectedGroups = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var sheet in manifest.sheets)
            {
                var path = activeRoot + "/Textures/" + sheet.file;
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
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(activeRoot + "/Tiles/" + entry.group + "/" + entry.name + ".asset");
                    Check(tile != null && tile.sprite == sprite && tile.colliderType == Tile.ColliderType.None,
                        "Invalid tile reference/collider: " + entry.name);
                    tileCount++;
                    expectedGroups[entry.group] = expectedGroups.TryGetValue(entry.group, out int count) ? count + 1 : 1;
                }
            }
            foreach (var group in expectedGroups)
            {
                var palette = AssetDatabase.LoadAssetAtPath<GameObject>(activeRoot + "/Palettes/GothicDungeon_" + group.Key + ".prefab");
                var paletteMap = palette != null ? palette.GetComponentInChildren<Tilemap>() : null;
                Check(paletteMap != null && paletteMap.GetUsedTilesCount() == group.Value,
                    "Palette is incomplete: " + group.Key);
            }
            Console.WriteLine("GOTHIC_TILESET_VERIFIED sprites=" + spriteCount + " tiles=" + tileCount + " palettes=" + expectedGroups.Count);
        }

        private static void ValidateManifestBeforeImport(Manifest manifest, string activeRoot)
        {
            Check(manifest != null && manifest.sheets != null && manifest.sheets.Length > 0, "Manifest contains no sheets.");
            var seenSprites = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sheet in manifest.sheets)
            {
                Check(sheet != null && !string.IsNullOrWhiteSpace(sheet.file), "Manifest sheet file name is empty.");
                Check(sheet.width > 0 && sheet.height > 0 && sheet.pixelsPerUnit > 0, "Invalid dimensions/PPU on sheet: " + sheet.file);
                string texPath = activeRoot + "/Textures/" + sheet.file;
                Check(File.Exists(texPath), "Missing texture: " + texPath);
                Check(sheet.sprites != null && sheet.sprites.Length > 0, "Sheet has no sprites: " + sheet.file);

                foreach (var entry in sheet.sprites)
                {
                    Check(entry != null && !string.IsNullOrWhiteSpace(entry.name), "Empty sprite name in sheet: " + sheet.file);
                    Check(seenSprites.Add(entry.name), "Duplicate sprite name in manifest: " + entry.name);
                    Check(entry.x >= 0 && entry.y >= 0 && entry.width > 0 && entry.height > 0
                        && entry.x + entry.width <= sheet.width && entry.y + entry.height <= sheet.height,
                        $"Sprite rect out of bounds for {entry.name} ({entry.x},{entry.y},{entry.width},{entry.height}) on {sheet.file} ({sheet.width}x{sheet.height}).");
                    if (!entry.referenceOnly)
                        Check(!string.IsNullOrWhiteSpace(entry.group), "Paintable sprite missing group: " + entry.name);
                }
            }
        }

        private static void UpsertGroupPalette(string activeRoot, string groupKey, List<Tile> groupTiles)
        {
            var name = "GothicDungeon_" + groupKey;
            var path = activeRoot + "/Palettes/" + name + ".prefab";
            bool existed = File.Exists(path);

            if (existed)
            {
                var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var existingMap = existingPrefab != null ? existingPrefab.GetComponentInChildren<Tilemap>() : null;
                if (existingMap != null)
                {
                    var placedSet = CollectPlacedTiles(existingMap);
                    if (groupTiles.All(t => placedSet.Contains(t)))
                        return;
                }
            }
            else
            {
                GridPaletteUtility.CreateNewPalette(activeRoot + "/Palettes", name, GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual, new Vector3(1f, 1f, 0f), GridLayout.CellSwizzle.XYZ);
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map = contents.GetComponentInChildren<Tilemap>();
                map.name = groupKey;
                map.GetComponent<TilemapRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

                var alreadyPlaced = CollectPlacedTiles(map);
                var tilesToPlace = existed
                    ? groupTiles.Where(t => !alreadyPlaced.Contains(t)).ToList()
                    : groupTiles;

                int column = 0;
                int rowY = 0;
                int rowHeight = 0;
                if (existed && alreadyPlaced.Count > 0)
                    rowY = ComputeNextAppendRowY(map);

                foreach (var tile in tilesToPlace)
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
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(contents, path);
                EnsureGridPaletteSubAsset(savedPrefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void EnsureGridPaletteSubAsset(GameObject savedPrefab, string prefabPath)
        {
            if (savedPrefab == null || AssetDatabase.LoadAssetAtPath<GridPalette>(prefabPath) != null)
                return;

            var gridPalette = ScriptableObject.CreateInstance<GridPalette>();
            gridPalette.name = "Palette Settings";
            gridPalette.cellSizing = GridPalette.CellSizing.Manual;
            gridPalette.transparencySortMode = TransparencySortMode.Default;
            gridPalette.transparencySortAxis = new Vector3(0f, 0f, 1f);
            AssetDatabase.AddObjectToAsset(gridPalette, savedPrefab);
            EditorUtility.SetDirty(savedPrefab);
        }

        private static HashSet<TileBase> CollectPlacedTiles(Tilemap map)
        {
            var set = new HashSet<TileBase>();
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var t = map.GetTile(pos);
                if (t != null)
                    set.Add(t);
            }
            return set;
        }

        private static int ComputeNextAppendRowY(Tilemap map)
        {
            int minBottomY = 0;
            bool any = false;
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                if (map.GetTile(pos) is not Tile tile || tile.sprite == null)
                    continue;
                int halfH = (Mathf.CeilToInt(tile.sprite.bounds.size.y) + 2) / 2 + 1;
                int bottomY = pos.y - halfH;
                if (!any || bottomY < minBottomY)
                {
                    minBottomY = bottomY;
                    any = true;
                }
            }
            return any ? minBottomY : 0;
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
