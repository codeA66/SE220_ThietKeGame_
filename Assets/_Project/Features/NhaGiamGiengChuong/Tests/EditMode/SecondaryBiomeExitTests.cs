using System.Collections.Generic;
using System.Reflection;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class SecondaryBiomeExitTests
    {
        private const string ShieldPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Shield.prefab";
        private const string ArcherPrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Archer.prefab";

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
        public void U20_SecondaryBiomeExitTrigger_RequiresSwitchDoorOpenAndBothGuardiansDefeated_BeforeSubmittingWinCandidate()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(206f, -16f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var sdGo = Track(new GameObject("SD_2_SewerExit"));
            sdGo.transform.position = new Vector3(206f, -16f, 0f);
            var sdTrig = sdGo.AddComponent<BoxCollider2D>();
            sdTrig.isTrigger = true;
            sdTrig.size = new Vector2(3f, 2f);
            var barrierGo = Track(new GameObject("Barrier"));
            var sd = sdGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_2", run, null, sdTrig, barrierGo, null, null, null, null, null, health, playerCol);

            var shieldP = SpawnEnemy(ShieldPrefabPath);
            var archerP = SpawnEnemy(ArcherPrefabPath);

            var exitGo = Track(new GameObject("I15_SewerExitTrigger"));
            exitGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var sewerExit = exitGo.AddComponent<SecondaryBiomeExitTrigger>();
            sewerExit.Configure("I15", "Toxic Sewers", run, sd, shieldP, archerP, null, health, playerCol);

            int candidates = 0;
            sewerExit.WinCandidateSubmitted += _ => candidates++;

            // 1. SwitchDoor closed & guardians alive -> rejected.
            Assert.IsFalse(sewerExit.IsUnlocked);
            Assert.IsFalse(sewerExit.TrySubmitForCollider(playerCol, 10), "[U20/Locked]");
            Assert.AreEqual(0, candidates);

            // 2. Open SwitchDoor SD_2, but only 1/2 guardians defeated -> still rejected!
            Assert.IsTrue(sd.TryInteract(), "[U20/OpenSwitchDoor]");
            sewerExit.NotifyGuardianDied(shieldP);
            Assert.IsFalse(sewerExit.AreGuardiansDefeated);
            Assert.IsFalse(sewerExit.IsUnlocked);
            Assert.IsFalse(sewerExit.TrySubmitForCollider(playerCol, 11), "[U20/OneGuardianAlive]");

            // 3. Defeat second guardian -> IsUnlocked becomes true, submits Win candidate at frame 12!
            sewerExit.NotifyGuardianDied(archerP);
            Assert.IsTrue(sewerExit.AreGuardiansDefeated);
            Assert.IsTrue(sewerExit.IsUnlocked);
            Assert.IsTrue(sewerExit.TrySubmitForCollider(playerCol, 12), "[U20/UnlockedWin]");
            Assert.AreEqual(1, candidates);
            Assert.AreEqual("Toxic Sewers", sewerExit.BiomeName);

            // Settle on next frame -> LevelOutcome.Win.
            Assert.IsTrue(run.ResolvePendingOutcome(13));
            Assert.AreEqual(LevelOutcome.Win, run.Outcome);
        }

        [Test]
        public void LockedSewerExit_ShowsLockedPromptWhenPlayerOverlaps_AndHidesWhenUnlocked()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(210f, -16f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);
            run.TickIntro(new DummyPlayState(), 0f);

            var sdGo = Track(new GameObject("SD_2_SewerExit"));
            sdGo.transform.position = new Vector3(206f, -16f, 0f);
            var sdTrig = sdGo.AddComponent<BoxCollider2D>();
            sdTrig.isTrigger = true;
            sdTrig.size = new Vector2(3f, 2f);
            var barrierGo = Track(new GameObject("Barrier"));
            var sd = sdGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_2", run, null, sdTrig, barrierGo, null, null, null, null, null, health, playerCol);

            var shieldP = SpawnEnemy(ShieldPrefabPath);
            var archerP = SpawnEnemy(ArcherPrefabPath);

            var exitGo = Track(new GameObject("I15_SewerExitTrigger"));
            exitGo.transform.position = new Vector3(224f, -15f, 0f);
            var exitTrig = exitGo.AddComponent<BoxCollider2D>();
            exitTrig.isTrigger = true;
            exitTrig.size = new Vector2(2f, 2f);
            var lockedPromptGo = Track(new GameObject("LockedPromptIndicator"));
            lockedPromptGo.transform.SetParent(exitGo.transform, false);
            lockedPromptGo.SetActive(false);
            var unlockedPromptGo = Track(new GameObject("PromptIndicator"));
            unlockedPromptGo.transform.SetParent(exitGo.transform, false);
            unlockedPromptGo.SetActive(false);

            var sewerExit = exitGo.AddComponent<SecondaryBiomeExitTrigger>();
            sewerExit.Configure("I15", "Toxic Sewers", run, sd, shieldP, archerP, null, health, playerCol, null, unlockedPromptGo);
            var so = new SerializedObject(sewerExit);
            var promptProp = so.FindProperty("_lockedPromptIndicator");
            if (promptProp != null)
            {
                promptProp.objectReferenceValue = lockedPromptGo;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            const BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Assert.IsNull(typeof(SecondaryBiomeExitTrigger).GetMethod("OnTriggerEnter2D", anyInstance),
                "[BiomeExit/NoTouchEnter] Biome exits must require [E] press instead of auto-exiting on touch.");
            Assert.IsNull(typeof(SecondaryBiomeExitTrigger).GetMethod("OnTriggerStay2D", anyInstance),
                "[BiomeExit/NoTouchStay] Biome exits must require [E] press instead of auto-exiting on stay.");

            // 1. Outside trigger while locked -> both prompts hidden.
            TickExitVisual(sewerExit);
            Assert.IsFalse(sewerExit.LockedPromptVisible, "[T1.5/SewerOutside]");
            Assert.IsFalse(lockedPromptGo.activeSelf);
            Assert.IsFalse(sewerExit.PromptVisible);
            Assert.IsFalse(unlockedPromptGo.activeSelf);

            // 2. Overlapping trigger while locked -> locked prompt shown, unlocked prompt hidden, TryInteract rejected.
            playerGo.transform.position = new Vector3(224f, -16f, 0f);
            TickExitVisual(sewerExit);
            Assert.IsTrue(sewerExit.LockedPromptVisible, "[T1.5/SewerLockedOverlap] Locked prompt must show when overlapping locked exit.");
            Assert.IsTrue(lockedPromptGo.activeSelf, "[T1.5/SewerLockedOverlapGo]");
            Assert.IsFalse(sewerExit.PromptVisible);
            Assert.IsFalse(unlockedPromptGo.activeSelf);
            Assert.IsFalse(sewerExit.TryInteract());

            // 3. Stepping back outside -> prompt hides.
            playerGo.transform.position = new Vector3(210f, -16f, 0f);
            TickExitVisual(sewerExit);
            Assert.IsFalse(sewerExit.LockedPromptVisible);
            Assert.IsFalse(lockedPromptGo.activeSelf);

            // 4. Unlock switch door + defeat both guardians, then overlap exit -> locked prompt hides, [E] prompt shows, TryInteract wins!
            playerGo.transform.position = new Vector3(206f, -16f, 0f);
            Assert.IsTrue(sd.TryInteract());
            sewerExit.NotifyGuardianDied(shieldP);
            sewerExit.NotifyGuardianDied(archerP);
            Assert.IsTrue(sewerExit.IsUnlocked);

            playerGo.transform.position = new Vector3(224f, -16f, 0f);
            TickExitVisual(sewerExit);
            Assert.IsFalse(sewerExit.LockedPromptVisible, "[T1.5/SewerUnlockedOverlap] Locked prompt must hide once unlocked.");
            Assert.IsFalse(lockedPromptGo.activeSelf);
            Assert.IsTrue(sewerExit.PromptVisible, "[T1.5/SewerUnlockedPrompt] [E] prompt must show when unlocked inside trigger.");
            Assert.IsTrue(unlockedPromptGo.activeSelf);
            Assert.IsTrue(sewerExit.TryInteract(), "[T1.5/SewerUnlockedInteract] Pressing [E] inside unlocked trigger must submit win.");
            Assert.IsTrue(run.ResolvePendingOutcome(Time.frameCount + 1));
            Assert.AreEqual(LevelOutcome.Win, run.Outcome);

            // 5. Null Health hides both prompts, and Configure(..., promptIndicator: null) clears PromptIndicator cleanly.
            var runNoHealth = Track(new GameObject("RunNoHealth")).AddComponent<LevelRunController>();
            runNoHealth.Configure(null, null);
            runNoHealth.TickIntro(new DummyPlayState(), 0f);
            sewerExit.Configure("I15", "Toxic Sewers", runNoHealth, null, null, null, null, null, playerCol, null, null);
            Assert.IsNull(sewerExit.PromptIndicator, "[T1.5/ConfigureClearPrompt]");
            TickExitVisual(sewerExit);
            Assert.IsFalse(sewerExit.PromptVisible, "[T1.5/NullHealthHidesPrompt]");
            Assert.IsFalse(sewerExit.LockedPromptVisible, "[T1.5/NullHealthHidesLockedPrompt]");
            Assert.IsFalse(sewerExit.TryInteract(), "[T1.5/NullHealthRejectsInteract]");
        }

        private static void TickExitVisual(SecondaryBiomeExitTrigger exit)
        {
            var refresh = typeof(SecondaryBiomeExitTrigger).GetMethod("RefreshVisualState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (refresh != null)
            {
                refresh.Invoke(exit, null);
                return;
            }
            var update = typeof(SecondaryBiomeExitTrigger).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            update?.Invoke(exit, null);
        }

        private EnemyController SpawnEnemy(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Missing prefab: " + prefabPath);
            var inst = Track(Object.Instantiate(prefab));
            return inst.GetComponent<EnemyController>();
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
