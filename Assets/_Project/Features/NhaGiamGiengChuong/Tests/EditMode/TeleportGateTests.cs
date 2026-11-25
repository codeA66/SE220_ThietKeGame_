using System.Collections.Generic;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Currency;
using DeadCells.Player;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class TeleportGateTests
    {
        private readonly List<GameObject> _created = new();

        private sealed class DummyPlayState : IPlayerState
        {
            public void Enter() { }
            public void Exit() { }
            public void Tick() { }
            public void FixedTick() { }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            Time.timeScale = 1f;
        }

        [Test]
        public void U18_TeleportGate_RequiresBothGatesDiscovered_WarpsFeetToPairedGate_EnforcesCooldownAndPromotesGoldPickupSorting()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var inactiveSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);
            var activeSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);

            var gateAGo = Track(new GameObject("TP_0_Spawn"));
            gateAGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var trigA = gateAGo.AddComponent<BoxCollider2D>();
            trigA.isTrigger = true;
            trigA.offset = new Vector2(0f, 1.0f);
            trigA.size = new Vector2(2.5f, 2.0f);
            var srA = gateAGo.AddComponent<SpriteRenderer>();
            var promptA = Track(new GameObject("PromptA"));

            var gateBGo = Track(new GameObject("TP_1_EastVault"));
            gateBGo.transform.position = new Vector3(161f, 0f, 0f);
            var trigB = gateBGo.AddComponent<BoxCollider2D>();
            trigB.isTrigger = true;
            trigB.offset = new Vector2(0f, 1.0f);
            trigB.size = new Vector2(2.5f, 2.0f);
            var srB = gateBGo.AddComponent<SpriteRenderer>();
            var promptB = Track(new GameObject("PromptB"));

            var gateA = gateAGo.AddComponent<TeleportGate>();
            var gateB = gateBGo.AddComponent<TeleportGate>();
            gateA.Configure("TP_0", run, null, trigA, gateB, true, Vector2.zero, null, promptA, srA, inactiveSp, activeSp, health, playerCol);
            gateB.Configure("TP_1", run, null, trigB, gateA, false, Vector2.zero, null, promptB, srB, inactiveSp, activeSp, health, playerCol);

            // 1. Gate A is discovered, Gate B is undiscovered -> CanTeleport is false, warp rejected!
            gateA.RefreshVisualState();
            Assert.IsTrue(gateA.IsDiscovered);
            Assert.IsFalse(gateB.IsDiscovered);
            Assert.IsFalse(gateA.CanTeleport, "[U18/UndiscoveredTarget] Cannot teleport to undiscovered vault gate.");
            Assert.IsFalse(gateA.PromptVisible);
            Assert.IsFalse(gateA.TryTeleport(10f), "[U18/UndiscoveredReject]");
            Assert.AreEqual(15.5f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);

            // 2. Player physically reaches Gate B at (161, 0) -> RefreshVisualState auto-discovers Gate B!
            playerGo.transform.position = new Vector3(161f, 0f, 0f);
            Physics2D.SyncTransforms();
            gateB.RefreshVisualState();
            Assert.IsTrue(gateB.IsDiscovered, "[U18/AutoDiscover] Entering Gate B trigger must discover it.");
            Assert.AreSame(activeSp, srB.sprite);
            Assert.IsTrue(gateB.CanTeleport);
            Assert.IsTrue(gateA.CanTeleport);

            // 3. Teleport from Gate B (161, 0) back to Gate A (15.5, 0) at t=20s -> succeeds once.
            Assert.IsTrue(gateB.TryTeleport(20f), "[U18/WarpBToA]");
            Vector2 feetAfterBToA = EncounterZone.GetColliderFeet(playerCol);
            Assert.AreEqual(15.5f, feetAfterBToA.x, 1e-3f);
            Assert.AreEqual(0f, feetAfterBToA.y, 1e-3f);
            Assert.AreEqual(1, gateB.TeleportCount);

            // 4. Immediate bounce-back on Gate A at t=20.05s (< 0.25s cooldown) -> rejected!
            Assert.IsFalse(gateA.TryTeleport(20.05f), "[U18/CooldownGuard] Must reject immediate bounce-back within 0.25s.");
            Assert.AreEqual(15.5f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);

            // 5. After cooldown at t=20.35s -> Gate A warps back to Gate B (161, 0).
            Assert.IsTrue(gateA.TryTeleport(20.35f), "[U18/WarpAToB]");
            Vector2 feetAfterAToB = EncounterZone.GetColliderFeet(playerCol);
            Assert.AreEqual(161f, feetAfterAToB.x, 1e-3f);
            Assert.AreEqual(0f, feetAfterAToB.y, 1e-3f);

            // 6. Verify GoldPickup sorting-layer promotion from Default (0) to VFX above Background.
            var coinRoot = Track(new GameObject("GoldPickupTest"));
            coinRoot.AddComponent<Rigidbody2D>();
            var coinChild = Track(new GameObject("Coin"));
            coinChild.transform.SetParent(coinRoot.transform, false);
            var coinSr = coinChild.AddComponent<SpriteRenderer>();
            coinSr.sortingLayerID = 0;
            coinSr.sortingOrder = 0;
            var pickup = coinRoot.AddComponent<GoldPickup>();
            pickup.EnsureVfxSortingLayer();
            Assert.AreEqual("VFX", coinSr.sortingLayerName, "[U18/GoldPickupSorting] Default sorting layer must promote to VFX.");
            Assert.GreaterOrEqual(coinSr.sortingOrder, 5);
        }

        [Test]
        public void TeleportGate_WhenPowerScrollIsFocused_BlocksTeleportOnSameFrameEPress()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(161f, 0f, 0f);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);
            var stats = new PlayerRunStats(health);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var gateAGo = Track(new GameObject("TP_0_Spawn"));
            gateAGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var trigA = gateAGo.AddComponent<BoxCollider2D>();
            trigA.isTrigger = true;
            trigA.offset = new Vector2(0f, 1.0f);
            trigA.size = new Vector2(2.2f, 2.0f);
            var promptA = Track(new GameObject("PromptA"));

            var gateBGo = Track(new GameObject("TP_1_EastVault"));
            gateBGo.transform.position = new Vector3(161f, 0f, 0f);
            var trigB = gateBGo.AddComponent<BoxCollider2D>();
            trigB.isTrigger = true;
            trigB.offset = new Vector2(0f, 1.0f);
            trigB.size = new Vector2(2.2f, 2.0f);
            var promptB = Track(new GameObject("PromptB"));

            var gateA = gateAGo.AddComponent<TeleportGate>();
            var gateB = gateBGo.AddComponent<TeleportGate>();
            gateA.Configure("TP_0", run, null, trigA, gateB, true, Vector2.zero, null, promptA, null, null, null, health, playerCol);
            gateB.Configure("TP_1", run, null, trigB, gateA, true, Vector2.zero, null, promptB, null, null, null, health, playerCol);

            var scrollGo = Track(new GameObject("I11_Scroll_Test"));
            scrollGo.transform.position = new Vector3(160.5f, 0f, 0f);
            var iconGo = Track(new GameObject("Icon"));
            iconGo.transform.SetParent(scrollGo.transform, false);
            var scrollPromptGo = Track(new GameObject("ScrollPrompt"));
            scrollPromptGo.transform.SetParent(scrollGo.transform, false);
            scrollPromptGo.SetActive(false);
            var pickup = scrollGo.AddComponent<PowerScrollPickup>();
            SetObjectProperty(pickup, "_icon", iconGo.transform);
            SetObjectProperty(pickup, "_prompt", scrollPromptGo);

            var interactionGo = Track(new GameObject("Power Scrolls"));
            var interaction = interactionGo.AddComponent<PowerScrollInteraction>();
            SetObjectArrayProperty(interaction, "_scrolls", new Object[] { pickup });

            interaction.Tick((Vector2)playerGo.transform.position, canInteract: true, pressed: false, stats);
            Assert.IsTrue(interaction.HasFocusedScroll, "[T1.4/ScrollFocus] PowerScrollInteraction must report HasFocusedScroll.");

            gateB.RefreshVisualState();
            Assert.IsFalse(gateB.PromptVisible, "[T1.4/ScrollFocus] TeleportGate prompt must stay hidden when a PowerScroll is focused.");
            Assert.IsFalse(gateB.TryTeleport(20f), "[T1.4/ScrollFocus] TeleportGate must block teleport on same-frame E press while PowerScroll is focused.");
            Assert.AreEqual(161f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);

            interaction.Tick((Vector2)playerGo.transform.position, canInteract: false, pressed: false, stats);
            Assert.IsFalse(interaction.HasFocusedScroll);
            gateB.RefreshVisualState();
            Assert.IsTrue(gateB.PromptVisible, "[T1.4/Unfocused] TeleportGate prompt restores once scroll is no longer focused.");
            Assert.IsTrue(gateB.TryTeleport(20f), "[T1.4/Unfocused] Teleport succeeds once scroll is no longer focused.");
            Assert.AreEqual(15.5f, EncounterZone.GetColliderFeet(playerCol).x, 1e-3f);
        }

        [Test]
        public void TryTeleport_SnapsCameraToArrival_AndShowsLockedHintWhenPairNotDiscovered()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var rb = playerGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var camGo = Track(new GameObject("Main Camera"));
            camGo.transform.position = new Vector3(161f, 1.5f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            var camFollow = camGo.AddComponent<CameraFollow2D>();
            SetObjectProperty(camFollow, "_target", playerGo.transform);
            SetVector2Property(camFollow, "_offset", new Vector2(0f, 1.5f));
            SetBoolProperty(camFollow, "_useBounds", true);
            SetRectProperty(camFollow, "_bounds", new Rect(-104f, -20f, 340f, 86f));

            var gateAGo = Track(new GameObject("TP_0_Spawn"));
            gateAGo.transform.position = new Vector3(15.5f, 0f, 0f);
            var trigA = gateAGo.AddComponent<BoxCollider2D>();
            trigA.isTrigger = true;
            trigA.offset = new Vector2(0f, 1.0f);
            trigA.size = new Vector2(2.2f, 2.0f);
            var promptA = Track(new GameObject("PromptA"));
            var lockedHintA = Track(new GameObject("LockedHintA"));
            lockedHintA.SetActive(false);

            var gateBGo = Track(new GameObject("TP_1_EastVault"));
            gateBGo.transform.position = new Vector3(161f, 0f, 0f);
            var trigB = gateBGo.AddComponent<BoxCollider2D>();
            trigB.isTrigger = true;
            trigB.offset = new Vector2(0f, 1.0f);
            trigB.size = new Vector2(2.2f, 2.0f);
            var promptB = Track(new GameObject("PromptB"));
            var lockedHintB = Track(new GameObject("LockedHintB"));
            lockedHintB.SetActive(false);

            var gateA = gateAGo.AddComponent<TeleportGate>();
            var gateB = gateBGo.AddComponent<TeleportGate>();
            gateA.Configure("TP_0", run, null, trigA, gateB, true, Vector2.zero, null, promptA, null, null, null, health, playerCol);
            gateB.Configure("TP_1", run, null, trigB, gateA, false, Vector2.zero, null, promptB, null, null, null, health, playerCol);
            SetObjectProperty(gateA, "_camera", camFollow);
            SetObjectProperty(gateA, "_lockedHintIndicator", lockedHintA);
            SetObjectProperty(gateB, "_camera", camFollow);
            SetObjectProperty(gateB, "_lockedHintIndicator", lockedHintB);

            // 1. Inside discovered Gate A while paired Gate B is undiscovered -> shows locked hint.
            gateA.RefreshVisualState();
            Assert.IsFalse(gateA.PromptVisible);
            Assert.IsTrue(gateA.LockedHintVisible, "[T1.5/LockedHint] Must show locked hint when pair is undiscovered.");
            Assert.IsTrue(lockedHintA.activeSelf, "[T1.5/LockedHintGo] _lockedHintIndicator must activate.");

            // Leaving Gate A hides locked hint.
            playerGo.transform.position = new Vector3(5f, 0f, 0f);
            gateA.RefreshVisualState();
            Assert.IsFalse(gateA.LockedHintVisible);
            Assert.IsFalse(lockedHintA.activeSelf);

            // 2. Discover Gate B and teleport back to Gate A -> camera snaps immediately to (15.5, 1.5).
            playerGo.transform.position = new Vector3(161f, 0f, 0f);
            camGo.transform.position = new Vector3(161f, 1.5f, -10f);
            gateB.RefreshVisualState();
            Assert.IsTrue(gateB.IsDiscovered);
            Assert.IsFalse(gateB.LockedHintVisible);
            Assert.IsFalse(lockedHintB.activeSelf);

            Assert.IsTrue(gateB.TryTeleport(20f), "[T1.5/Teleport]");
            Assert.AreEqual(15.5f, camGo.transform.position.x, 1e-3f, "[T1.5/CamSnapX] CameraFollow2D must snap immediately on teleport.");
            Assert.AreEqual(1.5f, camGo.transform.position.y, 1e-3f, "[T1.5/CamSnapY] CameraFollow2D must snap immediately on teleport.");

            gateA.RefreshVisualState();
            Assert.IsFalse(gateA.LockedHintVisible);
            Assert.IsFalse(lockedHintA.activeSelf);
        }

        private static void SetObjectProperty(Object target, string propName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null)
                return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArrayProperty(Object target, string propName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null)
                return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2Property(Object target, string propName, Vector2 value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null)
                return;
            prop.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolProperty(Object target, string propName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null)
                return;
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRectProperty(Object target, string propName, Rect value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop == null)
                return;
            prop.rectValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
