using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DeadCells.CameraSystem;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.RunFlow;
using DeadCells.VFX;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DeadCells.Core.Tests
{
    public sealed class MainLevelIntegrityTests
    {
        private const string MainLevelPath = "Assets/_Project/Scenes/Main_Level.unity";
        private static readonly Regex MiddlePlankNameRegex = new Regex(@"^woodenPlatform_[0-4]_00(_\d+)?$", RegexOptions.CultureInvariant);
        private string _previousScenePath;

        [SetUp]
        public void SetUp()
        {
            _previousScenePath = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(MainLevelPath, OpenSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_previousScenePath) && System.IO.File.Exists(_previousScenePath))
                EditorSceneManager.OpenScene(_previousScenePath, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void MainLevel_ScrollVats_UseSpriteLitDefaultInsteadOfPlayerPaletteSwap()
        {
            var scrollVats = UnityEngine.Object.FindObjectsByType<SpriteRenderer>()
                .Where(sr => sr.gameObject.name == "Scroll vat")
                .ToArray();
            Assert.GreaterOrEqual(scrollVats.Length, 2, "Main_Level must contain the 2 Scroll vat renderers.");

            foreach (var sr in scrollVats)
            {
                Assert.IsNotNull(sr.sharedMaterial, "Scroll vat must have a valid sharedMaterial.");
                StringAssert.DoesNotContain("Player_PaletteSwap", sr.sharedMaterial.name,
                    $"Scroll vat '{sr.transform.GetSiblingIndex()}' must not use Player_PaletteSwap material.");
            }
        }

        [Test]
        public void MainLevel_ClimbableChain_ConnectsFloor15TopToOneWayPlatform16()
        {
            var chain = GameObject.Find("Climbable chain");
            Assert.IsNotNull(chain, "Climbable chain must exist in Main_Level.");
            var box = chain.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(box);

            Bounds worldBounds = box.bounds;
            Assert.LessOrEqual(worldBounds.min.y, -0.5f,
                "Climbable chain trigger bottom must reach down to Floor 15 (y=-1.0u) so a grounded player can grab it.");
            Assert.GreaterOrEqual(worldBounds.max.y, 2.0f,
                "Climbable chain trigger top must reach One Way Platform 16 (y=2.0u).");
        }

        [Test]
        public void MainLevel_ArchiveGalleryPlatforms_UseLeftMiddleAndRightPlankSpritesPerPlatform()
        {
            var archiveGallery = GameObject.Find("Archive gallery");
            Assert.IsNotNull(archiveGallery, "Archive gallery parent container must exist in Main_Level.");
            Assert.AreEqual(6, archiveGallery.transform.childCount, "Archive gallery must contain 6 child one-way platforms.");

            int totalBoards = 0;
            foreach (Transform platform in archiveGallery.transform)
            {
                var boards = platform.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(sr => sr.transform.parent == platform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal) && sr.sprite != null)
                    .OrderBy(sr => sr.transform.localPosition.x)
                    .ToArray();
                Assert.GreaterOrEqual(boards.Length, 3, $"Platform '{platform.name}' must have at least 3 boards.");
                totalBoards += boards.Length;

                StringAssert.Contains("left", boards[0].sprite.name,
                    $"Leftmost board of '{platform.name}' must use woodenPlatform_left sprite.");
                StringAssert.Contains("right", boards[boards.Length - 1].sprite.name,
                    $"Rightmost board of '{platform.name}' must use woodenPlatform_right sprite.");
                for (int i = 1; i < boards.Length - 1; i++)
                {
                    Assert.IsTrue(MiddlePlankNameRegex.IsMatch(boards[i].sprite.name),
                        $"Interior board {i} of '{platform.name}' must use a Prison interior plank sprite (woodenPlatform_0..4_00), got '{boards[i].sprite.name}'.");
                }
                if (boards.Length >= 4)
                {
                    Assert.AreNotEqual(boards[1].sprite, boards[2].sprite,
                        $"Interior boards of '{platform.name}' must vary across plank sprites rather than repeating one sprite.");
                }
                for (int i = 0; i < boards.Length; i++)
                {
                    Assert.GreaterOrEqual(boards[i].transform.localScale.y, 0.70f,
                        $"Board '{boards[i].name}' of '{platform.name}' must maintain proportional vertical scale (>= 0.70), got {boards[i].transform.localScale.y:F3}.");
                    Assert.LessOrEqual(Mathf.Abs(boards[i].transform.localPosition.y), 0.05f,
                        $"Board '{boards[i].name}' of '{platform.name}' must align its opaque wood top with collider top (|localPosition.y| <= 0.05), got {boards[i].transform.localPosition.y:F3}.");
                }
            }
            Assert.AreEqual(40, totalBoards, "Archive gallery must contain 40 total boards across its 6 platforms.");
        }

        [Test]
        public void MainLevel_EastWing_ContainsEnemiesIncludingRatActiveHudInstructionAndFunctionalExitTrigger()
        {
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            Assert.IsTrue(enemies.Any(e => e.transform.position.x >= 55f),
                "East wing (x >= 55) must contain active combat encounters.");
            Assert.IsTrue(enemies.Any(e => e.gameObject.name.Contains("Enemy_Rat")),
                "Main_Level must include at least one Enemy_Rat instance.");

            var goldDrops = UnityEngine.Object.FindAnyObjectByType<EnemyGoldDrops>();
            Assert.IsNotNull(goldDrops, "EnemyGoldDrops component must exist in Main_Level.");
            var enemiesField = typeof(EnemyGoldDrops).GetField("_enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(enemiesField);
            var registeredEnemies = enemiesField.GetValue(goldDrops) as EnemyController[];
            Assert.IsNotNull(registeredEnemies);
            foreach (var enemy in enemies)
                CollectionAssert.Contains(registeredEnemies, enemy, $"Enemy '{enemy.name}' must be registered in EnemyGoldDrops._enemies.");

            var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>();
            Assert.GreaterOrEqual(scrolls.Length, 2, "Main_Level must contain both PowerScrollPickup instances.");
            float firstEnemyX = enemies.Min(e => e.transform.position.x);
            Assert.Greater(scrolls.Min(s => s.transform.position.x), firstEnemyX,
                "All Power Scrolls must be positioned after the first enemy encounter (not given for free at x=13.5).");

            var entranceScroll = GameObject.Find("Scroll of Power - Entrance");
            Assert.IsNotNull(entranceScroll);
            Assert.AreEqual(-1.0f, entranceScroll.transform.position.y, 0.05f,
                "Scroll of Power - Entrance at x=25.5 must sit grounded on Floor 15 - Zombie hall (y=-1.0).");

            var scrollHud = GameObject.Find("Power Scroll HUD");
            Assert.IsNotNull(scrollHud, "Power Scroll HUD must exist in Main_Level.");
            var upgradePanel = scrollHud.transform.Find("Choice backdrop/Upgrade panel") as RectTransform;
            Assert.IsNotNull(upgradePanel, "Upgrade panel must exist under Power Scroll HUD/Choice backdrop.");
            var titleRect = upgradePanel.Find("Title") as RectTransform;
            var instRect = upgradePanel.Find("Instruction") as RectTransform;
            Assert.IsNotNull(titleRect, "Title child must exist under Upgrade panel.");
            Assert.IsNotNull(instRect, "Instruction child must exist under Upgrade panel.");
            Assert.IsTrue(instRect.gameObject.activeSelf, "Instruction under Power Scroll HUD/Choice backdrop/Upgrade panel must have activeSelf == true.");

            float panelHalfH = upgradePanel.sizeDelta.y * 0.5f;
            float titleTop = titleRect.anchoredPosition.y + titleRect.sizeDelta.y * 0.5f;
            float titleBottom = titleRect.anchoredPosition.y - titleRect.sizeDelta.y * 0.5f;
            float instTop = instRect.anchoredPosition.y + instRect.sizeDelta.y * 0.5f;
            float instBottom = instRect.anchoredPosition.y - instRect.sizeDelta.y * 0.5f;

            Assert.LessOrEqual(titleTop, panelHalfH,
                $"Title top ({titleTop:F1}) must stay inside Upgrade panel top (+{panelHalfH:F1}).");
            Assert.LessOrEqual(instTop, titleBottom,
                $"Instruction top ({instTop:F1}) must sit at or below Title bottom ({titleBottom:F1}).");

            foreach (var cardName in new[] { "Brutality", "Tactics", "Survival" })
            {
                var card = upgradePanel.Find(cardName) as RectTransform;
                Assert.IsNotNull(card, $"Card '{cardName}' must exist under Upgrade panel.");
                float cardTop = card.anchoredPosition.y + card.sizeDelta.y * 0.5f;
                Assert.Greater(instBottom, cardTop,
                    $"Instruction bottom ({instBottom:F1}) must sit strictly above '{cardName}' card top ({cardTop:F1}).");
            }

            var defeatScreen = UnityEngine.Object.FindAnyObjectByType<DefeatScreen>();
            Assert.IsNotNull(defeatScreen, "DefeatScreen component must exist in Main_Level.");
            var headingField = typeof(DefeatScreen).GetField("_heading", BindingFlags.Instance | BindingFlags.NonPublic);
            var descriptionField = typeof(DefeatScreen).GetField("_description", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(headingField, "DefeatScreen must expose _heading field.");
            Assert.IsNotNull(descriptionField, "DefeatScreen must expose _description field.");
            var headingText = headingField.GetValue(defeatScreen) as Text;
            var descriptionText = descriptionField.GetValue(defeatScreen) as Text;
            Assert.IsNotNull(headingText, "DefeatScreen._heading must be wired in Main_Level.");
            Assert.IsNotNull(descriptionText, "DefeatScreen._description must be wired in Main_Level.");
            var descriptionRect = descriptionText.rectTransform;
            Assert.GreaterOrEqual(descriptionRect.sizeDelta.y, 50f,
                $"Defeat Screen Description height ({descriptionRect.sizeDelta.y:F1}) must be >= 50px to avoid 2-line Victory text truncation.");

            var exitTrigger = UnityEngine.Object.FindAnyObjectByType<LevelExitTrigger>();
            Assert.IsNotNull(exitTrigger, "Main_Level Exit Door must have a LevelExitTrigger component.");
            Assert.IsNotNull(exitTrigger.Session, "LevelExitTrigger must be wired to RunSession.");
            var exitCol = exitTrigger.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(exitCol);
            Assert.IsTrue(exitCol.isTrigger, "LevelExitTrigger collider must be a trigger.");
        }

        [Test]
        public void MainLevel_DeadCellsPrisonVisualStandard_ValidatesFloorsPillarsLightingAndPostProcess()
        {
            // 1. Solid floors: East wing stone has floorStamp_* caps at top row, dirt_00 at y <= -2 on BOTH wings, no orphaned x=52 wall, plus TM_SolidRim
            var tilemaps = UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>();
            var westStone = tilemaps.SingleOrDefault(t => t.gameObject.name == "Stone visuals");
            var eastStone = tilemaps.SingleOrDefault(t => t.gameObject.name == "East wing stone");
            var westBg = tilemaps.SingleOrDefault(t => t.gameObject.name == "Background");
            var solidRim = tilemaps.SingleOrDefault(t => t.gameObject.name == "TM_SolidRim");
            Assert.IsNotNull(westStone, "Stone visuals tilemap missing.");
            Assert.IsNotNull(eastStone, "East wing stone tilemap missing.");
            Assert.IsNotNull(westBg, "Background tilemap missing.");
            Assert.IsNotNull(solidRim, "TM_SolidRim tilemap missing.");
            Assert.IsNull(solidRim.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>(), "TM_SolidRim must be visual-only.");

            // Check East wing top floor caps: (56, -1) on Floor 45 [top=0], (75, -2) on Lower cell block [top=-1], (100, -1) on Exit hall [top=0]
            foreach (var cell in new[] { new Vector3Int(56, -1, 0), new Vector3Int(75, -2, 0), new Vector3Int(100, -1, 0) })
            {
                var sp = eastStone.GetSprite(cell);
                Assert.IsNotNull(sp, $"East wing stone missing top cap tile at {cell}.");
                StringAssert.StartsWith("floorStamp_", sp.name, $"East wing stone top cap at {cell} must use floorStamp_*, got '{sp.name}'.");
            }
            // Check underground solid core uses opaque dirt_00 (never transparent ground_* rim!) and no orphaned x=52 wall in Stone visuals
            Assert.AreEqual("dirt_00", westStone.GetSprite(new Vector3Int(6, -2, 0))?.name, "West wing underground row y=-2 must use opaque dirt_00.");
            Assert.AreEqual("dirt_00", eastStone.GetSprite(new Vector3Int(75, -3, 0))?.name, "East wing underground row y=-3 must use opaque dirt_00.");
            Assert.AreEqual("dirt_00", eastStone.GetSprite(new Vector3Int(100, -2, 0))?.name, "East wing underground row y=-2 must use opaque dirt_00.");
            Assert.IsNull(westStone.GetSprite(new Vector3Int(52, 4, 0)), "Orphaned wall column at x=52, y=0..9 in Stone visuals must be cleared.");

            // 2. West wing Background extended to y >= 14 (no void cliff at x=52)
            Assert.IsNotNull(westBg.GetSprite(new Vector3Int(40, 14, 0)), "West wing Background must extend up to y=14..15 to eliminate the x=52 void cliff.");

            // 3. East wing pillars grounded flush on their floors (never sunken 4u underground)
            var cellPillar = GameObject.Find("Cell block pillar")?.GetComponent<SpriteRenderer>();
            var exitPillar = GameObject.Find("Exit hall pillar")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(cellPillar);
            Assert.IsNotNull(exitPillar);
            Assert.AreEqual(-1.0f, cellPillar.bounds.min.y, 0.12f, "Cell block pillar bottom must rest flush on Lower cell block floor (y=-1.0).");
            Assert.AreEqual(0.0f, exitPillar.bounds.min.y, 0.12f, "Exit hall pillar bottom must rest flush on Exit hall floor (y=0.0).");

            // 4. West wing One Way Platform 16 / 21 / 28 unified to scale.y >= 0.70 and localPosition.y == 0
            foreach (var platName in new[] { "One Way Platform 16", "One Way Platform 21", "One Way Platform 28" })
            {
                var plat = GameObject.Find(platName);
                Assert.IsNotNull(plat, $"Missing '{platName}'.");
                var boards = plat.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(sr => sr.transform.parent == plat.transform && sr.gameObject.name.StartsWith("Board", StringComparison.Ordinal))
                    .ToArray();
                Assert.GreaterOrEqual(boards.Length, 3, $"'{platName}' must have >= 3 boards.");
                foreach (var b in boards)
                {
                    Assert.GreaterOrEqual(b.transform.localScale.y, 0.70f, $"West wing '{platName}' board must match 0.75 scale.");
                    Assert.LessOrEqual(Mathf.Abs(b.transform.localPosition.y), 0.05f, $"West wing '{platName}' board must align at y=0.");
                }
            }

            // 5. Exit Guard clear of Return to main route overhang (x >= 99)
            var exitGuard = GameObject.Find("Enemy_Shield - Exit Guard");
            Assert.IsNotNull(exitGuard);
            Assert.GreaterOrEqual(exitGuard.transform.position.x, 99.0f, "Exit Guard must stand clear of Return to main route platform overhang.");

            // 6. Dead Cells lighting (Global Light 2D in [0.48, 0.62]), URP Post-Processing (Bloom + Vignette + ACES), Ambient VFX
            var globalLight = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>()
                .FirstOrDefault(l => l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
            Assert.IsNotNull(globalLight, "Main_Level missing Global Light 2D.");
            Assert.GreaterOrEqual(globalLight.intensity, 0.48f, "Global Light 2D too dark.");
            Assert.LessOrEqual(globalLight.intensity, 0.62f, "Global Light 2D too flat/bright (must be <= 0.62 for Dead Cells contrast).");

            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.IsNotNull(cam);
            var uacd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Assert.IsNotNull(uacd);
            Assert.IsTrue(uacd.renderPostProcessing, "Main Camera must enable renderPostProcessing = true.");

            var vol = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>().FirstOrDefault(v => v.enabled && v.isGlobal);
            Assert.IsNotNull(vol, "Main_Level must contain an enabled Global Volume.");
            Assert.IsNotNull(vol.sharedProfile, "Global Volume must reference DeadCells_PostProcessProfile.asset.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom) && bloom.intensity.overrideState && bloom.intensity.value > 0f, "Bloom override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette) && vignette.intensity.overrideState && vignette.intensity.value > 0f, "Vignette override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tone) && tone.mode.overrideState && tone.mode.value == UnityEngine.Rendering.Universal.TonemappingMode.ACES, "ACES Tonemapping override missing.");

            var ambientVfx = UnityEngine.Object.FindObjectsByType<ParticleSystem>();
            Assert.GreaterOrEqual(ambientVfx.Length, 1, "Main_Level must include ambient ParticleSystem (Dust Motes).");
        }

        [Test]
        public void MainLevel_Round4_ParallaxLightingShadersAndCleanDressing_ValidatesVerticalSlice()
        {
            var activeScene = SceneManager.GetActiveScene();

            // 1. R3 P1 defects eliminated
            var forbiddenNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
            {
                "Beam_TR_West",
                "Beam_TL_East",
                "Web_TR_West",
                "Web_TL_East",
                "Beam_TL_West",
                "Beam_TR_East"
            };
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var tr in allTransforms)
            {
                if (tr.gameObject.scene != activeScene)
                    continue;
                Assert.IsFalse(forbiddenNames.Contains(tr.name),
                    $"Forbidden R3 floating object '{tr.name}' must be removed.");
                Assert.IsFalse(tr.name.StartsWith("CornerL_One Way Platform", StringComparison.Ordinal),
                    $"Floating corner brace '{tr.name}' must be removed.");
                Assert.IsFalse(tr.name.StartsWith("CornerR_One Way Platform", StringComparison.Ordinal),
                    $"Floating corner brace '{tr.name}' must be removed.");
            }

            var dressingRoot = GameObject.Find("Prison_Dressing_R3");
            Assert.IsNotNull(dressingRoot, "Prison_Dressing_R3 container must exist.");
            foreach (var sr in dressingRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite != null)
                {
                    Assert.IsFalse(sr.sprite.name.StartsWith("floorjunk_1_00", StringComparison.Ordinal),
                        $"Sarcophagus sprite '{sr.sprite.name}' on '{sr.name}' must be replaced with low rubble.");
                }
            }

            var bannerBig92 = GameObject.Find("BannerBig_92");
            Assert.IsNotNull(bannerBig92, "BannerBig_92 must exist.");
            var bannerBig92Sr = bannerBig92.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(bannerBig92Sr, "BannerBig_92 must have a SpriteRenderer.");
            Assert.GreaterOrEqual(bannerBig92Sr.bounds.min.y, 2.2f,
                $"BannerBig_92 bottom ({bannerBig92Sr.bounds.min.y:F2}) must clear Return to main route top (>= 2.2f).");

            float[] expectedWindowYs = { 5f, 5f, 5f, 6f, 8f, 5f };
            for (int i = 0; i < expectedWindowYs.Length; i++)
            {
                var godRay = GameObject.Find($"GodRay_{i}");
                Assert.IsNotNull(godRay, $"GodRay_{i} must exist.");
                var sr = godRay.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr, $"GodRay_{i} must have a SpriteRenderer.");
                Assert.IsNotNull(sr.sharedMaterial, $"GodRay_{i} must have a sharedMaterial.");
                Assert.IsNotNull(sr.sharedMaterial.shader, $"GodRay_{i} material must have a shader.");
                Assert.AreEqual("DeadCells/AdditiveGodRay2D", sr.sharedMaterial.shader.name,
                    $"GodRay_{i} must use DeadCells/AdditiveGodRay2D shader.");
                Assert.Less(godRay.transform.position.y, expectedWindowYs[i] - 0.5f,
                    $"GodRay_{i} y ({godRay.transform.position.y:F2}) must sit below paired window y ({expectedWindowYs[i]:F2}).");
            }

            // 2. 4-Tier Parallax & Carved Archways
            var parallaxRoot = GameObject.Find("Prison_Parallax_R4");
            Assert.IsNotNull(parallaxRoot, "Prison_Parallax_R4 container must exist.");

            var farTransform = parallaxRoot.transform.Find("ParallaxFar_Backdrop");
            Assert.IsNotNull(farTransform, "ParallaxFar_Backdrop missing under Prison_Parallax_R4.");
            Assert.IsNotNull(farTransform.GetComponent<ParallaxLayer2D>(), "ParallaxFar_Backdrop must have ParallaxLayer2D.");
            var farRenderers = farTransform.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(farRenderers.Length, 1, "ParallaxFar_Backdrop must contain SpriteRenderers.");
            Bounds farBounds = farRenderers[0].bounds;
            foreach (var sr in farRenderers)
            {
                Assert.AreEqual("ParallaxFar", sr.sortingLayerName, $"ParallaxFar renderer '{sr.name}' sortingLayer mismatch.");
                Assert.IsNotNull(sr.sharedMaterial, $"ParallaxFar renderer '{sr.name}' missing material.");
                Assert.AreEqual("DeadCells/ParallaxGradientMap2D", sr.sharedMaterial.shader.name);
                Assert.IsNotNull(sr.sprite, $"ParallaxFar renderer '{sr.name}' missing sprite.");
                StringAssert.Contains("T_Prison_ParallaxFar_Grayscale", sr.sprite.texture.name);
                farBounds.Encapsulate(sr.bounds);
            }
            Assert.LessOrEqual(farBounds.min.y, -3f, $"ParallaxFar vertical coverage minY ({farBounds.min.y:F2}) must be <= -3.");
            Assert.GreaterOrEqual(farBounds.max.y, 16f, $"ParallaxFar vertical coverage maxY ({farBounds.max.y:F2}) must be >= 16.");

            var midTransform = parallaxRoot.transform.Find("ParallaxMid_Alcoves");
            Assert.IsNotNull(midTransform, "ParallaxMid_Alcoves missing under Prison_Parallax_R4.");
            Assert.IsNotNull(midTransform.GetComponent<ParallaxLayer2D>(), "ParallaxMid_Alcoves must have ParallaxLayer2D.");
            var midRenderers = midTransform.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(midRenderers.Length, 1, "ParallaxMid_Alcoves must contain SpriteRenderers.");
            bool hasAlcove = false;
            bool hasVine = false;
            foreach (var sr in midRenderers)
            {
                Assert.AreEqual("ParallaxMid", sr.sortingLayerName, $"ParallaxMid renderer '{sr.name}' sortingLayer mismatch.");
                if (sr.sprite != null)
                {
                    Assert.IsFalse(sr.sprite.name.StartsWith("alccoveParalaxeB_00", StringComparison.Ordinal),
                        $"Opaque black box sprite '{sr.sprite.name}' on '{sr.name}' must not be placed in ParallaxMid_Alcoves.");
                    if (sr.sprite.name.StartsWith("alccoveParalaxeA_00", StringComparison.Ordinal))
                        hasAlcove = true;
                    if (sr.sprite.name.StartsWith("paralaxLiane", StringComparison.Ordinal))
                        hasVine = true;
                }
            }
            Assert.IsTrue(hasAlcove, "ParallaxMid_Alcoves must use open Gothic arch sprite alccoveParalaxeA_00.");
            Assert.IsTrue(hasVine, "ParallaxMid_Alcoves must include paralaxLiane* vines.");

            var fgTransform = parallaxRoot.transform.Find("Foreground_Silhouettes");
            Assert.IsNotNull(fgTransform, "Foreground_Silhouettes missing under Prison_Parallax_R4.");
            Assert.IsNotNull(fgTransform.GetComponent<ParallaxLayer2D>(), "Foreground_Silhouettes must have ParallaxLayer2D.");
            var fgRenderers = fgTransform.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(fgRenderers.Length, 1, "Foreground_Silhouettes must contain SpriteRenderers.");
            foreach (var sr in fgRenderers)
            {
                Assert.AreEqual("Foreground", sr.sortingLayerName, $"Foreground renderer '{sr.name}' sortingLayer mismatch.");
                Assert.GreaterOrEqual(sr.bounds.min.y, 8.5f,
                    $"Foreground silhouette '{sr.name}' min.y ({sr.bounds.min.y:F2}) must stay strictly above combat ceiling (>= 8.5f).");
            }

            var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>();
            var westBg = tilemaps.SingleOrDefault(t => t.gameObject.name == "Background");
            var eastBg = tilemaps.SingleOrDefault(t => t.gameObject.name == "East wing background");
            var wallDecals = tilemaps.SingleOrDefault(t => t.gameObject.name == "TM_WallDecals");
            Assert.IsNotNull(westBg, "Background tilemap missing.");
            Assert.IsNotNull(eastBg, "East wing background tilemap missing.");
            Assert.IsNotNull(wallDecals, "TM_WallDecals tilemap missing.");

            var bays = new[]
            {
                (xMin: 16, xMax: 22, yMin: 4, yMax: 12),
                (xMin: 31, xMax: 35, yMin: 4, yMax: 12),
                (xMin: 52, xMax: 60, yMin: 3, yMax: 13),
                (xMin: 69, xMax: 76, yMin: 5, yMax: 13),
                (xMin: 86, xMax: 93, yMin: 4, yMax: 13),
            };
            int carvedCellCount = 0;
            foreach (var bay in bays)
            {
                var tlCorner = new Vector3Int(bay.xMin, bay.yMax - 1, 0);
                var trCorner = new Vector3Int(bay.xMax - 1, bay.yMax - 1, 0);
                Assert.IsTrue(westBg.GetTile(tlCorner) != null || eastBg.GetTile(tlCorner) != null,
                    $"Top-left arch corner {tlCorner} must remain solid.");
                Assert.IsTrue(westBg.GetTile(trCorner) != null || eastBg.GetTile(trCorner) != null,
                    $"Top-right arch corner {trCorner} must remain solid.");

                for (int x = bay.xMin; x < bay.xMax; x++)
                {
                    for (int y = bay.yMin; y < bay.yMax; y++)
                    {
                        if (y == bay.yMax - 1 && (x == bay.xMin || x == bay.xMax - 1))
                            continue;

                        var cell = new Vector3Int(x, y, 0);
                        Assert.IsNull(westBg.GetTile(cell), $"Background cell {cell} inside carved arch bay must be null.");
                        Assert.IsNull(eastBg.GetTile(cell), $"East wing background cell {cell} inside carved arch bay must be null.");
                        Assert.IsNull(wallDecals.GetTile(cell), $"TM_WallDecals cell {cell} inside carved arch bay must be null.");
                        carvedCellCount++;
                    }
                }
            }
            Assert.GreaterOrEqual(carvedCellCount, 150, $"Expected >= 150 carved arch bay cells, got {carvedCellCount}.");
            Assert.IsNotNull(westBg.GetTile(new Vector3Int(40, 14, 0)), "(40, 14) on Background must remain non-null.");

            // 3. Sorting Layer Light Isolation, Volumetric, Flicker, ShadowCasters & HDR Emissive
            int parallaxFarId = SortingLayer.NameToID("ParallaxFar");
            int parallaxMidId = SortingLayer.NameToID("ParallaxMid");
            int foregroundId = SortingLayer.NameToID("Foreground");
            Assert.AreNotEqual(0, parallaxFarId, "ParallaxFar sorting layer must exist.");

            var allLights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
            int volumetricFlickerCount = 0;
            foreach (var light in allLights)
            {
                if (light.lightType != Light2D.LightType.Point)
                    continue;

                if (light.enabled && light.gameObject.activeInHierarchy)
                {
                    var soLight = new SerializedObject(light);
                    var layersProp = soLight.FindProperty("m_ApplyToSortingLayers");
                    Assert.IsNotNull(layersProp, $"Point Light2D '{light.name}' missing m_ApplyToSortingLayers.");
                    Assert.Greater(layersProp.arraySize, 0, $"Point Light2D '{light.name}' m_ApplyToSortingLayers must not be empty.");
                    var targetLayerIds = new System.Collections.Generic.HashSet<int>();
                    for (int i = 0; i < layersProp.arraySize; i++)
                        targetLayerIds.Add(layersProp.GetArrayElementAtIndex(i).intValue);

                    Assert.IsFalse(targetLayerIds.Contains(parallaxFarId),
                        $"Point Light2D '{light.name}' must exclude ParallaxFar from m_ApplyToSortingLayers.");
                    Assert.IsFalse(targetLayerIds.Contains(parallaxMidId),
                        $"Point Light2D '{light.name}' must exclude ParallaxMid from m_ApplyToSortingLayers.");
                    Assert.IsFalse(targetLayerIds.Contains(foregroundId),
                        $"Point Light2D '{light.name}' must exclude Foreground from m_ApplyToSortingLayers.");

                    if (light.volumetricEnabled && light.volumeIntensity > 0f &&
                        light.TryGetComponent<TorchFlicker2D>(out var flicker) && flicker.enabled)
                    {
                        volumetricFlickerCount++;
                    }
                }
            }
            Assert.GreaterOrEqual(volumetricFlickerCount, 4,
                $"Expected >= 4 enabled Point Light2Ds with volumetricEnabled + volumeIntensity > 0 + active TorchFlicker2D, found {volumetricFlickerCount}.");

            var shadowCasters = UnityEngine.Object.FindObjectsByType<ShadowCaster2D>()
                .Where(sc => sc.castsShadows && !sc.selfShadows)
                .ToArray();
            Assert.GreaterOrEqual(shadowCasters.Length, 3,
                $"Expected >= 3 ShadowCaster2D components with castsShadows && !selfShadows, found {shadowCasters.Length}.");

            var allSceneRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>();
            int emissiveCount = allSceneRenderers.Count(sr =>
                sr.sharedMaterial != null &&
                sr.sharedMaterial.shader != null &&
                sr.sharedMaterial.shader.name == "DeadCells/EmissiveHdr2D");
            Assert.GreaterOrEqual(emissiveCount, 5,
                $"Expected >= 5 SpriteRenderers using DeadCells/EmissiveHdr2D, found {emissiveCount}.");

            var groundMistRoot = GameObject.Find("GroundMist_R4");
            Assert.IsNotNull(groundMistRoot, "GroundMist_R4 must exist.");
            int groundMistCount = groundMistRoot.GetComponentsInChildren<SpriteRenderer>(true).Count(sr =>
                sr.sharedMaterial != null &&
                sr.sharedMaterial.shader != null &&
                sr.sharedMaterial.shader.name == "DeadCells/GroundMist2D");
            Assert.GreaterOrEqual(groundMistCount, 1, "GroundMist_R4 must contain >= 1 SpriteRenderer using DeadCells/GroundMist2D.");

            var bannerRenderers = allSceneRenderers
                .Where(sr => sr.gameObject.name.StartsWith("BannerBig_", StringComparison.Ordinal) ||
                             sr.gameObject.name.StartsWith("RoomBanner_", StringComparison.Ordinal))
                .ToArray();
            Assert.GreaterOrEqual(bannerRenderers.Length, 6, "Expected BannerBig_* and RoomBanner_* renderers.");
            foreach (var sr in bannerRenderers)
            {
                Assert.IsNotNull(sr.sharedMaterial, $"Banner '{sr.name}' missing sharedMaterial.");
                Assert.AreEqual("DeadCells/VertexSway2D", sr.sharedMaterial.shader.name,
                    $"Banner '{sr.name}' must use DeadCells/VertexSway2D shader.");
            }

            // 4. Upgraded DeadCells_PostProcessProfile.asset
            var vol = UnityEngine.Object.FindObjectsByType<Volume>().FirstOrDefault(v => v.enabled && v.isGlobal);
            Assert.IsNotNull(vol, "Main_Level must contain an enabled Global Volume.");
            Assert.IsNotNull(vol.sharedProfile, "Global Volume must reference DeadCells_PostProcessProfile.asset.");
            Assert.IsTrue(vol.sharedProfile.TryGet<Bloom>(out var bloom) && bloom.active && bloom.intensity.overrideState, "Bloom override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<Vignette>(out var vignette) && vignette.active && vignette.intensity.overrideState, "Vignette override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<Tonemapping>(out var tone) && tone.active && tone.mode.overrideState && tone.mode.value == TonemappingMode.ACES, "ACES Tonemapping override missing.");
            Assert.IsTrue(vol.sharedProfile.TryGet<ColorAdjustments>(out var colorAdj) && colorAdj.active &&
                          colorAdj.contrast.overrideState && colorAdj.saturation.overrideState,
                "ColorAdjustments (active, contrast & saturation overrides) missing on DeadCells_PostProcessProfile.");
            Assert.IsTrue(vol.sharedProfile.TryGet<SplitToning>(out var splitToning) && splitToning.active &&
                          splitToning.shadows.overrideState && splitToning.highlights.overrideState,
                "SplitToning (active, shadows & highlights overrides) missing on DeadCells_PostProcessProfile.");
        }

        [Test]
        public void MainLevel_Round41_MicroFixRegressionLocks_All7P1AndPolish()
        {
            // [P1-1] RoomBanner_* must use continuous world-Y pin on Mat_RoomBanner_Sway (zero seam tear across _Top/_Mid/_Bot)
            var roomBanners = UnityEngine.Object.FindObjectsByType<SpriteRenderer>()
                .Where(sr => sr.gameObject.name.StartsWith("RoomBanner_", StringComparison.Ordinal))
                .ToArray();
            Assert.AreEqual(9, roomBanners.Length, "Expected 9 sprite pieces across RoomBanner_23 / 61 / 84.");
            foreach (var sr in roomBanners)
            {
                Assert.IsNotNull(sr.sharedMaterial, $"RoomBanner piece '{sr.name}' missing material.");
                Assert.AreEqual("DeadCells/VertexSway2D", sr.sharedMaterial.shader.name);
                Assert.GreaterOrEqual(sr.sharedMaterial.GetFloat("_UseWorldYPin"), 0.5f,
                    $"RoomBanner piece '{sr.name}' must enable _UseWorldYPin for seamless multi-piece sway.");
                Assert.GreaterOrEqual(sr.sharedMaterial.GetFloat("_SwaySpanY"), 3.5f,
                    $"RoomBanner piece '{sr.name}' _SwaySpanY must cover full vertical banner height.");
            }

            // [P1-2] ParallaxFar 768x512 @ localScale == (1,1,1) square pixels, and 0 alccoveParalaxeB_00 in ParallaxMid
            var parallaxRoot = GameObject.Find("Prison_Parallax_R4");
            Assert.IsNotNull(parallaxRoot);
            var farRenderers = parallaxRoot.transform.Find("ParallaxFar_Backdrop").GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(farRenderers.Length, 3);
            foreach (var sr in farRenderers)
            {
                Assert.AreEqual(1f, sr.transform.localScale.x, 1e-4f, $"ParallaxFar '{sr.name}' scale.x must be 1.");
                Assert.AreEqual(1f, sr.transform.localScale.y, 1e-4f, $"ParallaxFar '{sr.name}' scale.y must be 1 (no vertical stretch).");
                Assert.AreEqual(768f, sr.sprite.rect.width, 1e-4f);
                Assert.AreEqual(512f, sr.sprite.rect.height, 1e-4f);
            }
            var midRenderers = parallaxRoot.transform.Find("ParallaxMid_Alcoves").GetComponentsInChildren<SpriteRenderer>(true);
            Assert.IsFalse(midRenderers.Any(sr => sr.sprite != null && sr.sprite.name.StartsWith("alccoveParalaxeB_00", StringComparison.Ordinal)),
                "ParallaxMid_Alcoves must contain 0 alccoveParalaxeB_00 renderers.");

            // [P1-3] Every LanternFlameCore_R4 must have a sibling LanternFixture_R4, and clear One Way Platform 21 & Exit Door banner
            var flameCores = UnityEngine.Object.FindObjectsByType<SpriteRenderer>()
                .Where(sr => sr.gameObject.name == "LanternFlameCore_R4")
                .ToArray();
            Assert.GreaterOrEqual(flameCores.Length, 8, "Expected >= 8 warm lanterns (7 corridor + Spawn lantern) with LanternFlameCore_R4.");
            foreach (var core in flameCores)
            {
                Transform parentLight = core.transform.parent;
                Assert.IsNotNull(parentLight, "LanternFlameCore_R4 must be parented to a Light2D.");
                Transform fixture = parentLight.Find("LanternFixture_R4");
                Assert.IsNotNull(fixture, $"Light '{parentLight.name}' at {parentLight.position} is missing LanternFixture_R4 prop.");
                var fixtureSr = fixture.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(fixtureSr, $"LanternFixture_R4 on '{parentLight.name}' missing SpriteRenderer.");
                Assert.IsNotNull(fixtureSr.sprite, $"LanternFixture_R4 on '{parentLight.name}' missing Sprite.");

                Vector3 p = parentLight.position;
                bool overlapsPlat21 = p.x >= 21.0f && p.x <= 28.0f && p.y >= 2.2f && p.y <= 3.4f;
                Assert.IsFalse(overlapsPlat21, $"Lantern '{parentLight.name}' at {p} overlaps One Way Platform 21.");
            }
            var exitLantern = GameObject.Find("Exit hall lantern");
            Assert.IsNotNull(exitLantern, "Exit hall lantern missing.");
            Assert.LessOrEqual(exitLantern.transform.position.x, 103.5f,
                $"Exit hall lantern at x={exitLantern.transform.position.x:F2} must stay clear of Exit Door grey banner (x=105).");

            // [P1-4] BannerBig_92 must sit on solid East wing background strictly outside all CarvedArchBays
            var bays = new[]
            {
                (xMin: 16, xMax: 22, yMin: 4, yMax: 12),
                (xMin: 31, xMax: 35, yMin: 4, yMax: 12),
                (xMin: 52, xMax: 60, yMin: 3, yMax: 13),
                (xMin: 69, xMax: 76, yMin: 5, yMax: 13),
                (xMin: 86, xMax: 93, yMin: 4, yMax: 13),
            };
            var bannerBig92 = GameObject.Find("BannerBig_92")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(bannerBig92, "BannerBig_92 missing.");
            Assert.GreaterOrEqual(bannerBig92.bounds.min.y, 2.2f);
            foreach (var bay in bays)
            {
                bool overlapsX = bannerBig92.bounds.max.x > bay.xMin && bannerBig92.bounds.min.x < bay.xMax;
                bool overlapsY = bannerBig92.bounds.max.y > bay.yMin && bannerBig92.bounds.min.y < bay.yMax;
                Assert.IsFalse(overlapsX && overlapsY,
                    $"BannerBig_92 bounds ({bannerBig92.bounds.min}..{bannerBig92.bounds.max}) overlaps CarvedArchBay [{bay.xMin}..{bay.xMax}, {bay.yMin}..{bay.yMax}].");
            }
            var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>();
            var eastBg = tilemaps.Single(t => t.gameObject.name == "East wing background");
            var eastStone = tilemaps.Single(t => t.gameObject.name == "East wing stone");
            var westStone = tilemaps.Single(t => t.gameObject.name == "Stone visuals");
            Assert.IsNotNull(eastBg.GetTile(new Vector3Int(Mathf.FloorToInt(bannerBig92.transform.position.x), Mathf.FloorToInt(bannerBig92.bounds.max.y - 0.2f), 0)),
                "BannerBig_92 top mount must rest on a solid East wing background tile.");

            // [P1-5] All 4 GlowingMushrooms use peak-luminance glowingMushroom_3_00 + Mat_Emissive_Mushroom (linear peak > 1.30 > Bloom 1.05)
            for (int i = 0; i < 4; i++)
            {
                var mGo = GameObject.Find($"GlowingMushroom_{i}");
                Assert.IsNotNull(mGo, $"GlowingMushroom_{i} missing.");
                var sr = mGo.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(sr);
                StringAssert.StartsWith("glowingMushroom_3_00", sr.sprite?.name,
                    $"GlowingMushroom_{i} must use peak-luminance glowingMushroom_3_00 for real GPU Bloom.");
                Color em = sr.sharedMaterial.GetColor("_EmissionColor");
                float maxEm = Mathf.Max(em.r, Mathf.Max(em.g, em.b));
                Assert.GreaterOrEqual(maxEm * 0.84f, 1.30f, $"GlowingMushroom_{i} linear peak output must exceed 1.30 (> Bloom 1.05).");
            }

            // [P1-6] Foreground_Silhouettes min.y in [8.5, 10.2] and left/right stone borders sealed to x <= -6 and x >= 118
            var fgRenderers = parallaxRoot.transform.Find("Foreground_Silhouettes").GetComponentsInChildren<SpriteRenderer>(true);
            Assert.GreaterOrEqual(fgRenderers.Length, 14);
            foreach (var sr in fgRenderers)
            {
                Assert.That(sr.bounds.min.y, Is.InRange(8.5f, 10.2f),
                    $"Foreground silhouette '{sr.name}' min.y ({sr.bounds.min.y:F2}) must lie in [8.5, 10.2] to graze ceiling edge without blocking combat.");
            }
            Assert.IsNotNull(eastStone.GetTile(new Vector3Int(118, 4, 0)), "East wing stone right border must be sealed through x >= 118.");
            Assert.IsNotNull(westStone.GetTile(new Vector3Int(-6, 4, 0)), "Stone visuals left border must be sealed through x <= -6.");

            // [P1-7] Spawn lantern exists near Player Spawn + ColorAdjustments postExposure >= 0.20
            var spawnLantern = GameObject.Find("Spawn lantern")?.GetComponent<Light2D>();
            Assert.IsNotNull(spawnLantern, "Spawn lantern missing near Player Spawn.");
            Assert.That(spawnLantern.transform.position.x, Is.InRange(4.5f, 7.5f));
            Assert.That(spawnLantern.transform.position.y, Is.InRange(1.8f, 3.6f));
            var vol = UnityEngine.Object.FindObjectsByType<Volume>().First(v => v.enabled && v.isGlobal);
            Assert.IsTrue(vol.sharedProfile.TryGet<ColorAdjustments>(out var colorAdj) &&
                          colorAdj.postExposure.overrideState && colorAdj.postExposure.value >= 0.20f,
                "DeadCells_PostProcessProfile ColorAdjustments.postExposure must be overridden >= 0.20f.");

            // [P2-2] CarvedArchBays bottom sills present
            var dressingRoot = GameObject.Find("Prison_Dressing_R3");
            int sillCount = dressingRoot.GetComponentsInChildren<Transform>(true)
                .Count(t => t.name.StartsWith("ArchSill_", StringComparison.Ordinal));
            Assert.GreaterOrEqual(sillCount, 25, $"Expected >= 25 ArchSill_* sprites along the 5 CarvedArchBays, found {sillCount}.");
        }

        [Test]
        public void MainLevel_Round42_VisualP1Closure_ZeroMixelLanternsAdditiveMushroomsOccludedSillsAndCleanExitGlow()
        {
            var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

            // [P1-R1] 8/8 LanternFixture_R4 use 24-PPU prisonWallLantern_00 at scale (1,1,1), localY == 0.125, zero LightAndBanner_005
            var lanternFixtures = allRenderers
                .Where(sr => sr.gameObject.name == "LanternFixture_R4")
                .ToArray();
            Assert.GreaterOrEqual(lanternFixtures.Length, 8, $"Expected >= 8 LanternFixture_R4 props, found {lanternFixtures.Length}.");
            foreach (var sr in lanternFixtures)
            {
                Assert.IsNotNull(sr.sprite, $"LanternFixture_R4 on '{sr.transform.parent?.name}' missing Sprite.");
                Assert.AreEqual("prisonWallLantern_00", sr.sprite.name,
                    $"LanternFixture_R4 on '{sr.transform.parent?.name}' must use 24-PPU prisonWallLantern_00.");
                Assert.AreEqual(24f, sr.sprite.pixelsPerUnit, 1e-4f,
                    $"LanternFixture_R4 sprite '{sr.sprite.name}' must use 24 PPU.");
                Assert.AreEqual(1f, sr.transform.localScale.x, 1e-4f);
                Assert.AreEqual(1f, sr.transform.localScale.y, 1e-4f);
                Assert.AreEqual(1f, sr.transform.localScale.z, 1e-4f);
                Assert.LessOrEqual(Mathf.Abs(sr.transform.localPosition.y - 0.125f), 1e-3f,
                    $"LanternFixture_R4 on '{sr.transform.parent?.name}' localPosition.y must equal 0.125f.");
            }
            Assert.IsFalse(allRenderers.Any(sr => sr.sprite != null && sr.sprite.name.IndexOf("LightAndBanner_005", StringComparison.Ordinal) >= 0),
                "Scene must contain zero SpriteRenderers referencing mixed-PPU LightAndBanner_005.");

            // [P1-R2] 4/4 GlowingMushrooms at 1:1 24-PPU, tiny additive cap-aligned MushroomHalo above floorTop, isolated local Point MushroomLight
            float[] mushroomFloorTops = { -1f, 0f, -1f, -1f };
            int parallaxFarId = SortingLayer.NameToID("ParallaxFar");
            int parallaxMidId = SortingLayer.NameToID("ParallaxMid");
            int foregroundId = SortingLayer.NameToID("Foreground");

            for (int i = 0; i < 4; i++)
            {
                var mGo = GameObject.Find($"GlowingMushroom_{i}");
                Assert.IsNotNull(mGo, $"GlowingMushroom_{i} missing.");
                Assert.AreEqual(1f, mGo.transform.localScale.x, 1e-4f, $"GlowingMushroom_{i} must use 1:1 scale.x.");
                Assert.AreEqual(1f, mGo.transform.localScale.y, 1e-4f, $"GlowingMushroom_{i} must use 1:1 scale.y.");
                Assert.AreEqual(1f, mGo.transform.localScale.z, 1e-4f, $"GlowingMushroom_{i} must use 1:1 scale.z.");

                var haloGo = GameObject.Find($"MushroomHalo_{i}");
                Assert.IsNotNull(haloGo, $"MushroomHalo_{i} missing.");
                var haloSr = haloGo.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(haloSr, $"MushroomHalo_{i} missing SpriteRenderer.");
                Assert.IsNotNull(haloSr.sharedMaterial, $"MushroomHalo_{i} missing sharedMaterial.");
                Assert.AreEqual("Mat_Emissive_MushroomHalo", haloSr.sharedMaterial.name,
                    $"MushroomHalo_{i} must use additive Mat_Emissive_MushroomHalo.");
                Assert.AreEqual(1f, haloSr.sharedMaterial.GetFloat("_DstBlend"), 1e-4f,
                    $"MushroomHalo_{i} material must use additive _DstBlend == 1.");
                Assert.LessOrEqual(haloGo.transform.localScale.x, 0.20f, $"MushroomHalo_{i} localScale.x too large.");
                Assert.LessOrEqual(haloGo.transform.localScale.y, 0.18f, $"MushroomHalo_{i} localScale.y too large.");
                Assert.Greater(haloSr.bounds.min.y, mushroomFloorTops[i],
                    $"MushroomHalo_{i} bounds.min.y ({haloSr.bounds.min.y:F3}) must stay strictly above floorTop ({mushroomFloorTops[i]:F1}).");

                var lightGo = GameObject.Find($"MushroomLight_{i}");
                Assert.IsNotNull(lightGo, $"MushroomLight_{i} missing.");
                var light = lightGo.GetComponent<Light2D>();
                Assert.IsNotNull(light, $"MushroomLight_{i} missing Light2D.");
                Assert.AreEqual(Light2D.LightType.Point, light.lightType);
                Assert.IsTrue(light.enabled, $"MushroomLight_{i} must be enabled.");
                Assert.That(light.intensity, Is.InRange(0.4f, 1.0f));
                Assert.That(light.pointLightOuterRadius, Is.InRange(1.5f, 2.8f));
                Assert.IsNull(lightGo.transform.Find("LanternFixture_R4"),
                    $"MushroomLight_{i} must not receive LanternFixture_R4 child.");
                Assert.IsNull(lightGo.GetComponent<TorchFlicker2D>(),
                    $"MushroomLight_{i} must not receive TorchFlicker2D.");

                var soLight = new SerializedObject(light);
                var layersProp = soLight.FindProperty("m_ApplyToSortingLayers");
                Assert.IsNotNull(layersProp);
                Assert.Greater(layersProp.arraySize, 0);
                var targetLayerIds = new System.Collections.Generic.HashSet<int>();
                for (int j = 0; j < layersProp.arraySize; j++)
                    targetLayerIds.Add(layersProp.GetArrayElementAtIndex(j).intValue);
                Assert.IsFalse(targetLayerIds.Contains(parallaxFarId), $"MushroomLight_{i} must exclude ParallaxFar.");
                Assert.IsFalse(targetLayerIds.Contains(parallaxMidId), $"MushroomLight_{i} must exclude ParallaxMid.");
                Assert.IsFalse(targetLayerIds.Contains(foregroundId), $"MushroomLight_{i} must exclude Foreground.");
            }

            // [P1-R3] 32/32 ArchSill_* use 24-PPU prisonArchSill_00 at scale (1,1,1) on Background/-17 strictly behind Stone pillars
            var archSills = allRenderers
                .Where(sr => sr.gameObject.name.StartsWith("ArchSill_", StringComparison.Ordinal))
                .ToArray();
            Assert.AreEqual(32, archSills.Length, $"Expected exactly 32 ArchSill_* renderers, found {archSills.Length}.");

            var stonePillars = allRenderers
                .Where(sr => sr.sprite != null && sr.sprite.name == "alcovePillar_00")
                .ToArray();
            Assert.GreaterOrEqual(stonePillars.Length, 1, "Expected >= 1 Stone pillar (alcovePillar_00) renderer in Main_Level.");

            foreach (var sillSr in archSills)
            {
                Assert.IsNotNull(sillSr.sprite);
                Assert.AreEqual("prisonArchSill_00", sillSr.sprite.name);
                Assert.AreEqual(24f, sillSr.sprite.pixelsPerUnit, 1e-4f);
                Assert.AreEqual(1f, sillSr.transform.localScale.x, 1e-4f);
                Assert.AreEqual(1f, sillSr.transform.localScale.y, 1e-4f);
                Assert.AreEqual(1f, sillSr.transform.localScale.z, 1e-4f);
                Assert.AreEqual("Background", sillSr.sortingLayerName);
                Assert.AreEqual(-17, sillSr.sortingOrder);
                foreach (var pillarSr in stonePillars)
                {
                    Assert.Less(sillSr.sortingOrder, pillarSr.sortingOrder,
                        $"ArchSill '{sillSr.name}' ({sillSr.sortingOrder}) must sort strictly behind Stone pillar '{pillarSr.name}' ({pillarSr.sortingOrder}).");
                }
            }

            // [P2-1] ExitDoor_BackGlow clears floor top crevices (bounds.min.y >= 0.25)
            var exitBackGlowSr = GameObject.Find("ExitDoor_BackGlow")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(exitBackGlowSr, "ExitDoor_BackGlow missing.");
            Assert.GreaterOrEqual(exitBackGlowSr.bounds.min.y, 0.25f,
                $"ExitDoor_BackGlow bounds.min.y ({exitBackGlowSr.bounds.min.y:F3}) must be >= 0.25f to prevent floor seam yellow leak.");

            // [P2-3] BannerBig_92 clears Exit hall pillar completely and stays outside CarvedArchBays[4] [86..93]
            var bannerBig92Sr = GameObject.Find("BannerBig_92")?.GetComponent<SpriteRenderer>();
            var exitHallPillarSr = GameObject.Find("Exit hall pillar")?.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(bannerBig92Sr, "BannerBig_92 missing.");
            Assert.IsNotNull(exitHallPillarSr, "Exit hall pillar missing.");
            Assert.Greater(bannerBig92Sr.bounds.min.y, exitHallPillarSr.bounds.max.y,
                $"BannerBig_92 bounds.min.y ({bannerBig92Sr.bounds.min.y:F3}) must sit strictly above Exit hall pillar bounds.max.y ({exitHallPillarSr.bounds.max.y:F3}).");
            Assert.GreaterOrEqual(bannerBig92Sr.bounds.min.x, 93.0f,
                $"BannerBig_92 bounds.min.x ({bannerBig92Sr.bounds.min.x:F3}) must remain strictly right of CarvedArchBays[4] (x=93).");
        }

        [Test]
        public void MainLevel_Round5_PlayModeFixes_OneWaySurfaceArcScrollAuraDisableLanternSortingArchJambsAndBrightness()
        {
            // [PM-1] All 9 PlatformEffector2D components use surfaceArc == 160f so side contacts never block walk/roll
            var effectors = UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsInactive.Include);
            Assert.AreEqual(9, effectors.Length, "Expected 9 PlatformEffector2D components in Main_Level.");
            foreach (var eff in effectors)
            {
                Assert.IsTrue(eff.useOneWay, $"PlatformEffector2D on '{eff.name}' must enable useOneWay.");
                Assert.AreEqual(160f, eff.surfaceArc, 1e-3f,
                    $"PlatformEffector2D on '{eff.name}' must use surfaceArc=160f (not 180f) so side edges do not block Player walk/roll.");
            }

            // [PM-2] Every ScrollAura_R4_{i} is parented under PowerScrollPickup._icon so Reveal(true) disables it
            var scrolls = UnityEngine.Object.FindObjectsByType<PowerScrollPickup>(FindObjectsInactive.Include);
            Assert.GreaterOrEqual(scrolls.Length, 2);
            foreach (var scroll in scrolls)
            {
                var soScroll = new SerializedObject(scroll);
                var iconTr = soScroll.FindProperty("_icon").objectReferenceValue as Transform;
                Assert.IsNotNull(iconTr, $"PowerScrollPickup '{scroll.name}' missing _icon.");
                var aura = iconTr.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(sr => sr.gameObject.name.StartsWith("ScrollAura_R4_", StringComparison.Ordinal));
                Assert.IsNotNull(aura, $"PowerScrollPickup '{scroll.name}' must parent ScrollAura_R4_* under _icon so collecting hides the glow.");
            }

            // [PM-3] LanternFlameCore_R4 (Background/-8) and TorchEmbers (Background/-7) sort strictly on Background behind Actors
            var allRenderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
            var flameCores = allRenderers.Where(sr => sr.gameObject.name == "LanternFlameCore_R4").ToArray();
            Assert.AreEqual(8, flameCores.Length);
            foreach (var core in flameCores)
            {
                Assert.AreEqual("Background", core.sortingLayerName, "LanternFlameCore_R4 must sort on Background (behind Actors), not VFX.");
                Assert.AreEqual(-8, core.sortingOrder, "LanternFlameCore_R4 must sort at Background/-8 in front of LanternFixture_R4 (-9).");
                var embersPsr = core.transform.parent.Find("TorchEmbers")?.GetComponent<ParticleSystemRenderer>();
                Assert.IsNotNull(embersPsr);
                Assert.AreEqual("Background", embersPsr.sortingLayerName, "TorchEmbers must sort on Background (behind Actors), not VFX.");
                Assert.AreEqual(-7, embersPsr.sortingOrder);
            }

            // [PM-4] Foreground_Silhouettes locks vertical offset to camera top (referenceCameraY = 3.8) with wrapWidthUnits = 32
            var fgLayer = GameObject.Find("Prison_Parallax_R4/Foreground_Silhouettes")?.GetComponent<ParallaxLayer2D>();
            Assert.IsNotNull(fgLayer);
            var soFg = new SerializedObject(fgLayer);
            Assert.IsTrue(soFg.FindProperty("_lockYToCameraStartOffset").boolValue,
                "Foreground_Silhouettes must lock Y to camera start offset so jumping never reveals floating top edges.");
            Assert.AreEqual(32f, soFg.FindProperty("_wrapWidthUnits").floatValue, 1e-3f);

            // [PM-5] All 5 CarvedArchBays have vertical 24-PPU ArchJamb_L/R_* and top ArchLintel_* on Background/-17
            var archJambs = allRenderers.Where(sr => sr.gameObject.name.StartsWith("ArchJamb_", StringComparison.Ordinal)).ToArray();
            Assert.GreaterOrEqual(archJambs.Length, 70, $"Expected >= 70 ArchJamb_* renderers framing vertical bay cuts, found {archJambs.Length}.");
            foreach (var jamb in archJambs)
            {
                Assert.AreEqual("prisonArchJamb_00", jamb.sprite.name);
                Assert.AreEqual(24f, jamb.sprite.pixelsPerUnit, 1e-4f);
                Assert.AreEqual("Background", jamb.sortingLayerName);
                Assert.AreEqual(-17, jamb.sortingOrder);
            }

            // [PM-6] Brightened postExposure >= 0.55f, Vignette <= 0.16f, Global Light == 0.62f, and >= 3 GalleryFillLight_*
            var globalLight = UnityEngine.Object.FindObjectsByType<Light2D>().Single(l => l.lightType == Light2D.LightType.Global);
            Assert.AreEqual(0.62f, globalLight.intensity, 1e-3f);
            var vol = UnityEngine.Object.FindObjectsByType<Volume>().First(v => v.enabled && v.isGlobal);
            Assert.IsTrue(vol.sharedProfile.TryGet<ColorAdjustments>(out var ca) && ca.postExposure.value >= 0.55f);
            Assert.IsTrue(vol.sharedProfile.TryGet<Vignette>(out var vig) && vig.intensity.value <= 0.16f);
            int fillLights = UnityEngine.Object.FindObjectsByType<Light2D>()
                .Count(l => l.gameObject.name.StartsWith("GalleryFillLight_", StringComparison.Ordinal));
            Assert.AreEqual(3, fillLights);
        }
    }
}
