using System;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.HUD;
using DeadCells.Player;
using DeadCells.RunFlow;
using DeadCells.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeadCells.Core.Tests
{
    public sealed class RunFlowAndHudTests
    {
        private GameObject _root;
        private float _savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            _savedTimeScale = Time.timeScale;
            _root = new GameObject("__RunFlowHudTestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _savedTimeScale;
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void DefeatScreen_WhenTimeScaleIsZero_RevealsPanelAfterUnscaledDelay()
        {
            var healthGo = new GameObject("PlayerHealth");
            healthGo.transform.SetParent(_root.transform);
            var health = healthGo.AddComponent<Health>();
            health.RestoreFull(); // Awake() does not run in EditMode; initialize Current = Max (100)

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            var panel = new GameObject("DefeatPanel");
            panel.transform.SetParent(_root.transform);
            panel.SetActive(false);

            var retryGo = new GameObject("RetryButton");
            retryGo.transform.SetParent(panel.transform);
            var retryButton = retryGo.AddComponent<Button>();

            var defeatGo = new GameObject("DefeatScreen");
            defeatGo.transform.SetParent(_root.transform);
            var defeatScreen = defeatGo.AddComponent<DefeatScreen>();
            SetPrivateField(defeatScreen, "_run", run);
            SetPrivateField(defeatScreen, "_panel", panel);
            SetPrivateField(defeatScreen, "_retry", retryButton);
            SetPrivateField(defeatScreen, "_revealDelay", 0.4f);
            InvokePrivateMethod(defeatScreen, "OnEnable");

            // Simulate fatal hit occurring while HitStop holds Time.timeScale == 0f
            Time.timeScale = 0f;
            health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root));
            Assert.IsTrue(run.IsDefeated, "Fatal damage must mark RunSession as defeated.");
            Assert.IsFalse(panel.activeSelf, "Panel must wait for reveal delay before becoming active.");

            defeatScreen.TickUnscaled(0.25f);
            Assert.IsFalse(panel.activeSelf, "Panel must still be hidden at 0.25s < 0.40s unscaled delay.");

            defeatScreen.TickUnscaled(0.20f);
            Assert.IsTrue(panel.activeSelf, "Panel must reveal once unscaled elapsed time reaches 0.40s even when Time.timeScale == 0.");

            InvokePrivateMethod(defeatScreen, "OnDisable");
            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void RunSession_CompleteRun_LocksOutcomeAndPreventsSubsequentDefeat()
        {
            var health = _root.AddComponent<Health>();
            health.RestoreFull(); // Awake() does not run in EditMode; initialize Current = Max (100)

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            int completedCount = 0;
            int defeatedCount = 0;
            run.Completed += () => completedCount++;
            run.Defeated += () => defeatedCount++;

            Assert.IsTrue(run.CompleteRun(), "First CompleteRun() on live session must succeed.");
            Assert.IsTrue(run.IsCompleted);
            Assert.IsFalse(run.IsDefeated);
            Assert.AreEqual(1, completedCount);

            // Duplicate CompleteRun or late damage after victory must be ignored
            Assert.IsFalse(run.CompleteRun(), "Second CompleteRun() must return false.");
            health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root));
            Assert.IsFalse(run.IsDefeated, "Player dying after CompleteRun() must not flip session to Defeated.");
            Assert.AreEqual(0, defeatedCount);
            Assert.AreEqual(1, completedCount);

            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void HealthBarView_WhenDamaged_SnapsPrimaryFillAndTrailsSecondaryFillSmoothly()
        {
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(_root.transform);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMax = Vector2.one;

            var trailGo = new GameObject("TrailFill", typeof(RectTransform));
            trailGo.transform.SetParent(_root.transform);
            var trailRect = trailGo.GetComponent<RectTransform>();
            trailRect.anchorMax = Vector2.one;

            var textGo = new GameObject("ValueText", typeof(RectTransform));
            textGo.transform.SetParent(_root.transform);
            var valueText = textGo.AddComponent<Text>();

            var view = _root.AddComponent<HealthBarView>();
            SetPrivateField(view, "_fill", fillRect);
            SetPrivateField(view, "_trailFill", trailRect);
            SetPrivateField(view, "_value", valueText);
            SetPrivateField(view, "_trailHoldDuration", 0.15f);
            SetPrivateField(view, "_trailShrinkSpeed", 2.0f);

            view.Render(100, 100);
            Assert.AreEqual(1.0f, fillRect.anchorMax.x, 0.001f);
            Assert.AreEqual(1.0f, trailRect.anchorMax.x, 0.001f);

            // Take damage from 100 -> 40
            view.Render(40, 100);
            Assert.AreEqual(0.40f, fillRect.anchorMax.x, 0.001f, "Primary fill must snap immediately to 0.40.");
            Assert.AreEqual(1.00f, trailRect.anchorMax.x, 0.001f, "Damage trail fill must hold at 1.00 initially.");
            Assert.AreEqual("40 / 100", valueText.text);

            // During hold window (0.10s < 0.15s), trail remains at 1.00
            view.TickTrail(0.10f);
            Assert.AreEqual(1.00f, trailRect.anchorMax.x, 0.001f);

            // Past hold window (+0.10s => 0.05s of shrink at 2.0/s => 1.00 - 0.10 = 0.90)
            view.TickTrail(0.10f);
            Assert.AreEqual(0.90f, trailRect.anchorMax.x, 0.001f);

            // Healing from 40 -> 80 while trail is at 0.90 snaps primary to 0.80; healing above trail snaps both
            view.Render(95, 100);
            Assert.AreEqual(0.95f, fillRect.anchorMax.x, 0.001f);
            Assert.AreEqual(0.95f, trailRect.anchorMax.x, 0.001f, "Healing above current trail must pull trail up to match current fill.");
        }

        [Test]
        public void DefeatScreen_WhenRunCompleted_RevealsVictoryPanelAndBlocksSubsequentPlayerDamage()
        {
            var healthGo = new GameObject("PlayerHealth");
            healthGo.transform.SetParent(_root.transform);
            var health = healthGo.AddComponent<Health>();
            health.RestoreFull();

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            var panel = new GameObject("EndPanel");
            panel.transform.SetParent(_root.transform);
            panel.SetActive(false);

            var heading = new GameObject("Heading", typeof(RectTransform)).AddComponent<Text>();
            heading.transform.SetParent(panel.transform);
            var description = new GameObject("Description", typeof(RectTransform)).AddComponent<Text>();
            description.transform.SetParent(panel.transform);
            var retryButton = new GameObject("Retry", typeof(RectTransform)).AddComponent<Button>();
            retryButton.transform.SetParent(panel.transform);

            var screen = new GameObject("DefeatScreen").AddComponent<DefeatScreen>();
            screen.transform.SetParent(_root.transform);
            SetPrivateField(screen, "_run", run);
            SetPrivateField(screen, "_panel", panel);
            SetPrivateField(screen, "_retry", retryButton);
            TrySetPrivateField(screen, "_heading", heading);
            TrySetPrivateField(screen, "_description", description);
            SetPrivateField(screen, "_revealDelay", 0.4f);
            InvokePrivateMethod(screen, "OnEnable");

            Assert.IsTrue(run.CompleteRun());
            Assert.IsTrue(panel.activeSelf, "Victory must immediately reveal the end-of-run panel with Retry available.");
            Assert.AreEqual("Hoàn thành màn!", heading.text);
            Assert.AreEqual("Bạn đã vượt qua khu giam giữ. Chọn Chơi lại để bắt đầu lượt mới.", description.text);

            // Late damage after Victory must be blocked by RunSession's DamageFilter
            Assert.IsFalse(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root)),
                "Player must be damage-immune once RunSession completes.");
            Assert.IsTrue(health.IsAlive);

            InvokePrivateMethod(screen, "OnDisable");
            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void RunSession_CompleteRun_BlocksDamageAndLocksPlayerControlAndBodyPhysics()
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(_root.transform);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.simulated = true;
            rb.linearVelocity = new Vector2(6f, -3f);
            rb.angularVelocity = 12f;

            var health = playerGo.AddComponent<Health>();
            health.RestoreFull(); // Awake() does not run in EditMode; initialize Current = Max (100)

            var inputReader = playerGo.AddComponent<PlayerInputReader>();
            var controller = playerGo.AddComponent<PlayerController>();
            Assert.IsTrue(inputReader.enabled);
            Assert.IsTrue(controller.enabled);

            var runGo = new GameObject("RunSession");
            runGo.transform.SetParent(_root.transform);
            var run = runGo.AddComponent<RunSession>();
            SetPrivateField(run, "_playerHealth", health);
            InvokePrivateMethod(run, "OnEnable");

            Assert.IsTrue(run.CompleteRun(), "CompleteRun() must succeed on a live session.");
            Assert.IsTrue(run.IsCompleted);
            Assert.IsFalse(health.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, _root)),
                "Player must be damage-immune once RunSession completes.");
            Assert.IsTrue(health.IsAlive);
            Assert.IsFalse(inputReader.enabled, "PlayerInputReader must be disabled once RunSession completes.");
            Assert.IsFalse(controller.enabled, "PlayerController must be disabled once RunSession completes.");
            Assert.IsFalse(rb.simulated, "Player Rigidbody2D simulation must be disabled once RunSession completes.");
            Assert.AreEqual(Vector2.zero, rb.linearVelocity, "Player linearVelocity must be zeroed once RunSession completes.");
            Assert.AreEqual(0f, rb.angularVelocity, 0.0001f, "Player angularVelocity must be zeroed once RunSession completes.");

            InvokePrivateMethod(run, "OnDisable");
        }

        [Test]
        public void PlayerHudPrefab_WiresHealthBarTrailFillImageBehindPrimaryFill()
        {
            var hudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Features/HUD/Prefabs/PlayerHUD.prefab");
            Assert.IsNotNull(hudPrefab, "PlayerHUD.prefab must exist.");

            var view = hudPrefab.GetComponentInChildren<HealthBarView>(true);
            Assert.IsNotNull(view, "PlayerHUD.prefab must contain HealthBarView.");

            var fillField = typeof(HealthBarView).GetField("_fill", BindingFlags.Instance | BindingFlags.NonPublic);
            var trailField = typeof(HealthBarView).GetField("_trailFill", BindingFlags.Instance | BindingFlags.NonPublic);
            var fillRect = fillField.GetValue(view) as RectTransform;
            var trailRect = trailField.GetValue(view) as RectTransform;

            Assert.IsNotNull(fillRect, "HealthBarView._fill must be assigned.");
            Assert.IsNotNull(trailRect, "PlayerHUD.prefab must wire HealthBarView._trailFill to a live TrailFill RectTransform.");
            Assert.AreNotEqual(fillRect, trailRect, "_trailFill must be a distinct RectTransform from _fill.");
            Assert.AreEqual(fillRect.parent, trailRect.parent, "TrailFill must be a sibling of Fill under Track.");
            Assert.Less(trailRect.GetSiblingIndex(), fillRect.GetSiblingIndex(),
                "TrailFill must render BEHIND Fill (lower sibling index).");
            Assert.IsNotNull(trailRect.GetComponent<Image>(), "TrailFill must have a UI Image component.");
        }

        [Test]
        public void EnemyImpactFeedback_DoesNotDuplicateHealthBarOrDamageNumber_WhileKeepingAudioAndVfx()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.red, Color.red, Color.red, Color.red });
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 32f);
            var bloodDef = VfxDefinition.Create("TestBlood", new[] { sprite }, 30f, 1f, 1, 1);
            var flashDef = VfxDefinition.Create("TestFlash", new[] { sprite }, 30f, 1f, 1, 1);
            var hitClip = AudioClip.Create("TestHit", 441, 1, 44100, false);

            try
            {
                var enemyGo = new GameObject("Enemy");
                enemyGo.transform.SetParent(_root.transform);
                var body = enemyGo.AddComponent<BoxCollider2D>();
                body.offset = new Vector2(0f, 0.75f);
                body.size = new Vector2(1f, 1.5f);
                var health = enemyGo.AddComponent<Health>();
                health.Initialize(100, 0f);

                var enemyBar = enemyGo.AddComponent<EnemyHealthBar>();
                enemyBar.Setup(health, body);

                var barRenderers = enemyGo.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(barRenderers, "EnemyHealthBar.Setup must create bar SpriteRenderers.");
                foreach (var sr in barRenderers)
                {
                    Assert.AreEqual("VFX", sr.sortingLayerName,
                        $"EnemyHealthBar '{sr.name}' must render on VFX sorting layer.");
                }

                DamagePopup.Prewarm();
                DamagePopup.Spawn(Vector3.zero, 15, 1, false);
                var popups = Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include);
                Assert.IsNotEmpty(popups, "DamagePopup must exist after Prewarm/Spawn.");
                foreach (var popup in popups)
                {
                    Assert.AreEqual("VFX", popup.GetComponent<MeshRenderer>().sortingLayerName,
                        "DamagePopup MeshRenderer must render on VFX sorting layer.");
                }

                var vfxGo = new GameObject("VfxSpawner");
                vfxGo.transform.SetParent(_root.transform);
                var vfx = vfxGo.AddComponent<VfxSpawner>();
                SetPrivateField(vfx, "_prewarm", Array.Empty<VfxDefinition>());
                InvokePrivateMethod(vfx, "Awake");

                var numberPrefabGo = new GameObject("NumberTemplate");
                numberPrefabGo.transform.SetParent(_root.transform);
                numberPrefabGo.SetActive(false);
                var labelText = new GameObject("Label").AddComponent<TextMesh>();
                labelText.transform.SetParent(numberPrefabGo.transform);
                var shadowText = new GameObject("Shadow").AddComponent<TextMesh>();
                shadowText.transform.SetParent(numberPrefabGo.transform);
                var numberPrefab = numberPrefabGo.AddComponent<FloatingDamageNumber>();
                SetPrivateField(numberPrefab, "_label", labelText);
                SetPrivateField(numberPrefab, "_shadow", shadowText);

                var barGo = new GameObject("LegacyBar");
                barGo.transform.SetParent(_root.transform);
                barGo.SetActive(true);
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(barGo.transform);

                var fbGo = new GameObject("EnemyImpactFeedback");
                fbGo.transform.SetParent(_root.transform);
                var audio = fbGo.AddComponent<AudioSource>();
                var feedback = fbGo.AddComponent<EnemyImpactFeedback>();
                SetPrivateField(feedback, "_target", health);
                SetPrivateField(feedback, "_body", body);
                SetPrivateField(feedback, "_vfx", vfx);
                SetPrivateField(feedback, "_impactBlood", bloodDef);
                SetPrivateField(feedback, "_defeatBlood", bloodDef);
                SetPrivateField(feedback, "_impactFlash", flashDef);
                SetPrivateField(feedback, "_audio", audio);
                SetPrivateField(feedback, "_hitSound", hitClip);
                SetPrivateField(feedback, "_fatalSound", hitClip);
                SetPrivateField(feedback, "_numberPrefab", numberPrefab);
                SetPrivateField(feedback, "_bar", barGo);
                SetPrivateField(feedback, "_fill", fillGo.transform);

                InvokePrivateMethod(feedback, "Awake");
                Assert.IsFalse(barGo.activeSelf, "EnemyImpactFeedback.Awake must hide legacy _bar.");
                InvokePrivateMethod(feedback, "OnEnable");

                Assert.IsTrue(health.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, _root)));
                Assert.IsFalse(barGo.activeSelf, "Legacy _bar must stay inactive on hit (EnemyHealthBar owns enemy bar).");
                Assert.AreEqual(0, fbGo.GetComponentsInChildren<FloatingDamageNumber>(true).Length,
                    "EnemyImpactFeedback must not instantiate FloatingDamageNumber children (DamagePopup owns numbers).");
                Assert.GreaterOrEqual(vfxGo.transform.childCount, 2,
                    "EnemyImpactFeedback must still play blood and impact flash VFX.");

                Assert.IsTrue(health.TakeDamage(new DamageInfo(200, 1, 0f, 0f, true, _root)));
                Assert.IsFalse(barGo.activeSelf, "Legacy _bar must stay inactive on defeat.");
                Assert.AreEqual(0, fbGo.GetComponentsInChildren<FloatingDamageNumber>(true).Length);

                InvokePrivateMethod(feedback, "OnDisable");
            }
            finally
            {
                DamagePopup.ClearPoolForTests();
                foreach (var popup in Object.FindObjectsByType<DamagePopup>(FindObjectsInactive.Include))
                {
                    if (popup != null)
                        Object.DestroyImmediate(popup.gameObject);
                }
                VfxRecolor.ClearCacheAndDestroy();
                Object.DestroyImmediate(hitClip);
                Object.DestroyImmediate(flashDef);
                Object.DestroyImmediate(bloodDef);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void VfxRecolor_ClearCacheAndDestroy_DestroysProceduralTexturesSpritesAndDefinitions()
        {
            VfxRecolor.ClearCacheAndDestroy();

            var sourceTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sourceTex.SetPixels(new[] { Color.red, Color.red, Color.red, Color.red });
            sourceTex.Apply();
            var sourceSprite = Sprite.Create(sourceTex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 32f);
            var sourceDef = VfxDefinition.Create("SourceRedVfx", new[] { sourceSprite }, 24f, 1f, 2, 1);

            try
            {
                VfxDefinition blueDef = VfxRecolor.Blue(sourceDef);
                Assert.IsNotNull(blueDef);
                Assert.AreNotSame(sourceDef, blueDef);
                Assert.AreEqual(1, blueDef.Frames.Length);

                Sprite blueSprite = blueDef.Frames[0];
                Texture2D blueTex = blueSprite != null ? blueSprite.texture : null;
                Assert.IsNotNull(blueSprite);
                Assert.IsNotNull(blueTex);
                Assert.AreEqual(HideFlags.HideAndDontSave, blueSprite.hideFlags);
                Assert.AreEqual(HideFlags.HideAndDontSave, blueTex.hideFlags);
                Assert.IsFalse(blueTex.isReadable, "Recolored texture must release CPU memory via makeNoLongerReadable.");

                VfxRecolor.ClearCacheAndDestroy();
                Assert.IsTrue(blueDef == null, "ClearCacheAndDestroy must destroy cached VfxDefinition.");
                Assert.IsTrue(blueSprite == null, "ClearCacheAndDestroy must destroy cached recolored Sprite.");
                Assert.IsTrue(blueTex == null, "ClearCacheAndDestroy must destroy cached recolored Texture2D.");
            }
            finally
            {
                VfxRecolor.ClearCacheAndDestroy();
                Object.DestroyImmediate(sourceDef);
                Object.DestroyImmediate(sourceSprite);
                Object.DestroyImmediate(sourceTex);
            }
        }

        [Test]
        public void DarkFantasyHud_ReusesHealthBarTrailFill_AndProvidesFallbackFont()
        {
            var rootRect = new GameObject("HealthBarRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            rootRect.SetParent(_root.transform, false);

            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(rootRect, false);

            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(rootRect, false);

            var trailGo = new GameObject("TrailFill", typeof(RectTransform), typeof(Image));
            trailGo.transform.SetParent(trackGo.transform, false);
            var trailRect = trailGo.GetComponent<RectTransform>();
            var trailImage = trailGo.GetComponent<Image>();
            trailImage.color = Color.white;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.color = Color.white;

            var highlightGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            highlightGo.transform.SetParent(trackGo.transform, false);

            var valueGo = new GameObject("Value", typeof(RectTransform));
            valueGo.transform.SetParent(rootRect, false);
            var valueText = valueGo.AddComponent<Text>();

            var view = rootRect.gameObject.AddComponent<HealthBarView>();
            SetPrivateField(view, "_fill", fillRect);
            SetPrivateField(view, "_trailFill", trailRect);
            SetPrivateField(view, "_value", valueText);
            Assert.AreSame(trailRect, view.TrailFill, "HealthBarView.TrailFill must expose _trailFill.");

            var hudGo = new GameObject("DarkFantasyHud");
            hudGo.transform.SetParent(_root.transform, false);
            var hud = hudGo.AddComponent<DarkFantasyHud>();

            try
            {
                InvokePrivateMethod(hud, "Start");

                Assert.IsNull(trackGo.transform.Find("DamageTrail"),
                    "DarkFantasyHud must not create a duplicate DamageTrail child.");
                Assert.AreEqual((Color)new Color32(196, 88, 30, 255), trailImage.color,
                    "DarkFantasyHud must tint HealthBarView.TrailFill to Ember.");
                Assert.IsNotNull(valueText.font, "DarkFantasyHud must resolve a non-null fallback or OS font.");
            }
            finally
            {
                typeof(DarkFantasyHud)
                    .GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
            }
        }

        [Test]
        public void ScriptableObjectDataAssets_DeserializeAllEncapsulatedFieldsCleanly()
        {
            var enemyExpectations = new (string Path, string DcId, int SkillCount, EnemyAttackKind PrimaryKind, int PrimaryDamage)[]
            {
                ("Assets/_Project/Features/Enemies/Data/Enemy_Zombie.asset", "Zombie", 2, EnemyAttackKind.Melee, 10),
                ("Assets/_Project/Features/Enemies/Data/Enemy_Shield.asset", "Shield", 1, EnemyAttackKind.Dash, 10),
                ("Assets/_Project/Features/Enemies/Data/Enemy_Archer.asset", "Archer", 1, EnemyAttackKind.Shoot, 12),
                ("Assets/_Project/Features/Enemies/Data/Enemy_Grenadier.asset", "Grenader", 1, EnemyAttackKind.Grenade, 15),
                ("Assets/_Project/Features/Enemies/Data/Enemy_Rat.asset", "Rat", 1, EnemyAttackKind.Lunge, 5),
            };

            foreach (var expected in enemyExpectations)
            {
                var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>(expected.Path);
                Assert.IsNotNull(enemy, $"Failed to load {expected.Path}");
                Assert.AreEqual(expected.DcId, enemy.DcId, $"Unexpected DcId on {expected.Path}");
                Assert.IsNotNull(enemy.Skills, $"{expected.Path} Skills must not be null.");
                Assert.AreEqual(expected.SkillCount, enemy.Skills.Length, $"Unexpected skill count on {expected.Path}");
                Assert.IsNotEmpty(enemy.Skills, $"{expected.Path} Skills must not be empty.");

                EnemyData.Skill primary = enemy.Skills[0];
                Assert.AreEqual(expected.PrimaryKind, primary.Kind, $"Unexpected primary skill kind on {expected.Path}");
                Assert.Greater(primary.Damage, 0, $"Primary skill damage must be > 0 on {expected.Path}");
                Assert.AreEqual(expected.PrimaryDamage, primary.Damage, $"Unexpected primary skill damage on {expected.Path}");
                Assert.IsFalse(string.IsNullOrEmpty(primary.ChargeAnim), $"ChargeAnim missing on {expected.Path}");
                Assert.IsFalse(string.IsNullOrEmpty(primary.ExecuteAnim), $"ExecuteAnim missing on {expected.Path}");
            }

            var zombie = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Features/Enemies/Data/Enemy_Zombie.asset");
            Assert.AreEqual("jump", zombie.Skills[1].DcId);
            Assert.AreEqual(EnemyAttackKind.Lunge, zombie.Skills[1].Kind);
            Assert.AreEqual(20, zombie.Skills[1].Damage);
            Assert.AreEqual(6f, zombie.Skills[1].TravelCells, 0.001f);

            var shield = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Features/Enemies/Data/Enemy_Shield.asset");
            Assert.IsTrue(shield.HasFrontShield);
            Assert.AreEqual(1f, shield.Skills[0].StunDealt, 0.001f);
            Assert.AreEqual(8f, shield.Skills[0].TravelCells, 0.001f);

            var archer = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Features/Enemies/Data/Enemy_Archer.asset");
            Assert.IsNotNull(archer.Skills[0].ProjectilePrefab, "Enemy_Archer must preserve ProjectilePrefab reference.");
            Assert.AreEqual(7f, archer.Skills[0].ProjectileSpeed, 0.001f);
            Assert.AreEqual(1, archer.Skills[0].ProjectileCount);

            var grenadier = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Features/Enemies/Data/Enemy_Grenadier.asset");
            Assert.IsNotNull(grenadier.Skills[0].ProjectilePrefab, "Enemy_Grenadier must preserve ProjectilePrefab reference.");
            Assert.IsNotNull(grenadier.Skills[0].BlastVfx, "Enemy_Grenadier must preserve BlastVfx reference.");
            Assert.AreEqual(3, grenadier.Skills[0].ProjectileCount);
            Assert.AreEqual(1.5f, grenadier.Skills[0].FuseTime, 0.001f);
            Assert.AreEqual(2.9f, grenadier.Skills[0].BlastRadiusCells, 0.001f);

            var sword = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Features/Combat/Data/Weapon_StartSword.asset");
            Assert.IsNotNull(sword, "Weapon_StartSword.asset must exist.");
            Assert.AreEqual(3, sword.ComboLength, "Starter sword must deserialize 3 strikes.");

            WeaponData.Strike strike1 = sword.GetStrike(0);
            Assert.AreEqual("AttackA", strike1.AnimState);
            Assert.AreEqual(25, strike1.Damage);
            Assert.AreEqual(5, strike1.HitFrame);
            Assert.AreEqual(1.4f, strike1.AnimSpeed, 0.001f);
            Assert.AreEqual(2.9f, strike1.RangeWidthCells, 0.001f);
            Assert.AreEqual(5.2f, strike1.Knockback, 0.001f);
            Assert.IsFalse(strike1.CanCrit);
            Assert.AreEqual(0f, strike1.Stun, 0.001f);
            Assert.IsNotNull(strike1.SwingVfx);

            WeaponData.Strike strike2 = sword.GetStrike(1);
            Assert.AreEqual("AttackB", strike2.AnimState);
            Assert.AreEqual(30, strike2.Damage);
            Assert.IsFalse(strike2.CanCrit);
            Assert.AreEqual(0f, strike2.Stun, 0.001f);
            Assert.IsNotNull(strike2.SwingVfx);

            WeaponData.Strike strike3 = sword.GetStrike(2);
            Assert.AreEqual("AttackC", strike3.AnimState);
            Assert.AreEqual(45, strike3.Damage);
            Assert.AreEqual(9, strike3.HitFrame);
            Assert.AreEqual(1.05f, strike3.AnimSpeed, 0.001f);
            Assert.AreEqual(3.85f, strike3.RangeWidthCells, 0.001f);
            Assert.AreEqual(2.7f, strike3.RangeHeightCells, 0.001f);
            Assert.AreEqual(10.5f, strike3.Knockback, 0.001f);
            Assert.IsTrue(strike3.CanCrit, "Starter sword strike 3 must preserve CanCrit == true.");
            Assert.AreEqual(0f, strike3.Stun, 0.001f, "Starter sword strike 3 must keep Stun == 0f.");
            Assert.IsNotNull(strike3.SwingVfx);

            var bow = UnityEditor.AssetDatabase.LoadAssetAtPath<BowWeaponData>("Assets/_Project/Features/Combat/Data/Bow_StartBow.asset");
            Assert.IsNotNull(bow, "Bow_StartBow.asset must exist.");
            Assert.AreEqual(3, bow.ShotCount, "Bow_StartBow must deserialize 3 shots.");
            Assert.IsTrue(bow.IsValid, "Bow_StartBow deserialized shot fields must satisfy IsValid.");
            Assert.AreEqual(35, bow.GetShot(0).Damage);
            Assert.AreEqual(40, bow.GetShot(1).Damage);
            Assert.AreEqual(60, bow.GetShot(2).Damage);
            Assert.AreEqual(0.4f, bow.GetShot(2).ChargeTime, 0.001f);
            Assert.AreEqual(2.5f, bow.GetShot(2).AnimationSpeed, 0.001f);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void TrySetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing private method '{methodName}' on {target.GetType().Name}");
            method.Invoke(target, null);
        }
    }
}
