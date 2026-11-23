using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class ExitTriggerTests
    {
        private const string ZombiePrefabPath = "Assets/_Project/Features/Enemies/Prefabs/Enemy_Zombie.prefab";
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
        public void U16_ExitTrigger_RejectsInvalidConditions_AndSubmitsWinCandidateOnlyWhenAlivePlayingAndGateOpen()
        {
            var playerGo = Track(new GameObject("Player"));
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            var playerHealth = playerGo.AddComponent<Health>();
            playerHealth.Initialize(100, 0f);

            var childProjCol = Track(new GameObject("ProjectileOrGhost")).AddComponent<BoxCollider2D>();
            childProjCol.transform.SetParent(playerGo.transform, false);

            var otherActorCol = Track(new GameObject("OtherActor")).AddComponent<BoxCollider2D>();

            var runGo = Track(new GameObject("Run"));
            var run = runGo.AddComponent<LevelRunController>();
            run.Configure(null, playerHealth);

            var zombieE = SpawnEnemy(ZombiePrefabPath);
            var archerE = SpawnEnemy(ArcherPrefabPath);
            var barrier = Track(new GameObject("Barrier"));
            var gate = Track(new GameObject("Gate")).AddComponent<EncounterGate>();
            gate.Configure(run, zombieE, archerE, barrier);

            var exitGo = Track(new GameObject("I5_ExitTrigger"));
            var exitTrigger = exitGo.AddComponent<ExitTrigger>();
            exitTrigger.Configure(run, gate, null, playerHealth, playerCol);

            int candidateCount = 0;
            int lastCandidateFrame = -1;
            int finishedCount = 0;
            LevelOutcome? lastOutcome = null;
            exitTrigger.WinCandidateSubmitted += f => { candidateCount++; lastCandidateFrame = f; };
            run.Finished += o => { finishedCount++; lastOutcome = o; };

            // 1. Intro phase -> rejected (0 candidates).
            Assert.IsFalse(exitTrigger.TrySubmitForCollider(playerCol, 10), "[U16/Intro]");
            Assert.AreEqual(0, candidateCount);

            // Transition to Playing.
            run.TickIntro(new DummyPlayState(), 0f);

            // 2. Gate I4 closed -> rejected.
            Assert.IsFalse(gate.IsOpen);
            Assert.IsFalse(exitTrigger.TrySubmitForCollider(playerCol, 11), "[U16/GateClosed]");
            Assert.AreEqual(0, candidateCount);

            // Open gate I4 by defeating both Room E enemies.
            gate.NotifyEnemyDied(zombieE);
            gate.NotifyEnemyDied(archerE);
            Assert.IsTrue(gate.IsOpen);

            // 3. Other actor collider -> rejected.
            Assert.IsFalse(exitTrigger.TrySubmitForCollider(otherActorCol, 12), "[U16/OtherActor]");
            Assert.AreEqual(0, candidateCount);

            // 4. Child projectile / ghost collider -> rejected.
            Assert.IsFalse(exitTrigger.TrySubmitForCollider(childProjCol, 13), "[U16/SecondaryCollider]");
            Assert.AreEqual(0, candidateCount);

            // 5. Dead player -> rejected.
            playerHealth.TakeDamage(new DamageInfo(999, 1, 0f, 0f, false, null));
            Assert.IsFalse(exitTrigger.TrySubmitForCollider(playerCol, 14), "[U16/DeadPlayer]");
            Assert.AreEqual(0, candidateCount);

            // Restore alive state on a fresh Playing run with open gate for Ended check + Positive check.
            var run2 = Track(new GameObject("Run2")).AddComponent<LevelRunController>();
            var health2 = Track(new GameObject("Health2")).AddComponent<Health>();
            health2.Initialize(100, 0f);
            run2.Configure(null, health2);
            run2.TickIntro(new DummyPlayState(), 0f);
            var gate2 = Track(new GameObject("Gate2")).AddComponent<EncounterGate>();
            gate2.Configure(run2, zombieE, archerE, Track(new GameObject("Barrier2")));
            gate2.NotifyEnemyDied(zombieE);
            gate2.NotifyEnemyDied(archerE);
            Assert.IsTrue(gate2.IsOpen);

            var exit2 = Track(new GameObject("Exit2")).AddComponent<ExitTrigger>();
            exit2.Configure(run2, gate2, null, health2, playerCol);

            // 6. Ended phase check on run2 after Win settles.
            int c2 = 0;
            int f2 = -1;
            int fin2 = 0;
            LevelOutcome? out2 = null;
            exit2.WinCandidateSubmitted += frame => { c2++; f2 = frame; };
            run2.Finished += o => { fin2++; out2 = o; };

            // Valid positive case: living assigned Player, Playing, I4 open -> submits Win candidate for frame 50, NOT finished yet.
            Assert.IsTrue(exit2.TrySubmitForCollider(playerCol, 50), "[U16/Valid] expected=true");
            Assert.AreEqual(1, c2, "[U16/Valid] candidateCount expected=1");
            Assert.AreEqual(50, f2, "[U16/Valid] candidateFrame expected=50");
            Assert.AreEqual(0, fin2, "[U16/Valid] Finished must only fire via LevelRunController settlement.");

            // Settle at frame 51 -> Win emitted once.
            Assert.IsTrue(run2.ResolvePendingOutcome(51));
            Assert.AreEqual(1, fin2);
            Assert.AreEqual(LevelOutcome.Win, out2);

            // After Ended phase -> further exit contact rejected.
            Assert.IsFalse(exit2.TrySubmitForCollider(playerCol, 52), "[U16/AfterEnded]");
            Assert.AreEqual(1, c2);
            Assert.AreEqual(0, finishedCount);
            Assert.IsNull(lastOutcome);
            Assert.AreEqual(-1, lastCandidateFrame);
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
