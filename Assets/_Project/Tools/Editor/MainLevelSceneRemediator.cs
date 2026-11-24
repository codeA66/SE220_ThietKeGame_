using System;
using System.Collections.Generic;
using System.Linq;
using DeadCells.CameraSystem;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.RunFlow;
using DeadCells.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DeadCells.Tools.Editor
{
    public static class MainLevelSceneRemediator
    {
        public const string SharedPostProcessProfilePath = "Assets/_Project/Settings/DeadCells_PostProcessProfile.asset";

        private const string MainLevelPath = "Assets/_Project/Scenes/Main_Level.unity";
        private const string PlayerHudPrefabPath = "Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab";
        private const string SpriteLitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string SpriteUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string ParticlesUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        private const string PrisonTilesDir = "Assets/_Project/Art/Environment/Prison/Tiles/";
        private const string PrisonSpritesDir = "Assets/_Project/Art/Environment/Prison/Sprites/";
        private const string EnvMaterialsDir = EnvironmentMaterialBuilder.MaterialDirectory + "/";
        private const string ParallaxFarSpritePath = "Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png";
        private const string FxGlowWhitePath = "Assets/_Project/Art/VFX/ReferenceFeedback/fxGlowWhite.png";
        private const string PlankLeftPath = PrisonSpritesDir + "woodenPlatform_left_00.png";
        private const string PlankRightPath = PrisonSpritesDir + "woodenPlatform_right_00.png";

        private static readonly string[] PlankMiddlePaths =
        {
            PrisonSpritesDir + "woodenPlatform_0_00.png",
            PrisonSpritesDir + "woodenPlatform_1_00.png",
            PrisonSpritesDir + "woodenPlatform_2_00.png",
            PrisonSpritesDir + "woodenPlatform_3_00.png",
            PrisonSpritesDir + "woodenPlatform_4_00.png",
        };

        private static readonly (int xMin, int xMax, int floorTop)[] MainLevelFloorSpans =
        {
            (0, 15, 0),
            (15, 28, -1),
            (28, 37, 0),
            (37, 41, 1),
            (41, 45, 2),
            (45, 68, 0),
            (68, 94, -1),
            (94, 112, 0),
        };

        private static readonly (int xMin, int xMax, int yMin, int yMax)[] CarvedArchBays =
        {
            (16, 22, 4, 12),
            (31, 35, 4, 12),
            (52, 60, 3, 13),
            (69, 76, 5, 13),
            (86, 93, 4, 13),
        };

        private static readonly Vector2[] PrisonWindowGodRayCoords =
        {
            new Vector2(7.8f, 5.0f),
            new Vector2(25.8f, 5.0f),
            new Vector2(45.8f, 5.0f),
            new Vector2(64.0f, 6.0f),
            new Vector2(79.0f, 8.0f),
            new Vector2(104.0f, 5.0f),
        };

        public static VolumeProfile EnsureSharedPostProcessProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings"))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedPostProcessProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, SharedPostProcessProfilePath);
            }

            if (!profile.TryGet<Bloom>(out var bloom))
                bloom = profile.Add<Bloom>(true);
            EnsureSubAsset(bloom, profile);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.42f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.60f;
            EditorUtility.SetDirty(bloom);

            if (!profile.TryGet<Vignette>(out var vignette))
                vignette = profile.Add<Vignette>(true);
            EnsureSubAsset(vignette, profile);
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.14f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;
            vignette.color.overrideState = true;
            vignette.color.value = new Color(8f / 255f, 12f / 255f, 18f / 255f, 1f); // #080C12
            EditorUtility.SetDirty(vignette);

            if (!profile.TryGet<Tonemapping>(out var tone))
                tone = profile.Add<Tonemapping>(true);
            EnsureSubAsset(tone, profile);
            tone.active = true;
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;
            EditorUtility.SetDirty(tone);

            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
                colorAdj = profile.Add<ColorAdjustments>(true);
            EnsureSubAsset(colorAdj, profile);
            colorAdj.active = true;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.55f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 8f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 10f;
            EditorUtility.SetDirty(colorAdj);

            if (!profile.TryGet<SplitToning>(out var splitToning))
                splitToning = profile.Add<SplitToning>(true);
            EnsureSubAsset(splitToning, profile);
            splitToning.active = true;
            splitToning.shadows.overrideState = true;
            splitToning.shadows.value = new Color(0x1D / 255f, 0x2E / 255f, 0x42 / 255f, 1f); // #1D2E42
            splitToning.highlights.overrideState = true;
            splitToning.highlights.value = new Color(0xF5 / 255f, 0xC0 / 255f, 0x7A / 255f, 1f); // #F5C07A
            splitToning.balance.overrideState = true;
            splitToning.balance.value = -10f;
            EditorUtility.SetDirty(splitToning);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureSubAsset(VolumeComponent component, VolumeProfile profile)
        {
            if (component != null && !AssetDatabase.Contains(component))
                AssetDatabase.AddObjectToAsset(component, profile);
        }

        [MenuItem("Tools/DeadCells/HUD/Wire PlayerHUD TrailFill")]
        public static void EnsurePlayerHudTrailFill()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerHudPrefabPath);
            if (root == null)
                throw new InvalidOperationException("Missing prefab: " + PlayerHudPrefabPath);

            try
            {
                var view = root.GetComponentInChildren<HealthBarView>(true);
                if (view == null)
                    throw new InvalidOperationException("Missing HealthBarView in " + PlayerHudPrefabPath);

                var soView = new SerializedObject(view);
                var fillProp = soView.FindProperty("_fill");
                var trailProp = soView.FindProperty("_trailFill");
                if (trailProp != null && trailProp.objectReferenceValue != null)
                    return;
                var fillRect = fillProp != null ? fillProp.objectReferenceValue as RectTransform : null;
                if (fillRect == null || fillRect.parent == null)
                    throw new InvalidOperationException("HealthBarView._fill or its Track parent is missing in " + PlayerHudPrefabPath);

                var track = fillRect.parent;
                var trailTransform = track.Find("TrailFill");
                RectTransform trailRect;
                if (trailTransform == null)
                {
                    var trailGo = new GameObject("TrailFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    trailGo.layer = fillRect.gameObject.layer;
                    trailRect = trailGo.GetComponent<RectTransform>();
                    trailRect.SetParent(track, false);
                }
                else
                {
                    trailRect = trailTransform as RectTransform ?? trailTransform.gameObject.AddComponent<RectTransform>();
                    trailTransform.gameObject.layer = fillRect.gameObject.layer;
                }

                int fillIndex = fillRect.GetSiblingIndex();
                if (trailRect.GetSiblingIndex() > fillIndex)
                    trailRect.SetSiblingIndex(fillIndex);

                trailRect.anchorMin = Vector2.zero;
                trailRect.anchorMax = Vector2.one;
                trailRect.pivot = new Vector2(0.5f, 0.5f);
                trailRect.anchoredPosition = Vector2.zero;
                trailRect.sizeDelta = Vector2.zero;
                trailRect.offsetMin = Vector2.zero;
                trailRect.offsetMax = Vector2.zero;
                trailRect.localScale = Vector3.one;
                trailRect.localRotation = Quaternion.identity;

                if (!trailRect.TryGetComponent<Image>(out var trailImage))
                    trailImage = trailRect.gameObject.AddComponent<Image>();
                trailImage.color = new Color(0.98f, 0.78f, 0.30f, 0.92f);
                trailImage.raycastTarget = false;

                if (trailProp == null)
                    throw new InvalidOperationException("Missing _trailFill serialized property on HealthBarView.");
                trailProp.objectReferenceValue = trailRect;
                soView.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(trailRect);
                EditorUtility.SetDirty(trailImage);
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void RunR41BatchNormalizeAndRemediate()
        {
            ParallaxTextureProcessor.NormalizeParallaxFarFromDefault();
            RemediateMainLevel();
        }

        [MenuItem("Tools/DeadCells/Scenes/Remediate Main_Level")]
        public static void RemediateMainLevel()
        {
            EnsurePlayerHudTrailFill();
            SortingLayerSetup.EnsureDeadCellsSortingLayers();
            PrisonPropSpriteGenerator.EnsureAll();
            EnvironmentMaterialBuilder.EnsureAll();
            CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials();

            var scene = EditorSceneManager.OpenScene(MainLevelPath, OpenSceneMode.Single);
            var litMat = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMatPath);
            if (litMat == null)
                throw new InvalidOperationException("Missing material: " + SpriteLitMatPath);

            // 1. Fix Scroll vat material from Player_PaletteSwap to Sprite-Lit-Default
            foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            {
                if (sr.gameObject.name == "Scroll vat")
                {
                    sr.sharedMaterial = litMat;
                    EditorUtility.SetDirty(sr);
                }
            }

            // 2. Fix Climbable chain at x=18.5 to connect Floor 15 (y=-1.0) to One Way Platform 16 (y=2.0)
            var chain = GameObject.Find("Climbable chain");
            if (chain != null)
            {
                chain.transform.position = new Vector3(18.5f, 0.5f, 0f);
                var box = chain.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.offset = Vector2.zero;
                    box.size = new Vector2(0.75f, 3.2f);
                    EditorUtility.SetDirty(box);
                }
                EditorUtility.SetDirty(chain);
            }

            // 3. Fix Archive gallery's 6 child one-way platforms (40 Board children total) using Prison left / 0..4 / right plank sprites,
            //    proportional vertical scale (0.75), and localPosition.y = 0 (opaque wood top +4/24 * 0.75 = +0.125u aligns with BoxCollider2D top).
            var archiveGallery = GameObject.Find("Archive gallery");
            if (archiveGallery != null)
            {
                var sLeft = LoadSpriteRequired(PlankLeftPath);
                var sRight = LoadSpriteRequired(PlankRightPath);
                var sMiddle = PlankMiddlePaths.Select(LoadSpriteRequired).ToArray();

                foreach (Transform platform in archiveGallery.transform)
                {
                    var boards = platform.GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(sr => sr.transform.parent == platform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal))
                        .OrderBy(sr => sr.transform.localPosition.x)
                        .ToArray();

                    for (int i = 0; i < boards.Length; i++)
                    {
                        if (i == 0)
                            boards[i].sprite = sLeft;
                        else if (i == boards.Length - 1)
                            boards[i].sprite = sRight;
                        else
                            boards[i].sprite = sMiddle[(i - 1) % sMiddle.Length];

                        var boardTransform = boards[i].transform;
                        boardTransform.localScale = new Vector3(1f, 0.75f, 1f);
                        var lp = boardTransform.localPosition;
                        boardTransform.localPosition = new Vector3(lp.x, 0f, lp.z);

                        EditorUtility.SetDirty(boardTransform);
                        EditorUtility.SetDirty(boards[i]);
                    }
                }
            }

            // 4. Resize Upgrade panel and position Title / Instruction strictly above Brutality / Tactics / Survival cards
            var scrollHud = GameObject.Find("Power Scroll HUD");
            var upgradePanel = scrollHud != null
                ? scrollHud.transform.Find("Choice backdrop/Upgrade panel") as RectTransform
                : null;
            if (upgradePanel != null)
            {
                upgradePanel.sizeDelta = new Vector2(1000f, 476f);
                EditorUtility.SetDirty(upgradePanel);

                var titleRect = upgradePanel.Find("Title") as RectTransform;
                if (titleRect != null)
                {
                    titleRect.anchoredPosition = new Vector2(0f, 200f);
                    EditorUtility.SetDirty(titleRect);
                }

                var instructionRect = upgradePanel.Find("Instruction") as RectTransform;
                if (instructionRect != null)
                {
                    instructionRect.anchoredPosition = new Vector2(0f, 158f);
                    instructionRect.gameObject.SetActive(true);
                    EditorUtility.SetDirty(instructionRect);
                    EditorUtility.SetDirty(instructionRect.gameObject);
                }
            }
            else
            {
                foreach (var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                {
                    if (tr.name == "Instruction" && tr.parent != null &&
                        (tr.parent.name == "Upgrade panel" || tr.root.name.Contains("Scroll", StringComparison.Ordinal)))
                    {
                        tr.gameObject.SetActive(true);
                        EditorUtility.SetDirty(tr.gameObject);
                    }
                }
            }

            // 5. Move Scroll of Power - Entrance from (13.5, 0) to (25.5, -1.0) on Floor 15 - Zombie hall
            var entranceScroll = GameObject.Find("Scroll of Power - Entrance");
            if (entranceScroll != null && entranceScroll.transform.position.x < 20f)
            {
                entranceScroll.transform.position = new Vector3(25.5f, -1.0f, 0f);
                EditorUtility.SetDirty(entranceScroll.transform);
            }

            // 6. Populate East Wing encounters under "Encounters" and register all enemies in EnemyGoldDrops._enemies
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var projectiles = UnityEngine.Object.FindAnyObjectByType<ProjectileSpawner>();
            var vfx = UnityEngine.Object.FindAnyObjectByType<VfxSpawner>();
            var encountersGo = GameObject.Find("Encounters");
            var encountersRoot = encountersGo != null ? encountersGo.transform : null;

            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab", "Enemy_Rat - Lower Cell Block",
                new Vector3(74f, -0.95f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);
            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab", "Enemy_Archer - Archive Gallery",
                new Vector3(78f, 5.30f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);
            SpawnEnemyIfMissing("Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab", "Enemy_Shield - Exit Guard",
                new Vector3(100.5f, 0.05f, 0f), encountersRoot, player != null ? player.transform : null, projectiles, vfx);

            var goldDrops = UnityEngine.Object.FindAnyObjectByType<EnemyGoldDrops>();
            if (goldDrops != null)
            {
                var allEnemies = UnityEngine.Object.FindObjectsByType<EnemyController>()
                    .OrderBy(e => e.transform.position.x)
                    .ToArray();
                var soGold = new SerializedObject(goldDrops);
                var enemiesProp = soGold.FindProperty("_enemies");
                if (enemiesProp != null)
                {
                    enemiesProp.arraySize = allEnemies.Length;
                    for (int i = 0; i < allEnemies.Length; i++)
                        enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = allEnemies[i];
                    soGold.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(goldDrops);
                }
            }

            // 7. Attach LevelExitTrigger to Exit Door at (108, 0) and bind RunSession
            var runSession = UnityEngine.Object.FindAnyObjectByType<RunSession>();
            var exitDoor = GameObject.Find("Exit Door (visual only)");
            if (exitDoor == null)
                exitDoor = GameObject.Find("Exit Door");
            if (exitDoor != null && runSession != null)
            {
                exitDoor.name = "Exit Door";
                if (!exitDoor.TryGetComponent<BoxCollider2D>(out var box))
                    box = exitDoor.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.offset = new Vector2(0f, 1.75f);
                box.size = new Vector2(2.0f, 3.5f);
                if (!exitDoor.TryGetComponent<LevelExitTrigger>(out var trigger))
                    trigger = exitDoor.AddComponent<LevelExitTrigger>();
                trigger.Configure(runSession);
                var soTrigger = new SerializedObject(trigger);
                soTrigger.FindProperty("_session").objectReferenceValue = runSession;
                soTrigger.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(box);
                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(exitDoor);
            }

            // 8. Resize Defeat Screen Description for 2-line Victory text and wire _heading / _description on DefeatScreen
            var defeatScreen = UnityEngine.Object.FindAnyObjectByType<DefeatScreen>();
            if (defeatScreen != null)
            {
                var headingTransform = defeatScreen.transform.Find("Backdrop/Frame/Card/Heading");
                var descriptionTransform = defeatScreen.transform.Find("Backdrop/Frame/Card/Description");
                var headingText = headingTransform != null ? headingTransform.GetComponent<Text>() : null;
                var descriptionText = descriptionTransform != null ? descriptionTransform.GetComponent<Text>() : null;

                if (descriptionTransform is RectTransform descRect)
                {
                    descRect.sizeDelta = new Vector2(400f, 54f);
                    descRect.anchoredPosition = new Vector2(0f, 4f);
                    EditorUtility.SetDirty(descRect);
                }

                var soDefeat = new SerializedObject(defeatScreen);
                var headingProp = soDefeat.FindProperty("_heading");
                var descriptionProp = soDefeat.FindProperty("_description");
                if (headingProp != null && headingText != null)
                    headingProp.objectReferenceValue = headingText;
                if (descriptionProp != null && descriptionText != null)
                    descriptionProp.objectReferenceValue = descriptionText;
                soDefeat.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(defeatScreen);
            }

            // 9. Clear & repaint solid floors (Stone visuals + East wing stone + TM_SolidRim + TM_WallDecals)
            var unlitMat = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMatPath);
            if (unlitMat == null)
                throw new InvalidOperationException("Missing material: " + SpriteUnlitMatPath);
            var particlesUnlitMat = AssetDatabase.LoadAssetAtPath<Material>(ParticlesUnlitMatPath);
            if (particlesUnlitMat == null)
                throw new InvalidOperationException("Missing material: " + ParticlesUnlitMatPath);

            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");

            var allTilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>();
            var westStone = allTilemaps.SingleOrDefault(t => t.gameObject.name == "Stone visuals");
            var eastStone = allTilemaps.SingleOrDefault(t => t.gameObject.name == "East wing stone");
            var westBg = allTilemaps.SingleOrDefault(t => t.gameObject.name == "Background");
            var eastBg = allTilemaps.SingleOrDefault(t => t.gameObject.name == "East wing background");
            if (westStone == null || eastStone == null || westBg == null || eastBg == null)
                throw new InvalidOperationException("Main_Level missing required tilemaps (Stone visuals / East wing stone / Background / East wing background).");

            var gridParent = westStone.transform.parent;
            SetTilemapSorting(westStone, groundLayerId, -5);
            SetTilemapSorting(eastStone, groundLayerId, -5);
            SetTilemapSorting(westBg, bgLayerId, -20);
            SetTilemapSorting(eastBg, bgLayerId, -20);

            var solidRim = EnsureSiblingVisualTilemap(gridParent, "TM_SolidRim", groundLayerId, -3, litMat);
            var wallDecals = EnsureSiblingVisualTilemap(gridParent, "TM_WallDecals", bgLayerId, -18, litMat);

            var dirtTile = LoadTileRequired(PrisonTilesDir + "dirt_00.asset");
            var floorStamps = Enumerable.Range(0, 10)
                .Select(i => LoadTileRequired($"{PrisonTilesDir}floorStamp_{i}_00.asset"))
                .ToArray();
            var groundTiles = Enumerable.Range(0, 4)
                .Select(i => LoadTileRequired($"{PrisonTilesDir}ground_{i}_00.asset"))
                .ToArray();
            var backWalls = Enumerable.Range(0, 10)
                .Select(i => LoadTileRequired($"{PrisonTilesDir}backWall_{i}_00.asset"))
                .ToArray();
            var stampWalls = Enumerable.Range(0, 6)
                .Select(i => LoadTileRequired($"{PrisonTilesDir}stampWallSmall_{i}_00.asset"))
                .ToArray();
            var wallJunks = Enumerable.Range(10, 4)
                .Select(i => LoadTileRequired($"{PrisonTilesDir}wallJunk_{i}.asset"))
                .ToArray();

            westStone.ClearAllTiles();
            eastStone.ClearAllTiles();
            solidRim.ClearAllTiles();
            wallDecals.ClearAllTiles();

            foreach (var (xMin, xMax, floorTop) in MainLevelFloorSpans)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Tilemap targetTm = x < 52 ? westStone : eastStone;
                    targetTm.SetTile(new Vector3Int(x, floorTop - 1, 0), floorStamps[CellHash(x, floorTop) % floorStamps.Length]);
                    for (int y = -4; y <= floorTop - 2; y++)
                        targetTm.SetTile(new Vector3Int(x, y, 0), dirtTile);
                    solidRim.SetTile(new Vector3Int(x, floorTop, 0), groundTiles[CellHash(x, floorTop) % groundTiles.Length]);
                }
            }

            for (int y = -4; y <= 15; y++)
            {
                for (int x = -8; x <= -1; x++)
                    westStone.SetTile(new Vector3Int(x, y, 0), dirtTile);
                for (int x = 112; x <= 120; x++)
                    eastStone.SetTile(new Vector3Int(x, y, 0), dirtTile);
            }

            westStone.RefreshAllTiles();
            eastStone.RefreshAllTiles();
            solidRim.RefreshAllTiles();
            EditorUtility.SetDirty(westStone);
            EditorUtility.SetDirty(eastStone);
            EditorUtility.SetDirty(solidRim);

            // 10. Fill West & East Backgrounds, scatter TM_WallDecals, then carve 5 Gothic arch viewing bays
            for (int x = -1; x <= 52; x++)
            {
                for (int y = -4; y <= 15; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (!westBg.HasTile(pos))
                        westBg.SetTile(pos, backWalls[CellHash(x, y) % backWalls.Length]);
                }
            }

            for (int y = -1; y <= 15; y++)
                eastBg.SetTile(new Vector3Int(52, y, 0), null);

            for (int x = 53; x <= 111; x++)
            {
                for (int y = -1; y <= 15; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (!eastBg.HasTile(pos))
                        eastBg.SetTile(pos, backWalls[CellHash(x, y) % backWalls.Length]);
                }
            }

            for (int x = 1; x <= 110; x++)
            {
                int floorTop = GetFloorTop(x);
                for (int y = 1; y <= 11; y++)
                {
                    if (y < floorTop)
                        continue;

                    int roll = CellHash(x * 3 + 17, y * 7 + 31) % 100;
                    var pos = new Vector3Int(x, y, 0);
                    if (roll < 14)
                        wallDecals.SetTile(pos, stampWalls[CellHash(x, y) % stampWalls.Length]);
                    else if (roll < 18)
                        wallDecals.SetTile(pos, wallJunks[CellHash(x, y) % wallJunks.Length]);
                }
            }

            foreach (var (xMin, xMax, yMin, yMax) in CarvedArchBays)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    for (int y = yMin; y < yMax; y++)
                    {
                        if (y == yMax - 1 && (x == xMin || x == xMax - 1))
                            continue;

                        var cell = new Vector3Int(x, y, 0);
                        westBg.SetTile(cell, null);
                        eastBg.SetTile(cell, null);
                        wallDecals.SetTile(cell, null);
                    }
                }
            }

            westBg.RefreshAllTiles();
            eastBg.RefreshAllTiles();
            wallDecals.RefreshAllTiles();
            EditorUtility.SetDirty(westBg);
            EditorUtility.SetDirty(eastBg);
            EditorUtility.SetDirty(wallDecals);

            // 11. Ground sunken East Wing pillars, unify West Wing platforms, shift Exit Guard
            var cellPillar = GameObject.Find("Cell block pillar");
            if (cellPillar != null)
            {
                cellPillar.transform.position = new Vector3(71f, 1.79f, 0f);
                EditorUtility.SetDirty(cellPillar.transform);
            }

            var exitPillar = GameObject.Find("Exit hall pillar");
            if (exitPillar != null)
            {
                exitPillar.transform.position = new Vector3(98f, 2.79f, 0f);
                EditorUtility.SetDirty(exitPillar.transform);
            }

            foreach (var platName in new[] { "One Way Platform 16", "One Way Platform 21", "One Way Platform 28" })
            {
                var plat = GameObject.Find(platName);
                if (plat == null)
                    continue;

                var boards = plat.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(sr => sr.transform.parent == plat.transform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal))
                    .ToArray();
                foreach (var b in boards)
                {
                    var bt = b.transform;
                    bt.localScale = new Vector3(1f, 0.75f, 1f);
                    var lp = bt.localPosition;
                    bt.localPosition = new Vector3(lp.x, 0f, lp.z);
                    EditorUtility.SetDirty(bt);
                    EditorUtility.SetDirty(b);
                }
            }

            foreach (var eff in UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include))
            {
                eff.useOneWay = true;
                eff.surfaceArc = 160f;
                eff.rotationalOffset = 0f;
                EditorUtility.SetDirty(eff);
            }

            var exitGuard = GameObject.Find("Enemy_Shield - Exit Guard");
            if (exitGuard != null)
            {
                exitGuard.transform.position = new Vector3(100.5f, 0.05f, 0f);
                EditorUtility.SetDirty(exitGuard.transform);
            }

            // 12. R3 P1 cleanup + arch-bay framing + swaying banners + HDR emissive dressing + live shader God-rays
            BuildPrisonDressing(litMat, bgLayerId, groundLayerId, vfxLayerId);

            // 13. 4-tier Parallax, Ground Mist, Sorting-Layer-Isolated Volumetric/Flickering Lighting, ShadowCasters & PostFX
            var mainCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (mainCam != null)
            {
                if (!mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var uacd))
                    uacd = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                uacd.renderPostProcessing = true;
                uacd.volumeLayerMask |= 1 << 0;
                EditorUtility.SetDirty(uacd);
            }

            var postProfile = EnsureSharedPostProcessProfile();
            var globalVol = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                .FirstOrDefault(v => v.gameObject.name == "Global Volume" || v.isGlobal);
            if (globalVol == null)
            {
                var volGo = new GameObject("Global Volume");
                globalVol = volGo.AddComponent<Volume>();
            }
            globalVol.gameObject.name = "Global Volume";
            globalVol.gameObject.layer = 0;
            globalVol.gameObject.SetActive(true);
            globalVol.enabled = true;
            globalVol.isGlobal = true;
            globalVol.priority = 0f;
            globalVol.weight = 1f;
            globalVol.sharedProfile = postProfile;
            EditorUtility.SetDirty(globalVol.gameObject);
            EditorUtility.SetDirty(globalVol);

            EnsureAmbientDustMotes(particlesUnlitMat, vfxLayerId);
            BuildParallaxAndAtmosphereR4(mainCam, litMat, unlitMat, particlesUnlitMat);

            if (defeatScreen != null)
            {
                var frameTransform = defeatScreen.transform.Find("Backdrop/Frame");
                if (frameTransform != null && frameTransform.TryGetComponent<Image>(out var frameImage))
                {
                    frameImage.color = new Color(0.72f, 0.56f, 0.28f, 0.92f);
                    EditorUtility.SetDirty(frameImage);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetTilemapSorting(Tilemap tm, int sortingLayerId, int sortingOrder)
        {
            if (tm != null && tm.TryGetComponent<TilemapRenderer>(out var r))
            {
                r.sortingLayerID = sortingLayerId;
                r.sortingOrder = sortingOrder;
                EditorUtility.SetDirty(r);
            }
        }

        private static Tilemap EnsureSiblingVisualTilemap(Transform gridParent, string tilemapName, int sortingLayerId, int sortingOrder, Material material)
        {
            Transform existing = gridParent != null ? gridParent.Find(tilemapName) : null;
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(tilemapName);
                if (gridParent != null)
                    go.transform.SetParent(gridParent, false);
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if (!go.TryGetComponent<Tilemap>(out var tm))
                tm = go.AddComponent<Tilemap>();
            if (!go.TryGetComponent<TilemapRenderer>(out var renderer))
                renderer = go.AddComponent<TilemapRenderer>();

            if (go.TryGetComponent<TilemapCollider2D>(out var col))
                UnityEngine.Object.DestroyImmediate(col);

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = material;

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(tm);
            EditorUtility.SetDirty(renderer);
            return tm;
        }

        private static void BuildPrisonDressing(Material litMat, int bgLayerId, int groundLayerId, int vfxLayerId)
        {
            var prisonGo = GameObject.Find("Prison");
            Transform decorationParent = prisonGo != null ? prisonGo.transform.Find("Decoration") : null;
            if (decorationParent == null)
            {
                var decGo = GameObject.Find("Decoration");
                decorationParent = decGo != null ? decGo.transform : null;
            }

            var existingContainer = decorationParent != null
                ? decorationParent.Find("Prison_Dressing_R3")?.gameObject
                : GameObject.Find("Prison_Dressing_R3");
            if (existingContainer != null)
                UnityEngine.Object.DestroyImmediate(existingContainer);

            RemoveStrayLegacyDressingObjects();

            var rootGo = new GameObject("Prison_Dressing_R3");
            if (decorationParent != null)
                rootGo.transform.SetParent(decorationParent, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localRotation = Quaternion.identity;
            rootGo.transform.localScale = Vector3.one;
            var root = rootGo.transform;

            var bannerSwayMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Banner_Sway.mat");
            var roomBannerSwayMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_RoomBanner_Sway.mat");
            var godRayMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_GodRay_Additive.mat");
            var mushroomMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_Mushroom.mat");
            var mushroomHaloMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_MushroomHalo.mat");
            var exitGlowMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_Exit.mat");
            var scrollGlowMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_Scroll.mat");

            // Frame each of the 5 carved arch bays with corner beams, subtle cobwebs & 24-PPU carved stone sills behind Stone pillars
            var beamTL = LoadSpriteRequired(PrisonSpritesDir + "littleBlackBeam_topLeft_0_00.png");
            var beamTR = LoadSpriteRequired(PrisonSpritesDir + "littleBlackBeam_topRight_0_00.png");
            var webSmallTL = LoadSpriteRequired(PrisonSpritesDir + "webSmallTL_00.png");
            var webSmallTR = LoadSpriteRequired(PrisonSpritesDir + "webSmallTR_00.png");
            var archSillSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchSillAssetPath);
            var archJambSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.ArchJambAssetPath);
            var webTint = new Color(0.85f, 0.88f, 0.90f, 0.65f);

            for (int i = 0; i < CarvedArchBays.Length; i++)
            {
                var (xMin, xMax, yMin, yMax) = CarvedArchBays[i];
                CreateDressingSprite(root, $"ArchBeam_TL_{i}", beamTL, new Vector3(xMin + 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
                CreateDressingSprite(root, $"ArchBeam_TR_{i}", beamTR, new Vector3(xMax - 0.5f, yMax - 0.5f, 0f), bgLayerId, -15, litMat, Color.white);
                CreateDressingSprite(root, $"ArchWeb_TL_{i}", webSmallTL, new Vector3(xMin + 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);
                CreateDressingSprite(root, $"ArchWeb_TR_{i}", webSmallTR, new Vector3(xMax - 0.9f, yMax - 0.9f, 0f), bgLayerId, -16, litMat, webTint);

                for (int sx = xMin; sx < xMax; sx++)
                {
                    var sillGo = CreateDressingSprite(
                        root,
                        $"ArchSill_{i}_{sx}",
                        archSillSprite,
                        new Vector3(sx + 0.5f, yMin + 0.04f, 0f),
                        bgLayerId,
                        -17,
                        litMat,
                        Color.white);
                    sillGo.transform.localScale = Vector3.one;
                    EditorUtility.SetDirty(sillGo.transform);

                    var lintelGo = CreateDressingSprite(
                        root,
                        $"ArchLintel_{i}_{sx}",
                        archSillSprite,
                        new Vector3(sx + 0.5f, yMax - 0.04f, 0f),
                        bgLayerId,
                        -17,
                        litMat,
                        Color.white);
                    lintelGo.transform.localScale = Vector3.one;
                    if (lintelGo.TryGetComponent<SpriteRenderer>(out var lintelSr))
                    {
                        lintelSr.flipY = true;
                        EditorUtility.SetDirty(lintelSr);
                    }
                    EditorUtility.SetDirty(lintelGo.transform);
                }

                for (int sy = yMin; sy < yMax; sy++)
                {
                    var jambLGo = CreateDressingSprite(
                        root,
                        $"ArchJamb_L_{i}_{sy}",
                        archJambSprite,
                        new Vector3(xMin + (4f / 24f), sy + 0.5f, 0f),
                        bgLayerId,
                        -17,
                        litMat,
                        Color.white);
                    jambLGo.transform.localScale = Vector3.one;
                    EditorUtility.SetDirty(jambLGo.transform);

                    var jambRGo = CreateDressingSprite(
                        root,
                        $"ArchJamb_R_{i}_{sy}",
                        archJambSprite,
                        new Vector3(xMax - (4f / 24f), sy + 0.5f, 0f),
                        bgLayerId,
                        -17,
                        litMat,
                        Color.white);
                    jambRGo.transform.localScale = Vector3.one;
                    if (jambRGo.TryGetComponent<SpriteRenderer>(out var jambRSr))
                    {
                        jambRSr.flipX = true;
                        EditorUtility.SetDirty(jambRSr);
                    }
                    EditorUtility.SetDirty(jambRGo.transform);
                }
            }

            // Swaying wall banners on solid piers / arch header (Mat_Banner_Sway for single-piece, Mat_RoomBanner_Sway for 3-piece)
            var bannerBig = LoadSpriteRequired(PrisonSpritesDir + "bannerBig_00.png");
            var bannerTop = LoadSpriteRequired(PrisonSpritesDir + "bannerRoomTop_00.png");
            var bannerTiled = LoadSpriteRequired(PrisonSpritesDir + "bannerRoomTiled_00.png");
            var bannerBot = LoadSpriteRequired(PrisonSpritesDir + "bannerRoomBot_00.png");

            CreateDressingSprite(root, "BannerBig_4", bannerBig, new Vector3(4f, 6.2f, 0f), bgLayerId, -12, bannerSwayMat, Color.white);
            CreateDressingSprite(root, "BannerBig_48", bannerBig, new Vector3(48f, 6.2f, 0f), bgLayerId, -12, bannerSwayMat, Color.white);
            var bannerBig92 = CreateDressingSprite(root, "BannerBig_92", bannerBig, new Vector3(95.2f, 9.10f, 0f), bgLayerId, -12, bannerSwayMat, Color.white);
            bannerBig92.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            EditorUtility.SetDirty(bannerBig92.transform);

            CreateVerticalRoomBanner(root, "RoomBanner_23", 23.5f, 7.6f, bannerTop, bannerTiled, bannerBot, bgLayerId, -12, roomBannerSwayMat);
            CreateVerticalRoomBanner(root, "RoomBanner_61", 60.6f, 7.6f, bannerTop, bannerTiled, bannerBot, bgLayerId, -12, roomBannerSwayMat);
            CreateVerticalRoomBanner(root, "RoomBanner_84", 84.0f, 7.6f, bannerTop, bannerTiled, bannerBot, bgLayerId, -12, roomBannerSwayMat);

            // Low rubble only (floorjunk_6..9_00, <= 0.62u), grounded near pillars & walls
            var lowRubble = new[]
            {
                LoadSpriteRequired(PrisonSpritesDir + "floorjunk_6_00.png"),
                LoadSpriteRequired(PrisonSpritesDir + "floorjunk_7_00.png"),
                LoadSpriteRequired(PrisonSpritesDir + "floorjunk_8_00.png"),
                LoadSpriteRequired(PrisonSpritesDir + "floorjunk_9_00.png"),
            };
            var junkTint = new Color(0.78f, 0.80f, 0.82f, 1f);
            float[] junkAnchors = { 2f, 14f, 30f, 52f, 70f, 95f };
            for (int i = 0; i < junkAnchors.Length; i++)
            {
                float x = junkAnchors[i];
                float floorTop = GetFloorTop(Mathf.RoundToInt(x));
                var spA = lowRubble[(i * 2) % lowRubble.Length];
                var spB = lowRubble[(i * 2 + 1) % lowRubble.Length];
                CreateDressingSprite(root, $"FloorJunk_{i}_A", spA, new Vector3(x, floorTop - spA.bounds.min.y, 0f), groundLayerId, -8, litMat, junkTint);
                CreateDressingSprite(root, $"FloorJunk_{i}_B", spB, new Vector3(x + 0.65f, floorTop - spB.bounds.min.y, 0f), groundLayerId, -8, litMat, junkTint);
            }

            // Re-aligned additive God-rays strictly below paired windows (Mat_GodRay_Additive)
            var godRaySprite = LoadSpriteRequired(PrisonSpritesDir + "godraysArcheTriainingRoom_00.png");
            for (int i = 0; i < PrisonWindowGodRayCoords.Length; i++)
            {
                var coord = PrisonWindowGodRayCoords[i];
                var rayPos = new Vector3(coord.x + 0.55f, coord.y - 1.25f, 0f);
                var go = CreateDressingSprite(root, $"GodRay_{i}", godRaySprite, rayPos, vfxLayerId, 10, godRayMat, Color.white);
                go.transform.localScale = new Vector3(0.42f, 0.48f, 1f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                EditorUtility.SetDirty(go.transform);
            }

            // 4 damp-corner glowing mushroom clusters at exact 1:1 24-PPU with tiny cap-aligned additive halo & isolated local cyan bounce light
            var fxGlowSprite = LoadSpriteRequired(FxGlowWhitePath);
            var mushroomBrightSprite = LoadSpriteRequired(PrisonSpritesDir + "glowingMushroom_3_00.png");
            float[] mushroomXs = { 16.5f, 51.0f, 69.0f, 93.0f };
            for (int i = 0; i < mushroomXs.Length; i++)
            {
                float mx = mushroomXs[i];
                float floorTop = GetFloorTop(Mathf.RoundToInt(mx));
                bool flipX = (i % 2) == 1;
                Vector3 mPos = new Vector3(mx, floorTop - mushroomBrightSprite.bounds.min.y, 0f);
                var mGo = CreateDressingSprite(
                    root,
                    $"GlowingMushroom_{i}",
                    mushroomBrightSprite,
                    mPos,
                    groundLayerId,
                    -7,
                    mushroomMat,
                    Color.white);
                mGo.transform.localScale = Vector3.one;
                if (mGo.TryGetComponent<SpriteRenderer>(out var mSr))
                {
                    mSr.flipX = flipX;
                    EditorUtility.SetDirty(mSr);
                }
                EditorUtility.SetDirty(mGo.transform);

                float budX = mx + (flipX ? 0.484f : -0.484f);
                float budY = floorTop + 0.410f;

                var haloGo = CreateDressingSprite(
                    root,
                    $"MushroomHalo_{i}",
                    fxGlowSprite,
                    new Vector3(budX, budY, 0f),
                    groundLayerId,
                    -8,
                    mushroomHaloMat,
                    new Color(1f, 1f, 1f, 0.24f));
                haloGo.transform.localScale = new Vector3(0.16f, 0.14f, 1f);
                EditorUtility.SetDirty(haloGo.transform);

                var lightGo = new GameObject($"MushroomLight_{i}");
                lightGo.transform.SetParent(root, false);
                lightGo.transform.position = new Vector3(budX, budY, 0f);
                var mLight = lightGo.AddComponent<Light2D>();
                mLight.lightType = Light2D.LightType.Point;
                mLight.color = new Color(0.17f, 0.90f, 0.84f, 1f);
                mLight.intensity = 0.72f;
                mLight.pointLightInnerRadius = 0.15f;
                mLight.pointLightOuterRadius = 2.1f;
                mLight.falloffIntensity = 0.55f;
                mLight.enabled = true;
                EditorUtility.SetDirty(lightGo);
                EditorUtility.SetDirty(mLight);
            }

            Vector3[] galleryFillPositions =
            {
                new Vector3(56.5f, 5.8f, 0f),
                new Vector3(70.5f, 6.8f, 0f),
                new Vector3(87.5f, 6.2f, 0f),
            };
            for (int i = 0; i < galleryFillPositions.Length; i++)
            {
                var fillGo = new GameObject($"GalleryFillLight_{i}");
                fillGo.transform.SetParent(root, false);
                fillGo.transform.position = galleryFillPositions[i];
                var fillLight = fillGo.AddComponent<Light2D>();
                fillLight.lightType = Light2D.LightType.Point;
                fillLight.color = new Color(1.0f, 0.84f, 0.58f, 1f);
                fillLight.intensity = 0.80f;
                fillLight.pointLightInnerRadius = 1.2f;
                fillLight.pointLightOuterRadius = 6.8f;
                fillLight.falloffIntensity = 0.50f;
                fillLight.enabled = true;
                EditorUtility.SetDirty(fillGo);
                EditorUtility.SetDirty(fillLight);
            }

            // Exit Door back-glow (Mat_Emissive_Exit raised & trimmed to clear floor top crevices)
            var exitBackGlow = CreateDressingSprite(
                root,
                "ExitDoor_BackGlow",
                fxGlowSprite,
                new Vector3(108f, 2.55f, 0f),
                bgLayerId,
                -11,
                exitGlowMat,
                new Color(1f, 0.90f, 0.62f, 0.52f));
            exitBackGlow.transform.localScale = new Vector3(1.40f, 1.40f, 1f);
            EditorUtility.SetDirty(exitBackGlow.transform);

            // Power Scroll emissive halos (Mat_Emissive_Scroll) parented under _icon so Reveal(true) hides them
            var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include)
                .OrderBy(s => s.transform.position.x)
                .ToArray();
            for (int i = 0; i < scrolls.Length; i++)
            {
                foreach (var oldSr in scrolls[i].GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (oldSr != null && oldSr.gameObject.name.StartsWith("ScrollAura_R4_", StringComparison.Ordinal))
                        UnityEngine.Object.DestroyImmediate(oldSr.gameObject);
                }

                var soScroll = new SerializedObject(scrolls[i]);
                var iconTr = soScroll.FindProperty("_icon")?.objectReferenceValue as Transform ?? scrolls[i].transform;
                var auraGo = CreateDressingSprite(
                    iconTr,
                    $"ScrollAura_R4_{i}",
                    fxGlowSprite,
                    iconTr.position,
                    vfxLayerId,
                    -2,
                    scrollGlowMat,
                    new Color(1f, 1f, 1f, 0.36f));
                auraGo.transform.localPosition = Vector3.zero;
                auraGo.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
                EditorUtility.SetDirty(auraGo.transform);

                var glowProp = soScroll.FindProperty("_glow");
                if (glowProp != null && glowProp.objectReferenceValue == null)
                {
                    glowProp.objectReferenceValue = auraGo;
                    soScroll.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(scrolls[i]);
                }
            }

            EditorUtility.SetDirty(rootGo);
        }

        private static void RemoveStrayLegacyDressingObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            var legacyNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "Beam_TL_West", "Beam_TR_West", "Beam_TL_East", "Beam_TR_East",
                "Web_TL_West", "Web_TR_West", "Web_TL_East", "Web_TR_East"
            };
            var toDelete = new List<GameObject>();
            foreach (var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (tr == null || tr.gameObject.scene != activeScene)
                    continue;
                if (legacyNames.Contains(tr.name) ||
                    tr.name.StartsWith("CornerL_One Way Platform", StringComparison.Ordinal) ||
                    tr.name.StartsWith("CornerR_One Way Platform", StringComparison.Ordinal))
                {
                    toDelete.Add(tr.gameObject);
                }
            }
            for (int i = 0; i < toDelete.Count; i++)
            {
                if (toDelete[i] != null)
                    UnityEngine.Object.DestroyImmediate(toDelete[i]);
            }
        }

        private static void BuildParallaxAndAtmosphereR4(Camera mainCam, Material litMat, Material unlitMat, Material particlesUnlitMat)
        {
            int defaultLayerId = SortingLayerSetup.GetSortingLayerId("Default");
            int parallaxFarLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxFar");
            int parallaxMidLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxMid");
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
            int actorsLayerId = SortingLayerSetup.GetSortingLayerId("Actors");
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
            int fgLayerId = SortingLayerSetup.GetSortingLayerId("Foreground");

            // 13a. Assign semantic Sorting Layers cleanly across existing scene renderers
            AssignSceneSortingLayers(bgLayerId, groundLayerId, actorsLayerId, vfxLayerId);

            // 13b. Build idempotent Prison_Parallax_R4 container (ParallaxFar, ParallaxMid, Foreground)
            var prisonGo = GameObject.Find("Prison");
            Transform prisonTransform = prisonGo != null ? prisonGo.transform : null;
            var existingParallax = GameObject.Find("Prison_Parallax_R4");
            if (existingParallax != null)
                UnityEngine.Object.DestroyImmediate(existingParallax);

            var parallaxRoot = new GameObject("Prison_Parallax_R4");
            if (prisonTransform != null)
                parallaxRoot.transform.SetParent(prisonTransform, false);
            parallaxRoot.transform.localPosition = Vector3.zero;
            parallaxRoot.transform.localRotation = Quaternion.identity;
            parallaxRoot.transform.localScale = Vector3.one;

            Transform camTransform = mainCam != null ? mainCam.transform : null;

            // Tier 0: ParallaxFar_Backdrop (768x512 @ 24 PPU => 32u x 21.333u at 1:1 square pixels, covering y = [-5.17, +16.17])
            var farGo = new GameObject("ParallaxFar_Backdrop");
            farGo.transform.SetParent(parallaxRoot.transform, false);
            farGo.transform.localPosition = Vector3.zero;
            var farParallax = farGo.AddComponent<ParallaxLayer2D>();
            farParallax.Configure(camTransform, new Vector2(0.15f, 0.06f), 32f, pixelSnap: true, pixelsPerUnit: 24f);

            var farSprite = LoadSpriteRequired(ParallaxFarSpritePath);
            var farMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_ParallaxFar_Prison.mat");
            var farPanelSpecs = new (string name, float x)[]
            {
                ("Panel_Left", -32f),
                ("Panel_Center", 0f),
                ("Panel_Right", 32f),
            };
            foreach (var spec in farPanelSpecs)
            {
                var panel = CreateDressingSprite(farGo.transform, spec.name, farSprite, new Vector3(spec.x, 5.5f, 0f), parallaxFarLayerId, 0, farMat, Color.white);
                panel.transform.localPosition = new Vector3(spec.x, 5.5f, 0f);
                panel.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(panel.transform);
            }
            EditorUtility.SetDirty(farParallax);

            // Tier 1: ParallaxMid_Alcoves (3 wrapped 32u-period chunks of open Gothic alcoves + swaying vines; excludes solid-black alccoveParalaxeB_00)
            var midGo = new GameObject("ParallaxMid_Alcoves");
            midGo.transform.SetParent(parallaxRoot.transform, false);
            midGo.transform.localPosition = Vector3.zero;
            var midParallax = midGo.AddComponent<ParallaxLayer2D>();
            midParallax.Configure(camTransform, new Vector2(0.38f, 0.14f), 32f, pixelSnap: true, pixelsPerUnit: 24f);

            var alcoveASprite = LoadSpriteRequired(PrisonSpritesDir + "alccoveParalaxeA_00.png");
            var vineASprite = LoadSpriteRequired(PrisonSpritesDir + "paralaxLianeA_00.png");
            var vineBSprite = LoadSpriteRequired(PrisonSpritesDir + "paralaxLianeB_00.png");
            var midMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_ParallaxMid_Prison.mat");
            var vineMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Vine_Sway.mat");
            var vineTint = new Color(0.58f, 0.72f, 0.64f, 0.90f);

            float[] chunkOffsets = { -32f, 0f, 32f };
            float[] alcoveAXs = { -13.3333f, -8f, -2.6667f, 2.6667f, 8f, 13.3333f };
            float[] vineXs = { -9.5f, 1.5f, 11.5f };

            for (int c = 0; c < chunkOffsets.Length; c++)
            {
                var chunkGo = new GameObject($"Chunk_{c}");
                chunkGo.transform.SetParent(midGo.transform, false);
                chunkGo.transform.localPosition = new Vector3(chunkOffsets[c], 0f, 0f);

                for (int a = 0; a < alcoveAXs.Length; a++)
                {
                    var spGo = CreateDressingSprite(chunkGo.transform, $"AlcoveA_{a}", alcoveASprite, Vector3.zero, parallaxMidLayerId, 0, midMat, Color.white);
                    spGo.transform.localPosition = new Vector3(alcoveAXs[a], 2.6f, 0f);
                    EditorUtility.SetDirty(spGo.transform);
                }

                for (int v = 0; v < vineXs.Length; v++)
                {
                    var vineSprite = (v % 2 == 0) ? vineASprite : vineBSprite;
                    var spGo = CreateDressingSprite(chunkGo.transform, $"Vine_{v}", vineSprite, Vector3.zero, parallaxMidLayerId, 2, vineMat, vineTint);
                    spGo.transform.localPosition = new Vector3(vineXs[v], 6.2f, 0f);
                    EditorUtility.SetDirty(spGo.transform);
                }
            }
            EditorUtility.SetDirty(midParallax);

            // Tier 4: Foreground_Silhouettes (dark ceiling beams/chains locked vertically to camera top while keeping bounds.min.y in [8.5, 10.2] at cam.y=3.8)
            var fgGo = new GameObject("Foreground_Silhouettes");
            fgGo.transform.SetParent(parallaxRoot.transform, false);
            fgGo.transform.localPosition = Vector3.zero;
            var fgParallax = fgGo.AddComponent<ParallaxLayer2D>();
            fgParallax.Configure(camTransform, new Vector2(1.18f, 1.0f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
            fgParallax.ConfigureVerticalCameraLock(true, 3.8f);

            var fgBeamTL = LoadSpriteRequired(PrisonSpritesDir + "blackBeam_topLeft_0_00.png");
            var fgBeamTR = LoadSpriteRequired(PrisonSpritesDir + "blackBeam_topRight_0_00.png");
            var fgChain = LoadSpriteRequired(PrisonSpritesDir + "cageChain_00.png");
            var fgTint = new Color(6f / 255f, 10f / 255f, 16f / 255f, 0.96f);

            float[] fgAnchorXs = { -20f, -12f, -4f, 4f, 12f, 20f, 28f, 36f };
            for (int i = 0; i < fgAnchorXs.Length; i++)
            {
                float x = fgAnchorXs[i];
                var beamSprite = (i % 2 == 0) ? fgBeamTL : fgBeamTR;
                var bGo = CreateDressingSprite(fgGo.transform, $"FgBeam_{i}", beamSprite, Vector3.zero, fgLayerId, 20, unlitMat, fgTint);
                bGo.transform.localPosition = new Vector3(x, 11.45f, 0f);
                EditorUtility.SetDirty(bGo.transform);

                var cGo = CreateDressingSprite(fgGo.transform, $"FgChain_{i}", fgChain, Vector3.zero, fgLayerId, 19, unlitMat, fgTint);
                cGo.transform.localPosition = new Vector3(x + 2.2f, 10.30f, 0f);
                cGo.transform.localScale = new Vector3(1.1f, 2.0f, 1f);
                EditorUtility.SetDirty(cGo.transform);
            }
            EditorUtility.SetDirty(fgParallax);

            // 13c. GroundMist_R4 along low cell blocks (x=[15,28] and x=[68,94])
            var existingMist = GameObject.Find("GroundMist_R4");
            if (existingMist != null)
                UnityEngine.Object.DestroyImmediate(existingMist);

            Transform decorationParent = prisonTransform != null ? prisonTransform.Find("Decoration") : null;
            var mistRoot = new GameObject("GroundMist_R4");
            if (decorationParent != null)
                mistRoot.transform.SetParent(decorationParent, false);
            else if (prisonTransform != null)
                mistRoot.transform.SetParent(prisonTransform, false);
            mistRoot.transform.localPosition = Vector3.zero;

            var whitePixelSprite = LoadSpriteRequired(PrisonSpritesDir + "whitePixel_00.png");
            var groundMistMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_GroundMist_Prison.mat");
            Vector3 wpSize = whitePixelSprite.bounds.size;
            var mistSpans = new (string name, float centerX, float width, float centerY, float height)[]
            {
                ("Mist_LowerWest", 21.5f, 13f, 0.0f, 2.0f),
                ("Mist_LowerEast", 81.0f, 26f, 0.0f, 2.0f),
            };
            foreach (var span in mistSpans)
            {
                var strip = CreateDressingSprite(mistRoot.transform, span.name, whitePixelSprite, new Vector3(span.centerX, span.centerY, 0f), vfxLayerId, -1, groundMistMat, Color.white);
                strip.transform.localScale = new Vector3(span.width / wpSize.x, span.height / wpSize.y, 1f);
                EditorUtility.SetDirty(strip.transform);
            }

            // 13d. Lighting: Global Light 2D + Spawn Lantern + Repositioned Clean Wall Lanterns + Fixtures, Flame Cores & Embers
            EnsureSpawnLantern(decorationParent);
            RepositionClashingLanterns();

            int[] allLayerIds = { defaultLayerId, parallaxFarLayerId, parallaxMidLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId, fgLayerId };
            int[] gameplayLayerIds = { defaultLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId };

            var allLights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .OrderBy(l => l.transform.position.x)
                .ThenBy(l => l.name, StringComparer.Ordinal)
                .ToArray();

            var fxGlowSprite = LoadSpriteRequired(FxGlowWhitePath);
            var lanternFixtureSprite = LoadSpriteRequired(PrisonPropSpriteGenerator.WallLanternAssetPath);
            var flameMat = LoadMaterialRequired(EnvMaterialsDir + "Mat_Emissive_Flame.mat");
            int primaryLanternIndex = 0;

            foreach (var light in allLights)
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    light.intensity = 0.62f;
                    light.color = new Color(0.74f, 0.86f, 0.92f, 1f);
                    SetLightSortingLayers(light, allLayerIds);
                    EditorUtility.SetDirty(light);
                    continue;
                }

                if (light.lightType != Light2D.LightType.Point)
                    continue;

                SetLightSortingLayers(light, gameplayLayerIds);
                SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);

                if (light.gameObject.name == "Accent light" &&
                    light.transform.parent != null &&
                    light.transform.parent.name == "Exit Door")
                {
                    light.intensity = 1.45f;
                    light.pointLightInnerRadius = 1.2f;
                    light.pointLightOuterRadius = 7.0f;
                    light.falloffIntensity = 0.40f;
                }

                if (IsPrimaryLanternLight(light))
                {
                    // Boost warm lantern pools for Linear + ACES + NormalMap lighting depth
                    if (light.color.r >= light.color.b)
                    {
                        if (light.gameObject.name == "Exit hall lantern")
                        {
                            light.intensity = 1.75f;
                            light.pointLightInnerRadius = 1.4f;
                            light.pointLightOuterRadius = 8.4f;
                            light.falloffIntensity = 0.38f;
                        }
                        else if (light.gameObject.name == "Spawn lantern")
                        {
                            light.intensity = 1.35f;
                            light.pointLightInnerRadius = 1.0f;
                            light.pointLightOuterRadius = 6.8f;
                            light.falloffIntensity = 0.45f;
                        }
                        else
                        {
                            light.intensity = 1.45f;
                            light.pointLightInnerRadius = 1.2f;
                            light.pointLightOuterRadius = 7.6f;
                            light.falloffIntensity = 0.45f;
                        }
                    }
                    else
                    {
                        light.intensity = 1.30f;
                        light.pointLightOuterRadius = 6.0f;
                    }

                    ConfigureLightVolumetric(light, true, 0.09f);

                    if (!light.TryGetComponent<TorchFlicker2D>(out var flicker))
                        flicker = light.gameObject.AddComponent<TorchFlicker2D>();

                    flicker.Configure(light, light.intensity, light.pointLightOuterRadius, amplitude: 0.08f, speed: 3.2f, phaseOffset: primaryLanternIndex * 1.15f);
                    flicker.enabled = true;
                    EditorUtility.SetDirty(flicker);

                    // Clean up previous children for idempotency
                    DestroyChildIfExists(light.transform, "LanternFixture_R4");
                    DestroyChildIfExists(light.transform, "LanternFlameCore_R4");
                    DestroyChildIfExists(light.transform, "TorchEmbers");

                    if (light.color.r >= light.color.b)
                    {
                        // 24-PPU wall-mounted wrought-iron sconce lantern (scale 1:1, flame cage center sits -0.125u below sprite center)
                        var fixtureGo = CreateDressingSprite(
                            light.transform,
                            "LanternFixture_R4",
                            lanternFixtureSprite,
                            light.transform.position,
                            bgLayerId,
                            -9,
                            litMat,
                            Color.white);
                        fixtureGo.transform.localPosition = new Vector3(0f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 0f);
                        fixtureGo.transform.localScale = Vector3.one;
                        EditorUtility.SetDirty(fixtureGo.transform);

                        var coreGo = CreateDressingSprite(light.transform, "LanternFlameCore_R4", fxGlowSprite, light.transform.position, bgLayerId, -8, flameMat, new Color(1f, 1f, 1f, 0.82f));
                        coreGo.transform.localPosition = Vector3.zero;
                        coreGo.transform.localScale = new Vector3(0.085f, 0.12f, 1f);
                        EditorUtility.SetDirty(coreGo.transform);

                        CreateTorchEmbers(light.transform, particlesUnlitMat, bgLayerId, -7);
                    }

                    primaryLanternIndex++;
                }

                EditorUtility.SetDirty(light);
            }

            // 13e. ShadowCasters_R4 framing solid stone step-terraces
            var existingCasters = GameObject.Find("ShadowCasters_R4");
            if (existingCasters != null)
                UnityEngine.Object.DestroyImmediate(existingCasters);

            var castersRoot = new GameObject("ShadowCasters_R4");
            if (prisonTransform != null)
                castersRoot.transform.SetParent(prisonTransform, false);
            castersRoot.transform.localPosition = Vector3.zero;

            int[] shadowLayerIds = { bgLayerId, groundLayerId };
            CreateBoxShadowCaster(castersRoot.transform, "StepWall_West_37_45", new Vector3(43f, -1f, 0f), 4f, 6f, shadowLayerIds);
            CreateBoxShadowCaster(castersRoot.transform, "DropOff_East_68", new Vector3(66f, -2f, 0f), 4f, 4f, shadowLayerIds);
            CreateBoxShadowCaster(castersRoot.transform, "ExitStep_94", new Vector3(96f, -2f, 0f), 4f, 4f, shadowLayerIds);

            EditorUtility.SetDirty(parallaxRoot);
            EditorUtility.SetDirty(mistRoot);
            EditorUtility.SetDirty(castersRoot);
        }

        private static void AssignSceneSortingLayers(int bgLayerId, int groundLayerId, int actorsLayerId, int vfxLayerId)
        {
            var activeScene = SceneManager.GetActiveScene();

            foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (sr == null || sr.gameObject.scene != activeScene)
                    continue;

                string goName = sr.gameObject.name;
                string rootName = sr.transform.root != null ? sr.transform.root.name : string.Empty;

                if (goName.Contains("pillar", StringComparison.OrdinalIgnoreCase) ||
                    goName.Contains("window", StringComparison.OrdinalIgnoreCase))
                {
                    sr.sortingLayerID = bgLayerId;
                    EditorUtility.SetDirty(sr);
                }
                else if (goName.StartsWith("Board", StringComparison.Ordinal) ||
                         goName == "Chain link" ||
                         goName == "Door art" ||
                         goName == "Scroll vat" ||
                         goName == "Floating scroll" ||
                         goName == "Floating icon")
                {
                    sr.sortingLayerID = groundLayerId;
                    EditorUtility.SetDirty(sr);
                }
                else if (rootName == "Systems" && IsDescendantOfAny(sr.transform, "Scroll cinematic", "Enemy health"))
                {
                    sr.sortingLayerID = vfxLayerId;
                    EditorUtility.SetDirty(sr);
                }
            }

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerID = actorsLayerId;
                    EditorUtility.SetDirty(sr);
                }
            }

            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                if (enemy == null || enemy.gameObject.scene != activeScene)
                    continue;
                foreach (var sr in enemy.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.sortingLayerID = actorsLayerId;
                    EditorUtility.SetDirty(sr);
                }
            }
        }

        private static bool IsDescendantOfAny(Transform t, string nameA, string nameB)
        {
            for (Transform cur = t.parent; cur != null; cur = cur.parent)
            {
                if (cur.name == nameA || cur.name == nameB)
                    return true;
            }
            return false;
        }

        private static void EnsureSpawnLantern(Transform decorationParent)
        {
            var existing = decorationParent != null
                ? decorationParent.Find("Spawn lantern")?.gameObject
                : GameObject.Find("Spawn lantern");

            GameObject go;
            if (existing != null)
            {
                go = existing;
            }
            else
            {
                go = new GameObject("Spawn lantern");
                if (decorationParent != null)
                    go.transform.SetParent(decorationParent, false);
            }

            go.transform.position = new Vector3(6.0f, 2.85f, 0f);
            if (!go.TryGetComponent<Light2D>(out var light))
                light = go.AddComponent<Light2D>();

            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1.0f, 0.72f, 0.44f, 1f);
            light.intensity = 1.65f;
            light.pointLightInnerRadius = 1.0f;
            light.pointLightOuterRadius = 6.2f;
            light.falloffIntensity = 0.42f;
            light.enabled = true;

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(light);
        }

        private static void RepositionClashingLanterns()
        {
            var decorationAccentLights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include)
                .Where(l => l != null && l.lightType == Light2D.LightType.Point &&
                            l.gameObject.name == "Accent light" &&
                            l.transform.parent != null &&
                            l.transform.parent.name == "Decoration")
                .OrderBy(l => l.transform.position.x)
                .ToArray();

            // Reposition the 2nd Decoration Accent light (was at (23.0, 2.7) colliding with One Way Platform 21)
            // onto the solid stone pier between CarvedArchBay #0 (xMax=22) and RoomBanner_23 (x=23.5).
            if (decorationAccentLights.Length >= 2)
            {
                var clashLight = decorationAccentLights[1];
                clashLight.transform.position = new Vector3(22.35f, 4.45f, 0f);
                EditorUtility.SetDirty(clashLight.transform);
            }

            // Reposition Exit hall lantern (was at (105.0, 2.0) pasted on top of the grey Exit Door banner)
            // onto clean brick wall left of the banner to illuminate both Enemy_Shield's floor and the door approach.
            var exitLantern = GameObject.Find("Exit hall lantern");
            if (exitLantern != null)
            {
                exitLantern.transform.position = new Vector3(102.2f, 2.65f, 0f);
                EditorUtility.SetDirty(exitLantern.transform);
            }
        }

        private static bool IsPrimaryLanternLight(Light2D light)
        {
            if (!light.enabled || !light.gameObject.activeInHierarchy)
                return false;

            string name = light.gameObject.name;
            if (name == "Spawn lantern" ||
                name == "Gallery lantern" ||
                name == "Archive lantern" ||
                name == "Exit hall lantern" ||
                name == "Lower cell block light")
            {
                return true;
            }

            return name == "Accent light" &&
                   light.transform.parent != null &&
                   light.transform.parent.name == "Decoration";
        }

        private static void SetLightSortingLayers(Light2D light, int[] layerIds)
        {
            light.targetSortingLayers = layerIds;
            var so = new SerializedObject(light);
            var prop = so.FindProperty("m_ApplyToSortingLayers");
            if (prop != null && prop.isArray)
            {
                prop.arraySize = layerIds.Length;
                for (int i = 0; i < layerIds.Length; i++)
                    prop.GetArrayElementAtIndex(i).intValue = layerIds[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(light);
        }

        private static void SetLightNormalMapQuality(Light2D light, Light2D.NormalMapQuality quality, float distance)
        {
            var so = new SerializedObject(light);
            var qProp = so.FindProperty("m_NormalMapQuality");
            var dProp = so.FindProperty("m_NormalMapDistance");
            if (qProp != null) qProp.intValue = (int)quality;
            if (dProp != null) dProp.floatValue = distance;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        private static void ConfigureLightVolumetric(Light2D light, bool enabled, float intensity)
        {
            light.volumetricEnabled = enabled;
            light.volumeIntensity = intensity;
            var so = new SerializedObject(light);
            var volEnProp = so.FindProperty("m_LightVolumeEnabled");
            var volIntProp = so.FindProperty("m_LightVolumeIntensity");
            if (volEnProp != null) volEnProp.boolValue = enabled;
            if (volIntProp != null) volIntProp.floatValue = intensity;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        private static void CreateTorchEmbers(Transform parent, Material particlesUnlitMat, int sortingLayerId, int sortingOrder = -7)
        {
            var go = new GameObject("TorchEmbers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 6;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.8f, 1.0f, 0.35f, 0.85f));
            main.gravityModifier = -0.15f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 3.5f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.78f, 0.30f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.38f, 0.12f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.2f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = particlesUnlitMat;
                psr.sortingLayerID = sortingLayerId;
                psr.sortingOrder = sortingOrder;
                EditorUtility.SetDirty(psr);
            }

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(ps);
        }

        private static void CreateBoxShadowCaster(Transform parent, string name, Vector3 worldCenter, float width, float height, int[] targetSortingLayerIds)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldCenter;

            var sc = go.AddComponent<ShadowCaster2D>();
            sc.castsShadows = true;
            sc.selfShadows = false;

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            var shape = new[]
            {
                new Vector3(-hw, -hh, 0f),
                new Vector3(-hw, hh, 0f),
                new Vector3(hw, hh, 0f),
                new Vector3(hw, -hh, 0f),
            };
            int shapeHash = ComputeShapePathHash(shape);

            var so = new SerializedObject(sc);
            so.FindProperty("m_ShadowCastingSource").intValue = 1; // ShadowCastingSources.ShapeEditor
            so.FindProperty("m_CastingOption").intValue = (int)ShadowCaster2D.ShadowCastingOptions.CastShadow;
            so.FindProperty("m_CastsShadows").boolValue = true;
            so.FindProperty("m_SelfShadows").boolValue = false;
            so.FindProperty("m_ShapePathHash").intValue = shapeHash;

            var pathProp = so.FindProperty("m_ShapePath");
            pathProp.arraySize = shape.Length;
            for (int i = 0; i < shape.Length; i++)
                pathProp.GetArrayElementAtIndex(i).vector3Value = shape[i];

            var layersProp = so.FindProperty("m_ApplyToSortingLayers");
            layersProp.arraySize = targetSortingLayerIds.Length;
            for (int i = 0; i < targetSortingLayerIds.Length; i++)
                layersProp.GetArrayElementAtIndex(i).intValue = targetSortingLayerIds[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(sc);
        }

        private static int ComputeShapePathHash(Vector3[] path)
        {
            unchecked
            {
                int hashCode = (int)2166136261;
                for (int i = 0; i < path.Length; i++)
                    hashCode = hashCode * 16777619 ^ path[i].GetHashCode();
                return hashCode;
            }
        }

        private static GameObject CreateDressingSprite(Transform parent, string name, Sprite sprite, Vector3 worldPos, int sortingLayerId, int sortingOrder, Material material, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = material;
            sr.color = color;
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(sr);
            return go;
        }

        private static void CreateVerticalRoomBanner(
            Transform parent,
            string baseName,
            float x,
            float topY,
            Sprite topSprite,
            Sprite tiledSprite,
            Sprite botSprite,
            int sortingLayerId,
            int sortingOrder,
            Material material)
        {
            float y = topY;
            var topGo = CreateDressingSprite(parent, baseName + "_Top", topSprite, new Vector3(x, y - topSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
            topGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
            y -= topSprite.bounds.size.y;
            var midGo = CreateDressingSprite(parent, baseName + "_Mid", tiledSprite, new Vector3(x, y - tiledSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
            midGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
            y -= tiledSprite.bounds.size.y;
            var botGo = CreateDressingSprite(parent, baseName + "_Bot", botSprite, new Vector3(x, y - botSprite.bounds.max.y, 0f), sortingLayerId, sortingOrder, material, Color.white);
            botGo.AddComponent<VertexSwayPin2D>().Configure(topY, 4.0f, true);
        }

        private static void EnsureAmbientDustMotes(Material particlesUnlitMat, int vfxLayerId)
        {
            var existing = GameObject.Find("AmbientDustMotes");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);

            var go = new GameObject("AmbientDustMotes");
            go.transform.position = new Vector3(56f, 5f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.30f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.92f, 0.82f, 0.24f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 3f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(112f, 10f, 0.1f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.92f, 0.82f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.92f, 0.82f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = particlesUnlitMat;
                renderer.sortingLayerID = vfxLayerId;
                renderer.sortingOrder = 12;
                EditorUtility.SetDirty(renderer);
            }

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(ps);
        }

        private static Material LoadMaterialRequired(string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                throw new InvalidOperationException("Missing material asset: " + path);
            return mat;
        }

        private static TileBase LoadTileRequired(string path)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
                throw new InvalidOperationException("Missing Prison tile asset: " + path);
            return tile;
        }

        private static int GetFloorTop(int x)
        {
            foreach (var (xMin, xMax, floorTop) in MainLevelFloorSpans)
            {
                if (x >= xMin && x < xMax)
                    return floorTop;
            }
            return 0;
        }

        private static int CellHash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (int)((h ^ (h >> 16)) & 0x7FFFFFFFu);
            }
        }

        private static Sprite LoadSpriteRequired(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("Missing plank sprite: " + path);
            return sprite;
        }

        private static void SpawnEnemyIfMissing(
            string prefabPath,
            string instanceName,
            Vector3 position,
            Transform parent,
            Transform playerTarget,
            ProjectileSpawner projectiles,
            VfxSpawner vfx)
        {
            if (GameObject.Find(instanceName) != null)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing enemy prefab: " + prefabPath);

            var inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (inst == null)
                throw new InvalidOperationException("Failed to instantiate prefab: " + prefabPath);

            inst.name = instanceName;
            inst.transform.position = position;
            var ctrl = inst.GetComponent<EnemyController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                if (playerTarget != null) so.FindProperty("_player").objectReferenceValue = playerTarget;
                if (projectiles != null) so.FindProperty("_projectiles").objectReferenceValue = projectiles;
                if (vfx != null) so.FindProperty("_vfx").objectReferenceValue = vfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ctrl);
            }
            EditorUtility.SetDirty(inst);
        }

        public static void CaptureRound4GpuVerificationFrames()
        {
            string outDir = Environment.GetEnvironmentVariable("R4_GPU_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(outDir))
                outDir = System.IO.Path.GetFullPath("Logs/r4-gpu-captures");
            System.IO.Directory.CreateDirectory(outDir);

            string[] r4ShaderNames =
            {
                "DeadCells/PaletteSwapLit2D",
                "DeadCells/MobFleshSwapLit2D",
                "DeadCells/ParallaxGradientMap2D",
                "DeadCells/EmissiveHdr2D",
                "DeadCells/AdditiveGodRay2D",
                "DeadCells/VertexSway2D",
                "DeadCells/GroundMist2D",
            };

            int shaderErrors = 0;
            foreach (string shaderName in r4ShaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                {
                    shaderErrors++;
                    Debug.LogError($"[R4_GPU_VERIFY] Shader error or missing: {shaderName}");
                }
            }

            Debug.Log($"[R4_GPU_VERIFY] gfxDevice={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) shaderErrors={shaderErrors}");

            var scene = EditorSceneManager.OpenScene(MainLevelPath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new InvalidOperationException("Failed to open Main_Level.unity for GPU verification.");

            var urpAsset = UniversalRenderPipeline.asset;
            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
                throw new InvalidOperationException("Main Camera missing in Main_Level.unity.");

            var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
            var globalVol = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                .FirstOrDefault(v => v.enabled && v.isGlobal);

            bool hdrOk = urpAsset != null && urpAsset.supportsHDR;
            bool camPostOk = uacd != null && uacd.renderPostProcessing;
            bool volMaskOk = uacd != null && globalVol != null && (uacd.volumeLayerMask & (1 << globalVol.gameObject.layer)) != 0;
            bool profileOk = globalVol != null && globalVol.sharedProfile != null &&
                             globalVol.sharedProfile.TryGet<Bloom>(out var b) && b.intensity.overrideState &&
                             globalVol.sharedProfile.TryGet<Vignette>(out var v) && v.intensity.overrideState &&
                             globalVol.sharedProfile.TryGet<Tonemapping>(out var tm) && tm.mode.overrideState &&
                             globalVol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.contrast.overrideState && ca.saturation.overrideState &&
                             globalVol.sharedProfile.TryGet<SplitToning>(out var st) && st.shadows.overrideState && st.highlights.overrideState;

            Debug.Log($"[R4_GPU_VERIFY] URP_PreFlight urpActive={urpAsset != null} hdr={hdrOk} camPost={camPostOk} volMask={volMaskOk} profileAll5={profileOk}");

            int defaultMatCount = UnityEngine.Object.FindObjectsByType<Renderer>()
                .Count(r => r.sharedMaterial != null && r.sharedMaterial.name == "Default-Material");
            Debug.Log($"[R4_GPU_VERIFY] PaletteScan defaultMaterialRenderers={defaultMatCount}");

            // Force Light2D meshes/boundingSpheres & ShadowCaster2D shapes to build in EditMode batchmode
            ForceInitializeLightsAndShadowsForCapture();

            // Force synchronous compile & render on GPU
            var parallaxLayers = UnityEngine.Object.FindObjectsByType<ParallaxLayer2D>(FindObjectsInactive.Include);
            var origParallaxPositions = parallaxLayers.Select(l => l != null ? l.transform.position : Vector3.zero).ToArray();
            var origPos = cam.transform.position;
            float origOrtho = cam.orthographicSize;
            var origTarget = cam.targetTexture;

            const int width = 1280;
            const int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            rt.antiAliasing = 1;
            rt.Create();
            var readTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, linear: true);
            var outTex = new Texture2D(width, height, TextureFormat.RGB24, false, linear: false);

            try
            {
                cam.targetTexture = rt;

                // Discard 1 warm-up frame per visual-scorecard.md Step 2
                cam.transform.position = new Vector3(18.5f, 3.8f, -10f);
                cam.orthographicSize = 5.5f;
                UpdateParallaxLayersForCapture(parallaxLayers, cam.transform.position);
                cam.Render();

                string filePrefix = Environment.GetEnvironmentVariable("R4_GPU_CAPTURE_PREFIX");
                if (string.IsNullOrWhiteSpace(filePrefix))
                    filePrefix = "gpu_r4";
                filePrefix = filePrefix.Trim();

                var shots = new (string file, Vector3 pos, float orthoSize)[]
                {
                    ($"{filePrefix}_01_corridor_west.png", new Vector3(18.5f, 3.8f, -10f), 5.5f),
                    ($"{filePrefix}_02_player_lantern_closeup.png", new Vector3(9.5f, 2.2f, -10f), 3.4f),
                    ($"{filePrefix}_03_east_arch_parallax.png", new Vector3(66.0f, 4.8f, -10f), 6.0f),
                    ($"{filePrefix}_04_exit_hall.png", new Vector3(103.5f, 3.6f, -10f), 5.5f),
                };

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.orthographicSize = shot.orthoSize;
                    UpdateParallaxLayersForCapture(parallaxLayers, shot.pos);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    readTex.Apply(false, false);
                    RenderTexture.active = null;

                    // Convert linear URP render output to exact display sRGB matching Game View
                    Color[] linearPixels = readTex.GetPixels();
                    var srgbPixels = new Color32[linearPixels.Length];
                    double sumLum = 0.0;
                    int clipCount = 0;
                    for (int i = 0; i < linearPixels.Length; i++)
                    {
                        Color lin = linearPixels[i];
                        byte cr = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.r))) * 255f);
                        byte cg = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.g))) * 255f);
                        byte cb = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(Mathf.Clamp01(lin.b))) * 255f);
                        srgbPixels[i] = new Color32(cr, cg, cb, 255);
                        sumLum += 0.2126 * cr + 0.7152 * cg + 0.0722 * cb;
                        if (cr >= 240 || cg >= 240 || cb >= 240)
                            clipCount++;
                    }
                    outTex.SetPixels32(srgbPixels);
                    outTex.Apply(false, false);

                    double meanLum = sumLum / Math.Max(1, linearPixels.Length);
                    double clipPct = (clipCount * 100.0) / Math.Max(1, linearPixels.Length);
                    string path = System.IO.Path.Combine(outDir, shot.file);
                    System.IO.File.WriteAllBytes(path, outTex.EncodeToPNG());
                    Debug.Log($"[R4_GPU_VERIFY] Captured {shot.file} ({width}x{height}) at cam={shot.pos} ortho={shot.orthoSize} meanLum={meanLum:F2} clip>=240={clipPct:F3}%");
                }
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = origTarget;
                cam.transform.position = origPos;
                cam.orthographicSize = origOrtho;
                for (int i = 0; i < parallaxLayers.Length; i++)
                {
                    if (parallaxLayers[i] != null)
                        parallaxLayers[i].transform.position = origParallaxPositions[i];
                }
                UnityEngine.Object.DestroyImmediate(outTex);
                UnityEngine.Object.DestroyImmediate(readTex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        private static void UpdateParallaxLayersForCapture(ParallaxLayer2D[] layers, Vector3 camPos)
        {
            foreach (var layer in layers)
            {
                if (layer != null && layer.isActiveAndEnabled)
                    layer.UpdateParallax(camPos);
            }
        }

        private static void ForceInitializeLightsAndShadowsForCapture()
        {
            const System.Reflection.BindingFlags NonPublicInstance =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var updateMeshMethod = typeof(Light2D).GetMethod("UpdateMesh", NonPublicInstance);
            var updateBoundsMethod = typeof(Light2D).GetMethod("UpdateBoundingSphere", NonPublicInstance);

            int lightsInit = 0;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>())
            {
                if (light == null || !light.isActiveAndEnabled)
                    continue;
                if (light.lightType != Light2D.LightType.Global)
                {
                    updateMeshMethod?.Invoke(light, new object[] { true });
                    updateBoundsMethod?.Invoke(light, null);
                }
                lightsInit++;
            }

            int shadowsInit = 0;
            foreach (var sc in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>())
            {
                if (sc == null || !sc.isActiveAndEnabled)
                    continue;
                sc.Update();
                shadowsInit++;
            }

            Debug.Log($"[R4_GPU_VERIFY] Initialized lights={lightsInit} shadowCasters={shadowsInit} before GPU render.");
        }
    }
}
