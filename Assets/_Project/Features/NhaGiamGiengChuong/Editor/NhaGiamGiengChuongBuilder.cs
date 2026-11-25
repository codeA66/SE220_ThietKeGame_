using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.VFX;
using DeadCells.Tools.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadCells.NhaGiamGiengChuong.Editor
{
    /// <summary>
    /// Deterministic Editor builder for Nha Giam Gieng Chuong v8 (Dead Cells Prison 24 PPU Visual Standard):
    /// builds Rooms/{CD,AB,G,HJK,ELF}.prefab, UI/ResultPanel.prefab,
    /// Assets/_Project/Scenes/NhaGiamGiengChuong.unity, and registers the scene in EditorBuildSettings.
    /// </summary>
    public static partial class NhaGiamGiengChuongBuilder
    {
        public const string FeatureRoot = "Assets/_Project/Features/NhaGiamGiengChuong";
        public const string RoomsFolder = FeatureRoot + "/Rooms";
        public const string UiFolder = FeatureRoot + "/UI";
        public const string ScenePath = "Assets/_Project/Scenes/NhaGiamGiengChuong.unity";
        public const string ResultPanelPrefabPath = UiFolder + "/ResultPanel.prefab";

        private const string MainLevelScenePath = "Assets/_Project/Scenes/Main_Level.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Features/Player/Prefabs/Player.prefab";
        private const string ZeroFrictionPhysicsMaterialPath = "Assets/_Project/Features/Player/Data/Player_NoFriction.physicsMaterial2D";
        private const string PlayerHudPrefabPath = "Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab";
        private const string ZombiePrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Zombie.prefab";
        private const string RatPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Rat.prefab";
        private const string ShieldPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab";
        private const string ArcherPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab";
        private const string GrenadierPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Grenadier.prefab";
        private const string ArrowPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Projectile_Arrow.prefab";
        private const string GrenadePrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Projectile_Grenade.prefab";
        private const string GoldPickupPrefabPath = "Assets/_Project/Features/Currency/Prefabs/GoldPickup.prefab";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
        private const string SpriteLitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string SpriteUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string ParticlesUnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        private const string SharedPostProcessProfilePath = "Assets/_Project/Settings/DeadCells_PostProcessProfile.asset";
        private const string TilesRoot = "Assets/_Project/Art/Environment/GothicDungeon/Tiles";
        private const string PrisonTilesRoot = "Assets/_Project/Art/Environment/Prison/Tiles";
        private const string PrisonSpritesRoot = "Assets/_Project/Art/Environment/Prison/Sprites";

        private const int LayerGround = 6;
        private const int LayerOneWay = 9;
        private const int LayerClimbable = 10;

        private sealed class PrisonPalette
        {
            public Tile Dirt;
            public Tile[] BackWalls;
            public Tile[] StampWalls;
            public Tile[] WallJunks;
            public Tile[] FloorStamps;
            public Tile[] Grounds;
            public Tile[] Ceilings;
            public Tile Stairs0;
            public Tile Stairs1;
            public Tile WebSmallTL;
            public Tile WebSmallTR;
            public Tile WebSmallBL;
            public Tile WebSmallBR;
            public Sprite DirtSprite;
            public Sprite WoodenPlatformLeft;
            public Sprite[] WoodenPlatformMid;
            public Sprite WoodenPlatformRight;
            public Sprite WoodenCornerLeft;
            public Sprite WoodenCornerRight;
            public Sprite LadderHole;
            public Sprite ChainTile;
            public Sprite ChainDown;
            public Sprite LittleBeamTL;
            public Sprite LittleBeamTR;
            public Sprite BigBeamTL;
            public Sprite BigBeamTR;
            public Sprite StampCeilingSmall;
            public Sprite StampCeilingSmall1;
            public Sprite StampCeilingMedium;
            public Sprite StampCeilingBig;
            public Sprite[] RoofMoss;
            public Sprite[] StampWallBig;
            public Sprite[] StampWallMedium;
            public Sprite[] StoneStamps;
            public Sprite[] FloorJunks;
            public Sprite WindowSmall;
            public Sprite GodRay;
            public Sprite BannerBig;
            public Sprite BannerRoomTop;
            public Sprite BannerRoomTiled;
            public Sprite BannerRoomBot;
            public Sprite WallBed;
            public Sprite Shelf;
            public Sprite[] Fioles;
            public Sprite AlcovePillar;
            public Sprite ArcheSmall;
            public Sprite PrisonBigAlcove;
            public Sprite AlchemyAlcove;
            public Sprite SuspendedCage;
            public Sprite HingeSmallDoor;
            public Sprite ExitDoor;
            public Sprite PortalBackOut;
            public Sprite WallLantern;
            public Sprite ArchSill;
            public Sprite ArchJamb;
            public Sprite[] ArchSills;
            public Sprite[] ArchJambs;
            public Sprite ArchSpandrel;
            public Sprite ArchMullion;
            public Sprite[] ShelfVials;
            public Sprite PrisonBell;
            public Sprite SpawnerJunk;
            public Sprite ChainAnchor;
            public Sprite SlabStrata;
            public Sprite GlowingMushroomBright;
            public Sprite FxGlowWhite;
            public Sprite ParallaxFarSprite;
            public Sprite AlcoveParallaxA;
            public Sprite VineParallaxA;
            public Sprite VineParallaxB;
            public Sprite BlackBeamTL;
            public Sprite BlackBeamTR;
            public Sprite CageChain;
            public Sprite WhitePixel;
            public Sprite WebSmallTLSprite;
            public Sprite WebSmallTRSprite;

            public Sprite PrisonFountain;
            public Sprite HomonculusFlush;
            public Sprite ArmoryDome;
            public Sprite ArmoryCorps;
            public Sprite WeaponDeco;
            public Sprite Dummy0;
            public Sprite MonsterDoorFront;
            public Sprite SmallDoor;
            public Sprite BigDoor0;
            public Sprite ExitDoorSewerShort;
            public Sprite HingeCorruptedDoor;
            public Sprite JudasSeat;
            public Sprite Billot;
            public Sprite SkeletonSpawn;
            public Sprite SnakeLandmark;
            public Sprite BigSword;
            public Sprite AlchemyBibli;
            public Sprite TableauNoir;
            public Sprite AspectsNpcStuffs;
            public Sprite ExitDoorPrisonCourtyard;
            public Sprite SpawnerPillar;
            public Sprite SwitchCopperOn;
            public Sprite SwitchCopperOff;
            public Sprite TpStoneUp;
            public Sprite TpStoneDown;
            public Sprite[] TpRocks;
            public Sprite[] OrnateCages;
            public Sprite[] ChainedPrisoners;
            public Sprite[] GroundFoliage;

            public Material MatParallaxFar;
            public Material MatParallaxMid;
            public Material MatGodRay;
            public Material MatBannerSway;
            public Material MatRoomBannerSway;
            public Material MatVineSway;
            public Material MatEmissiveFlame;
            public Material MatEmissiveMushroom;
            public Material MatEmissiveMushroomHalo;
            public Material MatEmissiveExit;
            public Material MatEmissiveScroll;
            public Material MatGroundMist;
            public Material MatParticlesUnlit;
        }

        [MenuItem("Tools/DeadCells/Scenes/Build Nha Giam Gieng Chuong (v8)")]
        public static void BuildAll()
        {
            EnsureFolder(RoomsFolder);
            EnsureFolder(UiFolder);

            SortingLayerSetup.EnsureDeadCellsSortingLayers();
            PrisonPropSpriteGenerator.EnsureAll();
            EnvironmentMaterialBuilder.EnsureAll();
            CharacterLitShaderMigrator.ApplyLitHybridToCharacterMaterials();
            MainLevelSceneRemediator.EnsureSharedPostProcessProfile();
            EnsurePlayerWallClingAnimation();
            EnsurePlayerPrefabVfxWiring();

            var spriteLit = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMatPath);
            if (spriteLit == null)
                throw new InvalidOperationException("Missing material: " + SpriteLitMatPath);
            var spriteUnlit = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMatPath);
            if (spriteUnlit == null)
                throw new InvalidOperationException("Missing material: " + SpriteUnlitMatPath);
            var particlesUnlit = AssetDatabase.LoadAssetAtPath<Material>(ParticlesUnlitMatPath);
            if (particlesUnlit == null)
                throw new InvalidOperationException("Missing material: " + ParticlesUnlitMatPath);

            ConfigureVaultASpriteImporters();
            var palette = LoadPrisonPalette();

            // 1. Build 9 Room Prefabs (5 Core Spine + 4 Expansion Wings) + VaultA Chamber Prefab
            SaveRoomPrefab("CD", root => BuildRoomCD(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("AB", root => BuildRoomAB(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("G", root => BuildRoomG(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("HJK", root => BuildRoomHJK(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("ELF", root => BuildRoomELF(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("W1_EastCourtyard", root => BuildRoomW1_EastCourtyard(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("W2_SewerCistern", root => BuildRoomW2_SewerCistern(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("W3_WestBastion", root => BuildRoomW3_WestBastion(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("U1_UpperPromenade", root => BuildRoomU1_UpperPromenade(root, spriteLit, spriteUnlit, palette));
            SaveRoomPrefab("VaultA", root => BuildRoomVaultA(root, spriteLit, spriteUnlit, particlesUnlit, palette));
            EditorUtility.UnloadUnusedAssetsImmediate(true);
            GC.Collect();

            // 2. Build ResultPanel UI Prefab
            BuildResultPanelPrefab();

            // 3. Build and save target scene
            BuildScene(spriteLit, particlesUnlit, palette);

            // 4. Register scene in EditorBuildSettings (keeping Main_Level at index 0)
            EnsureSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate(true);
            GC.Collect();
            AssetDatabase.Refresh();
            Debug.Log("[NhaGiamGiengChuongBuilder] BuildAll completed cleanly.");
        }

        [MenuItem("Tools/DeadCells/Build/Build Nha Giam Gieng Chuong (.exe)")]
        public static void BuildStandaloneWin64()
        {
            string outputPath = Path.GetFullPath("Build/NhaGiamGiengChuong/DeadCells_NG.exe");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-buildOutput", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    outputPath = Path.GetFullPath(args[i + 1]);
                    break;
                }
            }

            string outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"[NhaGiamGiengChuongBuilder] Win64 build failed: result={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings}");
            }

            Debug.Log(
                $"[NhaGiamGiengChuongBuilder] Win64 build succeeded: '{summary.outputPath}' ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F1}s, warnings={summary.totalWarnings}).");
        }

        private static PrisonPalette LoadPrisonPalette()
        {
            return new PrisonPalette
            {
                Dirt = LoadPrisonTile("dirt_00"),
                BackWalls = Enumerable.Range(0, 10).Select(i => LoadPrisonTile($"backWall_{i}_00")).ToArray(),
                StampWalls = Enumerable.Range(0, 6).Select(i => LoadPrisonTile($"stampWallSmall_{i}_00")).ToArray(),
                WallJunks = Enumerable.Range(10, 4).Select(i => LoadPrisonTile($"wallJunk_{i}")).ToArray(),
                FloorStamps = Enumerable.Range(0, 10).Select(i => LoadPrisonTile($"floorStamp_{i}_00")).ToArray(),
                Grounds = Enumerable.Range(0, 4).Select(i => LoadPrisonTile($"ground_{i}_00")).ToArray(),
                Ceilings = Enumerable.Range(0, 4).Select(i => LoadPrisonTile($"ceiling_{i}_00")).ToArray(),
                Stairs0 = LoadPrisonTile("stairs_0_00"),
                Stairs1 = LoadPrisonTile("stairs_1_00"),
                WebSmallTL = LoadPrisonTile("webSmallTL_00"),
                WebSmallTR = LoadPrisonTile("webSmallTR_00"),
                WebSmallBL = LoadPrisonTile("webSmallBL_00"),
                WebSmallBR = LoadPrisonTile("webSmallBR_00"),
                DirtSprite = LoadPrisonSprite("dirt_00.png"),
                WoodenPlatformLeft = LoadPrisonSprite("woodenPlatform_left_00.png"),
                WoodenPlatformMid = Enumerable.Range(0, 5).Select(i => LoadPrisonSprite($"woodenPlatform_{i}_00.png")).ToArray(),
                WoodenPlatformRight = LoadPrisonSprite("woodenPlatform_right_00.png"),
                WoodenCornerLeft = LoadPrisonSprite("woodenCornerLeft_00.png"),
                WoodenCornerRight = LoadPrisonSprite("woodenCornerRight_00.png"),
                LadderHole = LoadPrisonSprite("ladderHole_00.png"),
                ChainTile = LoadPrisonSprite("chainTile_00.png"),
                ChainDown = LoadPrisonSprite("chainDown_00.png"),
                LittleBeamTL = LoadPrisonSprite("littleBlackBeam_topLeft_0_00.png"),
                LittleBeamTR = LoadPrisonSprite("littleBlackBeam_topRight_0_00.png"),
                BigBeamTL = LoadPrisonSprite("blackBeam_topLeft_0_00.png"),
                BigBeamTR = LoadPrisonSprite("blackBeam_topRight_0_00.png"),
                StampCeilingSmall = LoadPrisonSprite("stampCeilingSmall_0_00.png"),
                StampCeilingSmall1 = LoadPrisonSprite("stampCeilingSmall_1_00.png"),
                StampCeilingMedium = LoadPrisonSprite("stampCeilingMedium_0_00.png"),
                StampCeilingBig = LoadPrisonSprite("stampCeilingBig_0_00.png"),
                RoofMoss = Enumerable.Range(0, 3).Select(i => LoadPrisonSprite($"roofMoss_{i}_00.png")).ToArray(),
                StampWallBig = Enumerable.Range(1, 5).Select(i => LoadPrisonSprite($"stampWallBig_{i}_00.png")).ToArray(),
                StampWallMedium = Enumerable.Range(0, 6).Select(i => LoadPrisonSprite($"stampWallMedium_{i}_00.png")).ToArray(),
                StoneStamps = Enumerable.Range(0, 6).Select(i => LoadPrisonSprite($"stoneStamp_{i}_00.png")).ToArray(),
                FloorJunks = Enumerable.Range(0, 10).Select(i => LoadPrisonSprite($"floorjunk_{i}_00.png")).ToArray(),
                WindowSmall = LoadPrisonSprite("prionSmallWindow_00.png"),
                GodRay = LoadPrisonSprite("godraysArcheTriainingRoom_00.png"),
                BannerBig = LoadPrisonSprite("bannerBig_00.png"),
                BannerRoomTop = LoadPrisonSprite("bannerRoomTop_00.png"),
                BannerRoomTiled = LoadPrisonSprite("bannerRoomTiled_00.png"),
                BannerRoomBot = LoadPrisonSprite("bannerRoomBot_00.png"),
                WallBed = LoadPrisonSprite("wallBed_00.png"),
                Shelf = LoadPrisonSprite("shelf_0_00.png"),
                Fioles = new[]
                {
                    LoadPrisonSprite("fioleA_00.png"),
                    LoadPrisonSprite("fioleB_00.png"),
                    LoadPrisonSprite("fioleC_00.png"),
                },
                AlcovePillar = LoadPrisonSprite("alcovePillar_00.png"),
                ArcheSmall = LoadPrisonSprite("archeSmall_00.png"),
                PrisonBigAlcove = LoadPrisonSprite("prisonBigAlcove_00.png"),
                AlchemyAlcove = LoadPrisonSprite("alchemyAlcove_00.png"),
                SuspendedCage = LoadPrisonSprite("suspendedCage_00.png"),
                HingeSmallDoor = LoadPrisonSprite("hingeSmallDoor_00.png"),
                ExitDoor = LoadPrisonSprite("exitDoor_00.png"),
                PortalBackOut = LoadPrisonSprite("PortalBackOut_0_00.png"),
                WallLantern = LoadSpriteAtPath(PrisonPropSpriteGenerator.WallLanternAssetPath),
                ArchSill = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSillAssetPath),
                ArchJamb = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchJambAssetPath),
                ArchSills = new[]
                {
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSillAssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSill01AssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSill02AssetPath),
                },
                ArchJambs = new[]
                {
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchJambAssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchJamb01AssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchJamb02AssetPath),
                },
                ArchSpandrel = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchSpandrelAssetPath),
                ArchMullion = LoadSpriteAtPath(PrisonPropSpriteGenerator.ArchMullionAssetPath),
                ShelfVials = new[]
                {
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ShelfVial00AssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ShelfVial01AssetPath),
                    LoadSpriteAtPath(PrisonPropSpriteGenerator.ShelfVial02AssetPath),
                },
                PrisonBell = LoadSpriteAtPath(PrisonPropSpriteGenerator.PrisonBellAssetPath),
                SpawnerJunk = LoadPrisonSprite("spawnerJunk_00.png"),
                ChainAnchor = LoadPrisonSprite("chainAnchor_00.png"),
                SlabStrata = LoadSpriteAtPath(PrisonPropSpriteGenerator.SlabStrataAssetPath),
                GlowingMushroomBright = LoadPrisonSprite("glowingMushroom_3_00.png"),
                FxGlowWhite = LoadSpriteAtPath("Assets/_Project/Art/VFX/ReferenceFeedback/fxGlowWhite.png"),
                ParallaxFarSprite = LoadSpriteAtPath("Assets/_Project/Art/Environment/Prison/Parallax/T_Prison_ParallaxFar_Grayscale.png"),
                AlcoveParallaxA = LoadPrisonSprite("alccoveParalaxeA_00.png"),
                VineParallaxA = LoadPrisonSprite("paralaxLianeA_00.png"),
                VineParallaxB = LoadPrisonSprite("paralaxLianeB_00.png"),
                BlackBeamTL = LoadPrisonSprite("blackBeam_topLeft_0_00.png"),
                BlackBeamTR = LoadPrisonSprite("blackBeam_topRight_0_00.png"),
                CageChain = LoadPrisonSprite("cageChain_00.png"),
                WhitePixel = LoadPrisonSprite("whitePixel_00.png"),
                WebSmallTLSprite = LoadPrisonSprite("webSmallTL_00.png"),
                WebSmallTRSprite = LoadPrisonSprite("webSmallTR_00.png"),

                PrisonFountain = LoadPrisonSprite("prisonFountain_00.png"),
                HomonculusFlush = LoadPrisonSprite("homonculusFlush_00.png"),
                ArmoryDome = LoadPrisonSprite("armory_dome_00.png"),
                ArmoryCorps = LoadPrisonSprite("armory_corps_00.png"),
                WeaponDeco = LoadPrisonSprite("weaponDeco_00.png"),
                Dummy0 = LoadPrisonSprite("dummy_0_00.png"),
                MonsterDoorFront = LoadPrisonSprite("monsterDoorFront_00.png"),
                SmallDoor = LoadPrisonSprite("smallDoor_00.png"),
                BigDoor0 = LoadPrisonSprite("bigDoor_0_00.png"),
                ExitDoorSewerShort = LoadPrisonSprite("exitDoorSewerShort_00.png"),
                HingeCorruptedDoor = LoadPrisonSprite("hingeCorruptedDoor_00.png"),
                JudasSeat = LoadPrisonSprite("judasSeat_00.png"),
                Billot = LoadPrisonSprite("billot_00.png"),
                SkeletonSpawn = LoadPrisonSprite("skeletonSpawn_00.png"),
                SnakeLandmark = LoadPrisonSprite("snakeLandmark_00.png"),
                BigSword = LoadPrisonSprite("bigSword_00.png"),
                AlchemyBibli = LoadPrisonSprite("alchemyBibli_00.png"),
                TableauNoir = LoadPrisonSprite("tableauNoir_00.png"),
                AspectsNpcStuffs = LoadPrisonSprite("aspectsNPCstuffs_00.png"),
                ExitDoorPrisonCourtyard = LoadPrisonSprite("exitDoorPrisonCourtyard_00.png"),
                SpawnerPillar = LoadPrisonSprite("spawnerPillar_00.png"),
                SwitchCopperOn = LoadPrisonSprite("switchCopper_00.png"),
                SwitchCopperOff = LoadPrisonSprite("switchCopper_off_00.png"),
                TpStoneUp = LoadPrisonSprite("tpStoneUp_00.png"),
                TpStoneDown = LoadPrisonSprite("tpStoneDown_00.png"),
                TpRocks = Enumerable.Range(0, 4).Select(i => LoadPrisonSprite($"tpRock_{i}_00.png")).ToArray(),
                OrnateCages = new[]
                {
                    LoadPrisonSprite("chainBot_0_00.png"),
                    LoadPrisonSprite("chainBot_1_00.png"),
                    LoadPrisonSprite("chainBot_2_00.png"),
                },
                ChainedPrisoners = new[]
                {
                    LoadPrisonSprite("wallJunk_14.png"),
                    LoadPrisonSprite("wallJunk_15.png"),
                    LoadPrisonSprite("wallJunk_17.png"),
                },
                GroundFoliage = new[]
                {
                    LoadPrisonSprite("groundFoliage_0_00.png"),
                    LoadPrisonSprite("groundFoliage_1_00.png"),
                },

                MatParallaxFar = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_ParallaxFar_Prison.mat"),
                MatParallaxMid = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_ParallaxMid_Prison.mat"),
                MatGodRay = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_GodRay_Additive.mat"),
                MatBannerSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Banner_Sway.mat"),
                MatRoomBannerSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_RoomBanner_Sway.mat"),
                MatVineSway = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Vine_Sway.mat"),
                MatEmissiveFlame = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Flame.mat"),
                MatEmissiveMushroom = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Mushroom.mat"),
                MatEmissiveMushroomHalo = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_MushroomHalo.mat"),
                MatEmissiveExit = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Exit.mat"),
                MatEmissiveScroll = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Scroll.mat"),
                MatGroundMist = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_GroundMist_Prison.mat"),
                MatParticlesUnlit = LoadMaterialAtPath(ParticlesUnlitMatPath),
            };
        }

        private static void SaveRoomPrefab(string roomName, Action<GameObject> populate)
        {
            string path = $"{RoomsFolder}/{roomName}.prefab";
            var root = new GameObject($"Room_{roomName}");
            try
            {
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                populate(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // ==================== ROOM PREFABS (v7 exact collision + Dead Cells Prison 24 PPU Art) ====================

        private static void BuildRoomCD(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Solid Floors (v7 exact)
            AddSolidBox(colRoot, "Floor_C", 42f, 56f, -1f, 0f);
            AddSolidBox(colRoot, "Floor_D", 22f, 42f, 7f, 8f);

            // One-Way Platforms (v7 exact) — P0-04 Part A removed the zero-purpose OneWay_C_Rest ledge
            AddOneWayBox(colRoot, "OneWay_C_Top", 42f, 56f, 8f);

            // Ceilings (v7 exact)
            AddSolidBox(colRoot, "Ceiling_C", 42f, 56f, 18.5f, 19f);
            AddSolidBox(colRoot, "Ceiling_D", 22f, 42f, 13.5f, 14f);

            // Walls owned by CD (v7 exact)
            AddSolidBox(colRoot, "Wall_BC_Header_42", 41.75f, 42.25f, 4f, 7f);
            AddSolidBox(colRoot, "Wall_CD_Upper_42", 41.75f, 42.25f, 12f, 19f);
            AddSolidBox(colRoot, "Wall_CG_Mid_56", 55.75f, 56.25f, 4f, 7f);
            AddSolidBox(colRoot, "Wall_CG_Upper_56", 55.75f, 56.25f, 12f, 19f);
            AddSolidBox(colRoot, "Wall_DH_Header_22", 21.75f, 22.25f, 12f, 13.5f);

            // Climbable Trigger I1 (P0-02: trimmed to [floorY - 0.20 .. deckTopY + 0.08], no air overhang)
            AddClimbableTrigger(root, "I1_Chain_C", new Vector2(46f, 3.94f), new Vector2(0.5f, 8.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 42f, 56f, 0f, 18.5f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 22f, 42f, 8f, 13.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 1, 46, 52, 11, 16);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "C", 42f, 56f, 0f, 18.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "D", 22f, 42f, 8f, 13.5f);

            PaintPrisonSolidFloor(solidTm, rimTm, pal, 42f, 56f, -1f, 0f);
            PaintVisualDirtSkirt(solidTm, pal, 42, 56, -8, -1);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 22f, 42f, 7f, 8f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 42f, 56f, 18.5f, 19f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 22f, 42f, 13.5f, 14f);

            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_BC_Header_42", 42f, 4f, 7f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_CD_Upper_42", 42f, 12f, 19f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_CG_Mid_56", 56f, 4f, 7f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_CG_Upper_56", 56f, 12f, 19f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_DH_Header_22", 22f, 12f, 13.5f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "CD_Sub", 42, 56, -8, -1);
            AddInterTierSlabStrata(artRoot, pal, mat, "CD_BtoD", 22f, 42f, 7.15f);

            // Planks (woodenPlatform_* flush with collider) + wall corner braces
            AddPrisonPlankRow(artRoot, "Art_C_TopPlanks", pal, mat, 42f, 56f, 8f, -2);
            AddPlatformCornerBrace(artRoot, "Art_C_TopBraceL", pal.WoodenCornerLeft, mat, 42f, 8f, true);
            AddPlatformCornerBrace(artRoot, "Art_C_TopBraceR", pal.WoodenCornerRight, mat, 56f, 8f, false);

            // Chain I1 capped cleanly
            AddPrisonChainArt(artRoot, "Art_I1_Chain", pal, mat, 46f, 0f, 8f);

            // Corner beams, ceiling stamps, moss & webs
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "C", 42f, 56f, 18.5f, 0f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "D", 22f, 42f, 13.5f, 8f, isTallRoom: false, hasLeftBottomWall: false, hasRightBottomWall: false);

            // Normalized hero bell in Room C
            AddScaledCeilingProp(artRoot, "Art_C_Bell", pal.PrisonBell, mat, 49f, 18.5f, 1.0f, -5, new Color(0.90f, 0.86f, 0.80f, 1f));

            // Room C & D architectural dressing
            AddVerticalRoomBanner(artRoot, "Art_C_Banner", pal, mat, 53f, 16.5f, -12);
            AddPrisonSpriteProp(artRoot, "Art_C_BannerBig", pal.BannerBig, pal.MatBannerSway, new Vector2(43.8f, 12.2f), 0.82f, -12, Color.white);
            AddWindowWithGodray(artRoot, "Art_D_Window1", mat, unlitMat, pal, new Vector2(27f, 11.1f));
            AddWindowWithGodray(artRoot, "Art_D_Window2", mat, unlitMat, pal, new Vector2(32f, 11.1f));
            AddWindowWithGodray(artRoot, "Art_D_Window3", mat, unlitMat, pal, new Vector2(37f, 11.1f));
            AddGroundedPrisonSprite(artRoot, "Art_D_Bed", pal.WallBed, mat, 24.5f, 8f, 1f, -11, Color.white);
            AddShelfWithFioles(artRoot, "Art_D_Shelf", pal, mat, 39.2f, 10.6f, -11);
            AddFloorJunkCluster(artRoot, "Art_C_Junk", pal, mat, 51.5f, 0f, 0);
            AddFloorJunkCluster(artRoot, "Art_D_Junk", pal, mat, 26.8f, 8f, 3);
            AddFloorJunkCluster(artRoot, "Art_D_JunkWest", pal, mat, 36.2f, 8f, 2);
            AddGroundedPrisonSprite(artRoot, "Art_D_Bed1", pal.WallBed, mat, 31.5f, 8f, 0.85f, -11, Color.white);
            AddFloorJunkCluster(artRoot, "Art_D_JunkEast2", pal, mat, 39.8f, 8f, 5);

            // Phase 3 (P2-04): Damp floor-lip moss foliage on Floor_C
            AddGroundedPrisonSprite(artRoot, "Art_C_Foliage1", pal.GroundFoliage[0], mat, 48.2f, 0f, 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));

            // 11/11 Room Lighting — Room C (3 lights) & Room D (2 lights)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            AddStandingTorchBeacon(artRoot, "Art_C_TorchLower", mat, 49f, 0f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_C_TorchUpper", mat, 49f, 8f, warmGold, 8.8f, 1.25f);
            AddCeilingLanternBeacon(artRoot, "Art_C_LanternCeiling", mat, 49f, 18.5f, warmGold, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_D_TorchWest", mat, 29.5f, 8f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_D_TorchEast", mat, 34.5f, 8f, warmAmber, 8.8f, 1.25f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomAB(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Solid Floor A-B, Ceilings, Outer Wall, Seam Header (v7 exact) — P0-04 Part A removed the OneWay_A_Practice ledge
            AddSolidBox(colRoot, "Floor_AB", 0f, 42f, -1f, 0f);
            AddSolidBox(colRoot, "Ceiling_A", -0.5f, 18f, 5.5f, 6f);
            AddSolidBox(colRoot, "Ceiling_B", 18f, 42f, 6f, 6.5f);
            AddSolidBox(colRoot, "Wall_A_West", -0.5f, 0f, -1f, 5.5f);
            AddSolidBox(colRoot, "Wall_AB_Header_18", 17.75f, 18.25f, 4f, 6f);

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 0f, 18f, 0f, 5.5f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 18f, 42f, 0f, 6f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 0, 8, 12, 2, 5);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "A", 0f, 18f, 0f, 5.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "B", 18f, 42f, 0f, 6f);

            // Solid floor, sub-floor & outer bedrock skirts (sealing orthoSize=7 spawn camera at (3, 1.5)), trimmed ceilings, seamless header
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 0f, 42f, -1f, 0f);
            PaintVisualDirtSkirt(solidTm, pal, -16, 42, -8, -1);
            PaintVisualDirtSkirt(solidTm, pal, -16, 0, -1, 15);
            PaintPrisonCeiling(solidTm, rimTm, pal, 0f, 18f, 5.5f, 6f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 18f, 42f, 6f, 6.5f);
            PaintVisualDirtSkirt(solidTm, pal, 0, 6, 6, 15);
            PaintVisualDirtSkirt(solidTm, pal, 6, 22, 6, 7);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_AB_Header_18", 18f, 4f, 6f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_West", -16, 0, -8, 15);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_Sub", 0, 42, -8, -1);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "AB_UpperA", 0, 6, 6, 15);

            // Planks (woodenPlatform_*) — P0-04 Part A removed Art_A_PracticePlanks together with its ledge

            // Corner beams, ceiling stamps, moss & webs
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "A", 0f, 18f, 5.5f, 0f, isTallRoom: false, hasLeftBottomWall: true, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "B", 18f, 42f, 6f, 0f, isTallRoom: false, hasLeftBottomWall: false, hasRightBottomWall: false);

            // Room A props: Homunculus spawn fountain/pipes (outside Bay #0 [8..12, 2..5]) + suspendedCage_00 anchored at ceiling 5.5f @ 14.8f (bottom 3.5u >= 3.0u, clear of pipes & Bay #0)
            AddGroundedPrisonSprite(artRoot, "Art_A_Fountain", pal.PrisonFountain, mat, 4.3f, 0f, 0.85f, -13, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_A_HomonculusPipes", pal.HomonculusFlush, mat, new Vector2(4.3f, 2.6f), 0.85f, -14, Color.white);
            AddScaledCeilingProp(artRoot, "Art_A_Cage", pal.SuspendedCage, mat, 14.8f, 5.5f, 1f, -7, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_A_Bed", pal.WallBed, mat, 1.8f, 0f, 0.90f, -11, Color.white);
            AddWindowWithGodray(artRoot, "Art_A_Window", mat, unlitMat, pal, new Vector2(6.2f, 3.6f));
            AddFloorJunkCluster(artRoot, "Art_A_Junk", pal, mat, 7.2f, 0f, 1);
            AddFloorJunkCluster(artRoot, "Art_A_JunkEast", pal, mat, 10.0f, 0f, 6);

            var vatLightGo = CreateChild(artRoot, "Art_A_VatFillLight");
            vatLightGo.transform.position = new Vector3(4.3f, 2.8f, 0f);
            var vatLight = vatLightGo.AddComponent<Light2D>();
            vatLight.lightType = Light2D.LightType.Point;
            vatLight.color = new Color(0.72f, 0.90f, 0.96f, 1f);
            vatLight.intensity = 0.52f;
            vatLight.pointLightInnerRadius = 0.6f;
            vatLight.pointLightOuterRadius = 3.4f;
            vatLight.falloffIntensity = 0.65f;
            ConfigureGameplayPointLight(vatLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);

            // Room B grounded landmarks (preserving A08 names & grounded y=0)
            AddGroundedPrisonSprite(artRoot, "Art_B_Arch1", pal.ArcheSmall, mat, 24f, 0f, 1f, -14, new Color(0.88f, 0.90f, 0.92f, 1f));
            AddGroundedPrisonSprite(artRoot, "Art_B_Pillar", pal.AlcovePillar, mat, 30f, 0f, 0.85f, -13, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_B_Arch2", pal.ArcheSmall, mat, 36f, 0f, 1f, -14, new Color(0.88f, 0.90f, 0.92f, 1f));
            AddVerticalRoomBanner(artRoot, "Art_B_Banner", pal, mat, 20.5f, 5.6f, -12, includeMid: false);
            AddFloorJunkCluster(artRoot, "Art_B_Junk", pal, mat, 39.2f, 0f, 5);

            // 11/11 Room Lighting — Room A (1 light on solid brick between Bay #0 and TP_0_Spawn) & Room B (2 lights)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            AddStandingTorchBeacon(artRoot, "Art_A_Torch", mat, 13.6f, 0f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_B_TorchWest", mat, 24f, 0f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_B_TorchEast", mat, 36f, 0f, warmAmber, 8.8f, 1.25f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomG(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Solid floors, one-way upper mid, ceiling, outer east wall, climbable I2 (v7 exact)
            AddSolidBox(colRoot, "Floor_G_Lower", 56f, 76f, -1f, 0f);
            AddSolidBox(colRoot, "Floor_G_UpperWest", 56f, 65f, 7f, 8f);
            AddOneWayBox(colRoot, "OneWay_G_UpperMid", 65f, 72f, 8f);
            AddSolidBox(colRoot, "Floor_G_UpperEast", 72f, 76f, 7f, 8f);
            AddSolidBox(colRoot, "Ceiling_G", 56f, 76.5f, 14.5f, 15f);
            AddSolidBox(colRoot, "Wall_GM_Mid_76", 76f, 76.5f, 4f, 7f);
            AddSolidBox(colRoot, "Wall_GN_Upper_76", 76f, 76.5f, 12f, 14.5f);
            AddClimbableTrigger(root, "I2_Chain_G", new Vector2(68f, 3.94f), new Vector2(0.5f, 8.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 56f, 76f, 0f, 14.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 2, 69, 74, 2, 6);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "G", 56f, 76f, 0f, 14.5f);

            PaintPrisonSolidFloor(solidTm, rimTm, pal, 56f, 76f, -1f, 0f);
            PaintVisualDirtSkirt(solidTm, pal, 56, 76, -21, -1);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 56f, 65f, 7f, 8f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 72f, 76f, 7f, 8f);
            PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, 4f, 7f);
            PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, 12f, 15f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 56f, 76f, 14.5f, 15f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "G_Sub", 56, 76, -21, -1);
            AddInterTierSlabStrata(artRoot, pal, mat, "G_LowerToUpper", 56f, 65f, 7.45f);

            // Planks + wall/ledge corner braces
            AddPrisonPlankRow(artRoot, "Art_G_UpperPlanks", pal, mat, 65f, 72f, 8f, -2);
            AddPlatformCornerBrace(artRoot, "Art_G_UpperBraceL", pal.WoodenCornerLeft, mat, 65f, 8f, true);
            AddPlatformCornerBrace(artRoot, "Art_G_UpperBraceR", pal.WoodenCornerRight, mat, 72f, 8f, false);

            // Chain I2 capped cleanly
            AddPrisonChainArt(artRoot, "Art_I2_Chain", pal, mat, 68f, 0f, 8f);

            // Corner beams, ceiling stamps, moss & webs
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "G", 56f, 76f, 14.5f, 0f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: false);

            // Native 24-PPU Prison props (1:1 scale discipline)
            AddScaledGroundedProp(artRoot, "Art_G_Winch", pal.ChainAnchor, mat, 73.6f, 8f, 0.90f, -1, new Color(0.86f, 0.86f, 0.90f, 1f));
            AddScaledGroundedProp(artRoot, "Art_G_Barrel", pal.FloorJunks[0], mat, 60f, 0f, 0.90f, -5, new Color(0.76f, 0.80f, 0.82f, 1f));
            AddScaledGroundedProp(artRoot, "Art_G_Crate", pal.FloorJunks[2], mat, 64.5f, 0f, 0.92f, -5, new Color(0.76f, 0.80f, 0.82f, 1f));

            // Room G architectural dressing
            AddGroundedPrisonSprite(artRoot, "Art_G_Alcove", pal.PrisonBigAlcove, mat, 60.5f, 8f, 1.00f, -14, Color.white);
            var alcoveLightGo = CreateChild(artRoot, "Art_G_AlcoveLight");
            alcoveLightGo.transform.position = new Vector3(60.5f, 10.4f, 0f);
            var alcoveLight = alcoveLightGo.AddComponent<Light2D>();
            alcoveLight.lightType = Light2D.LightType.Point;
            alcoveLight.color = new Color(1f, 202f / 255f, 122f / 255f, 1f);
            alcoveLight.intensity = 1.35f;
            alcoveLight.pointLightInnerRadius = 1.2f;
            alcoveLight.pointLightOuterRadius = 6.0f;
            alcoveLight.falloffIntensity = 0.62f;
            ConfigureGameplayPointLight(alcoveLight, enableVolumetric: true, volumeIntensity: 0.04f, addFlicker: false, phaseSeed: 0f);
            AddShelfWithFioles(artRoot, "Art_G_Shelf", pal, mat, 58.2f, 3.2f, -11);
            AddWindowWithGodray(artRoot, "Art_G_Window", mat, unlitMat, pal, new Vector2(74.2f, 11.6f));
            AddFloorJunkCluster(artRoot, "Art_G_Junk", pal, mat, 66.3f, 0f, 6);

            // Phase 3 (P2-04): Ornate iron hanging cage & damp floor-lip moss foliage
            AddScaledCeilingProp(artRoot, "Art_G_OrnateCage1", pal.OrnateCages[1], mat, 69.5f, 14.5f, 0.90f, -7, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_G_Foliage1", pal.GroundFoliage[1], mat, 62.2f, 0f, 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));
            AddGroundedPrisonSprite(artRoot, "Art_G_Foliage2", pal.GroundFoliage[1], mat, 57.5f, 8f, 0.85f, -2, new Color(0.84f, 0.94f, 0.86f, 0.95f));

            // 11/11 Room Lighting — Room G (3 torches here + I3 chest GlowHighlight in scene)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            AddStandingTorchBeacon(artRoot, "Art_G_TorchLower", mat, 60f, 0f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_G_TorchUpperWest", mat, 65.6f, 8f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_G_TorchUpperEast", mat, 71.8f, 8f, warmAmber, 8.8f, 1.25f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomHJK(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Staircase H PolygonCollider2D (v9): true 45-degree Dead Cells ramp rising 8u over 8u
            // flat bottom landing [18,22]@8 -> 45-degree run (18,8)->(10,16) -> flat top landing [6,10]@16, base y=7
            var stairsGo = CreateChild(colRoot, "Stairs_H");
            stairsGo.layer = LayerGround;
            var poly = stairsGo.AddComponent<PolygonCollider2D>();
            poly.pathCount = 1;
            poly.SetPath(0, new[]
            {
                new Vector2(6f, 7f),
                new Vector2(22f, 7f),
                new Vector2(22f, 8f),
                new Vector2(18f, 8f),
                new Vector2(10f, 16f),
                new Vector2(6f, 16f)
            });
            // Zero-friction surface so Floor_D (y=8) -> ramp -> Floor_KJ (y=16) keeps the Player's normal-velocity
            // ground check and tangential projection seamless (same material the Player collider uses).
            poly.sharedMaterial = LoadPhysicsMaterialAtPath(ZeroFrictionPhysicsMaterialPath);

            // Floors, OneWay, Ceilings, Walls, Climbable I6 (v7 exact)
            AddSolidBox(colRoot, "Floor_KJ", -30f, 6f, 15f, 16f);
            AddOneWayBox(colRoot, "OneWay_K_Top", -30f, -16f, 24f);
            AddSolidBox(colRoot, "Ceiling_HJ", -16f, 22f, 22.5f, 23f);
            AddSolidBox(colRoot, "Ceiling_K", -30.5f, -16f, 30.5f, 31f);
            AddSolidBox(colRoot, "Wall_KQ_Mid_Neg30", -30.5f, -30f, 20f, 23f);
            AddSolidBox(colRoot, "Wall_KR_Upper_Neg30", -30.5f, -30f, 28f, 30.5f);
            AddSolidBox(colRoot, "Wall_H_EastUpper_22", 21.75f, 22.25f, 14f, 22.5f);
            AddSolidBox(colRoot, "Wall_HJ_Header_6", 5.75f, 6.25f, 20f, 22.5f);
            AddSolidBox(colRoot, "Wall_JK_Mid_Neg16", -16.25f, -15.75f, 20f, 23.75f);
            AddSolidBox(colRoot, "Wall_KE_Header_Neg16", -16.25f, -15.75f, 28f, 31f);
            AddClimbableTrigger(root, "I6_Chain_K", new Vector2(-24f, 19.94f), new Vector2(0.5f, 8.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 6f, 22f, 8f, 22.5f);
            PaintPrisonBackWall(bgTm, decalTm, pal, -16f, 6f, 16f, 22.5f);
            PaintPrisonBackWall(bgTm, decalTm, pal, -30f, -16f, 16f, 30.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 3, -15, -9, 18, 21);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "H", 6f, 22f, 8f, 22.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "J", -16f, 6f, 16f, 22.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "K", -30f, -16f, 16f, 30.5f);

            // Solid staircase H (floorStamp top + dirt_00 core + stairs rim + stoneStamp core detail)
            PaintPrisonStairsH(solidTm, rimTm, artRoot, mat, pal);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, -30f, 6f, 15f, 16f);
            PaintVisualDirtSkirt(solidTm, pal, -30, 6, 7, 15);
            PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 20f, 23f);
            PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 28f, 31f);
            PaintPrisonCeiling(solidTm, rimTm, pal, -16f, 22f, 22.5f, 23f);
            PaintPrisonCeiling(solidTm, rimTm, pal, -30f, -16f, 30.5f, 31f);

            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_H_EastUpper_22", 22f, 14f, 22.5f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_HJ_Header_6", 6f, 20f, 22.5f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_JK_Mid_Neg16", -16f, 20f, 23.75f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_KE_Header_Neg16", -16f, 28f, 31f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "HJK_Sub", -30, 6, 7, 15);
            AddInterTierSlabStrata(artRoot, pal, mat, "HJK_DtoJ", -16f, 6f, 15.35f);

            // Planks + corner braces
            AddPrisonPlankRow(artRoot, "Art_K_TopPlanks", pal, mat, -30f, -16f, 24f, -2);
            AddPlatformCornerBrace(artRoot, "Art_K_TopBraceL", pal.WoodenCornerLeft, mat, -30f, 24f, true);
            AddPlatformCornerBrace(artRoot, "Art_K_TopBraceR", pal.WoodenCornerRight, mat, -16f, 24f, false);

            // Chain I6 capped cleanly
            AddPrisonChainArt(artRoot, "Art_I6_Chain", pal, mat, -24f, 16f, 24f);

            // Corner beams, ceiling stamps, moss & webs
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "H", 6f, 22f, 22.5f, 8f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "J", -16f, 6f, 22.5f, 16f, isTallRoom: false, hasLeftBottomWall: false, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "K", -30f, -16f, 30.5f, 16f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: false);

            // Grounded J landmark (A08 name preserved) & H/J/K Prison dressing
            AddGroundedPrisonSprite(artRoot, "Art_J_Arch", pal.PrisonBigAlcove, mat, -5f, 16f, 1f, -14, Color.white);
            AddVerticalRoomBanner(artRoot, "Art_J_Banner", pal, mat, 1.5f, 21.8f, -12);
            AddWindowWithGodray(artRoot, "Art_H_Window", mat, unlitMat, pal, new Vector2(15.5f, 19.2f));
            AddVerticalRoomBanner(artRoot, "Art_K_BannerBig", pal, mat, -27.6f, 30.0f, -12, includeMid: true);
            AddGroundedPrisonSprite(artRoot, "Art_J_Bed", pal.WallBed, mat, 3.6f, 16f, 1f, -11, Color.white);
            AddFloorJunkCluster(artRoot, "Art_J_Junk", pal, mat, -12.0f, 16f, 2);
            AddFloorJunkCluster(artRoot, "Art_K_Junk", pal, mat, -18.5f, 16f, 6);
            AddFloorJunkCluster(artRoot, "Art_K_JunkMid", pal, mat, -23.0f, 16f, 6);
            AddFloorJunkCluster(artRoot, "Art_H_LandingJunk", pal, mat, 20.2f, 8f, 1);
            AddFloorJunkCluster(artRoot, "Art_H_StepJunkTop", pal, mat, 6.5f, 16f, 7);

            // Phase 3 (P2-04): Ornate iron hanging cage in Room K
            AddScaledCeilingProp(artRoot, "Art_K_OrnateCage1", pal.OrnateCages[1], mat, -27.5f, 30.5f, 0.90f, -7, Color.white);

            // 11/11 Room Lighting — Room H (3 lanterns on the backwall above the 45-degree ramp), Room J (2), Room K (3)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            AddStandingTorchBeacon(artRoot, "Art_H_TorchLanding", mat, 18.8f, 8f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_H_TorchMid", mat, 13.8f, 12.5f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_H_TorchTop", mat, 8.4f, 16f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_J_TorchWest", mat, -8.3f, 16f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_J_TorchEast", mat, 0f, 16f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_K_TorchLower", mat, -20.5f, 16f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_K_TorchUpper", mat, -22f, 24f, warmGold, 8.8f, 1.25f);
            AddCeilingLanternBeacon(artRoot, "Art_K_LanternCeiling", mat, -23f, 30.5f, warmGold, 8.8f, 1.25f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomELF(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Solid Floors, Ceilings, Outer Walls, Seam Headers, One-Way Platforms (v7 exact)
            AddSolidBox(colRoot, "Floor_EL", -16f, 38f, 23f, 24f);
            AddSolidBox(colRoot, "Floor_F_Ceiling_E", 0f, 22f, 31f, 32f);
            AddSolidBox(colRoot, "Ceiling_E_West", -16f, 0f, 31f, 31.5f);
            AddSolidBox(colRoot, "Ceiling_L", 22f, 38.5f, 38.5f, 39f);
            AddSolidBox(colRoot, "Ceiling_F", -0.5f, 22f, 38f, 38.5f);
            AddSolidBox(colRoot, "Wall_LS_Upper_38", 38f, 38.5f, 28f, 38.5f);
            AddSolidBox(colRoot, "Wall_F_West", -0.5f, 0f, 31f, 38f);
            AddSolidBox(colRoot, "Wall_EL_Header_22", 21.75f, 22.25f, 28f, 31f);
            AddSolidBox(colRoot, "Wall_LF_Header_22", 21.75f, 22.25f, 36f, 39f);

            AddOneWayBox(colRoot, "OneWay_E_Combat", -4f, 2f, 26f);
            AddOneWayBox(colRoot, "OneWay_L_1", 32f, 38f, 26f);
            AddOneWayBox(colRoot, "OneWay_L_2", 28f, 34f, 28f);
            AddOneWayBox(colRoot, "OneWay_L_3", 24f, 30f, 30f);
            AddOneWayBox(colRoot, "OneWay_L_4", 22f, 26f, 32f);

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, -16f, 22f, 24f, 31f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 22f, 38f, 24f, 38.5f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 0f, 22f, 32f, 38f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 4, 6, 10, 25, 30);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "E", -16f, 22f, 24f, 31f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "L", 22f, 38f, 24f, 38.5f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "F", 0f, 22f, 32f, 38f);

            // Solid floors, ceilings & outer walls (zero cell overwrite)
            PaintPrisonSolidFloor(solidTm, rimTm, pal, -16f, 38f, 23f, 24f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 0f, 22f, 31f, 32f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 0f, 22f, 31f);
            PaintPrisonCeiling(solidTm, rimTm, pal, -16f, 0f, 31f, 31.5f);

            PaintPrisonOuterWall(solidTm, pal, 38f, 38.5f, 28f, 39f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 22f, 38f, 38.5f, 39f);

            // Wall_F_West [-0.5, 0]x[31, 38.5]: cy=31 belongs to Ceiling_E_West (-1, 31), so tile cy=32..38.5 + seamless sprite beam [31..38.5]
            PaintPrisonOuterWall(solidTm, pal, -0.5f, 0f, 32f, 38.5f);
            AddSolidBeamSprite(artRoot, "Beam_F_West", pal.DirtSprite, mat, -0.25f, 31f, 38.5f);
            PaintPrisonCeiling(solidTm, rimTm, pal, 0f, 22f, 38f, 38.5f);

            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_EL_Header_22", 22f, 28f, 31f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_LF_Header_22", 22f, 36f, 39f);
            AddInterTierSlabStrata(artRoot, pal, mat, "ELF_JtoE", -16f, 22f, 23.35f);
            AddInterTierSlabStrata(artRoot, pal, mat, "ELF_EtoF", 0f, 22f, 31.45f);

            // Planks (all woodenPlatform_*) + wall braces
            AddPrisonPlankRow(artRoot, "Art_E_CombatPlanks", pal, mat, -4f, 2f, 26f, -2);
            AddPrisonPlankRow(artRoot, "Art_L1_Planks", pal, mat, 32f, 38f, 26f, -2);
            AddPrisonPlankRow(artRoot, "Art_L2_Planks", pal, mat, 28f, 34f, 28f, -2);
            AddPrisonPlankRow(artRoot, "Art_L3_Planks", pal, mat, 24f, 30f, 30f, -2);
            AddPrisonPlankRow(artRoot, "Art_L4_Planks", pal, mat, 22f, 26f, 32f, -2);
            AddPlatformCornerBrace(artRoot, "Art_L4_BraceL", pal.WoodenCornerLeft, mat, 22f, 32f, true);

            // Corner beams, ceiling stamps, moss & webs
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "E", -16f, 22f, 31f, 24f, isTallRoom: false, hasLeftBottomWall: false, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "L", 22f, 38f, 38.5f, 24f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: false);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "F", 0f, 22f, 38f, 32f, isTallRoom: false, hasLeftBottomWall: true, hasRightBottomWall: false);

            // Room E grounded landmarks (A08 names preserved) & dressing
            AddGroundedPrisonSprite(artRoot, "Art_E_Pillar1", pal.AlcovePillar, mat, -11.5f, 24f, 0.85f, -13, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_E_Pillar2", pal.AlcovePillar, mat, 14.5f, 24f, 0.85f, -13, Color.white);
            AddVerticalRoomBanner(artRoot, "Art_E_Banner", pal, mat, 4f, 30.2f, -12);
            AddWindowWithGodray(artRoot, "Art_E_WindowWest", mat, unlitMat, pal, new Vector2(-3.5f, 28.6f));
            AddWindowWithGodray(artRoot, "Art_E_WindowEast", mat, unlitMat, pal, new Vector2(20.2f, 28.6f));
            AddFloorJunkCluster(artRoot, "Art_E_Junk", pal, mat, 10.0f, 24f, 4);
            AddFloorJunkCluster(artRoot, "Art_E_JunkWest", pal, mat, 4.1f, 24f, 1);
            AddFloorJunkCluster(artRoot, "Art_E_JunkMid", pal, mat, 1.5f, 24f, 6);
            AddFloorJunkCluster(artRoot, "Art_E_JunkEast", pal, mat, 18.0f, 24f, 9);

            // Room L dressing
            AddPrisonSpriteProp(artRoot, "Art_L_BannerBig", pal.BannerBig, pal.MatBannerSway, new Vector2(35.8f, 33.5f), 0.82f, -12, Color.white);
            AddWindowWithGodray(artRoot, "Art_L_Window", mat, unlitMat, pal, new Vector2(30.5f, 35.2f));
            AddFloorJunkCluster(artRoot, "Art_L_Junk", pal, mat, 24.5f, 24f, 8);
            AddFloorJunkCluster(artRoot, "Art_L_JunkMid", pal, mat, 29.8f, 24f, 3);
            AddFloorJunkCluster(artRoot, "Art_L_JunkEast", pal, mat, 35.2f, 24f, 5);

            // Room F Exit Arch + Grounded PortalBackOut_0_00 Emissive HDR Glow + Warm Beacon Light2D (A08 compatible)
            AddGroundedPrisonSprite(artRoot, "Art_F_ExitArch", pal.ExitDoor, mat, 5f, 32f, 0.80f, -5, Color.white);
            var exitGlowGo = CreateChild(artRoot, "Art_F_ExitGlow");
            const float exitGlowScale = 0.55f;
            exitGlowGo.transform.position = new Vector3(5f, 32f - pal.PortalBackOut.bounds.min.y * exitGlowScale, 0f);
            exitGlowGo.transform.localScale = new Vector3(exitGlowScale, exitGlowScale, 1f);
            var exitGlowSr = exitGlowGo.AddComponent<SpriteRenderer>();
            exitGlowSr.sprite = pal.PortalBackOut;
            exitGlowSr.sharedMaterial = pal.MatEmissiveExit;
            exitGlowSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Background");
            exitGlowSr.sortingOrder = -6;
            exitGlowSr.color = new Color(1f, 0.92f, 0.65f, 0.45f);
            var exitGlowLight = exitGlowGo.AddComponent<Light2D>();
            exitGlowLight.lightType = Light2D.LightType.Point;
            exitGlowLight.color = new Color(1f, 0.92f, 0.65f, 1f);
            exitGlowLight.intensity = 1.40f;
            exitGlowLight.pointLightOuterRadius = 7.2f;
            exitGlowLight.pointLightInnerRadius = 1.4f;
            ConfigureGameplayPointLight(exitGlowLight, enableVolumetric: true, volumeIntensity: 0.09f, addFlicker: true, phaseSeed: 5f);

            AddVerticalRoomBanner(artRoot, "Art_F_Banner", pal, mat, 12.5f, 37.2f, -12);
            AddShelfWithFioles(artRoot, "Art_F_Shelf", pal, mat, 18.2f, 34.6f, -11);
            AddFloorJunkCluster(artRoot, "Art_F_JunkMid", pal, mat, 11.5f, 32f, 2);
            AddFloorJunkCluster(artRoot, "Art_F_JunkEast", pal, mat, 18.5f, 32f, 7);

            // 11/11 Room Lighting — Room E (3), Room L (3, ceiling lantern anchored to Ceiling_L 38.5f), Room F (ExitGlow + 1 torch)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            AddStandingTorchBeacon(artRoot, "Art_E_TorchWest", mat, -6.8f, 24f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_E_TorchCenter", mat, 3f, 24f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_E_TorchEast", mat, 18.6f, 24f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_L_TorchLow", mat, 35f, 26f, warmAmber, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_L_TorchMid", mat, 27f, 30f, warmGold, 8.8f, 1.25f);
            AddCeilingLanternBeacon(artRoot, "Art_L_LanternCeiling", mat, 26.5f, 38.5f, warmGold, 8.8f, 1.25f);
            AddStandingTorchBeacon(artRoot, "Art_F_TorchEast", mat, 15f, 32f, warmGold, 8.8f, 1.25f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomW1_EastCourtyard(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Room M (x=76..164, y=0..7)
            AddSolidBox(colRoot, "Floor_M_West", 76f, 115f, -1f, 0f);
            AddOneWayBox(colRoot, "OneWay_M_WellBridge", 115f, 121f, 0f);
            AddSolidBox(colRoot, "Floor_M_East", 121f, 164f, -1f, 0f);
            AddOneWayBox(colRoot, "OneWay_M_Perch1", 88f, 96f, 3.5f);
            AddOneWayBox(colRoot, "OneWay_M_Perch2", 134f, 142f, 3.5f);

            // Room N (x=76..164, y=8..22.5)
            AddSolidBox(colRoot, "Floor_N_West", 76f, 101f, 7f, 8f);
            AddOneWayBox(colRoot, "OneWay_N_WellCap", 101f, 107f, 8f);
            AddSolidBox(colRoot, "Floor_N_East", 107f, 164f, 7f, 8f);
            AddOneWayBox(colRoot, "OneWay_N_Mid1", 86f, 96f, 12.5f);
            AddOneWayBox(colRoot, "OneWay_N_Mid2", 116f, 128f, 12.5f);
            AddSolidBox(colRoot, "Ceiling_N", 76f, 164.5f, 22.5f, 23f);

            // Walls & Headers
            AddSolidBox(colRoot, "Wall_MN_East", 164f, 164.5f, -1f, 22.5f);
            AddSolidBox(colRoot, "Wall_N_WestUpper_76", 76f, 76.5f, 15f, 22.5f);
            AddSolidBox(colRoot, "Wall_M_VaultHeader_150", 149.75f, 150.25f, 4f, 7f);

            // Climbable Chain I7 (x=104, y=-0.5..8.5)
            AddClimbableTrigger(root, "I7_Chain_MN", new Vector2(104f, 3.94f), new Vector2(0.5f, 8.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 76f, 164f, 0f, 7f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 76f, 164f, 8f, 22.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 5, 118, 126, 14, 19);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 9, 97, 102, 2, 6);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 10, 138, 144, 18, 22);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "M", 76f, 164f, 0f, 7f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "N", 76f, 164f, 8f, 22.5f);

            // Solid floors (NO sub-floor dirt skirt under Floor_M because Room O sits directly below at y=-16..-1)
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 76f, 115f, -1f, 0f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 76f, 115f, -1f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 121f, 164f, -1f, 0f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 121f, 164f, -1f);

            PaintPrisonSolidFloor(solidTm, rimTm, pal, 76f, 101f, 7f, 8f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 76f, 101f, 7f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 107f, 164f, 7f, 8f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 107f, 164f, 7f);

            PaintPrisonCeiling(solidTm, rimTm, pal, 76f, 164f, 22.5f, 23f);

            // Outer east bedrock (seals Wall_MN_East [164..164.5] and outer east margin [164..168] seamlessly)
            PaintVisualDirtSkirt(solidTm, pal, 164, 168, -1, 23);

            // Wall_N_WestUpper_76 [76, 76.5]x[15, 22.5]: cy=22 belongs to Ceiling_N (76, 22), so tile cy=15..22 + seamless beam
            PaintPrisonOuterWall(solidTm, pal, 76f, 76.5f, 15f, 22f);
            AddSolidBeamSprite(artRoot, "Beam_N_WestUpper_76", pal.DirtSprite, mat, 76.25f, 15f, 22.5f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_M_VaultHeader_150", 150f, 4f, 7f);
            AddInterTierSlabStrata(artRoot, pal, mat, "W1_MtoN_West", 76f, 101f, 7.45f);
            AddInterTierSlabStrata(artRoot, pal, mat, "W1_MtoN_East", 107f, 164f, 7.45f);

            // 7 Plank Rows + well edge corner braces
            AddPrisonPlankRow(artRoot, "Art_M_WellBridgePlanks", pal, mat, 115f, 121f, 0f, -2);
            AddPlatformCornerBrace(artRoot, "Art_M_WellBridgeBraceL", pal.WoodenCornerLeft, mat, 115f, 0f, true);
            AddPlatformCornerBrace(artRoot, "Art_M_WellBridgeBraceR", pal.WoodenCornerRight, mat, 121f, 0f, false);
            AddPrisonPlankRow(artRoot, "Art_M_Perch1Planks", pal, mat, 88f, 96f, 3.5f, -2);
            AddPrisonPlankRow(artRoot, "Art_M_Perch2Planks", pal, mat, 134f, 142f, 3.5f, -2);

            AddPrisonPlankRow(artRoot, "Art_N_WellCapPlanks", pal, mat, 101f, 107f, 8f, -2);
            AddPlatformCornerBrace(artRoot, "Art_N_WellCapBraceL", pal.WoodenCornerLeft, mat, 101f, 8f, true);
            AddPlatformCornerBrace(artRoot, "Art_N_WellCapBraceR", pal.WoodenCornerRight, mat, 107f, 8f, false);
            AddPrisonPlankRow(artRoot, "Art_N_Mid1Planks", pal, mat, 86f, 96f, 12.5f, -2);
            AddPrisonPlankRow(artRoot, "Art_N_Mid2Planks", pal, mat, 116f, 128f, 12.5f, -2);

            // Chain I7 art
            AddPrisonChainArt(artRoot, "Art_I7_Chain", pal, mat, 104f, 0f, 8f);

            // Corner beams, ceiling stamps, moss & webs (open west doorways at x=76, solid east wall at x=164; Room M ceiling has open N shaft [101, 107])
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "M", 76f, 164f, 7f, 0f, isTallRoom: false, hasLeftBottomWall: false, hasRightBottomWall: true, shaftMinX: 101f, shaftMaxX: 107f);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "N", 76f, 164f, 22.5f, 8f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: true);

            // Vertical Room Banner (Room N, clear of Bay #5 [118..126, 14..19] & Pilaster #1 [108.5])
            AddVerticalRoomBanner(artRoot, "Art_N_Banner", pal, mat, 115.5f, 21.8f, -12);

            // Room M Hero Landmarks
            AddGroundedPrisonSprite(artRoot, "Art_M_Dummy", pal.Dummy0, mat, 81.5f, 0f, 1.0f, -11, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_M_ArmoryDome1", pal.AlcovePillar, mat, 109.2f, 0f, 0.85f, -14, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_M_ArmoryCorps", pal.ArcheSmall, mat, 127.5f, 0f, 1.0f, -14, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_M_WeaponDeco", pal.SpawnerJunk, mat, 152.5f, 0f, 0.95f, -13, Color.white);

            // Room N Hero Landmarks
            AddGroundedPrisonSprite(artRoot, "Art_N_Alcove1", pal.PrisonBigAlcove, mat, 84f, 8f, 1.0f, -14, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_N_Alcove2", pal.PrisonBigAlcove, mat, 148.5f, 8f, 1.0f, -14, Color.white);
            AddScaledCeilingProp(artRoot, "Art_N_Cage1", pal.SuspendedCage, mat, 113f, 22.5f, 1.0f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_N_Cage2", pal.SuspendedCage, mat, 146.5f, 22.5f, 1.0f, -7, Color.white);

            // Phase 2 (P1-02 & P1-03): Room M & N Windows with GodRays, Buttress Piers & Storytelling Clusters
            AddWindowWithGodray(artRoot, "Art_M_Window1", mat, unlitMat, pal, new Vector2(80.5f, 4.2f));
            AddWindowWithGodray(artRoot, "Art_M_Window2", mat, unlitMat, pal, new Vector2(131.0f, 4.2f));
            AddWindowWithGodray(artRoot, "Art_M_Window3", mat, unlitMat, pal, new Vector2(158.0f, 4.2f));
            AddGroundedPrisonSprite(artRoot, "Art_M_Bed1", pal.WallBed, mat, 88.5f, 0f, 0.90f, -11, Color.white);
            AddShelfWithFioles(artRoot, "Art_M_Shelf1", pal, mat, 92.5f, 1.4f, -11);
            AddGroundedPrisonSprite(artRoot, "Art_M_Bed2", pal.WallBed, mat, 138.0f, 0f, 0.90f, -11, Color.white);
            AddFloorJunkCluster(artRoot, "Art_M_Junk1", pal, mat, 78.8f, 0f, 1);
            AddFloorJunkCluster(artRoot, "Art_M_Junk2", pal, mat, 131.0f, 0f, 4);
            AddFloorJunkCluster(artRoot, "Art_M_JunkWest2", pal, mat, 92.0f, 0f, 3);
            AddFloorJunkCluster(artRoot, "Art_M_JunkMid", pal, mat, 101.2f, 0f, 6);
            AddFloorJunkCluster(artRoot, "Art_M_JunkMid2", pal, mat, 113.4f, 0f, 8);
            AddFloorJunkCluster(artRoot, "Art_M_JunkEast2", pal, mat, 134.0f, 0f, 2);
            AddFloorJunkCluster(artRoot, "Art_M_JunkEast3", pal, mat, 141.0f, 0f, 9);

            AddWindowWithGodray(artRoot, "Art_N_Window1", mat, unlitMat, pal, new Vector2(80.5f, 18.2f));
            AddWindowWithGodray(artRoot, "Art_N_Window2", mat, unlitMat, pal, new Vector2(99.5f, 18.2f));
            AddWindowWithGodray(artRoot, "Art_N_Window3", mat, unlitMat, pal, new Vector2(158.0f, 18.2f));
            AddFullHeightStonePilaster(artRoot, "Art_N_Pillar1", pal, mat, 97.62f, 8f, 22.5f);
            AddFullHeightStonePilaster(artRoot, "Art_N_Pillar2", pal, mat, 131.5f, 8f, 22.5f);
            AddPrisonSpriteProp(artRoot, "Art_N_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(91.0f, 16.8f), 0.82f, -12, Color.white);
            AddShelfWithFioles(artRoot, "Art_N_Shelf1", pal, mat, 113.0f, 10.6f, -11);
            AddFloorJunkCluster(artRoot, "Art_N_Junk1", pal, mat, 78.5f, 8f, 2);
            AddFloorJunkCluster(artRoot, "Art_N_Junk2", pal, mat, 129.0f, 8f, 5);
            AddFloorJunkCluster(artRoot, "Art_N_JunkWest2", pal, mat, 89.2f, 8f, 1);
            AddFloorJunkCluster(artRoot, "Art_N_JunkMid1", pal, mat, 93.0f, 8f, 4);
            AddFloorJunkCluster(artRoot, "Art_N_JunkShaftEdge", pal, mat, 111.2f, 8f, 5);
            AddFloorJunkCluster(artRoot, "Art_N_JunkMid2", pal, mat, 115.5f, 8f, 3);
            AddFloorJunkCluster(artRoot, "Art_N_JunkMid3", pal, mat, 122.5f, 8f, 8);
            AddFloorJunkCluster(artRoot, "Art_N_JunkEast1", pal, mat, 142.5f, 8f, 2);
            AddFloorJunkCluster(artRoot, "Art_N_JunkEast2", pal, mat, 145.0f, 8f, 5);
            AddFloorJunkCluster(artRoot, "Art_N_JunkEast3", pal, mat, 139.0f, 8f, 9);

            // Phase 3 (P2-04): Full-height stone pilasters, ornate iron cage, anchored chained wall silhouettes & floor-lip foliage
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddAnchoredChainedPrisoner(artRoot, "Art_M_Chained1", pal.ChainedPrisoners[1], mat, pal, new Vector2(114.8f, 3.8f), 7f);
            AddGroundedPrisonSprite(artRoot, "Art_M_Foliage1", pal.GroundFoliage[1], mat, 111.5f, 0f, 0.85f, -2, foliageTint);
            AddFullHeightStonePilaster(artRoot, "Art_N_Truss1", pal, mat, 108.5f, 8f, 22.5f);
            AddFullHeightStonePilaster(artRoot, "Art_N_Truss2", pal, mat, 135.5f, 8f, 22.5f);
            AddScaledCeilingProp(artRoot, "Art_N_OrnateCage1", pal.OrnateCages[0], mat, 95.5f, 22.5f, 0.90f, -7, Color.white);
            AddAnchoredChainedPrisoner(artRoot, "Art_N_Chained1", pal.ChainedPrisoners[0], mat, pal, new Vector2(128.0f, 17.5f), 22.5f);

            // Visual 24-PPU lore stand & warm accent light for I19_LoreTablet_NorthGallery at (152.5, 8)
            AddGroundedPrisonSprite(artRoot, "Art_N_LoreTabletProp", pal.TableauNoir, mat, 152.5f, 8f, 0.85f, -12, Color.white);
            var nLoreLightGo = CreateChild(artRoot, "Art_N_LoreLight");
            nLoreLightGo.transform.position = new Vector3(152.5f, 10.2f, 0f);
            var nLoreLight = nLoreLightGo.AddComponent<Light2D>();
            nLoreLight.lightType = Light2D.LightType.Point;
            nLoreLight.color = new Color(1f, 0.85f, 0.56f, 1f);
            nLoreLight.intensity = 0.65f;
            nLoreLight.pointLightInnerRadius = 0.5f;
            nLoreLight.pointLightOuterRadius = 3.2f;
            nLoreLight.falloffIntensity = 0.65f;
            ConfigureGameplayPointLight(nLoreLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);

            // Lighting: Room M (5 floor beacons @ floorY=0f, r=9.5f) & Room N (5 floor + 4 ceiling @ r=10.0f)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            var deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f);

            float[] mFloorXs = { 84.5f, 96.2f, 123.0f, 146.5f, 162.8f };
            for (int i = 0; i < mFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_M_Torch_{i}", mat, mFloorXs[i], 0f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] nFloorXs = { 84f, 102f, 120f, 138f, 156f };
            for (int i = 0; i < nFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_N_Torch_{i}", mat, nFloorXs[i], 8f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] nCeilXs = { 90f, 112f, 133.2f, 154f };
            for (int i = 0; i < nCeilXs.Length; i++)
                AddCeilingLanternBeacon(artRoot, $"Art_N_Lantern_{i}", mat, nCeilXs[i], 22.5f, (i % 2 == 0) ? warmGold : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomW2_SewerCistern(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Rooms O (x=76..164, y=-16..-1) & P (x=164..234, y=-16..-1.5)
            AddSolidBox(colRoot, "Floor_O", 76f, 164f, -17f, -16f);
            AddSolidBox(colRoot, "Floor_P", 164f, 234f, -17f, -16f);
            AddSolidBox(colRoot, "Ceiling_P", 164f, 234.5f, -1.5f, -1f);

            AddOneWayBox(colRoot, "OneWay_O_ShaftStep1", 112f, 124f, -11f);
            AddOneWayBox(colRoot, "OneWay_O_ShaftStep2", 112f, 124f, -5.5f);
            AddOneWayBox(colRoot, "OneWay_O_East", 138f, 150f, -11.5f);
            AddOneWayBox(colRoot, "OneWay_P_Mid", 184f, 198f, -11.5f);

            AddSolidBox(colRoot, "Wall_O_West", 75.5f, 76f, -17f, -1f);
            AddSolidBox(colRoot, "Wall_OP_Header_164", 163.75f, 164.25f, -12f, -1f);
            AddSolidBox(colRoot, "Wall_P_SewerHeader_212", 211.75f, 212.25f, -12f, -1.5f);
            AddSolidBox(colRoot, "Wall_P_East", 234f, 234.5f, -17f, -1.5f);

            // Climbable Chain I8 (x=118, y=-16.5..0.5)
            AddClimbableTrigger(root, "I8_Chain_MO", new Vector2(118f, -8.06f), new Vector2(0.5f, 16.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 76f, 164f, -16f, -1f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 164f, 234f, -16f, -1.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 6, 100, 106, -13, -8);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 11, 140, 148, -9, -3);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 12, 187, 195, -9, -3);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "O", 76f, 164f, -16f, -1f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "P", 164f, 234f, -16f, -1.5f);

            // Solid floors, sub-floor dirt skirt (y=-22..-17), ceiling P, bedrock above Ceiling_P (y=-1..7), outer walls & interior headers
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 76f, 164f, -17f, -16f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 164f, 234f, -17f, -16f);
            PaintVisualDirtSkirt(solidTm, pal, 75, 238, -22, -17);
            PaintPrisonCeiling(solidTm, rimTm, pal, 164f, 234f, -1.5f, -1f);
            PaintVisualDirtSkirt(solidTm, pal, 164, 238, -1, 7);

            PaintPrisonOuterWall(solidTm, pal, 75.5f, 76f, -17f, -1f);
            // Outer east bedrock (seals Wall_P_East [234..234.5] and outer east margin [234..238] flush with top/bottom skirts)
            PaintVisualDirtSkirt(solidTm, pal, 234, 238, -17, -1);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_OP_Header_164", 164f, -12f, -1f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_P_SewerHeader_212", 212f, -12f, -1.5f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W2_Sub", 75, 238, -22, -17);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W2_CeilP", 164, 238, -1, 7);
            AddInterTierSlabStrata(artRoot, pal, mat, "W2_OtoM_West", 76f, 115f, -0.55f);
            AddInterTierSlabStrata(artRoot, pal, mat, "W2_OtoM_East", 121f, 164f, -0.55f);

            // 4 Plank Rows + shaft step corner braces — P0-04 Part A removed Art_O_WestPlanks
            AddPrisonPlankRow(artRoot, "Art_O_ShaftStep1Planks", pal, mat, 112f, 124f, -11f, -2);
            AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep1BraceL", pal.WoodenCornerLeft, mat, 112f, -11f, true);
            AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep1BraceR", pal.WoodenCornerRight, mat, 124f, -11f, false);
            AddPrisonPlankRow(artRoot, "Art_O_ShaftStep2Planks", pal, mat, 112f, 124f, -5.5f, -2);
            AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep2BraceL", pal.WoodenCornerLeft, mat, 112f, -5.5f, true);
            AddPlatformCornerBrace(artRoot, "Art_O_ShaftStep2BraceR", pal.WoodenCornerRight, mat, 124f, -5.5f, false);
            AddPrisonPlankRow(artRoot, "Art_O_EastPlanks", pal, mat, 138f, 150f, -11.5f, -2);
            AddPrisonPlankRow(artRoot, "Art_P_MidPlanks", pal, mat, 184f, 198f, -11.5f, -2);

            // Chain I8 art
            AddPrisonChainArt(artRoot, "Art_I8_Chain", pal, mat, 118f, -16f, 0f);

            // Corner beams, ceiling stamps, moss & webs (O has solid west wall, open east O<->P, and open M<->O shaft [115, 121]; P has open west O<->P, solid east wall)
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "O", 76f, 164f, -1f, -16f, isTallRoom: true, hasLeftBottomWall: true, hasRightBottomWall: false, shaftMinX: 115f, shaftMaxX: 121f);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "P", 164f, 234f, -1.5f, -16f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: true);

            // Vertical Room Banner (Room P)
            AddVerticalRoomBanner(artRoot, "Art_P_Banner", pal, mat, 180f, -2.2f, -12);

            // Room O Hero Landmarks
            AddGroundedPrisonSprite(artRoot, "Art_O_MonsterDoor", pal.MonsterDoorFront, mat, 130.5f, -16f, 0.80f, -14, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_O_CellDoor1", pal.SmallDoor, mat, 90.0f, -16f, 1.0f, -13, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_O_CellDoor2", pal.BigDoor0, mat, 154.0f, -16f, 0.90f, -14, Color.white);

            // Room P Hero Landmarks + Grounded PortalBackOut_0_00 Emissive HDR Glow + Warm Beacon Light2D
            AddGroundedPrisonSprite(artRoot, "Art_P_CorruptedDoor", pal.HingeCorruptedDoor, mat, 174f, -16f, 0.85f, -13, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_P_SewerExitDoor", pal.ExitDoorSewerShort, mat, 224f, -16f, 0.85f, -5, Color.white);

            var exitGlowGo = CreateChild(artRoot, "Art_P_ExitGlow");
            const float exitGlowScale = 0.55f;
            exitGlowGo.transform.position = new Vector3(224f, -16f - pal.PortalBackOut.bounds.min.y * exitGlowScale, 0f);
            exitGlowGo.transform.localScale = new Vector3(exitGlowScale, exitGlowScale, 1f);
            var exitGlowSr = exitGlowGo.AddComponent<SpriteRenderer>();
            exitGlowSr.sprite = pal.PortalBackOut;
            exitGlowSr.sharedMaterial = pal.MatEmissiveExit;
            exitGlowSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            exitGlowSr.sortingOrder = -4;
            exitGlowSr.color = new Color(1f, 0.92f, 0.65f, 0.45f);
            var exitGlowLight = exitGlowGo.AddComponent<Light2D>();
            exitGlowLight.lightType = Light2D.LightType.Point;
            exitGlowLight.color = new Color(1f, 0.92f, 0.65f, 1f);
            exitGlowLight.intensity = 1.40f;
            exitGlowLight.pointLightOuterRadius = 7.2f;
            exitGlowLight.pointLightInnerRadius = 1.4f;
            ConfigureGameplayPointLight(exitGlowLight, enableVolumetric: true, volumeIntensity: 0.09f, addFlicker: true, phaseSeed: 224f);

            // Lighting: Room O (5 floor + 4 ceiling @ r=10.0f) & Room P (4 floor + 3 ceiling @ r=10.0f + Art_P_ExitGlow)
            // Phase 2 (P1-02 & P1-03): Room O & P Windows with GodRays, Piers, Arches, Cages & Storytelling Clusters
            AddWindowWithGodray(artRoot, "Art_O_Window1", mat, unlitMat, pal, new Vector2(86.0f, -6.5f));
            AddWindowWithGodray(artRoot, "Art_O_Window2", mat, unlitMat, pal, new Vector2(150.5f, -6.5f));
            AddFullHeightStonePilaster(artRoot, "Art_O_Pillar1", pal, mat, 95.8f, -16f, -1f);
            AddScaledCeilingProp(artRoot, "Art_O_Cage1", pal.SuspendedCage, mat, 84.0f, -1f, 1.0f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_O_Cage2", pal.SuspendedCage, mat, 126.0f, -1f, 1.0f, -7, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_O_Bed1", pal.WallBed, mat, 84.5f, -16f, 0.90f, -11, Color.white);
            AddShelfWithFioles(artRoot, "Art_O_Shelf1", pal, mat, 143.5f, -13.5f, -11);
            AddFloorJunkCluster(artRoot, "Art_O_Junk1", pal, mat, 92.6f, -16f, 3);
            AddFloorJunkCluster(artRoot, "Art_O_JunkMid1", pal, mat, 101.0f, -16f, 2);
            AddFloorJunkCluster(artRoot, "Art_O_JunkMid3", pal, mat, 104.5f, -16f, 5);
            AddFloorJunkCluster(artRoot, "Art_O_JunkMid2", pal, mat, 116.5f, -16f, 8);
            AddFloorJunkCluster(artRoot, "Art_O_JunkEast4", pal, mat, 138.2f, -16f, 1);
            AddFloorJunkCluster(artRoot, "Art_O_JunkEast2", pal, mat, 143.5f, -16f, 4);
            AddFloorJunkCluster(artRoot, "Art_O_Junk2", pal, mat, 149.0f, -16f, 6);
            AddFloorJunkCluster(artRoot, "Art_O_JunkEast3", pal, mat, 160.0f, -16f, 7);

            AddWindowWithGodray(artRoot, "Art_P_Window1", mat, unlitMat, pal, new Vector2(173.5f, -6.5f));
            AddWindowWithGodray(artRoot, "Art_P_Window2", mat, unlitMat, pal, new Vector2(207.0f, -6.5f));
            AddGroundedPrisonSprite(artRoot, "Art_P_Arch1", pal.ArcheSmall, mat, 207.0f, -16f, 0.90f, -14, Color.white);
            AddScaledCeilingProp(artRoot, "Art_P_Cage1", pal.SuspendedCage, mat, 186.0f, -1.5f, 1.0f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_P_Cage2", pal.SuspendedCage, mat, 206.0f, -1.5f, 1.0f, -7, Color.white);
            AddShelfWithFioles(artRoot, "Art_P_Shelf1", pal, mat, 189.5f, -13.5f, -11);
            AddFloorJunkCluster(artRoot, "Art_P_JunkWest2", pal, mat, 168.0f, -16f, 3);
            AddFloorJunkCluster(artRoot, "Art_P_Junk1", pal, mat, 176.5f, -16f, 1);
            AddFloorJunkCluster(artRoot, "Art_P_JunkMid1", pal, mat, 188.5f, -16f, 6);
            AddFloorJunkCluster(artRoot, "Art_P_JunkMid2", pal, mat, 195.0f, -16f, 9);
            AddFloorJunkCluster(artRoot, "Art_P_Junk2", pal, mat, 213.5f, -16f, 8);
            AddFloorJunkCluster(artRoot, "Art_P_JunkEast2", pal, mat, 231.5f, -16f, 2);
            AddFloorJunkCluster(artRoot, "Art_P_JunkEast3", pal, mat, 229.0f, -16f, 5);

            // Phase 3 (P2-04): Full-height stone pilasters, ornate iron cages, anchored chained silhouettes & damp sewer floor-lip foliage
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddFullHeightStonePilaster(artRoot, "Art_O_Truss1", pal, mat, 110.5f, -16f, -1f);
            AddScaledCeilingProp(artRoot, "Art_O_OrnateCage1", pal.OrnateCages[2], mat, 88.0f, -1f, 0.90f, -7, Color.white);
            AddAnchoredChainedPrisoner(artRoot, "Art_O_Chained1", pal.ChainedPrisoners[2], mat, pal, new Vector2(157.5f, -7.2f), -1f);
            AddGroundedPrisonSprite(artRoot, "Art_O_Foliage1", pal.GroundFoliage[0], mat, 77.8f, -16f, 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_O_Foliage2", pal.GroundFoliage[1], mat, 122.5f, -16f, 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_O_Foliage3", pal.GroundFoliage[0], mat, 138.5f, -16f, 0.85f, -2, foliageTint);

            AddFullHeightStonePilaster(artRoot, "Art_P_Truss1", pal, mat, 199.8f, -16f, -1.5f);
            AddScaledCeilingProp(artRoot, "Art_P_OrnateCage1", pal.OrnateCages[1], mat, 176.0f, -1.5f, 0.90f, -7, Color.white);
            AddAnchoredChainedPrisoner(artRoot, "Art_P_Chained1", pal.ChainedPrisoners[0], mat, pal, new Vector2(182.0f, -7.2f), -1.5f);
            AddGroundedPrisonSprite(artRoot, "Art_P_Foliage1", pal.GroundFoliage[1], mat, 180.5f, -16f, 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_P_Foliage2", pal.GroundFoliage[0], mat, 203.2f, -16f, 0.85f, -2, foliageTint);

            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            var deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f);

            float[] oFloorXs = { 81.5f, 108.5f, 121.5f, 136.8f, 158.5f };
            for (int i = 0; i < oFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_O_Torch_{i}", mat, oFloorXs[i], -16f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] oCeilXs = { 90f, 113f, 134f, 154f };
            for (int i = 0; i < oCeilXs.Length; i++)
                AddCeilingLanternBeacon(artRoot, $"Art_O_Lantern_{i}", mat, oCeilXs[i], -1f, (i % 2 == 0) ? warmGold : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] pFloorXs = { 169.5f, 182.0f, 202.0f, 218.0f };
            for (int i = 0; i < pFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_P_Torch_{i}", mat, pFloorXs[i], -16f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] pCeilXs = { 178f, 198f, 218f };
            for (int i = 0; i < pCeilXs.Length; i++)
                AddCeilingLanternBeacon(artRoot, $"Art_P_Lantern_{i}", mat, pCeilXs[i], -1.5f, (i % 2 == 0) ? warmGold : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomW3_WestBastion(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Room Q (x=-102..-30, y=16..23)
            AddSolidBox(colRoot, "Floor_Q", -102f, -30f, 15f, 16f);
            AddOneWayBox(colRoot, "OneWay_Q_Mid", -88f, -76f, 19.5f);

            // Room R (x=-102..-30, y=24..38.5)
            AddSolidBox(colRoot, "Floor_R_West", -102f, -69f, 23f, 24f);
            AddOneWayBox(colRoot, "OneWay_R_WellCap", -69f, -63f, 24f);
            AddSolidBox(colRoot, "Floor_R_East", -63f, -30f, 23f, 24f);
            AddOneWayBox(colRoot, "OneWay_R_Mid1", -54f, -42f, 28.5f);
            AddOneWayBox(colRoot, "OneWay_R_Mid2", -82f, -70f, 28.5f);
            AddSolidBox(colRoot, "Ceiling_R", -102.5f, -30f, 38.5f, 39f);

            // Walls & Headers
            AddSolidBox(colRoot, "Wall_QR_West", -102.5f, -102f, 15f, 38.5f);
            AddSolidBox(colRoot, "Wall_R_EastUpper_Neg30", -30.5f, -30f, 31f, 38.5f);
            AddSolidBox(colRoot, "Wall_R_ArmoryHeader_Neg88", -88.25f, -87.75f, 28f, 38.5f);

            // Climbable Chain I9 (x=-66, y=15.5..24.5)
            AddClimbableTrigger(root, "I9_Chain_QR", new Vector2(-66f, 19.94f), new Vector2(0.5f, 8.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, -102f, -30f, 16f, 23f);
            PaintPrisonBackWall(bgTm, decalTm, pal, -102f, -30f, 24f, 38.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 7, -60, -54, 30, 35);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 13, -75, -69, 18, 22);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "Q", -102f, -30f, 16f, 23f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "R", -102f, -30f, 24f, 38.5f);

            // Solid floors, 8-row sub-floor dirt skirt under Floor_Q (y=7..15), ceilings, outer walls & interior header
            PaintPrisonSolidFloor(solidTm, rimTm, pal, -102f, -30f, 15f, 16f);
            PaintVisualDirtSkirt(solidTm, pal, -106, -30, 7, 15);

            PaintPrisonSolidFloor(solidTm, rimTm, pal, -102f, -69f, 23f, 24f);
            PaintPrisonCeilingRimOnly(rimTm, pal, -102f, -69f, 23f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, -63f, -30f, 23f, 24f);
            PaintPrisonCeilingRimOnly(rimTm, pal, -63f, -30f, 23f);

            PaintPrisonCeiling(solidTm, rimTm, pal, -102f, -30f, 38.5f, 39f);
            // Outer west bedrock (seals Wall_QR_West [-102.5..-102] and outer west margin [-106..-102] flush with bottom skirt)
            PaintVisualDirtSkirt(solidTm, pal, -106, -102, 15, 39);

            // Wall_R_EastUpper_Neg30 [-30.5, -30]x[31, 38.5]: cy=38 belongs to Ceiling_R (-31, 38), so tile cy=31..38 + seamless beam
            PaintPrisonOuterWall(solidTm, pal, -30.5f, -30f, 31f, 38f);
            AddSolidBeamSprite(artRoot, "Beam_R_EastUpper_Neg30", pal.DirtSprite, mat, -30.25f, 31f, 38.5f);
            PaintPrisonInteriorHeader(solidTm, artRoot, mat, pal, "Beam_R_ArmoryHeader_Neg88", -88f, 28f, 38.5f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "W3_Sub", -106, -30, 7, 15);
            AddInterTierSlabStrata(artRoot, pal, mat, "W3_QtoR_West", -102f, -69f, 23.45f);
            AddInterTierSlabStrata(artRoot, pal, mat, "W3_QtoR_East", -63f, -30f, 23.45f);

            // 4 Plank Rows + well cap corner braces
            AddPrisonPlankRow(artRoot, "Art_Q_MidPlanks", pal, mat, -88f, -76f, 19.5f, -2);
            AddPrisonPlankRow(artRoot, "Art_R_WellCapPlanks", pal, mat, -69f, -63f, 24f, -2);
            AddPlatformCornerBrace(artRoot, "Art_R_WellCapBraceL", pal.WoodenCornerLeft, mat, -69f, 24f, true);
            AddPlatformCornerBrace(artRoot, "Art_R_WellCapBraceR", pal.WoodenCornerRight, mat, -63f, 24f, false);
            AddPrisonPlankRow(artRoot, "Art_R_Mid1Planks", pal, mat, -54f, -42f, 28.5f, -2);
            AddPrisonPlankRow(artRoot, "Art_R_Mid2Planks", pal, mat, -82f, -70f, 28.5f, -2);

            // Chain I9 art
            AddPrisonChainArt(artRoot, "Art_I9_Chain", pal, mat, -66f, 16f, 24f);

            // Corner beams, ceiling stamps, moss & webs (solid west wall at x=-102, open east doorways at x=-30; Room Q ceiling has open Q<->R shaft [-69, -63])
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "Q", -102f, -30f, 23f, 16f, isTallRoom: false, hasLeftBottomWall: true, hasRightBottomWall: false, shaftMinX: -69f, shaftMaxX: -63f);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "R", -102f, -30f, 38.5f, 24f, isTallRoom: true, hasLeftBottomWall: true, hasRightBottomWall: false);

            // Vertical Room Banner (Room R, clear of Bay #7 [-60..-54, 30..35] & Window1 [-73.5])
            AddVerticalRoomBanner(artRoot, "Art_R_Banner", pal, mat, -70.5f, 37.8f, -12);

            // Room Q Hero Landmarks
            AddGroundedPrisonSprite(artRoot, "Art_Q_JudasSeat", pal.JudasSeat, mat, -38.0f, 16f, 0.85f, -11, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_Q_Billot", pal.Billot, mat, -61.0f, 16f, 0.85f, -11, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_Q_SkeletonPier", pal.SpawnerPillar, mat, -52.2f, 16f, 0.92f, -12, Color.white);
            var qPierGo = artRoot.transform.Find("Art_Q_SkeletonPier").gameObject;
            var qPierCap = CreateChild(qPierGo, "Art_Q_SkeletonPier_Cap");
            qPierCap.transform.localScale = Vector3.one;
            qPierCap.transform.position = new Vector3(-52.2f, 23f - pal.SpawnerPillar.bounds.max.y * 0.92f, 0f);
            var qPierCapSr = qPierCap.AddComponent<SpriteRenderer>();
            qPierCapSr.sprite = pal.SpawnerPillar;
            qPierCapSr.sharedMaterial = mat;
            qPierCapSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Background");
            qPierCapSr.sortingOrder = -12;
            qPierCapSr.color = Color.white;
            AddGroundedPrisonSprite(artRoot, "Art_Q_SkeletonSpawn", pal.PrisonBigAlcove, mat, -49.0f, 16f, 1.0f, -13, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_Q_SnakeLandmark", pal.ArcheSmall, mat, -91.5f, 16f, 1.0f, -14, Color.white);

            // Room R Hero Landmarks
            AddGroundedPrisonSprite(artRoot, "Art_R_ArmoryDome", pal.PrisonBigAlcove, mat, -60.0f, 24f, 0.90f, -14, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_R_BigSword", pal.BigSword, mat, new Vector2(-45.5f, 34.4f), 0.80f, -13, Color.white);
            AddScaledCeilingProp(artRoot, "Art_R_Cage", pal.SuspendedCage, mat, -57.0f, 38.5f, 1.0f, -7, Color.white);

            // Phase 2 (P1-02 & P1-03): Room Q & R Windows with GodRays, Banners, Piers & Storytelling Clusters
            AddWindowWithGodray(artRoot, "Art_Q_Window1", mat, unlitMat, pal, new Vector2(-63.5f, 20.4f));
            AddWindowWithGodray(artRoot, "Art_Q_Window2", mat, unlitMat, pal, new Vector2(-42.5f, 20.4f));
            AddShelfWithFioles(artRoot, "Art_Q_Shelf1", pal, mat, -84.5f, 17.3f, -11);
            AddGroundedPrisonSprite(artRoot, "Art_Q_Bed1", pal.WallBed, mat, -80.0f, 16f, 0.85f, -11, Color.white);
            AddFloorJunkCluster(artRoot, "Art_Q_Junk1", pal, mat, -86.5f, 16f, 2);
            AddFloorJunkCluster(artRoot, "Art_Q_JunkMid1", pal, mat, -73.0f, 16f, 4);
            AddFloorJunkCluster(artRoot, "Art_Q_JunkMid2", pal, mat, -65.5f, 16f, 8);
            AddFloorJunkCluster(artRoot, "Art_Q_Junk2", pal, mat, -44.0f, 16f, 7);
            AddFloorJunkCluster(artRoot, "Art_Q_JunkEast2", pal, mat, -36.0f, 16f, 1);

            AddWindowWithGodray(artRoot, "Art_R_Window1", mat, unlitMat, pal, new Vector2(-73.5f, 33.5f));
            AddWindowWithGodray(artRoot, "Art_R_Window2", mat, unlitMat, pal, new Vector2(-33.5f, 33.5f));
            AddPrisonSpriteProp(artRoot, "Art_R_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(-80.0f, 34.0f), 0.82f, -12, Color.white);
            AddFullHeightStonePilaster(artRoot, "Art_R_Pillar1", pal, mat, -38.5f, 24f, 38.5f);
            AddShelfWithFioles(artRoot, "Art_R_Shelf1", pal, mat, -49.0f, 25.8f, -11);
            AddFloorJunkCluster(artRoot, "Art_R_JunkWest2", pal, mat, -98.0f, 24f, 2);
            AddFloorJunkCluster(artRoot, "Art_R_JunkWest3", pal, mat, -90.0f, 24f, 6);
            AddFloorJunkCluster(artRoot, "Art_R_Junk1", pal, mat, -79.2f, 24f, 4);
            AddFloorJunkCluster(artRoot, "Art_R_JunkMid1", pal, mat, -76.5f, 24f, 9);
            AddFloorJunkCluster(artRoot, "Art_R_JunkMid2", pal, mat, -71.5f, 24f, 3);
            AddFloorJunkCluster(artRoot, "Art_R_JunkShaftEdge", pal, mat, -53.8f, 24f, 5);
            AddFloorJunkCluster(artRoot, "Art_R_JunkEast2", pal, mat, -52.5f, 24f, 8);
            AddFloorJunkCluster(artRoot, "Art_R_Junk2", pal, mat, -45.0f, 24f, 9);

            // Phase 3 (P2-04): Full-height stone pilaster, ornate iron cage, anchored chained silhouettes & floor-lip foliage
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddAnchoredChainedPrisoner(artRoot, "Art_Q_Chained1", pal.ChainedPrisoners[1], mat, pal, new Vector2(-58.5f, 19.6f), 23f);
            AddGroundedPrisonSprite(artRoot, "Art_Q_Foliage1", pal.GroundFoliage[1], mat, -57.8f, 16f, 0.85f, -2, foliageTint);
            AddFullHeightStonePilaster(artRoot, "Art_R_Truss1", pal, mat, -84.0f, 24f, 38.5f);
            AddScaledCeilingProp(artRoot, "Art_R_OrnateCage1", pal.OrnateCages[2], mat, -68.2f, 38.5f, 0.90f, -7, Color.white);
            AddAnchoredChainedPrisoner(artRoot, "Art_R_Chained1", pal.ChainedPrisoners[2], mat, pal, new Vector2(-63.8f, 34.0f), 38.5f);

            // Lighting: Room Q (4 floor beacons @ floorY=16f, r=9.5f) & Room R (4 floor + 3 ceiling @ r=10.0f)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            var deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f);

            float[] qFloorXs = { -98.5f, -76.2f, -56.0f, -34.0f };
            for (int i = 0; i < qFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_Q_Torch_{i}", mat, qFloorXs[i], 16f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] rFloorXs = { -90.0f, -76.0f, -55.5f, -35.0f };
            for (int i = 0; i < rFloorXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_R_Torch_{i}", mat, rFloorXs[i], 24f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);

            float[] rCeilXs = { -87f, -66f, -36f };
            for (int i = 0; i < rCeilXs.Length; i++)
                AddCeilingLanternBeacon(artRoot, $"Art_R_Lantern_{i}", mat, rCeilXs[i], 38.5f, (i % 2 == 0) ? warmGold : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        private static void BuildRoomU1_UpperPromenade(GameObject root, Material mat, Material unlitMat, PrisonPalette pal)
        {
            var colRoot = CreateChild(root, "Collision");
            var artRoot = CreateChild(root, "Art");

            // Room S (x=38..92, y=24..39)
            AddSolidBox(colRoot, "Floor_S", 38f, 92f, 23f, 24f);

            // Room T (x=38..140, y=40..62.5)
            AddSolidBox(colRoot, "Floor_T_West", 38f, 77f, 39f, 40f);
            AddOneWayBox(colRoot, "OneWay_T_ShaftCap", 77f, 83f, 40f);
            AddSolidBox(colRoot, "Floor_T_East", 83f, 140f, 39f, 40f);
            AddOneWayBox(colRoot, "OneWay_T_Mid1", 52f, 64f, 44.5f);
            AddOneWayBox(colRoot, "OneWay_T_Mid2", 92f, 106f, 44.5f);
            AddSolidBox(colRoot, "Ceiling_T", 37.5f, 140.5f, 62.5f, 63f);

            // Walls
            AddSolidBox(colRoot, "Wall_S_East", 92f, 92.5f, 23f, 39f);
            AddSolidBox(colRoot, "Wall_T_West", 37.5f, 38f, 39f, 62.5f);
            AddSolidBox(colRoot, "Wall_T_East", 140f, 140.5f, 39f, 62.5f);

            // Climbable Chain I10 (x=80, y=23.5..40.5)
            AddClimbableTrigger(root, "I10_Chain_ST", new Vector2(80f, 31.94f), new Vector2(0.5f, 16.28f));

            // Art: 4 visual-only Tilemaps on native 1.0u grid
            CreateRoomVisualGrid(artRoot, mat, out var bgTm, out var decalTm, out var solidTm, out var rimTm);

            PaintPrisonBackWall(bgTm, decalTm, pal, 38f, 92f, 24f, 39f);
            PaintPrisonBackWall(bgTm, decalTm, pal, 38f, 140f, 40f, 62.5f);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 8, 66, 74, 46, 52);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 14, 47, 55, 34, 38);
            CarveAndFrameArchBay(artRoot, bgTm, decalTm, pal, mat, 15, 96, 104, 47, 55);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "S", 38f, 92f, 24f, 39f);
            AddWeatheredMasonryOverlays(artRoot, bgTm, pal, mat, "T", 38f, 140f, 40f, 62.5f);

            // Solid floors + split non-overlapping bedrock skirts under Floor_S (ZERO tiles inside Room C [42..56, 0..18.5])
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 38f, 92f, 23f, 24f);
            PaintVisualDirtSkirt(solidTm, pal, 38, 42, 14, 23);
            PaintVisualDirtSkirt(solidTm, pal, 42, 56, 19, 23);
            PaintVisualDirtSkirt(solidTm, pal, 56, 76, 15, 23);

            PaintPrisonSolidFloor(solidTm, rimTm, pal, 38f, 77f, 39f, 40f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 38f, 77f, 39f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 83f, 140f, 39f, 40f);
            PaintPrisonCeilingRimOnly(rimTm, pal, 83f, 92f, 39f);

            // Wall_S_East lower portion [23..31] + seamless overhang bedrock [92..144, 31..39] below Floor_T_East
            PaintPrisonOuterWall(solidTm, pal, 92f, 92.5f, 23f, 31f);
            PaintVisualDirtSkirt(solidTm, pal, 92, 144, 31, 39);

            PaintPrisonCeiling(solidTm, rimTm, pal, 38f, 140f, 62.5f, 63f);
            PaintPrisonOuterWall(solidTm, pal, 37.5f, 38f, 39f, 63f);
            PaintPrisonOuterWall(solidTm, pal, 140f, 140.5f, 39f, 63f);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "U1_SubE", 56, 76, 15, 23);
            AddBedrockStrataStamps(artRoot, solidTm, pal, mat, "U1_Overhang", 92, 144, 31, 39);
            AddInterTierSlabStrata(artRoot, pal, mat, "U1_StoT_West", 38f, 77f, 39.45f);
            AddInterTierSlabStrata(artRoot, pal, mat, "U1_StoT_East", 83f, 140f, 39.45f);

            // 3 Plank Rows + platform & shaft cap corner braces — P0-04 Part A removed Art_S_GalleryPlanks/braces and Art_T_HighPlanks/braces
            AddPrisonPlankRow(artRoot, "Art_T_ShaftCapPlanks", pal, mat, 77f, 83f, 40f, -2);
            AddPlatformCornerBrace(artRoot, "Art_T_ShaftCapBraceL", pal.WoodenCornerLeft, mat, 77f, 40f, true);
            AddPlatformCornerBrace(artRoot, "Art_T_ShaftCapBraceR", pal.WoodenCornerRight, mat, 83f, 40f, false);
            AddPrisonPlankRow(artRoot, "Art_T_Mid1Planks", pal, mat, 52f, 64f, 44.5f, -2);
            AddPlatformCornerBrace(artRoot, "Art_T_Mid1BraceL", pal.WoodenCornerLeft, mat, 52f, 44.5f, true);
            AddPlatformCornerBrace(artRoot, "Art_T_Mid1BraceR", pal.WoodenCornerRight, mat, 64f, 44.5f, false);
            AddPrisonPlankRow(artRoot, "Art_T_Mid2Planks", pal, mat, 92f, 106f, 44.5f, -2);
            AddPlatformCornerBrace(artRoot, "Art_T_Mid2BraceL", pal.WoodenCornerLeft, mat, 92f, 44.5f, true);
            AddPlatformCornerBrace(artRoot, "Art_T_Mid2BraceR", pal.WoodenCornerRight, mat, 106f, 44.5f, false);

            // Chain I10 art
            AddPrisonChainArt(artRoot, "Art_I10_Chain", pal, mat, 80f, 24f, 40f);

            // Corner beams, ceiling stamps, moss & webs (S has open west L<->S doorway, solid east wall, and open S<->T shaft [77, 83]; T has solid west & east walls)
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "S", 38f, 92f, 39f, 24f, isTallRoom: true, hasLeftBottomWall: false, hasRightBottomWall: true, shaftMinX: 77f, shaftMaxX: 83f);
            AddCornerBeamsAndWebs(artRoot, rimTm, mat, pal, "T", 38f, 140f, 62.5f, 40f, isTallRoom: true, hasLeftBottomWall: true, hasRightBottomWall: true);

            // Vertical Room Banner (Room T, clear of Bay #8 [66..74, 46..52])
            AddVerticalRoomBanner(artRoot, "Art_T_Banner", pal, mat, 88f, 61.8f, -12);

            // Room S Hero Landmarks (Lore Room)
            AddGroundedPrisonSprite(artRoot, "Art_S_AlchemyBibli1", pal.AlchemyBibli, mat, 46.0f, 24f, 0.80f, -14, Color.white);
            AddAlchemyBibliCeilingChain(artRoot, "Art_S_AlchemyBibli1", pal, mat, 46.0f, 24f, 0.80f, 39f);
            AddGroundedPrisonSprite(artRoot, "Art_S_AlchemyBibli2", pal.AlchemyBibli, mat, 62.5f, 24f, 0.80f, -14, Color.white);
            AddAlchemyBibliCeilingChain(artRoot, "Art_S_AlchemyBibli2", pal, mat, 62.5f, 24f, 0.80f, 39f);
            AddGroundedPrisonSprite(artRoot, "Art_S_AspectsDesk", pal.AspectsNpcStuffs, mat, 73.0f, 24f, 0.82f, -11, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_S_TableauNoir", pal.TableauNoir, mat, new Vector2(73.0f, 29.8f), 0.82f, -13, Color.white);

            // Room T Hero Landmarks (High Bastion)
            AddGroundedPrisonSprite(artRoot, "Art_T_BigAlcove", pal.PrisonBigAlcove, mat, 44.5f, 40f, 1.0f, -14, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_T_Pillar1", pal.AlcovePillar, mat, 88.0f, 40f, 0.90f, -13, Color.white);
            AddFullHeightStonePilaster(artRoot, "Art_T_Pillar2", pal, mat, 109.75f, 40f, 62.5f);
            AddGroundedPrisonSprite(artRoot, "Art_T_CourtyardArch", pal.ExitDoorPrisonCourtyard, mat, 126.0f, 40f, 0.80f, -13, Color.white);

            // P0-04 Part B: the courtyard arch is now a live Promenade biome exit — warm golden emissive glow + beacon in front of it
            var promenadeGlowGo = CreateChild(artRoot, "Art_T_ExitGlow");
            const float promenadeGlowScale = 0.55f;
            promenadeGlowGo.transform.position = new Vector3(126f, 40f - pal.PortalBackOut.bounds.min.y * promenadeGlowScale, 0f);
            promenadeGlowGo.transform.localScale = new Vector3(promenadeGlowScale, promenadeGlowScale, 1f);
            var promenadeGlowSr = promenadeGlowGo.AddComponent<SpriteRenderer>();
            promenadeGlowSr.sprite = pal.PortalBackOut;
            promenadeGlowSr.sharedMaterial = pal.MatEmissiveExit;
            promenadeGlowSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            promenadeGlowSr.sortingOrder = -4;
            promenadeGlowSr.color = new Color(1f, 0.92f, 0.65f, 0.45f);
            var promenadeGlowLight = promenadeGlowGo.AddComponent<Light2D>();
            promenadeGlowLight.lightType = Light2D.LightType.Point;
            promenadeGlowLight.color = new Color(1f, 0.92f, 0.65f, 1f);
            promenadeGlowLight.intensity = 1.40f;
            promenadeGlowLight.pointLightOuterRadius = 7.2f;
            promenadeGlowLight.pointLightInnerRadius = 1.4f;
            ConfigureGameplayPointLight(promenadeGlowLight, enableVolumetric: true, volumeIntensity: 0.09f, addFlicker: true, phaseSeed: 126f);

            // Phase 2 (P1-02 & P1-03): Room S & T Windows with GodRays, Banners, Arches, Cages, Shelves & Storytelling Clusters
            AddWindowWithGodray(artRoot, "Art_S_Window1", mat, unlitMat, pal, new Vector2(60.0f, 35.2f));
            AddWindowWithGodray(artRoot, "Art_S_Window2", mat, unlitMat, pal, new Vector2(89.5f, 34.0f));
            AddPrisonSpriteProp(artRoot, "Art_S_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(84.8f, 34.0f), 0.82f, -12, Color.white);
            AddShelfWithFioles(artRoot, "Art_S_Shelf1", pal, mat, 86.5f, 26.2f, -11);
            AddShelfWithFioles(artRoot, "Art_S_Shelf2", pal, mat, 89.5f, 26.2f, -11);
            AddScaledCeilingProp(artRoot, "Art_S_Cage1", pal.SuspendedCage, mat, 58.5f, 39f, 1.0f, -7, Color.white);
            AddFloorJunkCluster(artRoot, "Art_S_Junk1", pal, mat, 53.4f, 24f, 3);
            AddFloorJunkCluster(artRoot, "Art_S_JunkMid1", pal, mat, 78.0f, 24f, 1);
            AddFloorJunkCluster(artRoot, "Art_S_JunkMid2", pal, mat, 80.5f, 24f, 4);
            AddFloorJunkCluster(artRoot, "Art_S_JunkMid3", pal, mat, 88.2f, 24f, 7);
            AddFloorJunkCluster(artRoot, "Art_S_Junk2", pal, mat, 82.6f, 24f, 6);
            AddFloorJunkCluster(artRoot, "Art_S_JunkEast2", pal, mat, 85.0f, 24f, 9);

            AddWindowWithGodray(artRoot, "Art_T_Window1", mat, unlitMat, pal, new Vector2(55.0f, 56.5f));
            AddWindowWithGodray(artRoot, "Art_T_Window2", mat, unlitMat, pal, new Vector2(82.0f, 56.5f));
            AddWindowWithGodray(artRoot, "Art_T_Window3", mat, unlitMat, pal, new Vector2(116.0f, 56.5f));
            AddWindowWithGodray(artRoot, "Art_T_Window4", mat, unlitMat, pal, new Vector2(135.5f, 56.5f));
            AddPrisonSpriteProp(artRoot, "Art_T_BannerBig1", pal.BannerBig, pal.MatBannerSway, new Vector2(58.5f, 55.5f), 0.82f, -12, Color.white);
            AddPrisonSpriteProp(artRoot, "Art_T_BannerBig2", pal.BannerBig, pal.MatBannerSway, new Vector2(123.0f, 55.5f), 0.82f, -12, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_T_Arch1", pal.ArcheSmall, mat, 71.8f, 40f, 0.90f, -14, Color.white);
            AddScaledCeilingProp(artRoot, "Art_T_Cage1", pal.SuspendedCage, mat, 58.0f, 62.5f, 1.0f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_T_Cage2", pal.SuspendedCage, mat, 102.0f, 62.5f, 1.0f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_T_Cage3", pal.SuspendedCage, mat, 124.5f, 62.5f, 1.0f, -7, Color.white);
            AddShelfWithFioles(artRoot, "Art_T_Shelf1", pal, mat, 56.0f, 41.8f, -11);
            AddShelfWithFioles(artRoot, "Art_T_Shelf2", pal, mat, 99.0f, 41.8f, -11);
            AddFloorJunkCluster(artRoot, "Art_T_JunkWest2", pal, mat, 53.5f, 40f, 2);
            AddFloorJunkCluster(artRoot, "Art_T_Junk1", pal, mat, 56.5f, 40f, 1);
            AddFloorJunkCluster(artRoot, "Art_T_JunkMid1", pal, mat, 59.5f, 40f, 6);
            AddFloorJunkCluster(artRoot, "Art_T_JunkShaft", pal, mat, 75.4f, 40f, 4);
            AddFloorJunkCluster(artRoot, "Art_T_JunkMid2", pal, mat, 63.5f, 40f, 8);
            AddFloorJunkCluster(artRoot, "Art_T_JunkMid3", pal, mat, 96.2f, 40f, 3);
            AddFloorJunkCluster(artRoot, "Art_T_Junk2", pal, mat, 99.2f, 40f, 5);
            AddFloorJunkCluster(artRoot, "Art_T_JunkEast1", pal, mat, 105.0f, 40f, 7);
            AddFloorJunkCluster(artRoot, "Art_T_Junk3", pal, mat, 102.0f, 40f, 9);
            AddFloorJunkCluster(artRoot, "Art_T_JunkEast3", pal, mat, 136.5f, 40f, 1);
            AddFloorJunkCluster(artRoot, "Art_T_JunkEast2", pal, mat, 111.5f, 40f, 5);

            // Phase 3 (P2-04): Full-height stone pilasters, ornate iron cages, anchored chained silhouettes & floor-lip foliage
            var foliageTint = new Color(0.84f, 0.94f, 0.86f, 0.95f);
            AddScaledCeilingProp(artRoot, "Art_S_OrnateCage1", pal.OrnateCages[0], mat, 67.2f, 39f, 0.90f, -7, Color.white);
            AddGroundedPrisonSprite(artRoot, "Art_S_Foliage1", pal.GroundFoliage[0], mat, 42.2f, 24f, 0.85f, -2, foliageTint);
            AddGroundedPrisonSprite(artRoot, "Art_S_Foliage2", pal.GroundFoliage[1], mat, 75.5f, 24f, 0.85f, -2, foliageTint);

            AddFullHeightStonePilaster(artRoot, "Art_T_Truss1", pal, mat, 50.2f, 40f, 62.5f);
            AddFullHeightStonePilaster(artRoot, "Art_T_Truss2", pal, mat, 119.5f, 40f, 62.5f);
            AddScaledCeilingProp(artRoot, "Art_T_OrnateCage1", pal.OrnateCages[2], mat, 70.2f, 62.5f, 0.90f, -7, Color.white);
            AddScaledCeilingProp(artRoot, "Art_T_OrnateCage2", pal.OrnateCages[0], mat, 127.0f, 62.5f, 0.90f, -7, Color.white);
            AddAnchoredChainedPrisoner(artRoot, "Art_T_Chained1", pal.ChainedPrisoners[0], mat, pal, new Vector2(63.5f, 55.5f), 62.5f);
            AddAnchoredChainedPrisoner(artRoot, "Art_T_Chained2", pal.ChainedPrisoners[2], mat, pal, new Vector2(114.0f, 55.5f), 62.5f);

            // Lighting: Room S (3 floor + 3 ceiling @ r=10.0f) & Room T (5 floor + 5 mid-wall @ floorY=49f + 5 ceiling @ r=10.5f)
            var warmAmber = new Color(1f, 0.80f, 0.48f, 1f);
            var warmGold = new Color(1f, 0.86f, 0.56f, 1f);
            var deepEmber = new Color(0.95f, 0.70f, 0.40f, 1f);

            float[] sFloorXs = { 39.2f, 69.8f, 76.5f };
            float[] sCeilXs = { 44.5f, 65.0f, 88.0f };
            for (int i = 0; i < sFloorXs.Length; i++)
            {
                AddStandingTorchBeacon(artRoot, $"Art_S_Torch_{i}", mat, sFloorXs[i], 24f, (i % 2 == 0) ? warmAmber : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);
                AddCeilingLanternBeacon(artRoot, $"Art_S_Lantern_{i}", mat, sCeilXs[i], 39f, (i % 2 == 0) ? warmGold : deepEmber, 13.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            }
            BuildWallLanternBeacon(artRoot, "Art_S_LibraryBeacon", "LibraryLight", mat, 54.3f, 27.5f, warmGold, 7.5f, 1.05f);

            float[] tFloorXs = { 40.5f, 68.0f, 94.0f, 116.5f, 134.8f };
            float[] tCeilXs = { 46.0f, 68.0f, 93.0f, 112.0f, 130.0f };
            for (int i = 0; i < tFloorXs.Length; i++)
            {
                AddStandingTorchBeacon(artRoot, $"Art_T_TorchLow_{i}", mat, tFloorXs[i], 40f, (i % 2 == 0) ? warmAmber : deepEmber, 14.5f, (i % 2 == 0) ? 1.34f : 0.94f);
                AddCeilingLanternBeacon(artRoot, $"Art_T_Lantern_{i}", mat, tCeilXs[i], 62.5f, (i % 2 == 0) ? warmGold : deepEmber, 14.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            }

            // 62f mounts on solid brick left of Bay #8 [66..74, 46..52]
            float[] tMidXs = { 45.5f, 62.0f, 84.0f, 108.0f, 134.0f };
            for (int i = 0; i < tMidXs.Length; i++)
                AddStandingTorchBeacon(artRoot, $"Art_T_TorchMid_{i}", mat, tMidXs[i], 49f, (i % 2 == 0) ? warmGold : deepEmber, 14.5f, (i % 2 == 0) ? 1.34f : 0.94f);
            ScrubRoomVisualClutter(artRoot, decalTm);
        }

        // ==================== RESULT PANEL PREFAB (Dead Cells Gothic Card Frame) ====================

        private static void BuildResultPanelPrefab()
        {
            var root = new GameObject("ResultPanel");
            try
            {
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                var controller = root.AddComponent<ResultPanel>();

                // Visual PanelRoot starts hidden; root ResultPanel remains active to receive Finished.
                var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
                panelRoot.transform.SetParent(root.transform, false);
                var panelRect = panelRoot.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                var bg = panelRoot.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.03f, 0.04f, 0.06f, 0.80f);
                bg.raycastTarget = true;

                // Dead Cells dark-gothic CardBorder (780x360) + CardFill (772x352) + Divider (560x2)
                var cardBorder = new GameObject("CardBorder", typeof(RectTransform));
                cardBorder.transform.SetParent(panelRoot.transform, false);
                var borderRect = cardBorder.GetComponent<RectTransform>();
                borderRect.anchorMin = new Vector2(0.5f, 0.5f);
                borderRect.anchorMax = new Vector2(0.5f, 0.5f);
                borderRect.sizeDelta = new Vector2(780f, 360f);
                borderRect.anchoredPosition = Vector2.zero;
                var borderImg = cardBorder.AddComponent<UnityEngine.UI.Image>();
                borderImg.color = new Color(0.68f, 0.54f, 0.28f, 0.95f);
                borderImg.raycastTarget = false;

                var cardFill = new GameObject("CardFill", typeof(RectTransform));
                cardFill.transform.SetParent(cardBorder.transform, false);
                var fillRect = cardFill.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0.5f, 0.5f);
                fillRect.anchorMax = new Vector2(0.5f, 0.5f);
                fillRect.sizeDelta = new Vector2(772f, 352f);
                fillRect.anchoredPosition = Vector2.zero;
                var fillImg = cardFill.AddComponent<UnityEngine.UI.Image>();
                fillImg.color = new Color(0.07f, 0.08f, 0.12f, 0.96f);
                fillImg.raycastTarget = false;

                var divider = new GameObject("Divider", typeof(RectTransform));
                divider.transform.SetParent(cardFill.transform, false);
                var dividerRect = divider.GetComponent<RectTransform>();
                dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
                dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
                dividerRect.sizeDelta = new Vector2(560f, 2f);
                dividerRect.anchoredPosition = new Vector2(0f, 32f);
                var dividerImg = divider.AddComponent<UnityEngine.UI.Image>();
                dividerImg.color = new Color(0.68f, 0.54f, 0.28f, 0.65f);
                dividerImg.raycastTarget = false;

                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                // Title Text ("Thắng" / "Thua")
                var titleGo = new GameObject("TitleText", typeof(RectTransform));
                titleGo.transform.SetParent(cardFill.transform, false);
                var titleRect = titleGo.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.sizeDelta = new Vector2(640f, 100f);
                titleRect.anchoredPosition = new Vector2(0f, 94f);
                var titleText = titleGo.AddComponent<UnityEngine.UI.Text>();
                titleText.font = font;
                titleText.fontSize = 64;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                titleText.text = ResultPanel.WinTitle;
                titleText.raycastTarget = false;

                // Subtitle Text (elapsed run time + [R]/Enter shortcut)
                var subtitleGo = new GameObject("SubtitleText", typeof(RectTransform));
                subtitleGo.transform.SetParent(cardFill.transform, false);
                var subtitleRect = subtitleGo.GetComponent<RectTransform>();
                subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
                subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
                subtitleRect.sizeDelta = new Vector2(700f, 52f);
                subtitleRect.anchoredPosition = new Vector2(0f, -12f);
                var subtitleText = subtitleGo.AddComponent<UnityEngine.UI.Text>();
                subtitleText.font = font;
                subtitleText.fontSize = 26;
                subtitleText.alignment = TextAnchor.MiddleCenter;
                subtitleText.color = new Color(0.84f, 0.86f, 0.92f, 1f);
                subtitleText.text = "00:00 — Nhấn [R] hoặc Enter để chơi lại";
                subtitleText.raycastTarget = false;

                // Replay Button ("Chơi lại")
                var buttonGo = new GameObject("ReplayButton", typeof(RectTransform));
                buttonGo.transform.SetParent(cardFill.transform, false);
                var buttonRect = buttonGo.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.sizeDelta = new Vector2(300f, 72f);
                buttonRect.anchoredPosition = new Vector2(0f, -104f);
                var buttonImg = buttonGo.AddComponent<UnityEngine.UI.Image>();
                buttonImg.color = new Color(0.22f, 0.24f, 0.30f, 1f);
                buttonImg.raycastTarget = true;
                var button = buttonGo.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = buttonImg;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var labelText = labelGo.AddComponent<UnityEngine.UI.Text>();
                labelText.font = font;
                labelText.fontSize = 34;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.color = Color.white;
                labelText.text = "Chơi lại";
                labelText.raycastTarget = false;

                panelRoot.SetActive(false);

                var so = new SerializedObject(controller);
                so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
                so.FindProperty("_titleText").objectReferenceValue = titleText;
                so.FindProperty("_subtitleText").objectReferenceValue = subtitleText;
                so.FindProperty("_replayButton").objectReferenceValue = button;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, ResultPanelPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddControlsHintBadge(GameObject playerHudGo)
        {
            var badgeGo = new GameObject("ControlsHintBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(playerHudGo.transform, false);
            var rect = badgeGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-20f, 18f);
            rect.sizeDelta = new Vector2(610f, 34f);

            var bg = badgeGo.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.05f, 0.06f, 0.10f, 0.72f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(badgeGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            var text = textGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.88f, 0.90f, 0.94f, 0.95f);
            text.text = "[Esc] Tạm dừng  ·  [1/2] Vũ khí  ·  [Shift] Lăn  ·  [E] Tương tác";
            text.raycastTarget = false;
        }

        private static void BuildPauseMenu(
            LevelRunController run,
            PlayerController player,
            PlayerInputReader playerInput,
            HitStop hitStop)
        {
            var root = new GameObject("PauseMenu");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var controller = root.AddComponent<PauseMenuController>();

            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var backdropImg = panelRoot.AddComponent<UnityEngine.UI.Image>();
            backdropImg.color = new Color(0.03f, 0.04f, 0.06f, 0.86f);
            backdropImg.raycastTarget = true;

            var cardBorder = new GameObject("CardBorder", typeof(RectTransform));
            cardBorder.transform.SetParent(panelRoot.transform, false);
            var borderRect = cardBorder.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(940f, 560f);
            borderRect.anchoredPosition = Vector2.zero;
            var borderImg = cardBorder.AddComponent<UnityEngine.UI.Image>();
            borderImg.color = new Color(0.68f, 0.54f, 0.28f, 0.95f);
            borderImg.raycastTarget = false;

            var cardFill = new GameObject("CardFill", typeof(RectTransform));
            cardFill.transform.SetParent(cardBorder.transform, false);
            var fillRect = cardFill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = new Vector2(932f, 552f);
            fillRect.anchoredPosition = Vector2.zero;
            var fillImg = cardFill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0.07f, 0.08f, 0.12f, 0.97f);
            fillImg.raycastTarget = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleGo = new GameObject("TitleText", typeof(RectTransform));
            titleGo.transform.SetParent(cardFill.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(760f, 70f);
            titleRect.anchoredPosition = new Vector2(0f, 214f);
            var titleText = titleGo.AddComponent<UnityEngine.UI.Text>();
            titleText.font = font;
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.96f, 0.88f, 0.62f, 1f);
            titleText.text = "TẠM DỪNG";
            titleText.raycastTarget = false;

            CreatePauseDivider(cardFill.transform, "DividerTop", 780f, 168f);

            string[] leftLegend =
            {
                "A/D — Di chuyển",
                "Space — Nhảy / Nhảy đôi / Bám tường",
                "Chuột trái / Enter — Tấn công",
                "Shift — Lăn né (i-frame)",
            };
            string[] rightLegend =
            {
                "S + Space — Đập đất / Tụt sàn",
                "W/S — Leo xích",
                "1/2 — Đổi Kiếm / Cung",
                "E — Tương tác",
            };
            for (int i = 0; i < leftLegend.Length; i++)
            {
                float y = 114f - i * 48f;
                CreatePauseLegendRow(cardFill.transform, $"LegendLeft_{i}", leftLegend[i], new Vector2(-210f, y), font);
                CreatePauseLegendRow(cardFill.transform, $"LegendRight_{i}", rightLegend[i], new Vector2(215f, y), font);
            }

            CreatePauseDivider(cardFill.transform, "DividerBottom", 780f, -80f);

            var resumeButton = CreatePauseButton(cardFill.transform, "ResumeButton", "Tiếp tục [Esc]", new Vector2(-264f, -164f), new Color(0.28f, 0.34f, 0.44f, 1f), font);
            var restartButton = CreatePauseButton(cardFill.transform, "RestartButton", "Chơi lại", new Vector2(0f, -164f), new Color(0.22f, 0.24f, 0.30f, 1f), font);
            var quitButton = CreatePauseButton(cardFill.transform, "QuitButton", "Thoát game", new Vector2(264f, -164f), new Color(0.30f, 0.16f, 0.18f, 1f), font);

            panelRoot.SetActive(false);

            var so = new SerializedObject(controller);
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerInput").objectReferenceValue = playerInput;
            so.FindProperty("_hitStop").objectReferenceValue = hitStop;
            so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("_resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("_restartButton").objectReferenceValue = restartButton;
            so.FindProperty("_quitButton").objectReferenceValue = quitButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePauseDivider(Transform parent, string name, float width, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 2f);
            rect.anchoredPosition = new Vector2(0f, y);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.68f, 0.54f, 0.28f, 0.65f);
            img.raycastTarget = false;
        }

        private static void CreatePauseLegendRow(Transform parent, string name, string textValue, Vector2 pos, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 40f);
            rect.anchoredPosition = pos;
            var text = go.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = 21;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.86f, 0.89f, 0.94f, 1f);
            text.text = textValue;
            text.raycastTarget = false;
        }

        private static UnityEngine.UI.Button CreatePauseButton(Transform parent, string name, string label, Vector2 pos, Color bgColor, Font font)
        {
            var btnGo = new GameObject(name, typeof(RectTransform));
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(236f, 64f);
            btnRect.anchoredPosition = pos;
            var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = bgColor;
            btnImg.raycastTarget = true;
            var button = btnGo.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = btnImg;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<UnityEngine.UI.Text>();
            labelText.font = font;
            labelText.fontSize = 26;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;
            labelText.raycastTarget = false;
            return button;
        }

        // ==================== SCENE ASSEMBLY (M6 + I1 + URP Post-Processing + Ambient VFX) ====================

        private static void EnsureGoldPickupSortingLayer()
        {
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
            var prefabRoot = PrefabUtility.LoadPrefabContents(GoldPickupPrefabPath);
            if (prefabRoot == null)
                throw new InvalidOperationException("Missing GoldPickup prefab: " + GoldPickupPrefabPath);

            try
            {
                bool changed = false;
                foreach (var sr in prefabRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null)
                        continue;

                    int targetOrder = Mathf.Max(sr.sortingOrder, 5);
                    if (sr.sortingLayerID != vfxLayerId || sr.sortingOrder != targetOrder)
                    {
                        sr.sortingLayerID = vfxLayerId;
                        sr.sortingOrder = targetOrder;
                        changed = true;
                    }
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, GoldPickupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void BuildScene(Material spriteLit, Material particlesUnlit, PrisonPalette pal)
        {
            EnsureGoldPickupSortingLayer();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Rooms container with all 9 modular prefab instances at origin (5 Core Spine + 4 Expansion Wings)
            var roomsRoot = new GameObject("Rooms");
            InstantiateRoom(roomsRoot.transform, "CD");
            InstantiateRoom(roomsRoot.transform, "AB");
            InstantiateRoom(roomsRoot.transform, "G");
            InstantiateRoom(roomsRoot.transform, "HJK");
            InstantiateRoom(roomsRoot.transform, "ELF");
            InstantiateRoom(roomsRoot.transform, "W1_EastCourtyard");
            InstantiateRoom(roomsRoot.transform, "W2_SewerCistern");
            InstantiateRoom(roomsRoot.transform, "W3_WestBastion");
            InstantiateRoom(roomsRoot.transform, "U1_UpperPromenade");

            // 2. Global Light 2D (Dead Cells chiaroscuro calibration: 0.44f cool slate ambient)
            var lightGo = new GameObject("Global Light 2D");
            var globalLight = lightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.44f;
            globalLight.color = new Color(0.56f, 0.70f, 0.84f, 1f);

            // 2b. Shared URP Post-Processing Global Volume (Bloom + Vignette + ACES)
            var postProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedPostProcessProfilePath);
            if (postProfile == null)
                throw new InvalidOperationException("Missing shared post-process profile: " + SharedPostProcessProfilePath);
            var volGo = new GameObject("Global Volume");
            volGo.layer = 0;
            var globalVol = volGo.AddComponent<Volume>();
            globalVol.isGlobal = true;
            globalVol.priority = 0f;
            globalVol.weight = 1f;
            globalVol.sharedProfile = postProfile;

            // 3. Systems (VfxSpawner, HitStop, ProjectileSpawner, LevelRunController)
            var allVfxPrewarm = LoadAllVfxDefinitions();
            var vfxGo = new GameObject("VfxSpawner");
            var vfxSpawner = vfxGo.AddComponent<VfxSpawner>();
            var soVfx = new SerializedObject(vfxSpawner);
            var prewarmProp = soVfx.FindProperty("_prewarm");
            prewarmProp.arraySize = allVfxPrewarm.Length;
            for (int i = 0; i < allVfxPrewarm.Length; i++)
                prewarmProp.GetArrayElementAtIndex(i).objectReferenceValue = allVfxPrewarm[i];
            soVfx.ApplyModifiedPropertiesWithoutUndo();

            var systemsGo = new GameObject("Systems");
            var hitStop = systemsGo.AddComponent<HitStop>();
            var projectileSpawner = systemsGo.AddComponent<ProjectileSpawner>();
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<Projectile>(ArrowPrefabPath);
            if (arrowPrefab == null)
                throw new InvalidOperationException("Missing Projectile_Arrow prefab: " + ArrowPrefabPath);
            var grenadePrefab = AssetDatabase.LoadAssetAtPath<Projectile>(GrenadePrefabPath);
            if (grenadePrefab == null)
                throw new InvalidOperationException("Missing Projectile_Grenade prefab: " + GrenadePrefabPath);
            var soProj = new SerializedObject(projectileSpawner);
            var projPrewarm = soProj.FindProperty("_prewarm");
            projPrewarm.arraySize = 2;
            projPrewarm.GetArrayElementAtIndex(0).objectReferenceValue = arrowPrefab;
            projPrewarm.GetArrayElementAtIndex(1).objectReferenceValue = grenadePrefab;
            soProj.FindProperty("_prewarmCount").intValue = 6;
            soProj.FindProperty("_vfx").objectReferenceValue = vfxSpawner;
            soProj.FindProperty("_targets").intValue = 128; // layer 7 (Player)
            soProj.FindProperty("_ground").intValue = 576;  // layers 6 (Ground) + 9 (OneWayPlatform)
            soProj.ApplyModifiedPropertiesWithoutUndo();

            var runController = systemsGo.AddComponent<LevelRunController>();

            var audioHubGo = new GameObject("CombatAudioHub");
            audioHubGo.AddComponent<CombatAudioHub>();

            // 4. Main Camera + CameraFollow2D (renderPostProcessing = true, orthoSize=7, full-scale 340u x 86u bounds)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(3f, 1.5f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camGo.AddComponent<AudioListener>();
            var uacd = camGo.AddComponent<UniversalAdditionalCameraData>();
            uacd.renderPostProcessing = true;
            uacd.volumeLayerMask |= 1 << 0;
            var camFollow = camGo.AddComponent<CameraFollow2D>();

            // 4b. Camera-attached Ambient VFX (Dust Motes + Dripping Water following Main Camera)
            BuildAmbientVfx(particlesUnlit, cam.transform);

            // 5. Player instance at (3, 0, 0) facing right
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                throw new InvalidOperationException("Missing Player prefab: " + PlayerPrefabPath);
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Player";
            playerGo.transform.position = new Vector3(3f, 0f, 0f);
            playerGo.transform.localScale = Vector3.one;

            var playerCtrl = playerGo.GetComponent<PlayerController>();
            var playerInput = playerGo.GetComponent<PlayerInputReader>();
            var playerHealth = playerGo.GetComponent<Health>();
            var playerCol = playerGo.GetComponent<BoxCollider2D>();

            // Wire Player scene overrides matching Sandbox_Player
            var playerVfx = playerGo.GetComponentInChildren<PlayerVfx>(true);
            if (playerVfx != null)
            {
                var soPvfx = new SerializedObject(playerVfx);
                soPvfx.FindProperty("_spawner").objectReferenceValue = vfxSpawner;
                soPvfx.ApplyModifiedPropertiesWithoutUndo();
            }

            var playerFeedback = playerGo.GetComponentInChildren<PlayerFeedback>(true);
            if (playerFeedback != null)
            {
                var soFb = new SerializedObject(playerFeedback);
                soFb.FindProperty("_camera").objectReferenceValue = camFollow;
                soFb.FindProperty("_hitStop").objectReferenceValue = hitStop;
                soFb.ApplyModifiedPropertiesWithoutUndo();
            }

            var spawnHead = playerGo.GetComponentInChildren<PlayerSpawnHead>(true);
            if (spawnHead != null)
            {
                var soHead = new SerializedObject(spawnHead);
                soHead.FindProperty("_vfx").objectReferenceValue = vfxSpawner;
                soHead.FindProperty("_camera").objectReferenceValue = camFollow;
                soHead.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire CameraFollow2D to Player with Dead Cells tight framing and full-scale bounds Rect(-104, -20, 340, 86)
            var soCam = new SerializedObject(camFollow);
            soCam.FindProperty("_target").objectReferenceValue = playerGo.transform;
            soCam.FindProperty("_offset").vector2Value = new Vector2(0f, 1.5f);
            soCam.FindProperty("_deadzone").vector2Value = new Vector2(0.55f, 0.35f);
            soCam.FindProperty("_smoothTime").floatValue = 0.09f;
            soCam.FindProperty("_useBounds").boolValue = true;
            soCam.FindProperty("_bounds").rectValue = new Rect(-104f, -20f, 340f, 86f);
            soCam.ApplyModifiedPropertiesWithoutUndo();

            // Wire LevelRunController
            var soRun = new SerializedObject(runController);
            soRun.FindProperty("_player").objectReferenceValue = playerCtrl;
            soRun.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            soRun.FindProperty("_playerInput").objectReferenceValue = playerInput;
            soRun.FindProperty("_projectileSpawner").objectReferenceValue = projectileSpawner;
            soRun.FindProperty("_hitStop").objectReferenceValue = hitStop;
            soRun.ApplyModifiedPropertiesWithoutUndo();

            // 6. Thirty-four Enemies (9 core spine + 25 wing enemies, all dormant before tick 0)
            var enemiesRoot = new GameObject("Enemies");
            // Core Spine (9) — P0-05b Dead Cells encounter pacing pack
            var zombieB = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_B", new Vector3(29f, 0f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratB2 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_B2", new Vector3(35f, 0f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var zombieD1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_D1", new Vector3(31f, 8f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratG = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_G", new Vector3(62f, 0f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var shieldJ = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_J", new Vector3(-4f, 16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratJ2 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_J2", new Vector3(-10f, 16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var zombieE = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_E", new Vector3(-9f, 24f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerE = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_E", new Vector3(10f, 24f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratE2 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_E2", new Vector3(0f, 24f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room M (4)
            var zombieM1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_M1", new Vector3(86f, 0f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var grenadierM1 = SpawnDormantEnemy(GrenadierPrefabPath, "Enemy_Grenadier_M1", new Vector3(92f, 3.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var shieldM1 = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_M1", new Vector3(132f, 0f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerM1 = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_M1", new Vector3(138f, 3.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room N (4)
            var zombieN1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_N1", new Vector3(90f, 8f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerN1 = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_N1", new Vector3(91f, 12.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var grenadierN1 = SpawnDormantEnemy(GrenadierPrefabPath, "Enemy_Grenadier_N1", new Vector3(122f, 12.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var shieldN1 = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_N1", new Vector3(142f, 8f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room O (4)
            var ratO1 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_O1", new Vector3(88f, -16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratO2 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_O2", new Vector3(102f, -16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var zombieO1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_O1", new Vector3(134f, -16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var grenadierO1 = SpawnDormantEnemy(GrenadierPrefabPath, "Enemy_Grenadier_O1", new Vector3(144f, -11.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room P (3)
            var ratP1 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_P1", new Vector3(176f, -16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var shieldP1 = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_P1", new Vector3(192f, -16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerP1 = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_P1", new Vector3(191f, -11.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room Q (3)
            var zombieQ1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_Q1", new Vector3(-45f, 16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var ratQ1 = SpawnDormantEnemy(RatPrefabPath, "Enemy_Rat_Q1", new Vector3(-58f, 16f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerQ1 = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_Q1", new Vector3(-82f, 19.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room R (3)
            var shieldR1 = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_R1", new Vector3(-48f, 24f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var grenadierR1 = SpawnDormantEnemy(GrenadierPrefabPath, "Enemy_Grenadier_R1", new Vector3(-48f, 28.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var zombieR1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_R1", new Vector3(-76f, 28.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            // Room T (4)
            var zombieT1 = SpawnDormantEnemy(ZombiePrefabPath, "Enemy_Zombie_T1", new Vector3(58f, 40f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var archerT1 = SpawnDormantEnemy(ArcherPrefabPath, "Enemy_Archer_T1", new Vector3(58f, 44.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var shieldT1 = SpawnDormantEnemy(ShieldPrefabPath, "Enemy_Shield_T1", new Vector3(98f, 40f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);
            var grenadierT1 = SpawnDormantEnemy(GrenadierPrefabPath, "Enemy_Grenadier_T1", new Vector3(99f, 44.5f, 0f), enemiesRoot.transform, playerGo.transform, projectileSpawner, vfxSpawner);

            var allEnemies = new[]
            {
                zombieB, ratB2, zombieD1, ratG, shieldJ, ratJ2, zombieE, archerE, ratE2,
                zombieM1, grenadierM1, shieldM1, archerM1,
                zombieN1, archerN1, grenadierN1, shieldN1,
                ratO1, ratO2, zombieO1, grenadierO1,
                ratP1, shieldP1, archerP1,
                zombieQ1, ratQ1, archerQ1,
                shieldR1, grenadierR1, zombieR1,
                zombieT1, archerT1, shieldT1, grenadierT1,
            };

            // 7. Twelve Encounter Zones (Spec §5.3 exact inclusive bounds and members)
            var zonesRoot = new GameObject("EncounterZones");
            CreateEncounterZone(zonesRoot.transform, "Zone_B1", "B1", new Vector2(20f, 0f), new Vector2(42f, 6f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieB, ratB2 });
            CreateEncounterZone(zonesRoot.transform, "Zone_D1", "D1", new Vector2(23f, 8f), new Vector2(41f, 13.5f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieD1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_G1", "G1", new Vector2(58f, 0f), new Vector2(76f, 7.75f), runController, playerCtrl, playerHealth, playerCol, new[] { ratG });
            CreateEncounterZone(zonesRoot.transform, "Zone_J1", "J1", new Vector2(-16f, 16f), new Vector2(6f, 22.5f), runController, playerCtrl, playerHealth, playerCol, new[] { shieldJ, ratJ2 });
            CreateEncounterZone(zonesRoot.transform, "Zone_E1", "E1", new Vector2(-16f, 24f), new Vector2(22f, 31f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieE, archerE, ratE2 });
            CreateEncounterZone(zonesRoot.transform, "Zone_M1", "M1", new Vector2(78f, 0f), new Vector2(148f, 6.8f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieM1, grenadierM1, shieldM1, archerM1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_N1", "N1", new Vector2(78f, 8f), new Vector2(160f, 22f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieN1, archerN1, grenadierN1, shieldN1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_O1", "O1", new Vector2(78f, -16f), new Vector2(162f, -2f), runController, playerCtrl, playerHealth, playerCol, new[] { ratO1, ratO2, zombieO1, grenadierO1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_P1", "P1", new Vector2(166f, -16f), new Vector2(210f, -2f), runController, playerCtrl, playerHealth, playerCol, new[] { ratP1, shieldP1, archerP1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_Q1", "Q1", new Vector2(-100f, 16f), new Vector2(-32f, 23f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieQ1, ratQ1, archerQ1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_R1", "R1", new Vector2(-86f, 24f), new Vector2(-32f, 38f), runController, playerCtrl, playerHealth, playerCol, new[] { shieldR1, grenadierR1, zombieR1 });
            CreateEncounterZone(zonesRoot.transform, "Zone_T1", "T1", new Vector2(40f, 40f), new Vector2(138f, 62f), runController, playerCtrl, playerHealth, playerCol, new[] { zombieT1, archerT1, shieldT1, grenadierT1 });

            // 8. Interactions (Unique I3 Chest, I4 Gate, I5 Exit + 3 SwitchDoors, 6 TeleportGates, I16 LoreTablet, I15 SewerExit)
            var interactionsRoot = new GameObject("Interactions");
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new InvalidOperationException("Missing InputActionAsset: " + InputActionsPath);

            // I3: Healing Chest at (72, 0), normalized 0.55x scale, PromptIndicator y=1.85f (+0.36u above lid)
            var chestGo = CreateChild(interactionsRoot, "I3_HealingChest");
            chestGo.transform.position = new Vector3(72f, 0f, 0f);
            var chestTrigger = chestGo.AddComponent<BoxCollider2D>();
            chestTrigger.isTrigger = true;
            chestTrigger.offset = new Vector2(0f, 0.75f);
            chestTrigger.size = new Vector2(2.5f, 1.5f);
            const float chestScale = 0.55f;
            var chestSprite = LoadTileSprite("Props/StorageAndVessel_003");
            var chestVisualGo = CreateChild(chestGo, "Visual");
            chestVisualGo.transform.localPosition = new Vector3(0f, -chestSprite.bounds.min.y * chestScale, 0f);
            chestVisualGo.transform.localScale = new Vector3(chestScale, chestScale, 1f);
            var chestVisualSr = chestVisualGo.AddComponent<SpriteRenderer>();
            chestVisualSr.sprite = chestSprite;
            chestVisualSr.sharedMaterial = spriteLit;
            chestVisualSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            chestVisualSr.sortingOrder = -1;
            chestVisualSr.color = new Color(0.86f, 0.80f, 0.66f, 1f);

            var promptGo = CreateChild(chestGo, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, "[E] Hồi đầy máu", new Color(1f, 0.88f, 0.42f, 1f));
            promptGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            promptGo.SetActive(false);

            var glowGo = CreateChild(chestGo, "GlowHighlight");
            glowGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            var glowLight = glowGo.AddComponent<Light2D>();
            glowLight.lightType = Light2D.LightType.Point;
            glowLight.color = new Color(0.4f, 1f, 0.65f, 1f);
            glowLight.intensity = 1.25f;
            glowLight.pointLightOuterRadius = 4.5f;
            glowLight.pointLightInnerRadius = 1.5f;

            var fullFeedbackGo = CreateChild(chestGo, "FullHealthFeedback");
            BuildWorldPromptBadge(fullFeedbackGo, HealingChest.FullHealthMessage, new Color(0.68f, 0.92f, 1f, 1f));
            fullFeedbackGo.transform.localPosition = new Vector3(0f, 2.20f, 0f);
            fullFeedbackGo.SetActive(false);

            var collectedGlintVfx = AssetDatabase.LoadAssetAtPath<VfxDefinition>("Assets/_Project/Features/VFX/Data/Vfx_CollectedGlint.asset");
            if (collectedGlintVfx == null)
                throw new InvalidOperationException("Missing Vfx_CollectedGlint.asset.");

            var chest = chestGo.AddComponent<HealingChest>();
            var soChest = new SerializedObject(chest);
            soChest.FindProperty("_run").objectReferenceValue = runController;
            soChest.FindProperty("_player").objectReferenceValue = playerCtrl;
            soChest.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            soChest.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            soChest.FindProperty("_triggerZone").objectReferenceValue = chestTrigger;
            soChest.FindProperty("_inputActions").objectReferenceValue = inputActions;
            soChest.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            soChest.FindProperty("_glowHighlight").objectReferenceValue = glowGo;
            soChest.FindProperty("_fullHealthFeedback").objectReferenceValue = fullFeedbackGo;
            soChest.FindProperty("_visualRenderer").objectReferenceValue = chestVisualSr;
            soChest.FindProperty("_openedSprite").objectReferenceValue = LoadTileSprite("Props/StorageAndVessel_004");
            soChest.FindProperty("_vfx").objectReferenceValue = vfxSpawner;
            soChest.FindProperty("_healVfx").objectReferenceValue = collectedGlintVfx;
            soChest.ApplyModifiedPropertiesWithoutUndo();

            // I4: Encounter Gate at E-L (x=22, y=24..28) — hingeSmallDoor_00 (1x3u) scaled (0.85, 4/3, 1) + compensated collider (0.5/0.85, 3) => world 0.5u x 4.0u
            var gateGo = CreateChild(interactionsRoot, "I4_EncounterGate");
            gateGo.transform.position = new Vector3(22f, 26f, 0f);
            var gateBarrier = CreateChild(gateGo, "GateBarrier");
            gateBarrier.layer = LayerGround;
            gateBarrier.transform.localPosition = Vector3.zero;
            gateBarrier.transform.localScale = new Vector3(0.85f, 4f / 3f, 1f);
            var gateCol = gateBarrier.AddComponent<BoxCollider2D>();
            gateCol.isTrigger = false;
            gateCol.offset = Vector2.zero;
            gateCol.size = new Vector2(0.5f / 0.85f, 3f);
            var gateSr = gateBarrier.AddComponent<SpriteRenderer>();
            gateSr.sprite = pal.HingeSmallDoor;
            gateSr.sharedMaterial = spriteLit;
            gateSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            gateSr.sortingOrder = 1;

            var gate = gateGo.AddComponent<EncounterGate>();
            var soGate = new SerializedObject(gate);
            soGate.FindProperty("_run").objectReferenceValue = runController;
            soGate.FindProperty("_firstEnemy").objectReferenceValue = zombieE;
            soGate.FindProperty("_secondEnemy").objectReferenceValue = archerE;
            soGate.FindProperty("_gateBarrierRoot").objectReferenceValue = gateBarrier;
            soGate.ApplyModifiedPropertiesWithoutUndo();

            // I5: Exit Trigger at F center (5, 33), size (2, 2)
            var exitGo = CreateChild(interactionsRoot, "I5_ExitTrigger");
            exitGo.transform.position = new Vector3(5f, 33f, 0f);
            var exitCol = exitGo.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.offset = Vector2.zero;
            exitCol.size = new Vector2(2f, 2f);
            var exitTrigger = exitGo.AddComponent<ExitTrigger>();
            var soExit = new SerializedObject(exitTrigger);
            soExit.FindProperty("_run").objectReferenceValue = runController;
            soExit.FindProperty("_gate").objectReferenceValue = gate;
            soExit.FindProperty("_player").objectReferenceValue = playerCtrl;
            soExit.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            soExit.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            soExit.ApplyModifiedPropertiesWithoutUndo();

            // 3 Copper Switch Doors (SD_1_EastVault, SD_2_SewerExit, SD_3_WestArmory)
            var switchDoorsRoot = CreateChild(interactionsRoot, "SwitchDoors");
            CreateSwitchDoor(switchDoorsRoot, "SD_1_EastVault", new Vector2(144f, 0f), new Vector2(150f, 2f), spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow, vfxSpawner, collectedGlintVfx);
            var sd2SewerExit = CreateSwitchDoor(switchDoorsRoot, "SD_2_SewerExit", new Vector2(206f, -16f), new Vector2(212f, -14f), spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow, vfxSpawner, collectedGlintVfx);
            CreateSwitchDoor(switchDoorsRoot, "SD_3_WestArmory", new Vector2(-82f, 24f), new Vector2(-88f, 26f), spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow, vfxSpawner, collectedGlintVfx);

            // 6 Teleportation Stones forming 3 bi-directional pairs with discovery gating
            var teleportersRoot = CreateChild(interactionsRoot, "TeleportGates");
            var tp0Spawn = CreateTeleportGate(teleportersRoot, "TP_0_Spawn", new Vector2(15.5f, 0f), startsDiscovered: true, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            var tp1EastVault = CreateTeleportGate(teleportersRoot, "TP_1_EastVault", new Vector2(161f, 0f), startsDiscovered: false, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            PairTeleportGates(tp0Spawn, tp1EastVault);

            var tp2Cistern = CreateTeleportGate(teleportersRoot, "TP_2_CisternJunction", new Vector2(80f, -16f), startsDiscovered: true, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            var tp3SewerExit = CreateTeleportGate(teleportersRoot, "TP_3_SewerExit", new Vector2(216f, -16f), startsDiscovered: false, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            PairTeleportGates(tp2Cistern, tp3SewerExit);

            var tp4WestCatacombs = CreateTeleportGate(teleportersRoot, "TP_4_WestCatacombs", new Vector2(-96f, 16f), startsDiscovered: false, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            var tp5HighBastion = CreateTeleportGate(teleportersRoot, "TP_5_HighBastion", new Vector2(132f, 40f), startsDiscovered: false, spriteLit, pal, runController, playerCtrl, playerHealth, playerCol, inputActions, camFollow);
            PairTeleportGates(tp4WestCatacombs, tp5HighBastion);

            // I16: Alchemy Lore Tablet at (73, 24) in Room S (dedicated PromptIndicator VFX/25 + LoreBanner VFX/26)
            CreateLoreTablet(interactionsRoot, "I16_LoreTablet", new Vector2(73f, 24f), runController, playerCtrl, playerHealth, playerCol, inputActions,
                "Biên Niên Sử Ngục Tối",
                "Kẻ vượt Ngục Giếng Chuông chọn một trong hai lối thoát:\nHành Lang Tử Tù phía trên hoặc Cống Ngầm Độc Dược phía dưới.",
                "[E] Đọc biên niên sử Ngục Tối");

            // I15: Secondary Biome Exit Trigger ("Toxic Sewers") at (224, -15) in Room P
            CreateSecondaryBiomeExitTrigger(interactionsRoot, "I15_SewerExitTrigger", "Toxic Sewers", new Vector2(224f, -15f), runController, sd2SewerExit, shieldP1, archerP1, playerCtrl, playerHealth, playerCol);

            // P0-04 Part B (Room N): 2nd lore tablet explains the Bell-Well uprising and points at the Promenade exit above Room S/T
            CreateLoreTablet(interactionsRoot, "I19_LoreTablet_NorthGallery", new Vector2(152.5f, 8f), runController, playerCtrl, playerHealth, playerCol, inputActions,
                "Nhật Ký Cai Ngục",
                "Đêm Giếng Chuông, tù nhân đập vỡ chuông để báo hiệu khởi nghĩa;\nnhững kẻ sống sót trèo lên Thượng Thành — lối ra Promenade nằm ngay trên Hành Lang Tử Tù.",
                "[E] Đọc nhật ký Cai Ngục");

            // P0-04 Part B (Room T): the courtyard arch becomes a live, door-less secondary biome exit guarded by Zone T1
            CreateSecondaryBiomeExitTrigger(interactionsRoot, "I18_PromenadeExitTrigger", "Promenade of the Condemned", new Vector2(126f, 41.2f), runController, null, shieldT1, grenadierT1, playerCtrl, playerHealth, playerCol, new Vector2(2.2f, 2.4f));

            // 9. UI: PlayerHUD.prefab + ResultPanel.prefab + EventSystem (UI action map)
            var playerHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHudPrefabPath);
            if (playerHudPrefab == null)
                throw new InvalidOperationException("Missing PlayerHUD prefab: " + PlayerHudPrefabPath);
            var playerHudGo = (GameObject)PrefabUtility.InstantiatePrefab(playerHudPrefab);
            playerHudGo.name = "PlayerHUD";

            var healthHud = playerHudGo.GetComponentInChildren<PlayerHealthHud>(true);
            if (healthHud == null)
                throw new InvalidOperationException("PlayerHUD.prefab is missing PlayerHealthHud.");
            var soHealthHud = new SerializedObject(healthHud);
            soHealthHud.FindProperty("_health").objectReferenceValue = playerHealth;
            soHealthHud.ApplyModifiedPropertiesWithoutUndo();

            var equipHud = playerHudGo.GetComponentInChildren<PlayerEquipmentHud>(true);
            if (equipHud == null)
                throw new InvalidOperationException("PlayerHUD.prefab is missing PlayerEquipmentHud.");
            var playerBow = playerGo.GetComponentInChildren<PlayerBow>(true);
            var soEquipHud = new SerializedObject(equipHud);
            soEquipHud.FindProperty("_player").objectReferenceValue = playerCtrl;
            soEquipHud.FindProperty("_bow").objectReferenceValue = playerBow;
            soEquipHud.ApplyModifiedPropertiesWithoutUndo();

            AddControlsHintBadge(playerHudGo);

            var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPanelPrefabPath);
            if (resultPrefab == null)
                throw new InvalidOperationException("Missing ResultPanel prefab: " + ResultPanelPrefabPath);
            var resultGo = (GameObject)PrefabUtility.InstantiatePrefab(resultPrefab);
            resultGo.name = "ResultPanel";
            var resultPanel = resultGo.GetComponent<ResultPanel>();
            var soPanel = new SerializedObject(resultPanel);
            soPanel.FindProperty("_run").objectReferenceValue = runController;
            soPanel.ApplyModifiedPropertiesWithoutUndo();

            BuildPauseMenu(runController, playerCtrl, playerInput, hitStop);

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            var uiModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            uiModule.actionsAsset = inputActions;
            BindUiInputModuleActions(uiModule, inputActions);

            // 9b. Clone Scrolls of Power + Power Scroll HUD + Scroll cinematic + Gold HUD + EnemyImpactFeedback read-only from Main_Level.unity
            //     and wire Run Gold (GoldWallet + EnemyGoldDrops with all 34 enemies) BEFORE atmosphere/lighting pass.
            CloneMainLevelScrollsAndGoldEconomy(scene, pal, playerGo, playerCtrl, playerHealth, hitStop, playerHudGo, allEnemies, vfxSpawner);

            var scrollInteractionRef = UnityEngine.Object.FindAnyObjectByType<PowerScrollInteraction>();
            if (scrollInteractionRef != null)
            {
                foreach (var gateComp in teleportersRoot.GetComponentsInChildren<TeleportGate>(true))
                {
                    var soGateTp = new SerializedObject(gateComp);
                    soGateTp.FindProperty("_powerScrolls").objectReferenceValue = scrollInteractionRef;
                    soGateTp.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 10. Expanded Round 5/6 Atmosphere, 5x4 ParallaxFar, 4-tier ParallaxMid, 8 Mushrooms, 6 Mist, 6 Casters & Final Light2D Isolation
            BuildRound5SceneAtmosphereAndLighting(cam.transform, spriteLit, pal, playerGo, enemiesRoot);

            // 11. Room A Weathered Masonry Alcove & Sunlit VaultA Chamber (saved inactive at rest)
            var spriteUnlit = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMatPath);
            BuildVaultAInScene(
                roomsRoot,
                interactionsRoot,
                runController,
                playerCtrl,
                playerHealth,
                playerCol,
                inputActions,
                camFollow,
                playerHudGo,
                spriteLit,
                spriteUnlit,
                particlesUnlit);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save scene: " + ScenePath);
        }

        private static SwitchDoor CreateSwitchDoor(
            GameObject parent,
            string name,
            Vector2 leverPos,
            Vector2 barrierCenter,
            Material spriteLit,
            PrisonPalette pal,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions,
            CameraFollow2D camera = null,
            VfxSpawner vfx = null,
            VfxDefinition openVfx = null)
        {
            var root = CreateChild(parent, name);
            root.transform.position = new Vector3(leverPos.x, leverPos.y, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.75f);
            trigger.size = new Vector2(2.2f, 1.5f);

            var switchVisualGo = CreateChild(root, "SwitchVisual");
            switchVisualGo.transform.localPosition = new Vector3(0f, -pal.SwitchCopperOff.bounds.min.y, 0f);
            switchVisualGo.transform.localScale = Vector3.one;
            var switchSr = switchVisualGo.AddComponent<SpriteRenderer>();
            switchSr.sprite = pal.SwitchCopperOff;
            switchSr.sharedMaterial = spriteLit;
            switchSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            switchSr.sortingOrder = -1;
            switchSr.color = Color.white;

            var promptGo = CreateChild(root, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, "[E] Gạt công tắc", new Color(1f, 0.88f, 0.42f, 1f));
            promptGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            promptGo.SetActive(false);

            var barrierGo = CreateChild(root, "DoorBarrier");
            barrierGo.layer = LayerGround;
            barrierGo.transform.position = new Vector3(barrierCenter.x, barrierCenter.y, 0f);
            barrierGo.transform.localScale = new Vector3(0.85f, 4f / 3f, 1f);
            var barrierCol = barrierGo.AddComponent<BoxCollider2D>();
            barrierCol.isTrigger = false;
            barrierCol.offset = Vector2.zero;
            barrierCol.size = new Vector2(0.5f / 0.85f, 3f);
            var barrierSr = barrierGo.AddComponent<SpriteRenderer>();
            barrierSr.sprite = pal.HingeSmallDoor;
            barrierSr.sharedMaterial = spriteLit;
            barrierSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            barrierSr.sortingOrder = 1;
            barrierSr.color = Color.white;

            var sd = root.AddComponent<SwitchDoor>();
            var so = new SerializedObject(sd);
            so.FindProperty("_doorId").stringValue = name;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_switchTrigger").objectReferenceValue = trigger;
            so.FindProperty("_doorBarrierRoot").objectReferenceValue = barrierGo;
            so.FindProperty("_switchRenderer").objectReferenceValue = switchSr;
            so.FindProperty("_switchOffSprite").objectReferenceValue = pal.SwitchCopperOff;
            so.FindProperty("_switchOnSprite").objectReferenceValue = pal.SwitchCopperOn;
            so.FindProperty("_inputActions").objectReferenceValue = inputActions;
            so.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_vfx").objectReferenceValue = vfx;
            so.FindProperty("_openVfx").objectReferenceValue = openVfx;
            so.ApplyModifiedPropertiesWithoutUndo();

            return sd;
        }

        private static TeleportGate CreateTeleportGate(
            GameObject parent,
            string name,
            Vector2 feetPos,
            bool startsDiscovered,
            Material spriteLit,
            PrisonPalette pal,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions,
            CameraFollow2D camera = null)
        {
            var root = CreateChild(parent, name);
            root.transform.position = new Vector3(feetPos.x, feetPos.y, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 1.0f);
            trigger.size = new Vector2(2.2f, 2.0f);

            var initialSprite = startsDiscovered ? pal.TpStoneDown : pal.TpStoneUp;
            var stoneGo = CreateChild(root, "StoneVisual");
            stoneGo.transform.localPosition = new Vector3(0f, -initialSprite.bounds.min.y, 0f);
            stoneGo.transform.localScale = Vector3.one;
            var stoneSr = stoneGo.AddComponent<SpriteRenderer>();
            stoneSr.sprite = initialSprite;
            stoneSr.sharedMaterial = spriteLit;
            stoneSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            stoneSr.sortingOrder = -2;
            stoneSr.color = Color.white;

            Vector2[] rockOffsets =
            {
                new Vector2(-0.85f, 1.35f),
                new Vector2(0.85f, 1.50f),
                new Vector2(-0.55f, 2.05f),
                new Vector2(0.60f, 1.95f),
            };
            for (int i = 0; i < pal.TpRocks.Length && i < rockOffsets.Length; i++)
            {
                var rockGo = CreateChild(root, $"Rock_{i}");
                rockGo.transform.localPosition = new Vector3(rockOffsets[i].x, rockOffsets[i].y, 0f);
                rockGo.transform.localScale = Vector3.one;
                var rockSr = rockGo.AddComponent<SpriteRenderer>();
                rockSr.sprite = pal.TpRocks[i];
                rockSr.sharedMaterial = spriteLit;
                rockSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
                rockSr.sortingOrder = -3;
                rockSr.color = new Color(0.88f, 0.94f, 1f, 0.92f);
            }

            var promptGo = CreateChild(root, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, "[E] Dịch chuyển", new Color(0.55f, 0.92f, 1f, 1f));
            promptGo.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            promptGo.SetActive(false);

            var lockedGo = CreateChild(root, "LockedHintIndicator");
            BuildWorldPromptBadge(lockedGo, "Chưa mở cổng cặp", new Color(0.75f, 0.82f, 0.88f, 1f), 280f);
            lockedGo.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            lockedGo.SetActive(false);

            var lightGo = CreateChild(root, "TeleportLight");
            lightGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            var tpLight = lightGo.AddComponent<Light2D>();
            tpLight.lightType = Light2D.LightType.Point;
            tpLight.color = new Color(0.38f, 0.88f, 1.0f, 1f);
            tpLight.intensity = 0.85f;
            tpLight.pointLightInnerRadius = 0.8f;
            tpLight.pointLightOuterRadius = 4.5f;
            tpLight.falloffIntensity = 0.58f;
            ConfigureGameplayPointLight(tpLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);

            var gate = root.AddComponent<TeleportGate>();
            var so = new SerializedObject(gate);
            so.FindProperty("_gateId").stringValue = name;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_triggerZone").objectReferenceValue = trigger;
            so.FindProperty("_startsDiscovered").boolValue = startsDiscovered;
            so.FindProperty("_arrivalOffset").vector2Value = Vector2.zero;
            so.FindProperty("_inputActions").objectReferenceValue = inputActions;
            so.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            so.FindProperty("_lockedHintIndicator").objectReferenceValue = lockedGo;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_stoneRenderer").objectReferenceValue = stoneSr;
            so.FindProperty("_inactiveSprite").objectReferenceValue = pal.TpStoneUp;
            so.FindProperty("_activeSprite").objectReferenceValue = pal.TpStoneDown;
            so.ApplyModifiedPropertiesWithoutUndo();

            return gate;
        }

        private static void PairTeleportGates(TeleportGate a, TeleportGate b)
        {
            var soA = new SerializedObject(a);
            soA.FindProperty("_pairedGate").objectReferenceValue = b;
            soA.ApplyModifiedPropertiesWithoutUndo();

            var soB = new SerializedObject(b);
            soB.FindProperty("_pairedGate").objectReferenceValue = a;
            soB.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLoreTablet(
            GameObject parent,
            string name,
            Vector2 floorPos,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            InputActionAsset inputActions,
            string title,
            string body,
            string promptLabel)
        {
            var root = CreateChild(parent, name);
            root.transform.position = new Vector3(floorPos.x, floorPos.y, 0f);

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 1.0f);
            trigger.size = new Vector2(3.0f, 2.2f);

            var promptGo = CreateChild(root, "PromptIndicator");
            BuildWorldPromptBadge(promptGo, promptLabel, new Color(1f, 0.88f, 0.42f, 1f), 380f);
            promptGo.transform.localPosition = new Vector3(0f, 2.40f, 0f);
            promptGo.SetActive(false);

            var bannerGo = CreateChild(root, "LoreBanner");
            var bannerText = BuildWorldLoreBanner(bannerGo, $"{title}\n{body}");
            bannerGo.transform.localPosition = new Vector3(0f, 3.85f, 0f);
            bannerGo.SetActive(false);

            var tablet = root.AddComponent<LoreTablet>();
            var so = new SerializedObject(tablet);
            so.FindProperty("_tabletId").stringValue = name;
            so.FindProperty("_loreTitle").stringValue = title;
            so.FindProperty("_loreBody").stringValue = body;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_triggerZone").objectReferenceValue = trigger;
            so.FindProperty("_inputActions").objectReferenceValue = inputActions;
            so.FindProperty("_promptIndicator").objectReferenceValue = promptGo;
            so.FindProperty("_loreBannerRoot").objectReferenceValue = bannerGo;
            so.FindProperty("_loreBannerText").objectReferenceValue = bannerText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSecondaryBiomeExitTrigger(
            GameObject parent,
            string name,
            string biomeName,
            Vector2 centerPos,
            LevelRunController run,
            SwitchDoor switchDoor,
            EnemyController firstGuardian,
            EnemyController secondGuardian,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            Vector2 triggerSize = default)
        {
            var root = CreateChild(parent, name);
            root.transform.position = new Vector3(centerPos.x, centerPos.y, 0f);

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = Vector2.zero;
            col.size = triggerSize.sqrMagnitude > 0f ? triggerSize : new Vector2(2f, 2f);

            // A door-less exit (switchDoor == null) unlocks on its guardians alone, so its locked badge must say so.
            string lockedLabel = switchDoor != null
                ? "Khóa: Cần mở công tắc & diệt 2 quái canh"
                : "Khóa: Cần diệt 2 quái canh cổng Promenade";
            var lockedGo = CreateChild(root, "LockedPromptIndicator");
            BuildWorldPromptBadge(lockedGo, lockedLabel, new Color(1f, 0.68f, 0.42f, 1f), 420f);
            lockedGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            lockedGo.SetActive(false);

            var exit = root.AddComponent<SecondaryBiomeExitTrigger>();
            var so = new SerializedObject(exit);
            so.FindProperty("_exitId").stringValue = name;
            so.FindProperty("_biomeName").stringValue = biomeName;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_switchDoor").objectReferenceValue = switchDoor;
            so.FindProperty("_firstGuardian").objectReferenceValue = firstGuardian;
            so.FindProperty("_secondGuardian").objectReferenceValue = secondGuardian;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            so.FindProperty("_lockedPromptIndicator").objectReferenceValue = lockedGo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CloneMainLevelScrollsAndGoldEconomy(
            Scene targetScene,
            PrisonPalette pal,
            GameObject playerGo,
            PlayerController playerCtrl,
            Health playerHealth,
            HitStop hitStop,
            GameObject playerHudGo,
            EnemyController[] allEnemies,
            VfxSpawner vfxSpawner)
        {
            Light2D disabledTargetGlobalLight = null;
            foreach (var rootGo in targetScene.GetRootGameObjects())
            {
                foreach (var light in rootGo.GetComponentsInChildren<Light2D>(true))
                {
                    if (light != null && light.lightType == Light2D.LightType.Global && light.enabled)
                    {
                        light.enabled = false;
                        disabledTargetGlobalLight = light;
                        break;
                    }
                }
                if (disabledTargetGlobalLight != null)
                    break;
            }

            Scene mainLevelScene = default;
            try
            {
                mainLevelScene = EditorSceneManager.OpenScene(MainLevelScenePath, OpenSceneMode.Additive);
                if (!mainLevelScene.IsValid() || !mainLevelScene.isLoaded)
                    throw new InvalidOperationException("Failed to open Main_Level.unity additively: " + MainLevelScenePath);
                var srcScroll = FindInSceneByName(mainLevelScene, "Scroll of Power - Entrance");
                var srcPowerScrollHud = FindInSceneByName(mainLevelScene, "Power Scroll HUD");
                var srcScrollCinematic = FindInSceneByName(mainLevelScene, "Scroll cinematic");
                var srcGoldHud = FindInSceneByName(mainLevelScene, "Gold HUD");

                if (srcScroll == null)
                    throw new InvalidOperationException("Main_Level.unity missing 'Scroll of Power - Entrance'.");
                if (srcPowerScrollHud == null)
                    throw new InvalidOperationException("Main_Level.unity missing 'Power Scroll HUD'.");
                if (srcScrollCinematic == null)
                    throw new InvalidOperationException("Main_Level.unity missing 'Scroll cinematic'.");
                if (srcGoldHud == null)
                    throw new InvalidOperationException("Main_Level.unity missing 'Gold HUD'.");

                EditorSceneManager.SetActiveScene(targetScene);

                // 1. Power Scrolls container + 3 cloned PowerScrollPickups with ScrollAura_R5_0/1/2 under _icon
                var scrollsRoot = new GameObject("Power Scrolls");
                SceneManager.MoveGameObjectToScene(scrollsRoot, targetScene);
                var scrollInteraction = scrollsRoot.AddComponent<PowerScrollInteraction>();

                var scrollEast = CloneAndConfigureScroll(srcScroll, targetScene, scrollsRoot.transform, "I11_Scroll_EastCourtyard", new Vector3(156.5f, 0f, 0f), 0, pal);
                var scrollWest = CloneAndConfigureScroll(srcScroll, targetScene, scrollsRoot.transform, "I12_Scroll_WestArmory", new Vector3(-95f, 24f, 0f), 1, pal);
                var scrollHighBastion = CloneAndConfigureScroll(srcScroll, targetScene, scrollsRoot.transform, "I17_Scroll_HighBastion", new Vector3(114f, 40f, 0f), 2, pal);
                var scrollVaultA1 = CloneAndConfigureScroll(srcScroll, targetScene, scrollsRoot.transform, "I20_Scroll_VaultA1", new Vector3(198.5f, 29f, 0f), 3, pal);
                var scrollVaultA2 = CloneAndConfigureScroll(srcScroll, targetScene, scrollsRoot.transform, "I21_Scroll_VaultA2", new Vector3(201.2f, 29f, 0f), 4, pal);

                var soInteraction = new SerializedObject(scrollInteraction);
                var scrollsProp = soInteraction.FindProperty("_scrolls");
                scrollsProp.arraySize = 5;
                scrollsProp.GetArrayElementAtIndex(0).objectReferenceValue = scrollEast;
                scrollsProp.GetArrayElementAtIndex(1).objectReferenceValue = scrollWest;
                scrollsProp.GetArrayElementAtIndex(2).objectReferenceValue = scrollHighBastion;
                scrollsProp.GetArrayElementAtIndex(3).objectReferenceValue = scrollVaultA1;
                scrollsProp.GetArrayElementAtIndex(4).objectReferenceValue = scrollVaultA2;
                soInteraction.ApplyModifiedPropertiesWithoutUndo();

                var soPlayer = new SerializedObject(playerCtrl);
                soPlayer.FindProperty("_powerScrolls").objectReferenceValue = scrollInteraction;
                soPlayer.ApplyModifiedPropertiesWithoutUndo();

                // 2. Clone Scroll cinematic + attach dedicated AudioSource + promote any Default-layer renderers to VFX
                var clonedCinematic = UnityEngine.Object.Instantiate(srcScrollCinematic);
                clonedCinematic.name = "Scroll cinematic";
                clonedCinematic.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(clonedCinematic, targetScene);
                int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
                foreach (var rend in clonedCinematic.GetComponentsInChildren<Renderer>(true))
                {
                    if (rend != null && rend.sortingLayerID == 0)
                        rend.sortingLayerID = vfxLayerId;
                }
                var cinematicAudio = clonedCinematic.GetComponent<AudioSource>();
                if (cinematicAudio == null)
                    cinematicAudio = clonedCinematic.AddComponent<AudioSource>();
                cinematicAudio.playOnAwake = false;
                cinematicAudio.spatialBlend = 0f;

                // 3. Clone Power Scroll HUD + rebind ScrollUpgradeMenu & ScrollPickupPresentation
                var clonedScrollHud = UnityEngine.Object.Instantiate(srcPowerScrollHud);
                clonedScrollHud.name = "Power Scroll HUD";
                clonedScrollHud.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(clonedScrollHud, targetScene);

                var scrollMenu = clonedScrollHud.GetComponent<ScrollUpgradeMenu>();
                var clonedPres = clonedScrollHud.GetComponent<ScrollPickupPresentation>();
                var srcPres = srcPowerScrollHud.GetComponent<ScrollPickupPresentation>();
                if (scrollMenu == null || clonedPres == null || srcPres == null)
                    throw new InvalidOperationException("Cloned Power Scroll HUD is missing ScrollUpgradeMenu or ScrollPickupPresentation.");

                var soMenu = new SerializedObject(scrollMenu);
                soMenu.FindProperty("_player").objectReferenceValue = playerCtrl;
                soMenu.FindProperty("_gameplayTime").objectReferenceValue = hitStop;
                soMenu.FindProperty("_presentation").objectReferenceValue = clonedPres;
                soMenu.ApplyModifiedPropertiesWithoutUndo();

                var playerSprite = new SerializedObject(playerCtrl).FindProperty("_sprite")?.objectReferenceValue as SpriteRenderer
                    ?? playerGo.GetComponentInChildren<SpriteRenderer>(true);
                var gameplayCanvas = playerHudGo.GetComponent<Canvas>() ?? playerHudGo.GetComponentInChildren<Canvas>(true);

                var soSrcPres = new SerializedObject(srcPres);
                var soPres = new SerializedObject(clonedPres);
                soPres.FindProperty("_playerSprite").objectReferenceValue = playerSprite;
                soPres.FindProperty("_gameplayHud").objectReferenceValue = gameplayCanvas;
                soPres.FindProperty("_audio").objectReferenceValue = cinematicAudio;

                RemapCinematicComponentRef(soSrcPres, soPres, "_pose", srcScrollCinematic.transform, clonedCinematic.transform);
                RemapCinematicComponentRef(soSrcPres, soPres, "_scroll", srcScrollCinematic.transform, clonedCinematic.transform);
                RemapCinematicComponentRef(soSrcPres, soPres, "_halo", srcScrollCinematic.transform, clonedCinematic.transform);
                RemapCinematicComponentRef(soSrcPres, soPres, "_light", srcScrollCinematic.transform, clonedCinematic.transform);
                RemapCinematicComponentArray(soSrcPres, soPres, "_rays", srcScrollCinematic.transform, clonedCinematic.transform);
                RemapCinematicComponentArray(soSrcPres, soPres, "_stars", srcScrollCinematic.transform, clonedCinematic.transform);
                soPres.ApplyModifiedPropertiesWithoutUndo();

                // 4. Create Run Gold (GoldWallet + EnemyGoldDrops with all 34 enemies) + clone Gold HUD under PlayerHUD
                var runGoldGo = new GameObject("Run Gold");
                SceneManager.MoveGameObjectToScene(runGoldGo, targetScene);
                var goldWallet = runGoldGo.AddComponent<GoldWallet>();
                var goldDrops = runGoldGo.AddComponent<EnemyGoldDrops>();
                var goldPickupPrefab = AssetDatabase.LoadAssetAtPath<GoldPickup>(GoldPickupPrefabPath);
                if (goldPickupPrefab == null)
                    throw new InvalidOperationException("Missing GoldPickup prefab: " + GoldPickupPrefabPath);

                var soDrops = new SerializedObject(goldDrops);
                soDrops.FindProperty("_wallet").objectReferenceValue = goldWallet;
                soDrops.FindProperty("_player").objectReferenceValue = playerHealth;
                soDrops.FindProperty("_pickupPrefab").objectReferenceValue = goldPickupPrefab;
                soDrops.FindProperty("_goldPerEnemy").intValue = 20;
                var enemiesProp = soDrops.FindProperty("_enemies");
                enemiesProp.arraySize = allEnemies.Length;
                for (int i = 0; i < allEnemies.Length; i++)
                    enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = allEnemies[i];
                soDrops.ApplyModifiedPropertiesWithoutUndo();

                var srcGoldRect = srcGoldHud.GetComponent<RectTransform>();
                var clonedGoldHud = UnityEngine.Object.Instantiate(srcGoldHud);
                clonedGoldHud.name = "Gold HUD";
                clonedGoldHud.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(clonedGoldHud, targetScene);
                clonedGoldHud.transform.SetParent(playerHudGo.transform, false);
                var clonedGoldRect = clonedGoldHud.GetComponent<RectTransform>();
                if (srcGoldRect != null && clonedGoldRect != null)
                {
                    clonedGoldRect.anchorMin = srcGoldRect.anchorMin;
                    clonedGoldRect.anchorMax = srcGoldRect.anchorMax;
                    clonedGoldRect.anchoredPosition = srcGoldRect.anchoredPosition;
                    clonedGoldRect.sizeDelta = srcGoldRect.sizeDelta;
                    clonedGoldRect.pivot = srcGoldRect.pivot;
                    clonedGoldRect.localRotation = srcGoldRect.localRotation;
                    clonedGoldRect.localScale = srcGoldRect.localScale;
                }

                var goldHud = clonedGoldHud.GetComponent<GoldHud>();
                if (goldHud == null)
                    throw new InvalidOperationException("Cloned Gold HUD is missing GoldHud component.");
                var soGoldHud = new SerializedObject(goldHud);
                soGoldHud.FindProperty("_wallet").objectReferenceValue = goldWallet;
                soGoldHud.ApplyModifiedPropertiesWithoutUndo();

                // 4b. P0-04 Part B (Room N): 4 statically placed treasure coins (25 each, 100 gold) resting above the gallery floor
                var goldCacheRoot = new GameObject("Room N Gold Cache");
                SceneManager.MoveGameObjectToScene(goldCacheRoot, targetScene);
                int goldCacheVfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
                float[] goldCacheXs = { 158f, 159f, 160f, 161f };
                for (int i = 0; i < goldCacheXs.Length; i++)
                {
                    var coinGo = (GameObject)PrefabUtility.InstantiatePrefab(goldPickupPrefab.gameObject, targetScene);
                    coinGo.name = $"GoldCache_N_{i}";
                    coinGo.transform.SetParent(goldCacheRoot.transform, true);
                    coinGo.transform.position = new Vector3(goldCacheXs[i], 8.45f, 0f);

                    foreach (var coinSr in coinGo.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        coinSr.sortingLayerID = goldCacheVfxLayerId;
                        if (coinSr.sortingOrder < 5)
                            coinSr.sortingOrder = 5;
                    }

                    var soCoin = new SerializedObject(coinGo.GetComponent<GoldPickup>());
                    soCoin.FindProperty("_wallet").objectReferenceValue = goldWallet;
                    soCoin.FindProperty("_player").objectReferenceValue = playerHealth;
                    soCoin.FindProperty("_value").intValue = 25;
                    soCoin.FindProperty("_staticPlacement").boolValue = true;
                    soCoin.ApplyModifiedPropertiesWithoutUndo();
                }

                // 5. Clone "Enemy_Shield hit feedback" from Main_Level's "Combat and Pickup Feedback" onto all 34 enemies
                var srcFeedbackRoot = FindInSceneByName(mainLevelScene, "Combat and Pickup Feedback");
                if (srcFeedbackRoot == null)
                    throw new InvalidOperationException("Main_Level.unity missing 'Combat and Pickup Feedback'.");
                var srcEnemyFeedback = srcFeedbackRoot.GetComponentsInChildren<EnemyImpactFeedback>(true)
                    .FirstOrDefault(f => f != null && f.gameObject.name == "Enemy_Shield hit feedback")
                    ?? srcFeedbackRoot.GetComponentInChildren<EnemyImpactFeedback>(true);
                if (srcEnemyFeedback == null)
                    throw new InvalidOperationException("Main_Level.unity 'Combat and Pickup Feedback' missing EnemyImpactFeedback template.");

                var feedbackRoot = new GameObject("Combat and Pickup Feedback");
                SceneManager.MoveGameObjectToScene(feedbackRoot, targetScene);
                var feedbackAudio = feedbackRoot.AddComponent<AudioSource>();
                feedbackAudio.playOnAwake = false;
                feedbackAudio.spatialBlend = 0f;

                var srcPickupFeedback = srcFeedbackRoot.GetComponent<ItemPickupFeedback>()
                    ?? srcFeedbackRoot.GetComponentInChildren<ItemPickupFeedback>(true);
                if (srcPickupFeedback == null)
                    throw new InvalidOperationException("Main_Level.unity 'Combat and Pickup Feedback' missing ItemPickupFeedback.");

                var soSrcPickup = new SerializedObject(srcPickupFeedback);
                var pickupFeedback = feedbackRoot.AddComponent<ItemPickupFeedback>();
                var soPickup = new SerializedObject(pickupFeedback);
                soPickup.FindProperty("_player").objectReferenceValue = playerGo.transform;
                soPickup.FindProperty("_wallet").objectReferenceValue = goldWallet;
                soPickup.FindProperty("_effects").objectReferenceValue = vfxSpawner;
                soPickup.FindProperty("_glint").objectReferenceValue = soSrcPickup.FindProperty("_glint").objectReferenceValue;
                soPickup.FindProperty("_audio").objectReferenceValue = feedbackAudio;
                soPickup.FindProperty("_goldSound").objectReferenceValue = soSrcPickup.FindProperty("_goldSound").objectReferenceValue;
                soPickup.FindProperty("_weaponSound").objectReferenceValue = soSrcPickup.FindProperty("_weaponSound").objectReferenceValue;
                var weaponsProp = soPickup.FindProperty("_weapons");
                weaponsProp.arraySize = 0;
                soPickup.ApplyModifiedPropertiesWithoutUndo();

                int enemyBarVfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
                foreach (var enemy in allEnemies)
                {
                    if (enemy == null)
                        continue;

                    var fbClone = UnityEngine.Object.Instantiate(srcEnemyFeedback.gameObject);
                    fbClone.name = $"{enemy.name} hit feedback";
                    fbClone.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(fbClone, targetScene);
                    fbClone.transform.SetParent(feedbackRoot.transform, false);

                    var fbComp = fbClone.GetComponent<EnemyImpactFeedback>();
                    var soFb = new SerializedObject(fbComp);
                    soFb.FindProperty("_target").objectReferenceValue = enemy.GetComponent<Health>();
                    soFb.FindProperty("_body").objectReferenceValue = enemy.GetComponent<BoxCollider2D>();
                    soFb.FindProperty("_vfx").objectReferenceValue = vfxSpawner;
                    soFb.FindProperty("_audio").objectReferenceValue = feedbackAudio;
                    soFb.ApplyModifiedPropertiesWithoutUndo();

                    var barGo = soFb.FindProperty("_bar").objectReferenceValue as GameObject;
                    if (barGo != null)
                    {
                        foreach (var sr in barGo.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            if (sr == null)
                                continue;
                            sr.sortingLayerID = enemyBarVfxLayerId;
                            if (sr.gameObject.name == "Outline")
                                sr.sortingOrder = 8;
                            else if (sr.gameObject.name == "Track")
                                sr.sortingOrder = 9;
                            else if (sr.gameObject.name == "Remaining health")
                                sr.sortingOrder = 10;
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.SetActiveScene(targetScene);
                if (mainLevelScene.IsValid() && mainLevelScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(mainLevelScene, true);
                    EditorUtility.UnloadUnusedAssetsImmediate(true);
                    GC.Collect();
                }
                if (disabledTargetGlobalLight != null)
                    disabledTargetGlobalLight.enabled = true;
            }
        }

        private static PowerScrollPickup CloneAndConfigureScroll(
            GameObject srcScroll,
            Scene targetScene,
            Transform parent,
            string instanceName,
            Vector3 worldPos,
            int auraIndex,
            PrisonPalette pal)
        {
            var clone = UnityEngine.Object.Instantiate(srcScroll);
            clone.name = instanceName;
            clone.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            clone.transform.SetParent(parent, true);
            clone.transform.position = worldPos;
            clone.transform.localScale = Vector3.one;

            // Remove any stale ScrollAura_R4_* or ScrollAura_R5_* children
            var staleAuras = clone.GetComponentsInChildren<Transform>(true)
                .Where(t => t != null && (t.name.StartsWith("ScrollAura_R4_", StringComparison.Ordinal) || t.name.StartsWith("ScrollAura_R5_", StringComparison.Ordinal)))
                .Select(t => t.gameObject)
                .ToArray();
            foreach (var stale in staleAuras)
                UnityEngine.Object.DestroyImmediate(stale);

            var pickup = clone.GetComponent<PowerScrollPickup>();
            if (pickup == null)
                throw new InvalidOperationException($"Cloned scroll '{instanceName}' is missing PowerScrollPickup.");

            var soScroll = new SerializedObject(pickup);
            var iconTr = soScroll.FindProperty("_icon").objectReferenceValue as Transform;
            var glowGo = soScroll.FindProperty("_glow").objectReferenceValue as GameObject;
            var promptGo = soScroll.FindProperty("_prompt")?.objectReferenceValue as GameObject;
            if (iconTr == null)
                throw new InvalidOperationException($"Cloned scroll '{instanceName}' is missing _icon Transform.");
            if (glowGo == null || glowGo.GetComponent<Light2D>() == null)
                throw new InvalidOperationException($"Cloned scroll '{instanceName}' _glow must point to Scroll glow Light2D.");

            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");

            // Promote Scroll prompt MeshRenderer(s) from Default (0) to VFX/25 so TM_Background never occludes them
            if (promptGo != null)
            {
                foreach (var promptRend in promptGo.GetComponentsInChildren<Renderer>(true))
                {
                    if (promptRend != null)
                    {
                        promptRend.sortingLayerID = vfxLayerId;
                        promptRend.sortingOrder = 25;
                    }
                }
            }

            // Guard against any other Default-layer renderer on the cloned scroll hierarchy
            foreach (var rend in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (rend != null && rend.sortingLayerID == 0)
                    rend.sortingLayerID = groundLayerId;
            }

            var auraGo = CreateChild(iconTr.gameObject, $"ScrollAura_R5_{auraIndex}");
            auraGo.transform.localPosition = Vector3.zero;
            auraGo.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
            var auraSr = auraGo.AddComponent<SpriteRenderer>();
            auraSr.sprite = pal.FxGlowWhite;
            auraSr.sharedMaterial = pal.MatEmissiveScroll;
            auraSr.sortingLayerID = vfxLayerId;
            auraSr.sortingOrder = -2;
            auraSr.color = new Color(1f, 0.90f, 0.65f, 0.72f);

            return pickup;
        }

        private static GameObject FindInSceneByName(Scene scene, string exactName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindRecursiveByName(root.transform, exactName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static GameObject FindRecursiveByName(Transform current, string exactName)
        {
            if (current.name == exactName)
                return current.gameObject;

            for (int i = 0; i < current.childCount; i++)
            {
                var found = FindRecursiveByName(current.GetChild(i), exactName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void RemapCinematicComponentRef(
            SerializedObject soSrc,
            SerializedObject soDst,
            string propName,
            Transform srcRoot,
            Transform dstRoot)
        {
            var srcComp = soSrc.FindProperty(propName)?.objectReferenceValue as Component;
            if (srcComp == null)
                throw new InvalidOperationException($"Source ScrollPickupPresentation.{propName} is null.");

            var mapped = MapComponentAcrossClone(srcComp, srcRoot, dstRoot);
            soDst.FindProperty(propName).objectReferenceValue = mapped;
        }

        private static void RemapCinematicComponentArray(
            SerializedObject soSrc,
            SerializedObject soDst,
            string propName,
            Transform srcRoot,
            Transform dstRoot)
        {
            var srcArr = soSrc.FindProperty(propName);
            var dstArr = soDst.FindProperty(propName);
            dstArr.arraySize = srcArr.arraySize;
            for (int i = 0; i < srcArr.arraySize; i++)
            {
                var srcComp = srcArr.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                if (srcComp == null)
                    throw new InvalidOperationException($"Source ScrollPickupPresentation.{propName}[{i}] is null.");

                dstArr.GetArrayElementAtIndex(i).objectReferenceValue = MapComponentAcrossClone(srcComp, srcRoot, dstRoot);
            }
        }

        private static Component MapComponentAcrossClone(Component srcComp, Transform srcRoot, Transform dstRoot)
        {
            var indices = new List<int>();
            Transform cursor = srcComp.transform;
            while (cursor != null && cursor != srcRoot)
            {
                indices.Add(cursor.GetSiblingIndex());
                cursor = cursor.parent;
            }

            if (cursor != srcRoot)
                throw new InvalidOperationException($"Component '{srcComp.name}' ({srcComp.GetType().Name}) is not under '{srcRoot.name}'.");

            Transform target = dstRoot;
            for (int i = indices.Count - 1; i >= 0; i--)
                target = target.GetChild(indices[i]);

            var mapped = target.GetComponent(srcComp.GetType());
            if (mapped == null)
                throw new InvalidOperationException($"Cloned child '{target.name}' is missing component {srcComp.GetType().Name}.");

            return mapped;
        }

        private static void BuildAmbientVfx(Material particlesUnlitMat, Transform camTransform)
        {
            var container = new GameObject("AmbientVFX");

            // 1. AmbientDustMotes attached to camera viewport (28u x 16u crisp visible box)
            var dustGo = CreateChild(container, "AmbientDustMotes");
            dustGo.transform.position = new Vector3(camTransform.position.x, camTransform.position.y, 0f);
            var dustPs = dustGo.AddComponent<ParticleSystem>();
            var dustMain = dustPs.main;
            dustMain.loop = true;
            dustMain.playOnAwake = true;
            dustMain.prewarm = true;
            dustMain.simulationSpace = ParticleSystemSimulationSpace.World;
            dustMain.maxParticles = 90;
            dustMain.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
            dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            dustMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            dustMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.91f, 0.80f, 0.24f));

            var dustEmission = dustPs.emission;
            dustEmission.enabled = true;
            dustEmission.rateOverTime = 18f;

            var dustShape = dustPs.shape;
            dustShape.enabled = true;
            dustShape.shapeType = ParticleSystemShapeType.Box;
            dustShape.scale = new Vector3(28f, 16f, 0.1f);

            var dustVel = dustPs.velocityOverLifetime;
            dustVel.enabled = true;
            dustVel.space = ParticleSystemSimulationSpace.World;
            dustVel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
            dustVel.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.08f);
            dustVel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var dustCol = dustPs.colorOverLifetime;
            dustCol.enabled = true;
            var dustGrad = new Gradient();
            dustGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.95f, 0.91f, 0.80f), 0f), new GradientColorKey(new Color(0.95f, 0.91f, 0.80f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            dustCol.color = new ParticleSystem.MinMaxGradient(dustGrad);

            var dustRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
            if (dustRenderer != null)
            {
                dustRenderer.sharedMaterial = particlesUnlitMat;
                dustRenderer.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
                dustRenderer.sortingOrder = 12;
            }

            var dustFollow = dustGo.AddComponent<CameraFollowEmitter2D>();
            dustFollow.Configure(camTransform, Vector2.zero);
            var soDustFollow = new SerializedObject(dustFollow);
            soDustFollow.FindProperty("_cameraTransform").objectReferenceValue = camTransform;
            soDustFollow.FindProperty("_offset").vector2Value = Vector2.zero;
            soDustFollow.ApplyModifiedPropertiesWithoutUndo();

            // 2. AmbientDrippingWater attached to top of camera viewport (26u x 1.5u ceiling strip)
            var dripGo = CreateChild(container, "AmbientDrippingWater");
            dripGo.transform.position = new Vector3(camTransform.position.x, camTransform.position.y + 6.0f, 0f);
            var dripPs = dripGo.AddComponent<ParticleSystem>();
            var dripMain = dripPs.main;
            dripMain.loop = true;
            dripMain.playOnAwake = true;
            dripMain.prewarm = true;
            dripMain.simulationSpace = ParticleSystemSimulationSpace.World;
            dripMain.maxParticles = 48;
            dripMain.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            dripMain.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            dripMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            dripMain.gravityModifier = 0.85f;
            dripMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.82f, 0.92f, 0.38f));

            var dripEmission = dripPs.emission;
            dripEmission.enabled = true;
            dripEmission.rateOverTime = 7f;

            var dripShape = dripPs.shape;
            dripShape.enabled = true;
            dripShape.shapeType = ParticleSystemShapeType.Box;
            dripShape.scale = new Vector3(26f, 1.5f, 0.1f);

            var dripRenderer = dripGo.GetComponent<ParticleSystemRenderer>();
            if (dripRenderer != null)
            {
                dripRenderer.sharedMaterial = particlesUnlitMat;
                dripRenderer.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
                dripRenderer.sortingOrder = 11;
            }

            var dripFollow = dripGo.AddComponent<CameraFollowEmitter2D>();
            var dripOffset = new Vector2(0f, 6.0f);
            dripFollow.Configure(camTransform, dripOffset);
            var soDripFollow = new SerializedObject(dripFollow);
            soDripFollow.FindProperty("_cameraTransform").objectReferenceValue = camTransform;
            soDripFollow.FindProperty("_offset").vector2Value = dripOffset;
            soDripFollow.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindUiInputModuleActions(InputSystemUIInputModule uiModule, InputActionAsset actions)
        {
            var uiMap = actions.FindActionMap("UI", throwIfNotFound: false);
            if (uiMap == null)
                return;

            var allRefs = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath).OfType<InputActionReference>().ToArray();
            InputActionReference FindRef(string actionName) =>
                allRefs.FirstOrDefault(r => r != null && r.action != null && r.action.actionMap == uiMap && r.action.name == actionName);

            uiModule.point = FindRef("Point");
            uiModule.leftClick = FindRef("Click");
            uiModule.rightClick = FindRef("RightClick");
            uiModule.middleClick = FindRef("MiddleClick");
            uiModule.scrollWheel = FindRef("ScrollWheel");
            uiModule.move = FindRef("Navigate");
            uiModule.submit = FindRef("Submit");
            uiModule.cancel = FindRef("Cancel");
        }

        private static void InstantiateRoom(Transform parent, string roomName)
        {
            string path = $"{RoomsFolder}/{roomName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new InvalidOperationException("Missing room prefab: " + path);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = $"Room_{roomName}";
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }

        private static EnemyController SpawnDormantEnemy(
            string prefabPath,
            string instanceName,
            Vector3 position,
            Transform parent,
            Transform playerTarget,
            ProjectileSpawner projectiles,
            VfxSpawner vfx)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Missing enemy prefab: " + prefabPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = instanceName;
            inst.transform.position = position;

            var ctrl = inst.GetComponent<EnemyController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_player").objectReferenceValue = playerTarget;
            so.FindProperty("_projectiles").objectReferenceValue = projectiles;
            so.FindProperty("_vfx").objectReferenceValue = vfx;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Dormant before tick 0 per Plan §4 W1
            ctrl.enabled = false;
            var rb = inst.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.simulated = false;

            return ctrl;
        }

        private static void CreateEncounterZone(
            Transform parent,
            string goName,
            string zoneId,
            Vector2 minBounds,
            Vector2 maxBounds,
            LevelRunController run,
            PlayerController player,
            Health playerHealth,
            BoxCollider2D playerCol,
            EnemyController[] enemies)
        {
            var go = CreateChild(parent.gameObject, goName);
            go.transform.position = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
            var zone = go.AddComponent<EncounterZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_minBounds").vector2Value = minBounds;
            so.FindProperty("_maxBounds").vector2Value = maxBounds;
            so.FindProperty("_run").objectReferenceValue = run;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("_playerCollider").objectReferenceValue = playerCol;
            var enemiesProp = so.FindProperty("_enemies");
            enemiesProp.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++)
                enemiesProp.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            so.FindProperty("_sleepEnemiesOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePlayerWallClingAnimation()
        {
            const string animPath = "Assets/_Project/Features/Player/Animations/Player_WallCling.anim";
            const string controllerPath = "Assets/_Project/Features/Player/Animations/Player.controller";

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (clip == null)
            {
                var keyframes = new ObjectReferenceKeyframe[12];
                for (int i = 0; i < 12; i++)
                {
                    string spritePath = $"Assets/_Project/Art/Characters/Player/Sprites/wallClimbA_{i:00}.png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                        throw new InvalidOperationException("Missing wall-cling sprite: " + spritePath);

                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = i / 12f,
                        value = sprite
                    };
                }

                clip = new AnimationClip { frameRate = 12f };
                var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                AssetDatabase.CreateAsset(clip, animPath);
                AssetDatabase.SaveAssets();
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                throw new InvalidOperationException("Missing Player AnimatorController: " + controllerPath);

            bool dirty = false;
            if (!controller.parameters.Any(p => p.name == "WallClinging"))
            {
                controller.AddParameter("WallClinging", AnimatorControllerParameterType.Bool);
                dirty = true;
            }

            var sm = controller.layers[0].stateMachine;
            var wallClingState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "WallCling");
            if (wallClingState == null)
            {
                wallClingState = sm.AddState("WallCling", new Vector3(380f, 520f, 0f));
                wallClingState.writeDefaultValues = true;
                dirty = true;
            }
            if (wallClingState.motion != clip)
            {
                wallClingState.motion = clip;
                dirty = true;
            }

            var anyToWallCling = sm.anyStateTransitions.FirstOrDefault(t => t.destinationState == wallClingState);
            if (anyToWallCling == null)
            {
                anyToWallCling = sm.AddAnyStateTransition(wallClingState);
                anyToWallCling.duration = 0f;
                anyToWallCling.hasExitTime = false;
                anyToWallCling.canTransitionToSelf = false;
                anyToWallCling.AddCondition(AnimatorConditionMode.If, 0f, "WallClinging");
                anyToWallCling.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
                anyToWallCling.AddCondition(AnimatorConditionMode.IfNot, 0f, "Hurt");
                dirty = true;
            }

            var fallState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Fall");
            if (fallState == null)
                throw new InvalidOperationException("Player.controller missing 'Fall' state.");

            var wallClingToFall = wallClingState.transitions.FirstOrDefault(t => t.destinationState == fallState);
            if (wallClingToFall == null)
            {
                wallClingToFall = wallClingState.AddTransition(fallState);
                wallClingToFall.duration = 0f;
                wallClingToFall.hasExitTime = false;
                wallClingToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "WallClinging");
                dirty = true;
            }

            var jumpState = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Jump");
            foreach (var tr in sm.anyStateTransitions)
            {
                if (tr.destinationState != fallState && tr.destinationState != jumpState)
                    continue;
                if (!tr.conditions.Any(c => c.parameter == "WallClinging"))
                {
                    tr.AddCondition(AnimatorConditionMode.IfNot, 0f, "WallClinging");
                    dirty = true;
                }
            }

            if (dirty)
                EditorUtility.SetDirty(controller);
        }

        private static void EnsurePlayerPrefabVfxWiring()
        {
            var headSlimeVfx = AssetDatabase.LoadAssetAtPath<VfxDefinition>("Assets/_Project/Features/VFX/Data/Vfx_HeadSlime.asset");
            if (headSlimeVfx == null)
                throw new InvalidOperationException("Missing Vfx_HeadSlime.asset.");

            var contactGlintVfx = AssetDatabase.LoadAssetAtPath<VfxDefinition>("Assets/_Project/Features/VFX/Data/Vfx_ContactGlint.asset");
            if (contactGlintVfx == null)
                throw new InvalidOperationException("Missing Vfx_ContactGlint.asset.");

            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefabRoot == null)
                throw new InvalidOperationException("Missing Player prefab: " + PlayerPrefabPath);

            try
            {
                bool changed = false;

                var spawnHead = prefabRoot.GetComponentInChildren<PlayerSpawnHead>(true);
                if (spawnHead != null)
                {
                    var soHead = new SerializedObject(spawnHead);
                    var slimeProp = soHead.FindProperty("_slimeVfx");
                    if (slimeProp != null && slimeProp.objectReferenceValue == null)
                    {
                        slimeProp.objectReferenceValue = headSlimeVfx;
                        soHead.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }

                    var headSr = spawnHead.GetComponent<SpriteRenderer>();
                    if (headSr != null && headSr.enabled)
                    {
                        headSr.enabled = false;
                        changed = true;
                    }

                    var headTrail = spawnHead.GetComponentInChildren<TrailRenderer>(true);
                    if (headTrail != null && headTrail.emitting)
                    {
                        headTrail.emitting = false;
                        changed = true;
                    }
                }

                var rimLight = prefabRoot.transform.Find("PlayerRimLight")?.GetComponent<Light2D>();
                if (rimLight != null)
                {
                    var soRim = new SerializedObject(rimLight);
                    var qProp = soRim.FindProperty("m_NormalMapQuality");
                    var useNmProp = soRim.FindProperty("m_UseNormalMap");
                    bool rimChanged = false;
                    if (qProp != null && qProp.intValue != 0)
                    {
                        qProp.intValue = 0;
                        rimChanged = true;
                    }
                    if (useNmProp != null && useNmProp.boolValue)
                    {
                        useNmProp.boolValue = false;
                        rimChanged = true;
                    }
                    if (rimChanged)
                    {
                        soRim.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                var playerVfx = prefabRoot.GetComponentInChildren<PlayerVfx>(true);
                if (playerVfx != null)
                {
                    var soPvfx = new SerializedObject(playerVfx);
                    var dodgeProp = soPvfx.FindProperty("_dodgeGlintVfx");
                    if (dodgeProp == null)
                        throw new InvalidOperationException("PlayerVfx missing serialized field '_dodgeGlintVfx'.");
                    if (dodgeProp.objectReferenceValue == null)
                    {
                        dodgeProp.objectReferenceValue = contactGlintVfx;
                        soPvfx.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static VfxDefinition[] LoadAllVfxDefinitions()
        {
            string[] paths =
            {
                "Assets/_Project/Features/VFX/Data/Vfx_AirJump.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_CollectedGlint.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_ContactBlood.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_ContactGlint.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_Explosion.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_FatalBlood.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_HeadSlime.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_Land.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_SwingStartSwordA.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_SwingStartSwordB.asset",
                "Assets/_Project/Features/VFX/Data/Vfx_SwingStartSwordC.asset",
            };

            var defs = new VfxDefinition[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                defs[i] = AssetDatabase.LoadAssetAtPath<VfxDefinition>(paths[i]);
                if (defs[i] == null)
                    throw new InvalidOperationException("Missing VfxDefinition: " + paths[i]);
            }
            return defs;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int existingIndex = scenes.FindIndex(s => string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                scenes[existingIndex].enabled = true;
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ==================== GEOMETRY & ASSET HELPERS ====================

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static BoxCollider2D AddSolidBox(GameObject parent, string name, float xMin, float xMax, float yMin, float yMax)
        {
            var go = CreateChild(parent, name);
            go.layer = LayerGround;
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(xMax - xMin, yMax - yMin);
            col.offset = Vector2.zero;
            col.isTrigger = false;
            return col;
        }

        private static BoxCollider2D AddOneWayBox(GameObject parent, string name, float xMin, float xMax, float topY)
        {
            const float thickness = 0.25f;
            var go = CreateChild(parent, name);
            go.layer = LayerOneWay;
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, topY - thickness * 0.5f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(xMax - xMin, thickness);
            col.offset = Vector2.zero;
            col.isTrigger = false;
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            effector.rotationalOffset = 0f;
            return col;
        }

        private static BoxCollider2D AddClimbableTrigger(GameObject parent, string name, Vector2 center, Vector2 size)
        {
            var go = CreateChild(parent, name);
            go.layer = LayerClimbable;
            go.transform.position = new Vector3(center.x, center.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = Vector2.zero;
            col.isTrigger = true;
            return col;
        }

        private static Tile LoadTile(string relativeTilePath)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>($"{TilesRoot}/{relativeTilePath}.asset");
            if (tile == null || tile.sprite == null)
                throw new InvalidOperationException($"Missing GothicDungeon tile/sprite: {TilesRoot}/{relativeTilePath}.asset");
            return tile;
        }

        private static Sprite LoadTileSprite(string relativeTilePath) => LoadTile(relativeTilePath).sprite;

        private static Tile LoadPrisonTile(string tileName)
        {
            string path = $"{PrisonTilesRoot}/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null || tile.sprite == null)
                throw new InvalidOperationException("Missing Prison tile: " + path);
            return tile;
        }

        private static Sprite LoadPrisonSprite(string spriteFile)
        {
            string path = $"{PrisonSpritesRoot}/{spriteFile}";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("Missing Prison sprite: " + path);
            return sprite;
        }

        private static Sprite LoadSpriteAtPath(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("Missing Sprite at path: " + path);
            return sprite;
        }

        private static Material LoadMaterialAtPath(string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                throw new InvalidOperationException("Missing Material at path: " + path);
            return mat;
        }

        private static PhysicsMaterial2D LoadPhysicsMaterialAtPath(string path)
        {
            var physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (physicsMaterial == null)
                throw new InvalidOperationException("Missing PhysicsMaterial2D at path: " + path);
            return physicsMaterial;
        }

        // ==================== DEAD CELLS PRISON 1.0u TILEMAP PAINTERS ====================

        private static void CreateRoomVisualGrid(
            GameObject artRoot,
            Material spriteLit,
            out Tilemap bgTm,
            out Tilemap decalTm,
            out Tilemap solidTm,
            out Tilemap rimTm)
        {
            var gridGo = CreateChild(artRoot, "VisualGrid");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");

            bgTm = CreateVisualTilemap(gridGo, "TM_Background", spriteLit, bgLayerId, -20, Color.white);
            decalTm = CreateVisualTilemap(gridGo, "TM_WallDecals", spriteLit, bgLayerId, -18, new Color(0.88f, 0.92f, 0.90f, 1f));
            solidTm = CreateVisualTilemap(gridGo, "TM_SolidVisual", spriteLit, groundLayerId, -5, Color.white);
            rimTm = CreateVisualTilemap(gridGo, "TM_SolidRim", spriteLit, groundLayerId, -3, Color.white);
        }

        private static Tilemap CreateVisualTilemap(GameObject gridGo, string name, Material spriteLit, int sortingLayerId, int sortingOrder, Color tint)
        {
            var go = CreateChild(gridGo, name);
            var tm = go.AddComponent<Tilemap>();
            tm.color = tint;
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = spriteLit;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
            return tm;
        }

        private static void SetTileWithMatrix(Tilemap tm, Vector3Int cell, Tile tile, Matrix4x4 matrix)
        {
            tm.SetTile(cell, tile);
            tm.SetTileFlags(cell, TileFlags.None);
            tm.SetTransformMatrix(cell, matrix);
        }

        private static void PaintPrisonBackWall(Tilemap bgTm, Tilemap decalTm, PrisonPalette pal, float xMin, float xMax, float yMin, float yMax)
        {
            // 1-cell bleed behind solid walls/floors/ceilings so zero void leaks at seams
            int cxMin = Mathf.FloorToInt(xMin) - 1;
            int cxMax = Mathf.CeilToInt(xMax);
            int cyMin = Mathf.FloorToInt(yMin) - 1;
            int cyMax = Mathf.CeilToInt(yMax);

            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                for (int cy = cyMin; cy <= cyMax; cy++)
                {
                    var cell = new Vector3Int(cx, cy, 0);
                    if (bgTm.GetTile(cell) == null)
                        SetTileWithMatrix(bgTm, cell, pal.BackWalls[CellHash(cx, cy) % pal.BackWalls.Length], Matrix4x4.identity);
                }
            }

            // Wall decals strictly inside room interior
            int decXMin = Mathf.FloorToInt(xMin) + 1;
            int decXMax = Mathf.CeilToInt(xMax) - 2;
            int decYMin = Mathf.FloorToInt(yMin) + 1;
            int decYMax = Mathf.CeilToInt(yMax) - 2;

            for (int cx = decXMin; cx <= decXMax; cx++)
            {
                for (int cy = decYMin; cy <= decYMax; cy++)
                {
                    var cell = new Vector3Int(cx, cy, 0);
                    if (decalTm.GetTile(cell) != null)
                        continue;

                    int roll = CellHash(cx * 3 + 19, cy * 7 + 37) % 100;
                    if (roll < 18)
                        SetTileWithMatrix(decalTm, cell, pal.StampWalls[CellHash(cx, cy) % pal.StampWalls.Length], Matrix4x4.identity);
                }
            }
        }

        private static void CarveAndFrameArchBay(
            GameObject artRoot,
            Tilemap bgTm,
            Tilemap decalTm,
            PrisonPalette pal,
            Material litMat,
            int bayIndex,
            int xMin,
            int xMax,
            int yMin,
            int yMax)
        {
            for (int cx = xMin; cx < xMax; cx++)
            {
                for (int cy = yMin; cy < yMax; cy++)
                {
                    var cell = new Vector3Int(cx, cy, 0);
                    bgTm.SetTile(cell, null);
                }
            }

            // Clear TM_WallDecals across a 2-cell halo around the carved bay so multi-cell wall decals never dangle over the lintel
            for (int cx = xMin - 2; cx <= xMax + 1; cx++)
            {
                for (int cy = yMin - 2; cy <= yMax + 2; cy++)
                {
                    decalTm.SetTile(new Vector3Int(cx, cy, 0), null);
                }
            }

            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");

            // Curved spandrel corner stone replacing rectangular blackBeam_topLeft/Right stickers
            CreateArchFrameSprite(artRoot, $"ArchSpandrel_TL_{bayIndex}", pal.ArchSpandrel, new Vector3(xMin + 0.5f, yMax - 0.5f, 0f), bgLayerId, -16, litMat, Color.white);
            var spandrelTRGo = CreateArchFrameSprite(artRoot, $"ArchSpandrel_TR_{bayIndex}", pal.ArchSpandrel, new Vector3(xMax - 0.5f, yMax - 0.5f, 0f), bgLayerId, -16, litMat, Color.white);
            spandrelTRGo.GetComponent<SpriteRenderer>().flipX = true;

            // Sills across [xMin..xMax-1] and trimmed lintels across [xMin+1..xMax-2] using rotating weathered variants
            for (int sx = xMin; sx < xMax; sx++)
            {
                int sillVariant = (((bayIndex + sx) % 3) + 3) % 3;
                var sillGo = CreateArchFrameSprite(artRoot, $"ArchSill_{bayIndex}_{sx}", pal.ArchSills[sillVariant], new Vector3(sx + 0.5f, yMin + 0.04f, 0f), bgLayerId, -17, litMat, Color.white);
                sillGo.GetComponent<SpriteRenderer>().flipX = ((bayIndex + sx) & 1) == 1;

                if (sx >= xMin + 1 && sx <= xMax - 2)
                {
                    int lintelVariant = (((bayIndex + sx + 1) % 3) + 3) % 3;
                    var lintelGo = CreateArchFrameSprite(artRoot, $"ArchLintel_{bayIndex}_{sx}", pal.ArchSills[lintelVariant], new Vector3(sx + 0.5f, yMax - 0.04f, 0f), bgLayerId, -17, litMat, Color.white);
                    var lintelSr = lintelGo.GetComponent<SpriteRenderer>();
                    lintelSr.flipY = true;
                    lintelSr.flipX = ((bayIndex + sx) & 1) == 1;
                }
            }

            // Side jambs trimmed below top spandrel cells: [yMin..yMax-2]
            for (int sy = yMin; sy <= yMax - 2; sy++)
            {
                int jambVariantL = (((bayIndex + sy) % 3) + 3) % 3;
                var jambLGo = CreateArchFrameSprite(artRoot, $"ArchJamb_L_{bayIndex}_{sy}", pal.ArchJambs[jambVariantL], new Vector3(xMin + (4f / 24f), sy + 0.5f, 0f), bgLayerId, -17, litMat, Color.white);
                jambLGo.GetComponent<SpriteRenderer>().flipY = ((bayIndex + sy) & 1) == 1;

                int jambVariantR = (((bayIndex + sy + 1) % 3) + 3) % 3;
                var jambRGo = CreateArchFrameSprite(artRoot, $"ArchJamb_R_{bayIndex}_{sy}", pal.ArchJambs[jambVariantR], new Vector3(xMax - (4f / 24f), sy + 0.5f, 0f), bgLayerId, -17, litMat, Color.white);
                var jambRSr = jambRGo.GetComponent<SpriteRenderer>();
                jambRSr.flipX = true;
                jambRSr.flipY = ((bayIndex + sy) & 1) == 1;
            }

            // Gothic wrought-iron mullions (1 centered if width <= 5, 2 thirds if width >= 6; broken missing section on bayIndex % 4 == 2)
            int width = xMax - xMin;
            int height = yMax - yMin;
            float[] mullionXs = width <= 5
                ? new[] { (xMin + xMax) * 0.5f }
                : new[] { xMin + width / 3f, xMin + (2f * width) / 3f };
            for (int col = 0; col < mullionXs.Length; col++)
            {
                for (int sy = yMin; sy < yMax; sy++)
                {
                    if ((bayIndex % 4) == 2 && col == 0 && sy == yMin + (height / 2))
                        continue;

                    CreateArchFrameSprite(artRoot, $"ArchMullion_{bayIndex}_{col}_{sy}", pal.ArchMullion, new Vector3(mullionXs[col], sy + 0.5f, 0f), bgLayerId, -14, litMat, Color.white);
                }
            }

            // Per-bay asymmetric dressing: 0 -> single corner cobweb (alpha 0.48), 1 -> hanging vine behind TM_Background, 2 -> weathered bare masonry
            int decoStyle = ((bayIndex % 3) + 3) % 3;
            if (decoStyle == 0)
            {
                var webTint = new Color(0.85f, 0.88f, 0.90f, 0.48f);
                if ((bayIndex & 1) == 0)
                    CreateArchFrameSprite(artRoot, $"ArchWeb_TL_{bayIndex}", pal.WebSmallTLSprite, new Vector3(xMin + 0.9f, yMax - 0.9f, 0f), bgLayerId, -15, litMat, webTint);
                else
                    CreateArchFrameSprite(artRoot, $"ArchWeb_TR_{bayIndex}", pal.WebSmallTRSprite, new Vector3(xMax - 0.9f, yMax - 0.9f, 0f), bgLayerId, -15, litMat, webTint);
            }
            else if (decoStyle == 1)
            {
                var vineSp = (bayIndex == 1 || bayIndex == 7) ? pal.VineParallaxB : pal.VineParallaxA;
                var vineMat = pal.MatVineSway != null ? pal.MatVineSway : litMat;
                const float vineScale = 0.80f;
                float topY = height <= 4 ? (yMax + 0.20f) : (yMax + 0.40f);
                float midX = (xMin + xMax) * 0.5f;
                float vineY = topY - vineSp.bounds.max.y * vineScale;
                var vineGo = CreateArchFrameSprite(artRoot, $"ArchVine_{bayIndex}", vineSp, new Vector3(midX, vineY, 0f), bgLayerId, -21, vineMat, new Color(0.52f, 0.66f, 0.58f, 0.78f));
                vineGo.transform.localScale = new Vector3(vineScale, vineScale, 1f);
            }

            // Subtle moonlight spill outside each Gothic aperture onto surrounding stone walls
            var spillGo = CreateChild(artRoot, $"ArchMoonSpill_{bayIndex}");
            spillGo.transform.position = new Vector3((xMin + xMax) * 0.5f, yMin + 1.2f, 0f);
            var spillLight = spillGo.AddComponent<Light2D>();
            spillLight.lightType = Light2D.LightType.Point;
            spillLight.color = new Color(116f / 255f, 180f / 255f, 204f / 255f, 1f);
            spillLight.intensity = 0.32f;
            spillLight.pointLightInnerRadius = 0.8f;
            spillLight.pointLightOuterRadius = 5.4f;
            spillLight.falloffIntensity = 0.72f;
            ConfigureGameplayPointLight(spillLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);
        }

        private static GameObject CreateArchFrameSprite(
            GameObject parent,
            string name,
            Sprite sprite,
            Vector3 worldPos,
            int sortingLayerId,
            int sortingOrder,
            Material material,
            Color color)
        {
            var go = CreateChild(parent, name);
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = material;
            sr.color = color;
            return go;
        }

        private static void PaintPrisonSolidFloor(Tilemap solidTm, Tilemap rimTm, PrisonPalette pal, float xMin, float xMax, float yMin, float floorTop)
        {
            int cxMin = Mathf.FloorToInt(xMin);
            int cxMax = Mathf.CeilToInt(xMax) - 1;
            int topRowY = Mathf.RoundToInt(floorTop) - 1;
            int botRowY = Mathf.FloorToInt(yMin);

            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                // Top walkable cap uses floorStamp_0_00..9_00
                SetTileWithMatrix(solidTm, new Vector3Int(cx, topRowY, 0), pal.FloorStamps[CellHash(cx, topRowY) % pal.FloorStamps.Length], Matrix4x4.identity);

                // Deeper solid rows use opaque dirt_00
                for (int cy = botRowY; cy < topRowY; cy++)
                    SetTileWithMatrix(solidTm, new Vector3Int(cx, cy, 0), pal.Dirt, Matrix4x4.identity);

                // Rim row right above floor top uses ground_0_00..3_00
                SetTileWithMatrix(rimTm, new Vector3Int(cx, topRowY + 1, 0), pal.Grounds[CellHash(cx, topRowY + 1) % pal.Grounds.Length], Matrix4x4.identity);
            }
        }

        private static void PaintVisualDirtSkirt(Tilemap solidTm, PrisonPalette pal, int xMin, int xMax, int yMin, int yMax)
        {
            for (int cx = xMin; cx < xMax; cx++)
            {
                for (int cy = yMin; cy < yMax; cy++)
                {
                    var cell = new Vector3Int(cx, cy, 0);
                    if (solidTm.GetTile(cell) == null)
                        SetTileWithMatrix(solidTm, cell, pal.Dirt, Matrix4x4.identity);
                }
            }
        }

        private static void AddWeatheredMasonryOverlays(
            GameObject artRoot,
            Tilemap bgTm,
            PrisonPalette pal,
            Material mat,
            string roomTag,
            float xMin,
            float xMax,
            float yMin,
            float yMax)
        {
            float width = xMax - xMin;
            float height = yMax - yMin;
            int strideX = width >= 30f ? 7 : 6;
            const int strideY = 5;
            int cxMin = Mathf.CeilToInt(xMin) + 2;
            int cxMax = Mathf.FloorToInt(xMax) - 3;
            int cyMin = Mathf.CeilToInt(yMin) + 2;
            int cyMax = Mathf.FloorToInt(yMax) - 3;
            var patchTint = new Color(0.96f, 1.00f, 0.96f, 0.58f);

            int idx = 0;
            for (int cx = cxMin; cx <= cxMax; cx += strideX)
            {
                for (int cy = cyMin; cy <= cyMax; cy += strideY)
                {
                    int jx = cx + (CellHash(cx, cy) % 3) - 1;
                    int jy = cy + (height >= 7f ? (CellHash(cx + 17, cy + 31) % 3) - 1 : 0);
                    bool useBig = height >= 9f && (CellHash(jx * 5, jy * 11) % 2 == 0);
                    float halfExt = useBig ? 1.5f : 1.0f;
                    float centerX = jx + 0.5f;
                    float centerY = jy + 0.5f;

                    if (centerX - halfExt < xMin + 0.6f || centerX + halfExt > xMax - 0.6f)
                        continue;
                    if (centerY - halfExt < yMin + 0.6f || centerY + halfExt > yMax - 0.6f)
                        continue;

                    // Strict 5x5 solid backWall guard so zero masonry patches bleed into carved parallax bays
                    bool clearOfBays = true;
                    for (int nx = jx - 2; nx <= jx + 2 && clearOfBays; nx++)
                    {
                        for (int ny = jy - 2; ny <= jy + 2; ny++)
                        {
                            if (bgTm.GetTile(new Vector3Int(nx, ny, 0)) == null)
                            {
                                clearOfBays = false;
                                break;
                            }
                        }
                    }
                    if (!clearOfBays)
                        continue;

                    Sprite sp = useBig
                        ? pal.StampWallBig[CellHash(jx, jy) % pal.StampWallBig.Length]
                        : pal.StampWallMedium[CellHash(jx, jy) % pal.StampWallMedium.Length];
                    AddPrisonSpriteProp(artRoot, $"Art_{roomTag}_WallPatch_{idx++}", sp, mat, new Vector2(centerX, centerY), 1.0f, -19, patchTint);
                }
            }
        }

        private static void AddBedrockStrataStamps(
            GameObject artRoot,
            Tilemap solidTm,
            PrisonPalette pal,
            Material mat,
            string regionTag,
            int xMin,
            int xMax,
            int yMin,
            int yMax,
            int strideX = 6,
            int strideY = 4)
        {
            var strataTint = new Color(0.46f, 0.50f, 0.56f, 0.40f);
            int idx = 0;

            for (int cx = xMin + 2; cx <= xMax - 3; cx += strideX)
            {
                for (int cy = yMin + 2; cy <= yMax - 3; cy += strideY)
                {
                    bool allOpaqueDirt = true;
                    for (int dx = -1; dx <= 1 && allOpaqueDirt; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            var cell = new Vector3Int(cx + dx, cy + dy, 0);
                            if (solidTm.GetTile(cell) != pal.Dirt || solidTm.GetTransformMatrix(cell) != Matrix4x4.identity)
                            {
                                allOpaqueDirt = false;
                                break;
                            }
                        }
                    }
                    if (!allOpaqueDirt)
                        continue;

                    var sp = pal.StoneStamps[CellHash(cx, cy) % pal.StoneStamps.Length];
                    AddPrisonSpriteProp(artRoot, $"Art_{regionTag}_BedrockStone_{idx++}", sp, mat, new Vector2(cx + 0.5f, cy + 0.5f), 0.90f, -4, strataTint);
                }
            }
        }

        private static void PaintPrisonOuterWall(Tilemap solidTm, PrisonPalette pal, float xMin, float xMax, float yMin, float yMax)
        {
            float width = xMax - xMin;
            float wallCenterX = (xMin + xMax) * 0.5f;
            int cx = Mathf.FloorToInt(wallCenterX);
            float offsetX = wallCenterX - (cx + 0.5f);

            int cyMin = Mathf.FloorToInt(yMin);
            int cyMax = Mathf.CeilToInt(yMax) - 1;
            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                float rowMin = Mathf.Max(cy, yMin);
                float rowMax = Mathf.Min(cy + 1f, yMax);
                float h = rowMax - rowMin;
                if (h <= 0.001f)
                    continue;
                float offsetY = (rowMin + rowMax) * 0.5f - (cy + 0.5f);
                var matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(width, h, 1f));
                SetTileWithMatrix(solidTm, new Vector3Int(cx, cy, 0), pal.Dirt, matrix);
            }
        }

        private static void PaintPrisonCeiling(Tilemap solidTm, Tilemap rimTm, PrisonPalette pal, float xMin, float xMax, float yMin, float yMax)
        {
            int cxMin = Mathf.FloorToInt(xMin);
            int cxMax = Mathf.CeilToInt(xMax) - 1;
            int cy = Mathf.FloorToInt(yMin);
            float height = yMax - yMin;
            float offsetY = (yMin + yMax) * 0.5f - (cy + 0.5f);
            var solidMatrix = Matrix4x4.TRS(new Vector3(0f, offsetY, 0f), Quaternion.identity, new Vector3(1f, height, 1f));

            for (int cx = cxMin; cx <= cxMax; cx++)
                SetTileWithMatrix(solidTm, new Vector3Int(cx, cy, 0), pal.Dirt, solidMatrix);

            PaintPrisonCeilingRimOnly(rimTm, pal, xMin, xMax, yMin);
        }

        private static void PaintPrisonCeilingRimOnly(Tilemap rimTm, PrisonPalette pal, float xMin, float xMax, float ceilingBottom)
        {
            int cxMin = Mathf.FloorToInt(xMin);
            int cxMax = Mathf.CeilToInt(xMax) - 1;
            int rimCy = Mathf.FloorToInt(ceilingBottom - 0.01f);
            float rimOffsetY = (ceilingBottom - 0.5f) - (rimCy + 0.5f);
            var rimMatrix = Matrix4x4.TRS(new Vector3(0f, rimOffsetY, 0f), Quaternion.identity, Vector3.one);

            for (int cx = cxMin; cx <= cxMax; cx++)
                SetTileWithMatrix(rimTm, new Vector3Int(cx, rimCy, 0), pal.Ceilings[CellHash(cx, rimCy) % pal.Ceilings.Length], rimMatrix);
        }

        private static void PaintPrisonInteriorHeader(
            Tilemap solidTm,
            GameObject artRoot,
            Material mat,
            PrisonPalette pal,
            string beamName,
            float centerX,
            float yMin,
            float yMax)
        {
            // 1. Paint TM_SolidVisual ONLY on cells not already occupied by ceilings/floors (preserves Ceiling_HJ at (-16,22) & covers (18,5) cleanly)
            int cx = Mathf.FloorToInt(centerX);
            float offsetX = centerX - (cx + 0.5f);
            int cyMin = Mathf.FloorToInt(yMin);
            int cyMax = Mathf.CeilToInt(yMax) - 1;

            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                var cell = new Vector3Int(cx, cy, 0);
                if (solidTm.GetTile(cell) != null)
                    continue;
                float rowMin = Mathf.Max(cy, yMin);
                float rowMax = Mathf.Min(cy + 1f, yMax);
                float h = rowMax - rowMin;
                if (h <= 0.001f)
                    continue;
                float offsetY = (rowMin + rowMax) * 0.5f - (cy + 0.5f);
                var matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(0.5f, h, 1f));
                SetTileWithMatrix(solidTm, cell, pal.Dirt, matrix);
            }

            // 2. Seamless exact [centerX - 0.25, centerX + 0.25] x [yMin, yMax] dirt_00.png beam so zero 0.5u notches appear at junctions
            AddSolidBeamSprite(artRoot, beamName, pal.DirtSprite, mat, centerX, yMin, yMax);
        }

        private static void AddSolidBeamSprite(GameObject parent, string name, Sprite dirtSprite, Material mat, float centerX, float yMin, float yMax)
        {
            var go = CreateChild(parent, name);
            go.transform.position = new Vector3(centerX, (yMin + yMax) * 0.5f, 0f);
            go.transform.localScale = new Vector3(0.5f, yMax - yMin, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dirtSprite;
            sr.sharedMaterial = mat;
            sr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            sr.sortingOrder = -5;
            sr.color = Color.white;
        }

        private static void PaintPrisonStairsH(Tilemap solidTm, Tilemap rimTm, GameObject artRoot, Material mat, PrisonPalette pal)
        {
            // 45-degree Dead Cells staircase H: flat bottom landing [18,22]@8, continuous 45-degree run
            // x = 10..17 with stone surface yTop = 25 - x, flat top landing [6,10]@16, common stone base y=7.
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 18f, 22f, 7f, 8f);
            PaintPrisonSolidFloor(solidTm, rimTm, pal, 6f, 10f, 7f, 16f);

            // Solid stone core under the ramp: dirt_00 from base y=7 up to one row below the surface.
            for (int x = 10; x <= 17; x++)
            {
                int yTop = 25 - x;
                for (int cy = 7; cy <= yTop - 1; cy++)
                    SetTileWithMatrix(solidTm, new Vector3Int(x, cy, 0), pal.Dirt, Matrix4x4.identity);
            }

            // EXACTLY ONE clean diagonal row of 8 flipped 45-degree stair tiles on the stone surface.
            // Each diagonal cell rests on dirt_00 core only, so no double-stacked stair tiles exist.
            var flipX = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
            for (int x = 17; x >= 10; x--)
            {
                int yTop = 25 - x;
                var stairTile = ((17 - x) % 2 == 0) ? pal.Stairs0 : pal.Stairs1;
                SetTileWithMatrix(rimTm, new Vector3Int(x, yTop, 0), stairTile, flipX);
            }

            // Subtle stoneStamp_0_00..5_00 texture detail inside the solid black stair core,
            // every stamp strictly inside dirt_00 mass (cell center, y <= 24 - x).
            var coreTint = new Color(0.55f, 0.58f, 0.62f, 0.45f);
            Vector2[] stampPositions =
            {
                new Vector2(8.5f, 9.5f),
                new Vector2(11.5f, 10.5f),
                new Vector2(13.5f, 9.5f),
                new Vector2(16.5f, 7.5f),
            };
            for (int i = 0; i < stampPositions.Length; i++)
            {
                var sp = pal.StoneStamps[i % pal.StoneStamps.Length];
                AddPrisonSpriteProp(artRoot, $"Art_H_StairCoreStone_{i}", sp, mat, stampPositions[i], 0.85f, -4, coreTint);
            }
        }

        // ==================== PRISON PLANKS, CHAINS, ANTI-BOX DRESSING & LIGHTING ====================

        private static void AddPrisonPlankRow(GameObject parent, string name, PrisonPalette pal, Material mat, float xMin, float xMax, float topY, int sortingOrder)
        {
            var container = CreateChild(parent, name);
            int count = Mathf.Max(1, Mathf.RoundToInt(xMax - xMin));
            float posY = topY - 0.125f;

            for (int i = 0; i < count; i++)
            {
                Sprite sp;
                if (count == 1)
                    sp = pal.WoodenPlatformMid[0];
                else if (i == 0)
                    sp = pal.WoodenPlatformLeft;
                else if (i == count - 1)
                    sp = pal.WoodenPlatformRight;
                else
                    sp = pal.WoodenPlatformMid[CellHash(Mathf.RoundToInt(xMin) + i, Mathf.RoundToInt(topY)) % pal.WoodenPlatformMid.Length];

                var piece = CreateChild(container, $"Plank_{i}");
                piece.transform.position = new Vector3(xMin + i + 0.5f, posY, 0f);
                piece.transform.localScale = new Vector3(1f, 0.75f, 1f);
                var sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sharedMaterial = mat;
                sr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
                sr.sortingOrder = sortingOrder;
            }
        }

        private static void AddPlatformCornerBrace(GameObject parent, string name, Sprite cornerSprite, Material mat, float edgeX, float topY, bool isLeft)
        {
            var go = CreateChild(parent, name);
            const float scale = 0.78f;
            float x = isLeft
                ? edgeX - cornerSprite.bounds.min.x * scale
                : edgeX - cornerSprite.bounds.max.x * scale;
            float y = (topY - 0.25f) - cornerSprite.bounds.max.y * scale;
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cornerSprite;
            sr.sharedMaterial = mat;
            sr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Ground");
            sr.sortingOrder = -3;
        }

        private static void AddPrisonChainArt(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float floorTopY, float deckTopY)
        {
            var container = CreateChild(parent, name);
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");

            // Top anchor hole flush with the upper deck: bounds.max.y == deckTopY so the iron collar sits
            // directly beneath the planks (Ground/-3, behind the -2 wooden row) and never pokes black horns above bridges.
            var holeGo = CreateChild(container, "AnchorTop");
            holeGo.transform.position = new Vector3(x, deckTopY - 0.5f, 0f);
            var holeSr = holeGo.AddComponent<SpriteRenderer>();
            holeSr.sprite = pal.LadderHole;
            holeSr.sharedMaterial = mat;
            holeSr.sortingLayerID = groundLayerId;
            holeSr.sortingOrder = -3;

            // Repeating 1.0u chain links from floorTopY + 1.0 up to deckTopY - 0.5
            int idx = 0;
            for (float y = floorTopY + 1.0f; y <= deckTopY - 0.5f + 0.01f; y += 1.0f)
            {
                var linkGo = CreateChild(container, $"Link_{idx++}");
                linkGo.transform.position = new Vector3(x, y, 0f);
                var linkSr = linkGo.AddComponent<SpriteRenderer>();
                linkSr.sprite = pal.ChainTile;
                linkSr.sharedMaterial = mat;
                linkSr.sortingLayerID = groundLayerId;
                linkSr.sortingOrder = -2;
            }

            // Bottom cap cleanly above floor using dark-iron chainTile_00 (zero floor stab, no lavender chainDown_00 mismatch)
            var capGo = CreateChild(container, "CapBottom");
            capGo.transform.position = new Vector3(x, floorTopY + 0.45f, 0f);
            capGo.transform.localScale = new Vector3(1f, 0.80f, 1f);
            var capSr = capGo.AddComponent<SpriteRenderer>();
            capSr.sprite = pal.ChainTile;
            capSr.sharedMaterial = mat;
            capSr.sortingLayerID = groundLayerId;
            capSr.sortingOrder = -2;
        }

        private static void AddCornerBeamsAndWebs(
            GameObject artRoot,
            Tilemap rimTm,
            Material mat,
            PrisonPalette pal,
            string roomTag,
            float xMin,
            float xMax,
            float ceilingBottom,
            float floorY,
            bool isTallRoom,
            bool hasLeftBottomWall = true,
            bool hasRightBottomWall = true,
            float shaftMinX = float.NaN,
            float shaftMaxX = float.NaN)
        {
            var beamTL = isTallRoom ? pal.BigBeamTL : pal.LittleBeamTL;
            var beamTR = isTallRoom ? pal.BigBeamTR : pal.LittleBeamTR;
            float beamScale = isTallRoom ? 0.85f : 0.80f;
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");

            var tlGo = CreateChild(artRoot, $"Art_{roomTag}_BeamTL");
            tlGo.transform.position = new Vector3(xMin - beamTL.bounds.min.x * beamScale, ceilingBottom - beamTL.bounds.max.y * beamScale, 0f);
            tlGo.transform.localScale = new Vector3(beamScale, beamScale, 1f);
            var tlSr = tlGo.AddComponent<SpriteRenderer>();
            tlSr.sprite = beamTL;
            tlSr.sharedMaterial = mat;
            tlSr.sortingLayerID = groundLayerId;
            tlSr.sortingOrder = -4;

            var trGo = CreateChild(artRoot, $"Art_{roomTag}_BeamTR");
            trGo.transform.position = new Vector3(xMax - beamTR.bounds.max.x * beamScale, ceilingBottom - beamTR.bounds.max.y * beamScale, 0f);
            trGo.transform.localScale = new Vector3(beamScale, beamScale, 1f);
            var trSr = trGo.AddComponent<SpriteRenderer>();
            trSr.sprite = beamTR;
            trSr.sharedMaterial = mat;
            trSr.sortingLayerID = groundLayerId;
            trSr.sortingOrder = -4;

            // Ceiling stamp & roof moss accents to break flat ceiling edge (shifted clear of open vertical shafts; Background / -11 behind lanterns & cages)
            float midX = ShiftClearOfShaft((xMin + xMax) * 0.5f, xMin, xMax, shaftMinX, shaftMaxX);
            var stampSp = isTallRoom ? pal.StampCeilingMedium : pal.StampCeilingSmall;
            AddScaledCeilingProp(artRoot, $"Art_{roomTag}_CeilStamp", stampSp, mat, midX, ceilingBottom, 0.80f, -11, new Color(0.90f, 0.92f, 0.90f, 0.92f));

            float rawMossX = Mathf.Lerp(xMin + 3f, xMax - 3f, (CellHash(Mathf.RoundToInt(xMin), Mathf.RoundToInt(ceilingBottom)) % 100) / 100f);
            float mossX = ShiftClearOfShaft(rawMossX, xMin, xMax, shaftMinX, shaftMaxX);
            var mossSp = pal.RoofMoss[CellHash(Mathf.RoundToInt(midX), Mathf.RoundToInt(ceilingBottom)) % pal.RoofMoss.Length];
            AddScaledCeilingProp(artRoot, $"Art_{roomTag}_RoofMoss", mossSp, mat, mossX, ceilingBottom, 0.80f, -11, new Color(0.86f, 0.94f, 0.86f, 0.9f));

            // Phase 3 (P2-03): Multi-span ceiling masonry & roof moss across long rooms (> 28u) on Background / -11 (strictly behind lanterns & cages)
            if (xMax - xMin > 28f)
            {
                var multiStampSp = isTallRoom ? pal.StampCeilingBig : pal.StampCeilingSmall1;
                const float multiStampScale = 0.80f;
                float stampHalfW = multiStampSp.bounds.extents.x * multiStampScale;
                int stampIdx = 0;
                int mossIdx = 0;

                for (float spanX = xMin + 9f; spanX <= xMax - 9f + 0.01f; spanX += 18f)
                {
                    bool stampHitsShaft = !float.IsNaN(shaftMinX) &&
                                          spanX + stampHalfW + 0.5f > shaftMinX &&
                                          spanX - stampHalfW - 0.5f < shaftMaxX;
                    if (!stampHitsShaft && Mathf.Abs(spanX - midX) >= 4.0f)
                    {
                        AddScaledCeilingProp(
                            artRoot,
                            $"Art_{roomTag}_CeilStamp_{stampIdx++}",
                            multiStampSp,
                            mat,
                            spanX,
                            ceilingBottom,
                            multiStampScale,
                            -11,
                            new Color(0.90f, 0.92f, 0.90f, 0.92f));
                    }

                    float mossSpanX = spanX + 6.5f;
                    if (mossSpanX <= xMax - 4.0f)
                    {
                        var spanMossSp = pal.RoofMoss[CellHash(Mathf.RoundToInt(mossSpanX), Mathf.RoundToInt(ceilingBottom)) % pal.RoofMoss.Length];
                        float mossHalfW = spanMossSp.bounds.extents.x * 0.80f;
                        bool mossHitsShaft = !float.IsNaN(shaftMinX) &&
                                             mossSpanX + mossHalfW + 0.5f > shaftMinX &&
                                             mossSpanX - mossHalfW - 0.5f < shaftMaxX;
                        if (!mossHitsShaft && Mathf.Abs(mossSpanX - mossX) >= 3.0f)
                        {
                            AddScaledCeilingProp(
                                artRoot,
                                $"Art_{roomTag}_RoofMoss_{mossIdx++}",
                                spanMossSp,
                                mat,
                                mossSpanX,
                                ceilingBottom,
                                0.80f,
                                -11,
                                new Color(0.86f, 0.94f, 0.86f, 0.90f));
                        }
                    }
                }
            }

            // Cobwebs on TM_SolidRim strictly anchored against solid corner walls (never dangling in open doorway air)
            int webCyTop = Mathf.FloorToInt(ceilingBottom - 1.05f);
            int webCxL = Mathf.FloorToInt(xMin);
            int webCxR = Mathf.CeilToInt(xMax) - 1;
            var cellTL = new Vector3Int(webCxL, webCyTop, 0);
            var cellTR = new Vector3Int(webCxR, webCyTop, 0);
            if (hasLeftBottomWall && rimTm.GetTile(cellTL) == null)
                SetTileWithMatrix(rimTm, cellTL, pal.WebSmallTL, Matrix4x4.identity);
            if (hasRightBottomWall && rimTm.GetTile(cellTR) == null)
                SetTileWithMatrix(rimTm, cellTR, pal.WebSmallTR, Matrix4x4.identity);

            int webCyBot = Mathf.RoundToInt(floorY) + 1;
            var cellBL = new Vector3Int(webCxL, webCyBot, 0);
            var cellBR = new Vector3Int(webCxR, webCyBot, 0);
            if (hasLeftBottomWall && rimTm.GetTile(cellBL) == null)
                SetTileWithMatrix(rimTm, cellBL, pal.WebSmallBL, Matrix4x4.identity);
            if (hasRightBottomWall && rimTm.GetTile(cellBR) == null)
                SetTileWithMatrix(rimTm, cellBR, pal.WebSmallBR, Matrix4x4.identity);
        }

        private static float ShiftClearOfShaft(float x, float xMin, float xMax, float shaftMinX, float shaftMaxX)
        {
            if (float.IsNaN(shaftMinX) || float.IsNaN(shaftMaxX))
                return x;

            float exclMin = shaftMinX - 2f;
            float exclMax = shaftMaxX + 2f;
            if (x < exclMin || x > exclMax)
                return x;

            float safeMin = xMin + 4f;
            float safeMax = xMax - 4f;
            float leftSpace = exclMin - safeMin;
            float rightSpace = safeMax - exclMax;
            bool preferLeft = (x - exclMin) <= (exclMax - x);

            float candidate;
            if (preferLeft && leftSpace >= 0f)
                candidate = exclMin;
            else if (!preferLeft && rightSpace >= 0f)
                candidate = exclMax;
            else if (leftSpace >= rightSpace)
                candidate = exclMin;
            else
                candidate = exclMax;

            return Mathf.Clamp(candidate, safeMin, safeMax);
        }

        private static void ScrubRoomVisualClutter(GameObject artRoot, Tilemap decalTm)
        {
            if (artRoot == null)
                return;

            var renderers = artRoot.GetComponentsInChildren<SpriteRenderer>(true);

            // 1. Collect ceiling obstacles (LanternFixture_R5, hanging cages, and full carved arch bay apertures)
            var ceilingObstacles = new List<Bounds>();
            var bayBoundsByKey = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            foreach (var sr in renderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;

                string n = sr.gameObject.name;
                if (n == "LanternFixture_R5" || n.Contains("Cage") || n == "Art_A_HomonculusPipes")
                {
                    ceilingObstacles.Add(sr.bounds);
                    continue;
                }

                if (n.StartsWith("Arch", StringComparison.Ordinal))
                {
                    string[] p = n.Split('_');
                    string bayKey = (n.StartsWith("ArchSpandrel_", StringComparison.Ordinal) || n.StartsWith("ArchJamb_", StringComparison.Ordinal))
                        ? (p.Length > 2 ? p[2] : null)
                        : (p.Length > 1 ? p[1] : null);
                    if (!string.IsNullOrEmpty(bayKey))
                    {
                        if (bayBoundsByKey.TryGetValue(bayKey, out var cur))
                        {
                            cur.Encapsulate(sr.bounds);
                            bayBoundsByKey[bayKey] = cur;
                        }
                        else
                        {
                            bayBoundsByKey[bayKey] = sr.bounds;
                        }
                    }
                }
            }
            foreach (var bb in bayBoundsByKey.Values)
                ceilingObstacles.Add(bb);

            static bool OverlapsCeilingObstacle(Bounds a, List<Bounds> obstacles, float pad)
            {
                for (int i = 0; i < obstacles.Count; i++)
                {
                    Bounds obs = obstacles[i];
                    if (a.min.x < obs.max.x + pad && a.max.x > obs.min.x - pad &&
                        a.min.y < obs.max.y + pad && a.max.y > obs.min.y - pad)
                    {
                        return true;
                    }
                }
                return false;
            }

            // 2. Relocate any _CeilStamp or _RoofMoss overlapping a lantern, cage, or carved arch bay into a clear ceiling span
            float[] deltas = { 2.2f, -2.2f, 2.8f, -2.8f, 3.6f, -3.6f, 4.5f, -4.5f, 5.5f, -5.5f, 6.2f, -6.2f, 7.5f, -7.5f, 9.0f, -9.0f, 11.0f, -11.0f, 13.0f, -13.0f };
            foreach (var sr in renderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;

                string n = sr.gameObject.name;
                if (!n.Contains("_CeilStamp") && !n.Contains("_RoofMoss"))
                    continue;

                if (!OverlapsCeilingObstacle(sr.bounds, ceilingObstacles, 0.10f))
                    continue;

                string[] parts = n.Split('_');
                string roomTag = parts.Length > 1 ? parts[1] : "";

                string beamTLName = $"Art_{roomTag}_BeamTL";
                string beamTRName = $"Art_{roomTag}_BeamTR";
                SpriteRenderer beamTL = null;
                SpriteRenderer beamTR = null;
                foreach (var sr2 in renderers)
                {
                    if (sr2 == null)
                        continue;
                    if (sr2.gameObject.name == beamTLName)
                        beamTL = sr2;
                    else if (sr2.gameObject.name == beamTRName)
                        beamTR = sr2;
                }

                float safeMinX = beamTL != null ? beamTL.bounds.max.x + 0.1f : sr.bounds.min.x - 10f;
                float safeMaxX = beamTR != null ? beamTR.bounds.min.x - 0.1f : sr.bounds.max.x + 10f;

                (float sMin, float sMax) = roomTag switch
                {
                    "M" => (101f, 107f),
                    "O" => (115f, 121f),
                    "Q" => (-69f, -63f),
                    "S" => (77f, 83f),
                    _ => (float.NaN, float.NaN),
                };

                Vector3 origPos = sr.transform.position;
                float origX = origPos.x;
                float halfW = sr.bounds.extents.x;
                bool placed = false;

                bool TryCandidate(float nx)
                {
                    if (nx - halfW < safeMinX || nx + halfW > safeMaxX)
                        return false;
                    bool clearOfShaft = float.IsNaN(sMin) || nx + halfW + 0.1f <= sMin || nx - halfW - 0.1f >= sMax;
                    if (!clearOfShaft)
                        return false;
                    sr.transform.position = new Vector3(nx, origPos.y, origPos.z);
                    return !OverlapsCeilingObstacle(sr.bounds, ceilingObstacles, 0.10f);
                }

                for (int i = 0; i < deltas.Length; i++)
                {
                    if (TryCandidate(origX + deltas[i]))
                    {
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    for (float nx = safeMinX + halfW + 0.05f; nx <= safeMaxX - halfW - 0.05f; nx += 0.5f)
                    {
                        if (TryCandidate(nx))
                        {
                            placed = true;
                            break;
                        }
                    }
                }

                if (!placed)
                    sr.transform.position = origPos;
            }

            // 3. Scrub TM_WallDecals behind wall/architectural props so wallJunk decals never bleed through props
            if (decalTm != null)
            {
                foreach (var sr in renderers)
                {
                    if (sr == null || sr.sprite == null)
                        continue;

                    string n = sr.gameObject.name;
                    if (n.Contains("_WallPatch_") ||
                        n.Contains("_BedrockStone_") ||
                        n.Contains("_StairCoreStone_") ||
                        n.Contains("_FloorPlank_") ||
                        n.Contains("_Foliage") ||
                        n.Contains("_CeilStamp") ||
                        n.Contains("_RoofMoss"))
                    {
                        continue;
                    }

                    Bounds b = sr.bounds;
                    int x0 = Mathf.FloorToInt(b.min.x) - 1;
                    int x1 = Mathf.FloorToInt(b.max.x) + 1;
                    int y0 = Mathf.FloorToInt(b.min.y) - 1;
                    int y1 = Mathf.FloorToInt(b.max.y) + 1;
                    for (int cx = x0; cx <= x1; cx++)
                    {
                        for (int cy = y0; cy <= y1; cy++)
                        {
                            decalTm.SetTile(new Vector3Int(cx, cy, 0), null);
                        }
                    }
                }
            }
        }

        private static void AddStandingTorchBeacon(GameObject parent, string name, Material mat, float x, float floorY, Color lightColor, float outerRadius, float intensity)
        {
            BuildWallLanternBeacon(parent, name, "FlameLight", mat, x, floorY + 2.2f, lightColor, Mathf.Max(outerRadius, 7.6f), Mathf.Clamp(intensity, 0.75f, 1.45f));
        }

        private static void AddCeilingLanternBeacon(GameObject parent, string name, Material mat, float x, float ceilingBottomY, Color lightColor, float outerRadius, float intensity)
        {
            BuildWallLanternBeacon(parent, name, "LanternLight", mat, x, ceilingBottomY - 1.35f, lightColor, Mathf.Max(outerRadius, 7.6f), Mathf.Clamp(intensity, 0.75f, 1.45f));
        }

        private static void BuildWallLanternBeacon(
            GameObject parent,
            string name,
            string lightChildName,
            Material litMat,
            float x,
            float flameY,
            Color lightColor,
            float outerRadius,
            float intensity)
        {
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            var lanternSprite = LoadSpriteAtPath(PrisonPropSpriteGenerator.WallLanternAssetPath);
            var fxGlowSprite = LoadSpriteAtPath("Assets/_Project/Art/VFX/ReferenceFeedback/fxGlowWhite.png");
            var flameMat = LoadMaterialAtPath(EnvironmentMaterialBuilder.MaterialDirectory + "/Mat_Emissive_Flame.mat");
            var particlesUnlitMat = LoadMaterialAtPath(ParticlesUnlitMatPath);

            var go = CreateChild(parent, name);
            go.transform.position = new Vector3(x, flameY, 0f);
            go.transform.localScale = Vector3.one;

            var fixtureGo = CreateChild(go, "LanternFixture_R5");
            fixtureGo.transform.localPosition = new Vector3(0f, PrisonPropSpriteGenerator.WallLanternFlameOffsetLocalY, 0f);
            fixtureGo.transform.localScale = Vector3.one;
            var fixtureSr = fixtureGo.AddComponent<SpriteRenderer>();
            fixtureSr.sprite = lanternSprite;
            fixtureSr.sharedMaterial = litMat;
            fixtureSr.sortingLayerID = bgLayerId;
            fixtureSr.sortingOrder = -9;
            fixtureSr.color = Color.white;

            var coreGo = CreateChild(go, "LanternFlameCore_R5");
            coreGo.transform.localPosition = Vector3.zero;
            coreGo.transform.localScale = new Vector3(0.085f, 0.12f, 1f);
            var coreSr = coreGo.AddComponent<SpriteRenderer>();
            coreSr.sprite = fxGlowSprite;
            coreSr.sharedMaterial = flameMat;
            coreSr.sortingLayerID = bgLayerId;
            coreSr.sortingOrder = -8;
            coreSr.color = new Color(1f, 1f, 1f, 0.82f);

            CreateTorchEmbers(go.transform, particlesUnlitMat, bgLayerId, -7);

            var lightGo = CreateChild(go, lightChildName);
            lightGo.transform.localPosition = Vector3.zero;
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = lightColor;
            light.intensity = intensity;
            light.pointLightOuterRadius = outerRadius;
            light.pointLightInnerRadius = 1.2f;
            light.falloffIntensity = 0.62f;
            ConfigureGameplayPointLight(light, enableVolumetric: true, volumeIntensity: 0.065f, addFlicker: true, phaseSeed: x * 0.37f + flameY * 0.19f);
        }

        private static void CreateTorchEmbers(Transform parent, Material particlesUnlitMat, int sortingLayerId, int sortingOrder)
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
            }
        }

        private static void ConfigureGameplayPointLight(Light2D light, bool enableVolumetric, float volumeIntensity, bool addFlicker, float phaseSeed)
        {
            int[] gameplayLayerIds =
            {
                SortingLayerSetup.GetSortingLayerId("Default"),
                SortingLayerSetup.GetSortingLayerId("Background"),
                SortingLayerSetup.GetSortingLayerId("Ground"),
                SortingLayerSetup.GetSortingLayerId("Actors"),
                SortingLayerSetup.GetSortingLayerId("VFX"),
            };
            SetLightSortingLayers(light, gameplayLayerIds);
            SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);

            if (enableVolumetric)
            {
                light.volumetricEnabled = true;
                light.volumeIntensity = volumeIntensity;
                var so = new SerializedObject(light);
                var volEnProp = so.FindProperty("m_LightVolumeEnabled");
                var volIntProp = so.FindProperty("m_LightVolumeIntensity");
                if (volEnProp != null) volEnProp.boolValue = true;
                if (volIntProp != null) volIntProp.floatValue = volumeIntensity;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (addFlicker)
            {
                if (!light.TryGetComponent<TorchFlicker2D>(out var flicker))
                    flicker = light.gameObject.AddComponent<TorchFlicker2D>();
                flicker.Configure(light, light.intensity, light.pointLightOuterRadius, amplitude: 0.08f, speed: 3.2f, phaseOffset: phaseSeed);
                flicker.enabled = true;
            }
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
        }

        private static void SetLightNormalMapQuality(Light2D light, Light2D.NormalMapQuality quality, float distance)
        {
            var so = new SerializedObject(light);
            var qProp = so.FindProperty("m_NormalMapQuality");
            var dProp = so.FindProperty("m_NormalMapDistance");
            if (qProp != null) qProp.intValue = (int)quality;
            if (dProp != null) dProp.floatValue = distance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddWindowWithGodray(GameObject parent, string name, Material matLit, Material matUnlit, PrisonPalette pal, Vector2 pos)
        {
            var winGo = CreateChild(parent, name);
            winGo.transform.position = new Vector3(pos.x, pos.y, 0f);
            var winSr = winGo.AddComponent<SpriteRenderer>();
            winSr.sprite = pal.WindowSmall;
            winSr.sharedMaterial = matLit;
            winSr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("Background");
            winSr.sortingOrder = -15;
            winSr.color = new Color(0.86f, 0.92f, 1f, 1f);

            var rayGo = CreateChild(winGo, "GodRay");
            rayGo.transform.localPosition = new Vector3(0.45f, -0.95f, 0f);
            rayGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            rayGo.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var raySr = rayGo.AddComponent<SpriteRenderer>();
            raySr.sprite = pal.GodRay;
            raySr.sharedMaterial = pal.MatGodRay != null ? pal.MatGodRay : matUnlit;
            raySr.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
            raySr.sortingOrder = 10;
            raySr.color = Color.white;

            var moonGo = CreateChild(winGo, "WindowMoonLight");
            moonGo.transform.localPosition = Vector3.zero;
            var moonLight = moonGo.AddComponent<Light2D>();
            moonLight.lightType = Light2D.LightType.Point;
            moonLight.color = new Color(0.55f, 0.74f, 0.96f, 1f);
            moonLight.intensity = 0.72f;
            moonLight.pointLightInnerRadius = 1.2f;
            moonLight.pointLightOuterRadius = 9.5f;
            moonLight.falloffIntensity = 0.58f;
            ConfigureGameplayPointLight(moonLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);
        }

        private static void AddVerticalRoomBanner(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float topY, int sortingOrder, bool includeMid = true)
        {
            var container = CreateChild(parent, name);
            var swayMat = pal.MatRoomBannerSway != null ? pal.MatRoomBannerSway : mat;
            const float scale = 1.0f;
            float y = topY;
            AddPrisonSpriteProp(container, "Top", pal.BannerRoomTop, swayMat, new Vector2(x, y - pal.BannerRoomTop.bounds.max.y * scale), scale, sortingOrder, Color.white);
            y -= pal.BannerRoomTop.bounds.size.y * scale;
            if (includeMid)
            {
                AddPrisonSpriteProp(container, "Mid", pal.BannerRoomTiled, swayMat, new Vector2(x, y - pal.BannerRoomTiled.bounds.max.y * scale), scale, sortingOrder, Color.white);
                y -= pal.BannerRoomTiled.bounds.size.y * scale;
            }
            AddPrisonSpriteProp(container, "Bot", pal.BannerRoomBot, swayMat, new Vector2(x, y - pal.BannerRoomBot.bounds.max.y * scale), scale, sortingOrder, Color.white);

            var pin = container.AddComponent<VertexSwayPin2D>();
            float spanHeight = pal.BannerRoomTop.bounds.size.y + (includeMid ? pal.BannerRoomTiled.bounds.size.y : 0f) + pal.BannerRoomBot.bounds.size.y;
            pin.Configure(topY, spanHeight, true);
        }

        private static void AddShelfWithFioles(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float y, int sortingOrder)
        {
            var container = CreateChild(parent, name);
            AddPrisonSpriteProp(container, "Shelf", pal.Shelf, mat, new Vector2(x, y), 0.85f, sortingOrder, Color.white);
            float shelfTopY = y + pal.Shelf.bounds.max.y * 0.85f - 0.14f;
            for (int i = 0; i < pal.ShelfVials.Length; i++)
            {
                var vial = pal.ShelfVials[i];
                const float vScale = 1.00f;
                float vx = x + (i - 1) * 0.58f;
                AddPrisonSpriteProp(container, $"Fiole_{i}", vial, mat, new Vector2(vx, shelfTopY - vial.bounds.min.y * vScale), vScale, sortingOrder + 1, Color.white);
            }
        }

        private static void AddFloorJunkCluster(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float floorY, int seed)
        {
            var tint = new Color(0.70f, 0.74f, 0.76f, 1f);
            var spA = pal.FloorJunks[seed % pal.FloorJunks.Length];
            var spB = pal.FloorJunks[(seed + 3) % pal.FloorJunks.Length];
            float halfA = spA.bounds.extents.x * 0.90f;
            float halfB = spB.bounds.extents.x * 0.85f;
            float offsetX = halfA + halfB + 0.16f;
            AddGroundedPrisonSprite(parent, name + "_A", spA, mat, x, floorY, 0.90f, -8, tint);
            AddGroundedPrisonSprite(parent, name + "_B", spB, mat, x + offsetX, floorY, 0.85f, -8, tint);
        }

        private static void AddFullHeightStonePilaster(GameObject parent, string name, PrisonPalette pal, Material mat, float x, float floorY, float ceilingY)
        {
            var container = CreateChild(parent, name);
            const float scale = 0.85f;
            var sp = pal.SpawnerPillar;
            var tint = new Color(0.84f, 0.88f, 0.86f, 1f);
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            float segH = sp.bounds.size.y * scale;
            float stepY = segH * 0.88f;
            float minYOff = -sp.bounds.min.y * scale;
            float topSegCenterY = ceilingY - sp.bounds.max.y * scale;

            int idx = 0;
            float curBottom = floorY;
            while (curBottom + segH < ceilingY - 0.12f)
            {
                var segGo = CreateChild(container, $"Seg_{idx++}");
                segGo.transform.position = new Vector3(x, curBottom + minYOff, 0f);
                segGo.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = segGo.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sharedMaterial = mat;
                sr.sortingLayerID = bgLayerId;
                sr.sortingOrder = -13;
                sr.color = tint;
                curBottom += stepY;
            }

            var topGo = CreateChild(container, $"Seg_{idx}");
            topGo.transform.position = new Vector3(x, Mathf.Max(floorY + minYOff, topSegCenterY), 0f);
            topGo.transform.localScale = new Vector3(scale, scale, 1f);
            var topSr = topGo.AddComponent<SpriteRenderer>();
            topSr.sprite = sp;
            topSr.sharedMaterial = mat;
            topSr.sortingLayerID = bgLayerId;
            topSr.sortingOrder = -13;
            topSr.color = tint;
        }

        private static void AddAnchoredChainedPrisoner(GameObject parent, string name, Sprite sprite, Material mat, PrisonPalette pal, Vector2 pos, float ceilingY)
        {
            const float scale = 0.85f;
            var prisonerTint = new Color(0.90f, 0.94f, 0.98f, 0.95f);
            AddPrisonSpriteProp(parent, name, sprite, mat, pos, scale, -14, prisonerTint);

            var chainSp = pal.CageChain;
            var chainTint = new Color(0.62f, 0.68f, 0.72f, 0.92f);
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            float prisonerTopY = pos.y + sprite.bounds.max.y * scale;
            float startY = prisonerTopY + chainSp.bounds.extents.y - 0.14f;
            float topCenterY = ceilingY - chainSp.bounds.max.y;
            float xOff = sprite.bounds.extents.x * 0.62f * scale;

            foreach (var side in new[] { (suffix: "_AnchorL_", x: pos.x - xOff), (suffix: "_AnchorR_", x: pos.x + xOff) })
            {
                int idx = 0;
                for (float y = startY; y < topCenterY - 0.25f; y += 0.88f)
                {
                    var anchorGo = CreateChild(parent, $"{name}{side.suffix}{idx++}");
                    anchorGo.transform.position = new Vector3(side.x, y, 0f);
                    var sr = anchorGo.AddComponent<SpriteRenderer>();
                    sr.sprite = chainSp;
                    sr.sharedMaterial = mat;
                    sr.sortingLayerID = bgLayerId;
                    sr.sortingOrder = -15;
                    sr.color = chainTint;
                }

                var topAnchorGo = CreateChild(parent, $"{name}{side.suffix}{idx}");
                topAnchorGo.transform.position = new Vector3(side.x, topCenterY, 0f);
                var topSr = topAnchorGo.AddComponent<SpriteRenderer>();
                topSr.sprite = chainSp;
                topSr.sharedMaterial = mat;
                topSr.sortingLayerID = bgLayerId;
                topSr.sortingOrder = -15;
                topSr.color = chainTint;
            }
        }

        private static void AddAlchemyBibliCeilingChain(
            GameObject parent,
            string bibliName,
            PrisonPalette pal,
            Material mat,
            float bibliX,
            float floorY,
            float chainScale,
            float ceilingY)
        {
            var chainSp = pal.CageChain;
            var chainTint = new Color(0.78f, 0.56f, 0.38f, 0.95f);
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            float chainX = bibliX - 0.0333f;
            float startY = 32.95f - chainSp.bounds.min.y * chainScale;
            float topCenterY = ceilingY - chainSp.bounds.max.y * chainScale;

            int idx = 0;
            for (float y = startY; y < topCenterY - 0.25f; y += 0.72f)
            {
                var segGo = CreateChild(parent, $"{bibliName}_Chain_{idx++}");
                segGo.transform.position = new Vector3(chainX, y, 0f);
                segGo.transform.localScale = new Vector3(chainScale, chainScale, 1f);
                var sr = segGo.AddComponent<SpriteRenderer>();
                sr.sprite = chainSp;
                sr.sharedMaterial = mat;
                sr.sortingLayerID = bgLayerId;
                sr.sortingOrder = -15;
                sr.color = chainTint;
            }

            var topSegGo = CreateChild(parent, $"{bibliName}_Chain_{idx}");
            topSegGo.transform.position = new Vector3(chainX, topCenterY, 0f);
            topSegGo.transform.localScale = new Vector3(chainScale, chainScale, 1f);
            var topSr = topSegGo.AddComponent<SpriteRenderer>();
            topSr.sprite = chainSp;
            topSr.sharedMaterial = mat;
            topSr.sortingLayerID = bgLayerId;
            topSr.sortingOrder = -15;
            topSr.color = chainTint;
        }

        private static void AddInterTierSlabStrata(GameObject parent, PrisonPalette pal, Material mat, string tag, float xMin, float xMax, float centerY)
        {
            const float scale = 0.95f;
            var tint = new Color(0.88f, 0.94f, 0.94f, 0.92f);
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
            int idx = 0;
            for (float x = xMin + 1.5f; x <= xMax - 1.5f; x += 5.5f)
            {
                var go = CreateChild(parent, $"Art_{tag}_SlabStrata_{idx++}");
                go.transform.position = new Vector3(x, centerY, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pal.SlabStrata;
                sr.sharedMaterial = mat;
                sr.sortingLayerID = groundLayerId;
                sr.sortingOrder = -4;
                sr.color = tint;
                sr.flipX = (idx & 1) == 0;
            }
        }

        private static void AddPrisonSpriteProp(GameObject parent, string name, Sprite sprite, Material mat, Vector2 pos, float scale, int sortingOrder, Color color)
        {
            var go = CreateChild(parent, name);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingLayerID = sortingOrder <= -9
                ? SortingLayerSetup.GetSortingLayerId("Background")
                : SortingLayerSetup.GetSortingLayerId("Ground");
            sr.sortingOrder = sortingOrder;
            sr.color = color;
        }

        private static void AddGroundedPrisonSprite(GameObject parent, string name, Sprite sprite, Material mat, float x, float floorY, float scale, int sortingOrder, Color color)
        {
            float y = floorY - sprite.bounds.min.y * scale;
            AddPrisonSpriteProp(parent, name, sprite, mat, new Vector2(x, y), scale, sortingOrder, color);
        }

        private static void AddScaledGroundedProp(GameObject parent, string name, Sprite sprite, Material mat, float x, float floorY, float scale, int sortingOrder, Color color)
        {
            float y = floorY - sprite.bounds.min.y * scale;
            AddPrisonSpriteProp(parent, name, sprite, mat, new Vector2(x, y), scale, sortingOrder, color);
        }

        private static void AddScaledCeilingProp(GameObject parent, string name, Sprite sprite, Material mat, float x, float ceilingBottomY, float scale, int sortingOrder, Color color)
        {
            float y = ceilingBottomY - sprite.bounds.max.y * scale;
            AddPrisonSpriteProp(parent, name, sprite, mat, new Vector2(x, y), scale, sortingOrder, color);
        }

        private static void BuildWorldPromptBadge(GameObject targetGo, string label, Color textColor, float width = 260f)
        {
            var canvas = targetGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
            canvas.sortingOrder = 25;

            var canvasRect = targetGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, 48f);
            canvasRect.localScale = Vector3.one * 0.015f;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(targetGo.transform, false);
            var backdropRect = backdropGo.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImg = backdropGo.AddComponent<UnityEngine.UI.Image>();
            backdropImg.color = new Color(0.08f, 0.08f, 0.12f, 0.86f);
            backdropImg.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(targetGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            var text = labelGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = textColor;
            text.text = label;
            text.raycastTarget = false;
        }

        private static UnityEngine.UI.Text BuildWorldLoreBanner(GameObject targetGo, string initialText)
        {
            var canvas = targetGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerID = SortingLayerSetup.GetSortingLayerId("VFX");
            canvas.sortingOrder = 26;

            var canvasRect = targetGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(460f, 150f);
            canvasRect.localScale = Vector3.one * 0.015f;

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(targetGo.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGo.AddComponent<UnityEngine.UI.Image>();
            borderImg.color = new Color(0.68f, 0.54f, 0.28f, 0.92f);
            borderImg.raycastTarget = false;

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(targetGo.transform, false);
            var backdropRect = backdropGo.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(3f, 3f);
            backdropRect.offsetMax = new Vector2(-3f, -3f);
            var backdropImg = backdropGo.AddComponent<UnityEngine.UI.Image>();
            backdropImg.color = new Color(0.06f, 0.07f, 0.11f, 0.94f);
            backdropImg.raycastTarget = false;

            var labelGo = new GameObject("LoreText", typeof(RectTransform));
            labelGo.transform.SetParent(targetGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 12f);
            labelRect.offsetMax = new Vector2(-16f, -12f);
            var text = labelGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.92f, 0.84f, 1f);
            text.text = initialText;
            text.raycastTarget = false;
            return text;
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void BuildRound5SceneAtmosphereAndLighting(
            Transform camTransform,
            Material spriteLit,
            PrisonPalette pal,
            GameObject playerGo,
            GameObject enemiesRoot)
        {
            int defaultLayerId = SortingLayerSetup.GetSortingLayerId("Default");
            int parallaxFarLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxFar");
            int parallaxMidLayerId = SortingLayerSetup.GetSortingLayerId("ParallaxMid");
            int bgLayerId = SortingLayerSetup.GetSortingLayerId("Background");
            int groundLayerId = SortingLayerSetup.GetSortingLayerId("Ground");
            int actorsLayerId = SortingLayerSetup.GetSortingLayerId("Actors");
            int vfxLayerId = SortingLayerSetup.GetSortingLayerId("VFX");
            int fgLayerId = SortingLayerSetup.GetSortingLayerId("Foreground");

            int[] allLayerIds = { defaultLayerId, parallaxFarLayerId, parallaxMidLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId, fgLayerId };
            int[] gameplayLayerIds = { defaultLayerId, bgLayerId, groundLayerId, actorsLayerId, vfxLayerId };

            // Assign Actors sorting layer to Player and all 30 Enemies
            foreach (var sr in playerGo.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerID = actorsLayerId;
            foreach (var sr in enemiesRoot.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerID = actorsLayerId;

            // 1. 4-Tier Hybrid Parallax container Prison_Parallax_R5 (5x4 ParallaxFar + 4-tier ParallaxMid)
            var parallaxRoot = new GameObject("Prison_Parallax_R5");
            parallaxRoot.transform.position = Vector3.zero;

            var farGo = CreateChild(parallaxRoot, "ParallaxFar_Backdrop");
            var farParallax = farGo.AddComponent<ParallaxLayer2D>();
            farParallax.Configure(camTransform, new Vector2(0.15f, 0.06f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
            float[] farXs = { -64f, -32f, 0f, 32f, 64f };
            float[] farYs = { -17.3333f, 4.0f, 25.3333f, 46.6667f };
            for (int ix = 0; ix < farXs.Length; ix++)
            {
                for (int iy = 0; iy < farYs.Length; iy++)
                {
                    var panel = CreateArchFrameSprite(
                        farGo,
                        $"Panel_{ix}_{iy}",
                        pal.ParallaxFarSprite,
                        new Vector3(farXs[ix], farYs[iy], 0f),
                        parallaxFarLayerId,
                        0,
                        pal.MatParallaxFar,
                        Color.white);
                    panel.transform.localPosition = new Vector3(farXs[ix], farYs[iy], 0f);
                }
            }

            var midGo = CreateChild(parallaxRoot, "ParallaxMid_Alcoves");
            var midParallax = midGo.AddComponent<ParallaxLayer2D>();
            midParallax.Configure(camTransform, new Vector2(0.38f, 0.14f), 32f, pixelSnap: true, pixelsPerUnit: 24f);
            float[] chunkOffsets = { -32f, 0f, 32f };
            float[] alcoveAXs = { -13.3333f, -8f, -2.6667f, 2.6667f, 8f, 13.3333f };
            float[] tierYs = { -13.0f, 2.6f, 22.0f, 44.0f };
            float[] vineXs = { -9.5f, 1.5f, 11.5f };
            var vineTint = new Color(0.58f, 0.72f, 0.64f, 0.90f);
            for (int c = 0; c < chunkOffsets.Length; c++)
            {
                var chunkGo = CreateChild(midGo, $"Chunk_{c}");
                chunkGo.transform.localPosition = new Vector3(chunkOffsets[c], 0f, 0f);
                for (int t = 0; t < tierYs.Length; t++)
                {
                    float ty = tierYs[t];
                    for (int a = 0; a < alcoveAXs.Length; a++)
                    {
                        var spGo = CreateArchFrameSprite(chunkGo, $"AlcoveA_{t}_{a}", pal.AlcoveParallaxA, Vector3.zero, parallaxMidLayerId, 0, pal.MatParallaxMid, Color.white);
                        spGo.transform.localPosition = new Vector3(alcoveAXs[a], ty, 0f);
                    }
                    for (int v = 0; v < vineXs.Length; v++)
                    {
                        var vineSp = (v % 2 == 0) ? pal.VineParallaxA : pal.VineParallaxB;
                        var vGo = CreateArchFrameSprite(chunkGo, $"Vine_{t}_{v}", vineSp, Vector3.zero, parallaxMidLayerId, 2, pal.MatVineSway, vineTint);
                        vGo.transform.localPosition = new Vector3(vineXs[v], ty + 3.6f, 0f);
                    }
                }
            }

            var fgGo = CreateChild(parallaxRoot, "Foreground_Silhouettes");
            var fgParallax = fgGo.AddComponent<ParallaxLayer2D>();
            fgParallax.Configure(camTransform, new Vector2(0f, 1.0f), 0f, pixelSnap: true, pixelsPerUnit: 24f);
            fgParallax.ConfigureVerticalCameraLock(true, 1.5f);
            var unlitMat = LoadMaterialAtPath(SpriteUnlitMatPath);
            var fgTint = new Color(4f / 255f, 8f / 255f, 14f / 255f, 0.15f);
            float[] fgColXs = { -38.0f, -12.85f, 12.85f, 38.0f };
            float[] fgSegLocalYs = { -10.0f, -5.2f, -0.4f, 4.4f, 9.2f, 14.0f };
            for (int i = 0; i < fgColXs.Length; i++)
            {
                var colGo = CreateChild(fgGo, $"FgColumn_{i}");
                colGo.transform.localPosition = new Vector3(fgColXs[i], 0f, 0f);
                for (int s = 0; s < fgSegLocalYs.Length; s++)
                {
                    var segGo = CreateArchFrameSprite(colGo, $"Seg_{s}", pal.SpawnerPillar, Vector3.zero, fgLayerId, 20, unlitMat, fgTint);
                    segGo.transform.localPosition = new Vector3(0f, fgSegLocalYs[s], 0f);
                }
            }
            fgParallax.ConfigureFrustumEdgePinning(true, 0.46f, 26f);

            // 2. 8 Damp-Corner GlowingMushroom_0..7 + Cap-Aligned Additive MushroomHalo_0..7 + Isolated MushroomLight_0..7 (Spec §5.5 #3)
            var dressingRoot = new GameObject("Prison_Dressing_R5");
            (float x, float floorTop)[] mushroomSpecs =
            {
                (43.5f, 0f),
                (58.5f, 0f),
                (-28.5f, 16f),
                (-14.5f, 24f),
                (113.8f, 0f),
                (98.0f, -16f),
                (170.0f, -16f),
                (-68.0f, 16f),
            };
            for (int i = 0; i < mushroomSpecs.Length; i++)
            {
                float mx = mushroomSpecs[i].x;
                float floorTop = mushroomSpecs[i].floorTop;
                bool flipX = (i % 2) == 1;
                Vector3 mPos = new Vector3(mx, floorTop - pal.GlowingMushroomBright.bounds.min.y, 0f);
                var mGo = CreateArchFrameSprite(dressingRoot, $"GlowingMushroom_{i}", pal.GlowingMushroomBright, mPos, groundLayerId, -7, pal.MatEmissiveMushroom, Color.white);
                mGo.GetComponent<SpriteRenderer>().flipX = flipX;

                float budX = mx + (flipX ? 0.484f : -0.484f);
                float budY = floorTop + 0.410f;
                var haloGo = CreateArchFrameSprite(dressingRoot, $"MushroomHalo_{i}", pal.FxGlowWhite, new Vector3(budX, budY, 0f), groundLayerId, -8, pal.MatEmissiveMushroomHalo, new Color(1f, 1f, 1f, 0.24f));
                haloGo.transform.localScale = new Vector3(0.16f, 0.14f, 1f);

                var lightGo = CreateChild(dressingRoot, $"MushroomLight_{i}");
                lightGo.transform.position = new Vector3(budX, budY, 0f);
                var mLight = lightGo.AddComponent<Light2D>();
                mLight.lightType = Light2D.LightType.Point;
                mLight.color = new Color(0.17f, 0.90f, 0.84f, 1f);
                mLight.intensity = 0.72f;
                mLight.pointLightInnerRadius = 0.15f;
                mLight.pointLightOuterRadius = 2.1f;
                mLight.falloffIntensity = 0.55f;
                ConfigureGameplayPointLight(mLight, enableVolumetric: false, volumeIntensity: 0f, addFlicker: false, phaseSeed: 0f);
            }

            // 3. 9 GroundMist_R5 strips (2.8u height) across dungeon lower wells, tiers & sewers (Spec §5.5 #4 + Round 9 Task 3.2b)
            var mistRoot = new GameObject("GroundMist_R5");
            (string name, Vector3 pos, Vector3 scale)[] mistSpecs =
            {
                ("Mist_RoomAB", new Vector3(21f, 1.4f, 0f), new Vector3(38f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomC", new Vector3(49f, 1.4f, 0f), new Vector3(14f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomG", new Vector3(66f, 1.4f, 0f), new Vector3(20f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomK", new Vector3(-23f, 17.4f, 0f), new Vector3(14f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomE", new Vector3(3f, 25.4f, 0f), new Vector3(36f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomO", new Vector3(120f, -14.6f, 0f), new Vector3(88f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomP", new Vector3(198f, -14.6f, 0f), new Vector3(70f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomQ", new Vector3(-66f, 17.4f, 0f), new Vector3(72f * 24f, 2.8f * 24f, 1f)),
                ("Mist_RoomT", new Vector3(89f, 41.4f, 0f), new Vector3(96f * 24f, 2.8f * 24f, 1f)),
            };
            var mistTint = new Color(0.84f, 0.90f, 0.92f, 0.86f);
            foreach (var spec in mistSpecs)
            {
                var strip = CreateArchFrameSprite(mistRoot, spec.name, pal.WhitePixel, spec.pos, vfxLayerId, -1, pal.MatGroundMist, mistTint);
                strip.transform.localScale = spec.scale;
            }

            // 4. 12 ShadowCasters_R5 on [Background, Ground] (Spec §5.5 #4 + Round 8 cross-room light containment)
            var castersRoot = new GameObject("ShadowCasters_R5");
            int[] shadowTargetLayers = { bgLayerId, groundLayerId };
            CreatePolygonShadowCaster(castersRoot.transform, "Caster_StairsH", new[]
            {
                new Vector3(6f, 7f, 0f),
                new Vector3(22f, 7f, 0f),
                new Vector3(22f, 8f, 0f),
                new Vector3(18f, 8f, 0f),
                new Vector3(10f, 16f, 0f),
                new Vector3(6f, 16f, 0f),
            }, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorG_Upper", new Vector3(60.5f, 7.5f, 0f), 9f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorF_CeilingE", new Vector3(11f, 31.5f, 0f), 22f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorN_West", new Vector3(88.5f, 7.5f, 0f), 25f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorR_East", new Vector3(-46.5f, 23.5f, 0f), 33f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorT_West", new Vector3(57.5f, 39.5f, 0f), 39f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorM_CeilingO", new Vector3(95.0f, -0.5f, 0f), 38f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorM_East_CeilingO", new Vector3(142.5f, -0.5f, 0f), 43f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorR_West_CeilingQ", new Vector3(-85.5f, 23.5f, 0f), 33f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorD_CeilingB", new Vector3(32.0f, 7.5f, 0f), 20f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorJ_West", new Vector3(-5.0f, 15.5f, 0f), 22f, 1f, shadowTargetLayers);
            CreateBoxShadowCaster(castersRoot.transform, "Caster_FloorT_East_CeilingS", new Vector3(111.5f, 39.5f, 0f), 57f, 1f, shadowTargetLayers);

            // 5. Ensure all scene lights (Global + Point, including cloned Scroll glow & Flash light) have strict sorting-layer masks and Fast NormalMapQuality
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    SetLightSortingLayers(light, allLayerIds);
                }
                else if (light.lightType == Light2D.LightType.Point)
                {
                    SetLightSortingLayers(light, gameplayLayerIds);
                    if (light.gameObject.name != "PlayerRimLight" && light.GetComponentInParent<PlayerController>() == null)
                        SetLightNormalMapQuality(light, Light2D.NormalMapQuality.Fast, 3.2f);
                    if (light.volumetricEnabled)
                    {
                        if (light.volumeIntensity > 0.10f)
                            light.volumeIntensity = 0.09f;
                        if (light.intensity > 1.50f)
                            light.intensity = 1.40f;
                    }
                }
            }
        }

        private static void CreateBoxShadowCaster(Transform parent, string name, Vector3 worldCenter, float width, float height, int[] targetSortingLayerIds)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            var worldVertices = new[]
            {
                worldCenter + new Vector3(-hw, -hh, 0f),
                worldCenter + new Vector3(-hw, hh, 0f),
                worldCenter + new Vector3(hw, hh, 0f),
                worldCenter + new Vector3(hw, -hh, 0f),
            };
            CreatePolygonShadowCaster(parent, name, worldVertices, targetSortingLayerIds);
        }

        private static void CreatePolygonShadowCaster(Transform parent, string name, Vector3[] worldVertices, int[] targetSortingLayerIds)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            // Keep m_ShapePath in compact local space: anchor the GameObject at the polygon bounds center.
            Vector3 min = worldVertices[0];
            Vector3 max = worldVertices[0];
            for (int i = 1; i < worldVertices.Length; i++)
            {
                min = Vector3.Min(min, worldVertices[i]);
                max = Vector3.Max(max, worldVertices[i]);
            }
            Vector3 worldCenter = (min + max) * 0.5f;
            go.transform.position = worldCenter;

            var sc = go.AddComponent<ShadowCaster2D>();
            sc.castsShadows = true;
            sc.selfShadows = false;

            var shape = new Vector3[worldVertices.Length];
            for (int i = 0; i < worldVertices.Length; i++)
                shape[i] = worldVertices[i] - worldCenter;

            int shapeHash;
            unchecked
            {
                shapeHash = (int)2166136261;
                for (int i = 0; i < shape.Length; i++)
                    shapeHash = shapeHash * 16777619 ^ shape[i].GetHashCode();
            }

            var so = new SerializedObject(sc);
            so.FindProperty("m_ShadowCastingSource").intValue = 1;
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
        }

        public static void CaptureRound5GpuVerificationFrames()
        {
            var shots = new (string file, Vector3 pos, float orthoSize)[]
            {
                ("gpu_r5_ng_01_room_ab.png", new Vector3(20f, 3.0f, -10f), 5.8f),
                ("gpu_r5_ng_02_room_cd.png", new Vector3(45f, 6.5f, -10f), 6.5f),
                ("gpu_r5_ng_03_room_g.png", new Vector3(66f, 5.5f, -10f), 6.2f),
                ("gpu_r5_ng_04_room_hjk.png", new Vector3(-8f, 19.5f, -10f), 6.0f),
                ("gpu_r5_ng_05_room_elf.png", new Vector3(8f, 29.5f, -10f), 6.5f),
            };

            CaptureGpuVerificationFramesCore(
                envVarName: "R5_GPU_CAPTURE_DIR",
                defaultRelativeDir: "Logs/r5-gpu-captures",
                logTag: "R5_NG_GPU_VERIFY",
                shots: shots,
                enforceStrictGates: false);
        }

        public static void CaptureRound6GpuVerificationFrames()
        {
            var shots = new (string file, Vector3 pos, float orthoSize)[]
            {
                ("gpu_r6_ng_01_room_ab.png", new Vector3(18f, 3.0f, -10f), 5.8f),
                ("gpu_r6_ng_02_room_cd.png", new Vector3(45f, 6.5f, -10f), 6.5f),
                ("gpu_r6_ng_03_room_g.png", new Vector3(66f, 5.5f, -10f), 6.2f),
                ("gpu_r6_ng_04_room_hjk.png", new Vector3(-8f, 19.5f, -10f), 6.0f),
                ("gpu_r6_ng_05_room_elf.png", new Vector3(8f, 29.5f, -10f), 6.5f),
                ("gpu_r6_ng_06_wing_w1_mn.png", new Vector3(122f, 11.5f, -10f), 7.0f),
                ("gpu_r6_ng_07_wing_w2_op.png", new Vector3(198f, -10.5f, -10f), 6.8f),
                ("gpu_r6_ng_08_wing_w3_qr.png", new Vector3(-66f, 27.5f, -10f), 7.0f),
                ("gpu_r6_ng_09_wing_u1_st.png", new Vector3(72f, 38.0f, -10f), 7.0f),
            };

            CaptureGpuVerificationFramesCore(
                envVarName: "R6_GPU_CAPTURE_DIR",
                defaultRelativeDir: "Logs/r6-gpu-captures",
                logTag: "R6_NG_GPU_VERIFY",
                shots: shots,
                enforceStrictGates: true);
        }

        public static void CaptureRound9GpuVerificationFrames()
        {
            int total = Round9VerificationShots.Length;
            int start = 1;
            int end = total;
            if (int.TryParse(Environment.GetEnvironmentVariable("R9_CAPTURE_START"), out int envStart))
                start = Mathf.Clamp(envStart, 1, total);
            if (int.TryParse(Environment.GetEnvironmentVariable("R9_CAPTURE_END"), out int envEnd))
                end = Mathf.Clamp(envEnd, start, total);

            CaptureRound9GpuVerificationFramesRange(start, end);
        }

        public static void CaptureRound9GpuVerificationFrames_Part1()
        {
            CaptureRound9GpuVerificationFramesRange(1, 8);
        }

        public static void CaptureRound9GpuVerificationFrames_Part2()
        {
            CaptureRound9GpuVerificationFramesRange(9, 15);
        }

        private static readonly (string file, Vector3 pos, float orthoSize)[] Round9VerificationShots =
        {
            ("gpu_r9_ng_01_room_ab.png", new Vector3(18f, 3.0f, -10f), 5.8f),
            ("gpu_r9_ng_02_room_cd.png", new Vector3(47.0f, 9.8f, -10f), 10.2f),
            ("gpu_r9_ng_03_room_g.png", new Vector3(66f, 5.5f, -10f), 6.2f),
            ("gpu_r9_ng_04_room_hjk.png", new Vector3(-8f, 19.5f, -10f), 6.0f),
            ("gpu_r9_ng_05_room_elf.png", new Vector3(8f, 29.5f, -10f), 6.5f),
            ("gpu_r9_ng_06_wing_w1_mn.png", new Vector3(122f, 11.5f, -10f), 7.0f),
            ("gpu_r9_ng_07_wing_w2_op.png", new Vector3(198f, -10.5f, -10f), 6.8f),
            ("gpu_r9_ng_08_wing_w3_qr.png", new Vector3(-66f, 27.5f, -10f), 7.0f),
            ("gpu_r9_ng_09_wing_u1_st.png", new Vector3(72f, 38.0f, -10f), 7.0f),
            ("gpu_r9_ng_10_stairs_h_45deg.png", new Vector3(14.0f, 12.5f, -10f), 5.8f),
            ("gpu_r9_ng_11_player_spawn_window_mist.png", new Vector3(9.0f, 2.8f, -10f), 5.2f),
            ("gpu_r9_ng_12_room_n_payoff_pilasters.png", new Vector3(148.5f, 13.5f, -10f), 7.8f),
            ("gpu_r9_ng_13_room_t_scroll_promenade.png", new Vector3(119.0f, 45.0f, -10f), 6.8f),
            ("gpu_r9_ng_14_room_qr_mixels_sword.png", new Vector3(-49.5f, 26.8f, -10f), 11.2f),
            ("gpu_r9_ng_15_room_s_alchemy_library.png", new Vector3(58.5f, 31.0f, -10f), 8.8f),
        };

        private static void CaptureRound9GpuVerificationFramesRange(int startOneBased, int endOneBased)
        {
            int startIdx = Mathf.Clamp(startOneBased - 1, 0, Round9VerificationShots.Length - 1);
            int endIdx = Mathf.Clamp(endOneBased - 1, startIdx, Round9VerificationShots.Length - 1);
            int count = (endIdx - startIdx) + 1;
            var sliced = new (string file, Vector3 pos, float orthoSize)[count];
            Array.Copy(Round9VerificationShots, startIdx, sliced, 0, count);

            CaptureGpuVerificationFramesCore(
                envVarName: "R9_GPU_CAPTURE_DIR",
                defaultRelativeDir: "Logs/r9-gpu-captures",
                logTag: "R9_NG_GPU_VERIFY",
                shots: sliced,
                enforceStrictGates: true,
                ultraLowRam: true);
        }

        private static void CaptureGpuVerificationFramesCore(
            string envVarName,
            string defaultRelativeDir,
            string logTag,
            (string file, Vector3 pos, float orthoSize)[] shots,
            bool enforceStrictGates,
            bool ultraLowRam = false)
        {
            const double MaxClipPct = 0.500;

            string outDir = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrWhiteSpace(outDir))
                outDir = Path.GetFullPath(defaultRelativeDir);
            Directory.CreateDirectory(outDir);

            string[] shaderNames =
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
            foreach (string shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                {
                    shaderErrors++;
                    Debug.LogError($"[{logTag}] Shader error or missing: {shaderName}");
                }
            }
            Debug.Log($"[{logTag}] gfxDevice={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) shaderErrors={shaderErrors}");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new InvalidOperationException("Failed to open NhaGiamGiengChuong.unity for GPU verification.");

            var urpAsset = UniversalRenderPipeline.asset;
            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
                throw new InvalidOperationException("Main Camera missing in NhaGiamGiengChuong.unity.");

            var uacd = cam.GetComponent<UniversalAdditionalCameraData>();
            var globalVol = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include)
                .FirstOrDefault(v => v.enabled && v.isGlobal);

            bool urpActive = urpAsset != null;
            bool hdrOk = urpAsset != null && urpAsset.supportsHDR;
            bool camPostOk = uacd != null && uacd.renderPostProcessing;
            bool volMaskOk = uacd != null && globalVol != null && (uacd.volumeLayerMask & (1 << globalVol.gameObject.layer)) != 0;
            bool profileOk = globalVol != null && globalVol.sharedProfile != null &&
                             globalVol.sharedProfile.TryGet<Bloom>(out var b) && b.intensity.overrideState &&
                             globalVol.sharedProfile.TryGet<Vignette>(out var v) && v.intensity.overrideState &&
                             globalVol.sharedProfile.TryGet<Tonemapping>(out var tm) && tm.mode.overrideState &&
                             globalVol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.contrast.overrideState && ca.saturation.overrideState &&
                             globalVol.sharedProfile.TryGet<SplitToning>(out var st) && st.shadows.overrideState && st.highlights.overrideState;

            int preFlightPassCount = (urpActive ? 1 : 0) + (hdrOk ? 1 : 0) + (camPostOk ? 1 : 0) + (volMaskOk ? 1 : 0) + (profileOk ? 1 : 0);
            Debug.Log($"[{logTag}] URP_PreFlight {preFlightPassCount}/5 {(preFlightPassCount == 5 ? "True" : "False")} urpActive={urpActive} hdr={hdrOk} camPost={camPostOk} volMask={volMaskOk} profileAll5={profileOk}");

            int defaultMatCount = UnityEngine.Object.FindObjectsByType<Renderer>()
                .Count(r => r.sharedMaterial != null && r.sharedMaterial.name == "Default-Material");
            Debug.Log($"[{logTag}] PaletteScan defaultMaterialRenderers={defaultMatCount}");

            foreach (var pin in UnityEngine.Object.FindObjectsByType<VertexSwayPin2D>(FindObjectsInactive.Include))
                pin.ApplyPropertyBlock();

            const System.Reflection.BindingFlags NonPublicInstance =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var updateMeshMethod = typeof(Light2D).GetMethod("UpdateMesh", NonPublicInstance);
            var updateBoundsMethod = typeof(Light2D).GetMethod("UpdateBoundingSphere", NonPublicInstance);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light != null && light.isActiveAndEnabled && light.lightType != Light2D.LightType.Global)
                {
                    updateMeshMethod?.Invoke(light, new object[] { true });
                    updateBoundsMethod?.Invoke(light, null);
                }
            }
            foreach (var sc in UnityEngine.Object.FindObjectsByType<ShadowCaster2D>())
            {
                if (sc != null && sc.isActiveAndEnabled)
                    sc.Update();
            }

            var parallaxLayers = UnityEngine.Object.FindObjectsByType<ParallaxLayer2D>(FindObjectsInactive.Include);
            var origParallaxPositions = parallaxLayers.Select(l => l != null ? l.transform.position : Vector3.zero).ToArray();
            var origPos = cam.transform.position;
            float origOrtho = cam.orthographicSize;
            var origTarget = cam.targetTexture;

            const int width = 1280;
            const int height = 720;
            var rt = ultraLowRam
                ? new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                : new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            rt.hideFlags = HideFlags.DontUnloadUnusedAsset;
            rt.antiAliasing = 1;
            rt.Create();
            var readTex = ultraLowRam
                ? new Texture2D(width, height, TextureFormat.RGBA32, false)
                : new Texture2D(width, height, TextureFormat.RGBAFloat, false, linear: true);
            readTex.hideFlags = HideFlags.DontUnloadUnusedAsset;

            var gateFailures = new List<string>();
            if (shaderErrors != 0)
                gateFailures.Add($"shaderErrors={shaderErrors} (expected 0)");
            if (preFlightPassCount != 5)
                gateFailures.Add($"URP_PreFlight={preFlightPassCount}/5 (expected 5/5)");
            if (defaultMatCount != 0)
                gateFailures.Add($"defaultMaterialRenderers={defaultMatCount} (expected 0)");

            double maxClipPct = 0.0;

            try
            {
                cam.targetTexture = rt;

                if (shots.Length > 0)
                {
                    cam.transform.position = shots[0].pos;
                    cam.orthographicSize = shots[0].orthoSize;
                    foreach (var layer in parallaxLayers)
                        if (layer != null && layer.isActiveAndEnabled) layer.UpdateParallax(cam.transform.position);
                    cam.Render();
                }

                for (int shotIndex = 0; shotIndex < shots.Length; shotIndex++)
                {
                    var shot = shots[shotIndex];
                    cam.transform.position = shot.pos;
                    cam.orthographicSize = shot.orthoSize;
                    foreach (var layer in parallaxLayers)
                        if (layer != null && layer.isActiveAndEnabled) layer.UpdateParallax(shot.pos);
                    cam.Render();

                    RenderTexture.active = rt;
                    readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    RenderTexture.active = null;

                    double meanLum;
                    double clipPct;
                    byte[] pngBytes;

                    if (ultraLowRam)
                    {
                        var rawPixels = readTex.GetPixelData<Color32>(0);
                        double sumLum = 0.0;
                        int clipCount = 0;
                        for (int i = 0; i < rawPixels.Length; i++)
                        {
                            Color32 px = rawPixels[i];
                            sumLum += 0.2126 * px.r + 0.7152 * px.g + 0.0722 * px.b;
                            if (px.r >= 240 || px.g >= 240 || px.b >= 240)
                                clipCount++;
                        }
                        meanLum = sumLum / Math.Max(1, rawPixels.Length);
                        clipPct = (clipCount * 100.0) / Math.Max(1, rawPixels.Length);
                        pngBytes = readTex.EncodeToPNG();
                    }
                    else
                    {
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
                        meanLum = sumLum / Math.Max(1, linearPixels.Length);
                        clipPct = (clipCount * 100.0) / Math.Max(1, linearPixels.Length);
                        pngBytes = ImageConversion.EncodeArrayToPNG(
                            srgbPixels,
                            UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                            (uint)width,
                            (uint)height);
                    }

                    if (clipPct > maxClipPct)
                        maxClipPct = clipPct;

                    File.WriteAllBytes(Path.Combine(outDir, shot.file), pngBytes);
                    pngBytes = null;
                    Debug.Log($"[{logTag}] Captured {shot.file} ({width}x{height}) at cam={shot.pos} ortho={shot.orthoSize} meanLum={meanLum:F2} clip>=240={clipPct:F3}%");

                    if (clipPct > MaxClipPct)
                        gateFailures.Add($"{shot.file} clip>=240={clipPct:F3}% > {MaxClipPct:F3}%");

                    if (ultraLowRam)
                    {
                        if ((shotIndex + 1) % 3 == 0)
                            EditorUtility.UnloadUnusedAssetsImmediate(true);
                        GC.Collect();
                    }
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
                    {
                        parallaxLayers[i].transform.position = origParallaxPositions[i];
                        parallaxLayers[i].ResetFrustumPinnedColumnsToRest();
                    }
                }
                UnityEngine.Object.DestroyImmediate(readTex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                EditorUtility.UnloadUnusedAssetsImmediate(true);
                GC.Collect();
            }

            Debug.Log($"[{logTag}] Summary captured={shots.Length}/{shots.Length} shaderErrors={shaderErrors} preFlight={preFlightPassCount}/5 defaultMaterialRenderers={defaultMatCount} maxClip={maxClipPct:F3}%");

            if (enforceStrictGates && gateFailures.Count > 0)
            {
                throw new InvalidOperationException(
                    $"[{logTag}] GPU verification gate check failed: {string.Join("; ", gateFailures)}");
            }
        }
    }
}
