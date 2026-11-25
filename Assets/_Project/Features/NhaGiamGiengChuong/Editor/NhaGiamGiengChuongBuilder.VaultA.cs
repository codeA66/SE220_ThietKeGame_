using System;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.Tools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DeadCells.NhaGiamGiengChuong.Editor
{
    public static partial class NhaGiamGiengChuongBuilder
    {
        private const string VaultASpriteFolder = "Assets/_Project/Art/Environment/Prison/Sprites";
        private const string VaultAAudioPath = FeatureRoot + "/Audio/RoomAmbient_VaultA.wav";
        private const string VaultAInscriptionTitle = "Bức Họa Không Đề Tên";
        private const string VaultAInscriptionBody =
            "Giữa vương quốc đổ nát và muôn vàn vòng lặp u tối, vẫn còn một khoảng nắng chẳng bao giờ tắt.\n" +
            "Nơi lưu giữ hình bóng của một tình yêu thuần khiết và vĩnh hằng.";

        private static readonly Rect MainMapBounds = new Rect(-104f, -20f, 340f, 86f);
        private static readonly Rect VaultABounds = new Rect(176f, 27f, 28f, 24f);

        private static void ConfigureVaultASpriteImporters()
        {
            ConfigureSingleSpriteImporter($"{VaultASpriteFolder}/WallTapestry_04.png", 48f);
            string[] props24 =
            {
                "VaultA_SunArchWindow.png",
                "VaultA_SunbeamShaft.png",
                "VaultA_SandstoneColumn.png",
                "VaultA_RoyalBanner.png",
                "VaultA_Candelabra.png",
                "VaultA_HangingIvy.png",
                "VaultA_RoyalCarpet.png",
                "VaultA_MossFlowers.png",
                "VaultA_StonePlinth.png",
                "VaultA_CrackedWallPatch.png",
                "VaultA_DarkFissure.png",
            };
            foreach (string fileName in props24)
                ConfigureSingleSpriteImporter($"{VaultASpriteFolder}/{fileName}", 24f);
        }

        private static void ConfigureSingleSpriteImporter(string assetPath, float ppu)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing texture asset for VaultA: {assetPath}");

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f)
            {
                importer.spritePixelsPerUnit = ppu;
                dirty = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }

        private static Sprite LoadVaultASprite(string fileName)
        {
            string path = $"{VaultASpriteFolder}/{fileName}";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null)
                throw new InvalidOperationException($"Missing VaultA sprite at: {path}");
            return sp;
        }

        private static void BuildRoomVaultA(GameObject root, Material mat, Material unlitMat, Material particlesUnlit, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Sealed sunlit royal sanctuary: X=[176..204] (28u), Y=[29..49] (20u), strictly inside A06 envelope
            AddSolidBox(colRoot, "Floor_VaultA", 176f, 204f, 28f, 29f);
            AddSolidBox(colRoot, "Ceiling_VaultA", 175.5f, 204.5f, 49f, 49.5f);
            AddSolidBox(colRoot, "Wall_VaultA_West", 175.5f, 176f, 28f, 49f);
            AddSolidBox(colRoot, "Wall_VaultA_East", 204f, 204.5f, 28f, 49f);

            // Dedicated VaultVisualGrid (distinct names from VisualGrid/TM_Background/TM_SolidVisual to preserve A08/A10/A20 == 9 counts)
            var gridGo = CreateChild(artRoot, "VaultVisualGrid");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");

            var warmSandstoneWall = new Color(1.00f, 0.90f, 0.72f, 1f);
            var warmSandstoneSolid = new Color(0.98f, 0.88f, 0.70f, 1f);
            var bgTm = CreateVisualTilemap(gridGo, "TM_VaultBack", mat, bgLayerId, -20, warmSandstoneWall);
            var decalTm = CreateVisualTilemap(gridGo, "TM_VaultDecals", mat, bgLayerId, -18, new Color(0.95f, 0.88f, 0.74f, 1f));
            var solidTm = CreateVisualTilemap(gridGo, "TM_VaultSolid", mat, groundLayerId, -5, warmSandstoneSolid);
            var rimTm = CreateVisualTilemap(gridGo, "TM_VaultRim", mat, groundLayerId, -3, warmSandstoneSolid);

            PaintPrisonBackWall(bgTm, decalTm, pal, 176f, 204f, 29f, 49f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 176f, 204f, 28f, 29f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 176f, 204f, 49f, 49.5f);
            PaintPrisonOuterWall(solidTm, pal, 175.5f, 176f, 28f, 49.5f);
            PaintPrisonOuterWall(solidTm, pal, 204f, 204.5f, 28f, 49.5f);
            PaintVisualDirtSkirt(solidTm, pal, 172, 208, 25, 28);
            PaintVisualDirtSkirt(solidTm, pal, 172, 208, 49, 52);
            PaintVisualDirtSkirt(solidTm, pal, 172, 176, 28, 50);
            PaintVisualDirtSkirt(solidTm, pal, 204, 208, 28, 50);

            // 1. High Gothic Sunlit Arched Skylight & Diagonal Golden Godrays
            PlaceVaultASprite(artRoot, "VaultA_Skylight", LoadVaultASprite("VaultA_SunArchWindow.png"), unlitMat, bgLayerId, -16, new Vector3(188.8f, 44.6f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_GodrayMain", LoadVaultASprite("VaultA_SunbeamShaft.png"), unlitMat, vfxLayerId, 2, new Vector3(190.4f, 39.2f, 0f), new Color(1f, 0.94f, 0.78f, 0.78f), new Vector3(1.45f, 1.35f, 1f));
            PlaceVaultASprite(artRoot, "VaultA_GodrayAccent", LoadVaultASprite("VaultA_SunbeamShaft.png"), unlitMat, vfxLayerId, 2, new Vector3(192.6f, 37.8f, 0f), new Color(1f, 0.92f, 0.74f, 0.50f), new Vector3(1.10f, 1.25f, 1f));

            // 2. Flanking Weathered Sandstone Columns (10u tall, bottom at floor y=29 -> center y=34)
            var colSprite = LoadVaultASprite("VaultA_SandstoneColumn.png");
            PlaceVaultASprite(artRoot, "VaultA_Column_West", colSprite, mat, bgLayerId, -14, new Vector3(185.4f, 34.0f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Column_East", colSprite, mat, bgLayerId, -14, new Vector3(197.6f, 34.0f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Column_Entry", colSprite, mat, bgLayerId, -14, new Vector3(177.2f, 34.0f, 0f), new Color(0.92f, 0.88f, 0.80f, 1f));

            // 3. Flanking Tattered Crimson-and-Gold Royal Banners echoing the tapestry
            var bannerSprite = LoadVaultASprite("VaultA_RoyalBanner.png");
            PlaceVaultASprite(artRoot, "VaultA_Banner_West", bannerSprite, mat, bgLayerId, -13, new Vector3(185.8f, 38.2f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Banner_East", bannerSprite, mat, bgLayerId, -13, new Vector3(197.2f, 38.2f, 0f), Color.white);

            // 4. Centerpiece Royal Tapestry (238x320 @ 48 PPU = 4.96u x 6.67u, centered at (191.5, 37.2))
            PlaceVaultASprite(artRoot, "WallTapestry_04", LoadVaultASprite("WallTapestry_04.png"), mat, bgLayerId, -12, new Vector3(191.5f, 37.2f, 0f), Color.white);

            // 5. Draped Sunlit Ivy Vines connecting upper stonework into tapestry moss border
            var ivySprite = LoadVaultASprite("VaultA_HangingIvy.png");
            PlaceVaultASprite(artRoot, "VaultA_Ivy_TopL", ivySprite, mat, bgLayerId, -11, new Vector3(188.6f, 41.2f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Ivy_TopR", ivySprite, mat, bgLayerId, -11, new Vector3(194.4f, 41.6f, 0f), Color.white, new Vector3(-1f, 1f, 1f));
            PlaceVaultASprite(artRoot, "VaultA_Ivy_CeilW", ivySprite, mat, bgLayerId, -11, new Vector3(181.5f, 47.0f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Ivy_CeilE", ivySprite, mat, bgLayerId, -11, new Vector3(200.5f, 47.0f, 0f), Color.white, new Vector3(-1f, 1f, 1f));

            // 6. Twin Antique Brass Candelabras (4u tall, grounded y=29 -> center y=31)
            var candSprite = LoadVaultASprite("VaultA_Candelabra.png");
            PlaceVaultASprite(artRoot, "VaultA_Candelabra_W", candSprite, mat, bgLayerId, -10, new Vector3(187.8f, 31.0f, 0f), Color.white);
            PlaceVaultASprite(artRoot, "VaultA_Candelabra_E", candSprite, mat, bgLayerId, -10, new Vector3(195.2f, 31.0f, 0f), Color.white);

            // 7. Frayed Crimson Velvet Carpet along floor (0.67u tall, resting on floor y=29)
            var carpetSprite = LoadVaultASprite("VaultA_RoyalCarpet.png");
            for (float cx = 182.0f; cx <= 198.0f; cx += 4.0f)
                PlaceVaultASprite(artRoot, $"VaultA_Carpet_{cx:F0}", carpetSprite, mat, groundLayerId, -2, new Vector3(cx, 29.15f, 0f), Color.white);

            // 8. Sunlit Moss & Wildflower Clusters along cracked floor
            var mossSprite = LoadVaultASprite("VaultA_MossFlowers.png");
            foreach (float mx in new[] { 181.2f, 184.5f, 189.2f, 193.8f, 196.4f, 202.8f })
                PlaceVaultASprite(artRoot, $"VaultA_Flora_{mx:F1}", mossSprite, mat, groundLayerId, -1, new Vector3(mx, 29.38f, 0f), Color.white);

            // 9. Mossy Sandstone Plinths beneath the 2 bonus scrolls (grounded at y=29)
            var plinthSprite = LoadVaultASprite("VaultA_StonePlinth.png");
            PlaceVaultASprite(artRoot, "VaultA_Plinth_1", plinthSprite, mat, bgLayerId, -9, new Vector3(198.5f, 29.55f, 0f), Color.white, new Vector3(0.85f, 0.70f, 1f));
            PlaceVaultASprite(artRoot, "VaultA_Plinth_2", plinthSprite, mat, bgLayerId, -9, new Vector3(201.2f, 29.55f, 0f), Color.white, new Vector3(0.85f, 0.70f, 1f));

            // 10. Return Fissure Visual inside VaultA west wall (x=178.5, grounded y=29 -> center y=30.33)
            PlaceVaultASprite(artRoot, "VaultA_ReturnFissure", LoadVaultASprite("VaultA_DarkFissure.png"), mat, bgLayerId, -9, new Vector3(178.5f, 30.33f, 0f), Color.white);

            // 11. Warm Sunlit Sanctuary Lighting (isolated inside Room_VaultA; does not touch Global Light 2D)
            AddVaultAPointLight(artRoot, "VaultA_SunFill_Center", new Vector3(191.5f, 38.5f, 0f), new Color(1.00f, 0.93f, 0.78f, 1f), 1.35f, 4.5f, 18.0f, 0.45f, flicker: false, seed: 0f);
            AddVaultAPointLight(artRoot, "VaultA_SunFill_West", new Vector3(181.5f, 34.5f, 0f), new Color(1.00f, 0.92f, 0.76f, 1f), 0.95f, 2.5f, 13.0f, 0.50f, flicker: false, seed: 0f);
            AddVaultAPointLight(artRoot, "VaultA_SunFill_East", new Vector3(199.8f, 33.5f, 0f), new Color(1.00f, 0.91f, 0.74f, 1f), 0.95f, 2.5f, 12.0f, 0.50f, flicker: false, seed: 0f);
            AddVaultAPointLight(artRoot, "VaultA_CandleLight_W", new Vector3(187.8f, 33.0f, 0f), new Color(1.00f, 0.80f, 0.44f, 1f), 1.10f, 0.8f, 6.5f, 0.62f, flicker: true, seed: 1.7f);
            AddVaultAPointLight(artRoot, "VaultA_CandleLight_E", new Vector3(195.2f, 33.0f, 0f), new Color(1.00f, 0.80f, 0.44f, 1f), 1.10f, 0.8f, 6.5f, 0.62f, flicker: true, seed: 4.3f);

            // 12. Drifting Golden Sunbeam Dust Motes & Falling Petals
            CreateVaultAAmbientParticles(artRoot, "VaultA_SunMotes", particlesUnlit, vfxLayerId, new Vector3(191.5f, 38.5f, 0f), new Vector3(18f, 12f, 0.1f), new Color(1f, 0.93f, 0.70f, 0.55f), 0.09f, 12f);
            CreateVaultAAmbientParticles(artRoot, "VaultA_Petals", particlesUnlit, vfxLayerId, new Vector3(191.0f, 44.5f, 0f), new Vector3(14f, 3f, 0.1f), new Color(0.94f, 0.68f, 0.68f, 0.65f), 0.13f, 4.5f);
        }

        private static void BuildVaultAInScene(
            GameObject roomsRoot,
            GameObject interactionsRoot,
            LevelRunController runController,
            PlayerController playerCtrl,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions,
            CameraFollow2D camFollow,
            GameObject playerHudGo,
            Material spriteLit,
            Material spriteUnlit,
            Material particlesUnlit)
        {
            // 1. Instantiate Room_VaultA prefab under Rooms and keep inactive at rest (zero A06/GPU cost until entered)
            var vaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoomsFolder}/VaultA.prefab");
            if (vaultPrefab == null)
                throw new InvalidOperationException($"Missing VaultA prefab at {RoomsFolder}/VaultA.prefab");
            var vaultRoomGo = (GameObject)PrefabUtility.InstantiatePrefab(vaultPrefab, roomsRoot.transform);
            vaultRoomGo.name = "Room_VaultA";
            vaultRoomGo.transform.position = Vector3.zero;
            vaultRoomGo.SetActive(false);

            var parallaxRoot = GameObject.Find("Prison_Parallax_R5");

            // 2. Full-screen ScreenFadeOverlay + Letterbox Canvas (sortingOrder 85, strictly below PauseMenu 90)
            var fader = CreateVaultAScreenFadeOverlay();

            // 3. Dedicated Sanctuary Felt-Piano AudioSource
            var chamberAudioGo = CreateChild(interactionsRoot, "VaultA_ChamberAudio");
            var chamberAudio = chamberAudioGo.AddComponent<AudioSource>();
            chamberAudio.playOnAwake = false;
            chamberAudio.loop = true;
            chamberAudio.spatialBlend = 0f;
            chamberAudio.volume = 0f;
            var pianoClip = AssetDatabase.LoadAssetAtPath<AudioClip>(VaultAAudioPath);
            if (pianoClip == null)
                throw new InvalidOperationException("Missing VaultA piano clip at: " + VaultAAudioPath);
            chamberAudio.clip = pianoClip;

            // 4. I24_VaultA_Inscription (under tapestry center, unlocked only after framing sequence)
            var inscription = CreateVaultAInscription(interactionsRoot, runController, playerCtrl, playerHealth, playerCol, inputActions);

            // 5. VaultA_FramingSequence (one-shot trigger at x=183.5 -> slow walk to 190.0 + camera pan/zoom to (191.5, 37.2))
            var gameplayCanvas = playerHudGo.GetComponent<Canvas>() ?? playerHudGo.GetComponentInChildren<Canvas>(true);
            var scrollPres = UnityEngine.Object.FindAnyObjectByType<ScrollPickupPresentation>(FindObjectsInactive.Include);
            GameObject statsStrip = scrollPres != null
                ? new SerializedObject(scrollPres).FindProperty("_statsStrip")?.objectReferenceValue as GameObject
                : null;
            var framingSeq = CreateVaultAFramingSequence(
                interactionsRoot, runController, playerCtrl, playerHealth, playerCol, camFollow, fader, chamberAudio, gameplayCanvas, statsStrip, inscription);

            // 6. Entry & Exit PassageTransitions
            var entryPassage = CreateVaultAPassage(
                interactionsRoot,
                "I22_VaultA_Entry",
                new Vector2(0.75f, 0f),
                new Vector2(1.8f, 2.4f),
                "[E] Vào",
                runController,
                playerCtrl,
                playerHealth,
                playerCol,
                inputActions,
                fader,
                camFollow,
                destination: new Vector2(180f, 29f),
                arrivalFacing: 1,
                targetBounds: VaultABounds,
                isEnteringChamber: true,
                chamberRoot: vaultRoomGo,
                dungeonParallaxRoot: parallaxRoot,
                chamberAudio: chamberAudio,
                framingSequence: framingSeq,
                startsUnlocked: false);

            CreateVaultAPassage(
                interactionsRoot,
                "I23_VaultA_Exit",
                new Vector2(178.5f, 29f),
                new Vector2(2.2f, 2.4f),
                "[E] Quay lại",
                runController,
                playerCtrl,
                playerHealth,
                playerCol,
                inputActions,
                fader,
                camFollow,
                destination: new Vector2(1.2f, 0f),
                arrivalFacing: 1,
                targetBounds: MainMapBounds,
                isEnteringChamber: false,
                chamberRoot: vaultRoomGo,
                dungeonParallaxRoot: parallaxRoot,
                chamberAudio: chamberAudio,
                framingSequence: framingSeq,
                startsUnlocked: true);

            // 7. I22_WallA_Masonry on Room A West Wall (x=0, behind spawn & Art_A_Bed)
            CreateRoomACrackedMasonry(interactionsRoot, entryPassage, spriteLit, particlesUnlit);
        }

        private static ScreenFadeOverlay CreateVaultAScreenFadeOverlay()
        {
            var root = new GameObject("VaultA_ScreenFadeOverlay");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 85;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var fadeGo = new GameObject("FadeCurtain", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            fadeGo.transform.SetParent(root.transform, false);
            var fadeRect = fadeGo.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            var fadeImg = fadeGo.GetComponent<Image>();
            fadeImg.color = Color.black;
            fadeImg.raycastTarget = false;
            var fadeGroup = fadeGo.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            RectTransform CreateBar(string name, bool top)
            {
                var barGo = new GameObject(name, typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(root.transform, false);
                var rect = barGo.GetComponent<RectTransform>();
                rect.anchorMin = top ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
                rect.anchorMax = top ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
                rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 0f);
                var img = barGo.GetComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = false;
                barGo.SetActive(false);
                return rect;
            }

            var topBar = CreateBar("LetterboxTop", true);
            var bottomBar = CreateBar("LetterboxBottom", false);

            var overlay = root.AddComponent<ScreenFadeOverlay>();
            var so = new SerializedObject(overlay);
            so.FindProperty("_fadeGroup").objectReferenceValue = fadeGroup;
            so.FindProperty("_topBar").objectReferenceValue = topBar;
            so.FindProperty("_bottomBar").objectReferenceValue = bottomBar;
            so.FindProperty("_barMaxHeight").floatValue = 96f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
        }

        private static WallInscription CreateVaultAInscription(
            GameObject parent,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions)
        {
            var root = CreateChild(parent, "I24_VaultA_Inscription");
            root.transform.position = new Vector3(191.5f, 29f, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 1.2f);
            trigger.size = new Vector2(3.5f, 2.4f);

            var promptGo = CreateChild(root, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, "[E] Quan sát", new Color(1f, 0.90f, 0.56f, 1f), 260f);
            promptGo.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            promptGo.SetActive(false);

            var bannerGo = CreateChild(root, "InscriptionBanner");
            var bannerText = BuildWorldLoreBanner(bannerGo, $"{VaultAInscriptionTitle}\n{VaultAInscriptionBody}");
            bannerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 165f);
            bannerGo.transform.localPosition = new Vector3(0f, 3.65f, 0f);
            bannerGo.SetActive(false);

            var ins = root.AddComponent<WallInscription>();
            var so = new SerializedObject(ins);
            so.FindProperty("_title").stringValue = VaultAInscriptionTitle;
            so.FindProperty("_body").stringValue = VaultAInscriptionBody;
            so.FindProperty("_startsUnlocked").boolValue = false;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_triggerZone").objectReferenceValue = trigger;
            so.FindProperty("_inputActions").objectReferenceValue = inputActions;
            so.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            so.FindProperty("_bannerRoot").objectReferenceValue = bannerGo;
            so.FindProperty("_bannerText").objectReferenceValue = bannerText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ins;
        }

        private static ChamberFramingSequence CreateVaultAFramingSequence(
            GameObject parent,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            CameraFollow2D camFollow,
            ScreenFadeOverlay fader,
            AudioSource chamberAudio,
            Canvas gameplayHud,
            GameObject statsStrip,
            WallInscription inscription)
        {
            var root = CreateChild(parent, "VaultA_FramingSequence");
            root.transform.position = new Vector3(183.85f, 29f, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 9.75f);
            trigger.size = new Vector2(1.3f, 19.5f);

            var seq = root.AddComponent<ChamberFramingSequence>();
            var so = new SerializedObject(seq);
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_triggerZone").objectReferenceValue = trigger;
            so.FindProperty("_cameraFollow").objectReferenceValue = camFollow;
            so.FindProperty("_fader").objectReferenceValue = fader;
            so.FindProperty("_chamberAudio").objectReferenceValue = chamberAudio;
            so.FindProperty("_targetAudioVolume").floatValue = 0.58f;
            so.FindProperty("_gameplayHud").objectReferenceValue = gameplayHud;
            so.FindProperty("_statsStrip").objectReferenceValue = statsStrip;
            so.FindProperty("_inscription").objectReferenceValue = inscription;
            so.FindProperty("_walkTargetX").floatValue = 190.0f;
            so.FindProperty("_walkSpeedMultiplier").floatValue = 0.38f;
            so.FindProperty("_cameraFocusPoint").vector2Value = new Vector2(191.5f, 37.2f);
            so.FindProperty("_focusSmoothTime").floatValue = 1.5f;
            so.FindProperty("_focusOrthoSize").floatValue = 5.2f;
            so.FindProperty("_holdDuration").floatValue = 3.2f;
            so.FindProperty("_returnSmoothTime").floatValue = 0.9f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return seq;
        }

        private static PassageTransition CreateVaultAPassage(
            GameObject parent,
            string name,
            Vector2 floorPos,
            Vector2 triggerSize,
            string promptLabel,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions,
            ScreenFadeOverlay fader,
            CameraFollow2D camFollow,
            Vector2 destination,
            int arrivalFacing,
            Rect targetBounds,
            bool isEnteringChamber,
            GameObject chamberRoot,
            GameObject dungeonParallaxRoot,
            AudioSource chamberAudio,
            ChamberFramingSequence framingSequence,
            bool startsUnlocked)
        {
            var root = CreateChild(parent, name);
            root.transform.position = new Vector3(floorPos.x, floorPos.y, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, triggerSize.y * 0.5f);
            trigger.size = triggerSize;

            var promptGo = CreateChild(root, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, promptLabel, new Color(0.98f, 0.88f, 0.52f, 1f), 220f);
            promptGo.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            promptGo.SetActive(false);

            var passage = root.AddComponent<PassageTransition>();
            var so = new SerializedObject(passage);
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_triggerZone").objectReferenceValue = trigger;
            so.FindProperty("_inputActions").objectReferenceValue = inputActions;
            so.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            so.FindProperty("_fader").objectReferenceValue = fader;
            so.FindProperty("_cameraFollow").objectReferenceValue = camFollow;
            so.FindProperty("_destination").vector2Value = destination;
            so.FindProperty("_arrivalFacing").intValue = arrivalFacing;
            so.FindProperty("_targetCameraBounds").rectValue = targetBounds;
            so.FindProperty("_isEnteringChamber").boolValue = isEnteringChamber;
            so.FindProperty("_chamberRoot").objectReferenceValue = chamberRoot;
            so.FindProperty("_dungeonParallaxRoot").objectReferenceValue = dungeonParallaxRoot;
            so.FindProperty("_chamberAudio").objectReferenceValue = chamberAudio;
            so.FindProperty("_chamberAudioVolume").floatValue = 0.58f;
            so.FindProperty("_framingSequence").objectReferenceValue = framingSequence;
            so.FindProperty("_startsUnlocked").boolValue = startsUnlocked;
            so.FindProperty("_fadeDuration").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return passage;
        }

        private static void CreateRoomACrackedMasonry(
            GameObject parent,
            PassageTransition entryPassage,
            Material spriteLit,
            Material particlesUnlit)
        {
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");

            var root = CreateChild(parent, "I22_WallA_Masonry");
            root.transform.position = new Vector3(0.25f, 0f, 0f);

            var health = root.AddComponent<Health>();
            var soH = new SerializedObject(health);
            soH.FindProperty("_maxHealth").intValue = 1;
            soH.FindProperty("_invulnerabilityAfterHit").floatValue = 0f;
            soH.ApplyModifiedPropertiesWithoutUndo();

            var hurtboxGo = CreateChild(root, "Hurtbox");
            hurtboxGo.layer = 8; // Enemy layer — hit cleanly by PlayerCombat & PlayerBow without touching Player.prefab
            hurtboxGo.transform.localPosition = new Vector3(0.05f, 1.1f, 0f);
            var hurtCol = hurtboxGo.AddComponent<BoxCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.size = new Vector2(0.70f, 2.2f);
            var hurtbox = hurtboxGo.AddComponent<Hurtbox>();
            var soHurt = new SerializedObject(hurtbox);
            soHurt.FindProperty("_health").objectReferenceValue = health;
            soHurt.ApplyModifiedPropertiesWithoutUndo();

            var intactGo = PlaceVaultASprite(
                root,
                "IntactMasonry",
                LoadVaultASprite("VaultA_CrackedWallPatch.png"),
                spriteLit,
                bgLayerId,
                -10,
                new Vector3(0.45f, 1.25f, 0f),
                new Color(0.85f, 0.88f, 0.94f, 0.85f));

            var fissureGo = PlaceVaultASprite(
                root,
                "WallFissure",
                LoadVaultASprite("VaultA_DarkFissure.png"),
                spriteLit,
                bgLayerId,
                -9,
                new Vector3(0.65f, 1.33f, 0f),
                Color.white);
            fissureGo.SetActive(false);

            var psGo = CreateChild(root, "BreakDebris");
            psGo.transform.position = new Vector3(0.45f, 1.1f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.gravityModifier = 1.6f;
            main.startColor = new Color(0.32f, 0.37f, 0.46f, 1f);
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.4f, 1.8f, 0.1f);
            var psRend = psGo.GetComponent<ParticleSystemRenderer>();
            psRend.sharedMaterial = particlesUnlit;
            psRend.sortingLayerID = vfxLayerId;
            psRend.sortingOrder = 10;

            var masonry = root.AddComponent<CrackedMasonry>();
            var soM = new SerializedObject(masonry);
            soM.FindProperty("_health").objectReferenceValue = health;
            soM.FindProperty("_hurtboxCollider").objectReferenceValue = hurtCol;
            soM.FindProperty("_intactVisual").objectReferenceValue = intactGo;
            soM.FindProperty("_fissureVisual").objectReferenceValue = fissureGo;
            soM.FindProperty("_entryPassage").objectReferenceValue = entryPassage;
            soM.FindProperty("_breakParticles").objectReferenceValue = ps;
            soM.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject PlaceVaultASprite(
            GameObject parent,
            string name,
            Sprite sprite,
            Material material,
            int sortingLayerId,
            int sortingOrder,
            Vector3 worldPos,
            Color color,
            Vector3? scale = null)
        {
            var go = CreateChild(parent, name);
            go.transform.position = worldPos;
            if (scale.HasValue)
                go.transform.localScale = scale.Value;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            return go;
        }

        private static void AddVaultAPointLight(
            GameObject parent,
            string name,
            Vector3 pos,
            Color color,
            float intensity,
            float innerRadius,
            float outerRadius,
            float falloff,
            bool flicker,
            float seed)
        {
            var go = CreateChild(parent, name);
            go.transform.position = pos;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.falloffIntensity = falloff;
            ConfigureGameplayPointLight(light, enableVolumetric: false, volumeIntensity: 0f, addFlicker: flicker, phaseSeed: seed);
        }

        private static void CreateVaultAAmbientParticles(
            GameObject parent,
            string name,
            Material particlesUnlit,
            int vfxLayerId,
            Vector3 pos,
            Vector3 boxSize,
            Color color,
            float size,
            float rate)
        {
            var go = CreateChild(parent, name);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.0f, 7.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.2f);
            main.gravityModifier = 0.03f;
            main.startColor = color;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.08f);
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = particlesUnlit;
            rend.sortingLayerID = vfxLayerId;
            rend.sortingOrder = 5;
        }
    }
}
