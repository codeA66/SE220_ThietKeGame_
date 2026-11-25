using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadCells.Combat;
using DeadCells.Enemies;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class RunPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P05_SameFrameExitAndLethalDamage_AlwaysSettlesLoss_AndArcherArrowCrossesRoomEOneWayPlatform()
        {
            // Part A: Same-frame I5 contact THEN lethal Health.TakeDamage -> Loss only
            yield return RunSameFrameConflictVariant(exitFirst: true);

            // Part B: Same-frame lethal Health.TakeDamage THEN I5 contact -> Loss only
            yield return RunSameFrameConflictVariant(exitFirst: false);

            // Part C: Explicit PlayMode proof that Archer arrow crosses Room E center OneWayPlatform [-4, 2] @ y=26 without despawning
            yield return PlayModeTestHelper.LoadLevelScene();
            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var projSpawner = UnityEngine.Object.FindAnyObjectByType<ProjectileSpawner>();
            var archerE = UnityEngine.Object.FindObjectsByType<EnemyController>()
                .Single(e => e.name == "Enemy_Archer_E");
            yield return PlayModeTestHelper.WaitForPlaying(run);

            var arrowPrefab = archerE.Data.Skills.Select(s => s.ProjectilePrefab).FirstOrDefault(p => p != null);
            Assert.IsNotNull(arrowPrefab, "[P05/RoomEArrow] Missing Projectile_Arrow prefab on ArcherData.");
            var arrow = projSpawner.Get(arrowPrefab);
            // Fire directly across OneWay_E_Combat (x=[-4,2], y=[25.75,26.0]) at y=25.875 from x=4 leftward
            arrow.LaunchStraight(new Vector2(4f, 25.875f), new Vector2(-18f, 0f), damage: 15, knockback: 1f, owner: archerE.gameObject);

            bool crossedCenterPlatform = false;
            yield return PlayModeTestHelper.WaitUntil(() =>
            {
                if (arrow.IsLive && arrow.transform.position.x < -5f)
                    crossedCenterPlatform = true;
                return crossedCenterPlatform || !arrow.IsLive;
            }, 2.5f, "[P05/RoomEArrow] Timed out tracking arrow across Room E OneWayPlatform.");

            Assert.IsTrue(crossedCenterPlatform,
                $"[P05/RoomEArrow] Archer arrow must fly straight across Room E OneWayPlatform [-4,2]@26 without despawning. pos={arrow.transform.position}, live={arrow.IsLive}");
        }

        private IEnumerator RunSameFrameConflictVariant(bool exitFirst)
        {
            string label = exitFirst ? "ExitThenLethal" : "LethalThenExit";
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var gate = UnityEngine.Object.FindAnyObjectByType<EncounterGate>();
            var exit = UnityEngine.Object.FindAnyObjectByType<ExitTrigger>();
            var panel = UnityEngine.Object.FindAnyObjectByType<ResultPanel>();
            yield return PlayModeTestHelper.WaitForPlaying(run);

            // Open gate I4 first
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
            var zombieE = enemies.Single(e => e.name == "Enemy_Zombie_E");
            var archerE = enemies.Single(e => e.name == "Enemy_Archer_E");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-13.5f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(zombieE.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            Assert.IsTrue(archerE.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            yield return null;
            Assert.IsTrue(gate.IsOpen, $"[P05/{label}] Gate I4 must be open.");

            var shownOutcomes = new List<LevelOutcome>();
            int winCandidates = 0;
            bool diedFired = false;
            int diedFrame = -1;
            panel.PanelShown += shownOutcomes.Add;
            exit.WinCandidateSubmitted += _ => winCandidates++;
            player.Health.Died += _ => { diedFired = true; diedFrame = Time.frameCount; };

            var playerCol = player.GetComponent<BoxCollider2D>();
            // Wait for start of a fresh frame then invoke both without yielding
            yield return null;
            int frameStart = Time.frameCount;
            int frameExit;
            int frameKill;

            if (exitFirst)
            {
                frameExit = Time.frameCount;
                bool submitted = exit.TrySubmitForCollider(playerCol, frameExit);
                Assert.IsTrue(submitted, $"[P05/{label}] Exit must register win candidate before lethal damage.");
                frameKill = Time.frameCount;
                Assert.IsTrue(player.Health.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null)));
            }
            else
            {
                frameKill = Time.frameCount;
                Assert.IsTrue(player.Health.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null)));
                frameExit = Time.frameCount;
                exit.TrySubmitForCollider(playerCol, frameExit);
            }

            Assert.AreEqual(frameStart, frameExit, $"[P05/{label}] Must execute in identical frame.");
            Assert.AreEqual(frameStart, frameKill, $"[P05/{label}] Must execute in identical frame.");
            Assert.IsTrue(diedFired, $"[P05/{label}] Real Health.Died must fire.");
            Assert.AreEqual(frameStart, diedFrame, $"[P05/{label}] Died frame must match.");
            if (exitFirst)
                Assert.AreEqual(1, winCandidates, $"[P05/{label}] ExitFirst must submit 1 win candidate before death.");

            // Wait for settlement on next frame(s)
            yield return PlayModeTestHelper.WaitUntil(() => run.IsEnded, 2f, $"[P05/{label}] Run did not settle.");
            yield return null;

            Assert.AreEqual(LevelOutcome.Loss, run.Outcome, $"[P05/{label}] Loss must win same-frame tie.");
            Assert.AreEqual(1, shownOutcomes.Count, $"[P05/{label}] Panel must show exactly once.");
            Assert.AreEqual(LevelOutcome.Loss, shownOutcomes[0]);
            Assert.IsTrue(panel.IsVisible);
            Assert.AreEqual(ResultPanel.LossTitle, panel.TitleText.text);
        }

        [UnityTest]
        public IEnumerator P06_PostOutcomeLock_SurvivesHitStopAndRespawnDelay_WhileKeepingReplayUIActive()
        {
            yield return RunPostEndLockVariant(isWin: false);
            yield return RunPostEndLockVariant(isWin: true);
        }

        private IEnumerator RunPostEndLockVariant(bool isWin)
        {
            string label = isWin ? "AfterWin" : "AfterLoss";
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var hitStop = UnityEngine.Object.FindAnyObjectByType<HitStop>();
            var gate = UnityEngine.Object.FindAnyObjectByType<EncounterGate>();
            var chest = UnityEngine.Object.FindAnyObjectByType<HealingChest>();
            var exit = UnityEngine.Object.FindAnyObjectByType<ExitTrigger>();
            var panel = UnityEngine.Object.FindAnyObjectByType<ResultPanel>();
            yield return PlayModeTestHelper.WaitForPlaying(run);

            if (isWin)
            {
                var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>();
                PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-13.5f, 24f));
                yield return new WaitForFixedUpdate();
                yield return null;
                enemies.Single(e => e.name == "Enemy_Zombie_E").GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject));
                enemies.Single(e => e.name == "Enemy_Archer_E").GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject));
                yield return null;
                hitStop.Trigger(0.12f);
                Assert.AreEqual(0f, Time.timeScale, 0.0001f);
                exit.TrySubmitForCollider(player.GetComponent<BoxCollider2D>(), Time.frameCount);
            }
            else
            {
                hitStop.Trigger(0.12f);
                Assert.AreEqual(0f, Time.timeScale, 0.0001f);
                player.Health.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null));
            }

            yield return PlayModeTestHelper.WaitUntil(() => run.IsEnded && panel.IsVisible, 2f, $"[P06/{label}] Outcome did not settle.");

            // Wait for HitStop to finish and restore Time.timeScale == 1f
            yield return PlayModeTestHelper.WaitUntil(() => Time.timeScale >= 0.99f, 2f, $"[P06/{label}] HitStop did not resume timeScale.");

            int lockedHp = player.Health.Current;
            Vector3 lockedPos = player.transform.position;
            bool lockedChestReward = chest.HasReward;
            bool lockedGateOpen = gate.IsOpen;

            // Hold movement & interact while waiting > 1.6s normal gameplay time (past PlayerController's 1.5s auto-respawn delay)
            Press(_keyboard.dKey);
            Press(_keyboard.eKey);
            float startGameTime = Time.time;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Time.time - startGameTime < 1.65f)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"[P06/{label}] Gameplay clock stalled.");
                yield return null;
            }
            Release(_keyboard.dKey);
            Release(_keyboard.eKey);

            // Attempt damage while locked -> must be vetoed
            Assert.IsFalse(player.Health.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, null)),
                $"[P06/{label}] Damage after end must be blocked.");

            Assert.IsTrue(panel.IsVisible, $"[P06/{label}] ResultPanel must remain visible.");
            Assert.AreEqual(1, panel.ShowCount, $"[P06/{label}] ResultPanel must only show once.");
            Assert.AreEqual(lockedHp, player.Health.Current, $"[P06/{label}] Player must NOT auto-respawn or change HP.");
            Assert.AreEqual(lockedPos, player.transform.position, $"[P06/{label}] Player must NOT move while locked.");
            Assert.AreEqual(lockedChestReward, chest.HasReward, $"[P06/{label}] Chest state preserved.");
            Assert.AreEqual(lockedGateOpen, gate.IsOpen, $"[P06/{label}] Gate state preserved.");
            Assert.IsTrue(panel.ReplayButton.interactable, $"[P06/{label}] Replay button remains interactive.");
        }

        [UnityTest]
        public IEnumerator P07_ReplayButton_ReloadsCleanRunAfterWinAndLoss_AndClearsOldProjectiles()
        {
            yield return RunReplayVariant(winFirstRun: true);
            yield return RunReplayVariant(winFirstRun: false);
        }

        private IEnumerator RunReplayVariant(bool winFirstRun)
        {
            string label = winFirstRun ? "AfterWin" : "AfterLoss";
            yield return PlayModeTestHelper.LoadLevelScene();

            var run1 = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player1 = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var gate1 = UnityEngine.Object.FindAnyObjectByType<EncounterGate>();
            var chest1 = UnityEngine.Object.FindAnyObjectByType<HealingChest>();
            var exit1 = UnityEngine.Object.FindAnyObjectByType<ExitTrigger>();
            var panel1 = UnityEngine.Object.FindAnyObjectByType<ResultPanel>();
            var spawner1 = UnityEngine.Object.FindAnyObjectByType<ProjectileSpawner>();
            yield return PlayModeTestHelper.WaitForPlaying(run1);

            // Mutate world state: wake E1, kill both E enemies (opens I4), damage Player & consume I3, launch an Archer arrow in flight
            var enemies1 = UnityEngine.Object.FindObjectsByType<EnemyController>();
            var zombieE1 = enemies1.Single(e => e.name == "Enemy_Zombie_E");
            var archerE1 = enemies1.Single(e => e.name == "Enemy_Archer_E");

            PlayModeTestHelper.PositionPlayerAtFeet(player1, new Vector2(-13.5f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            var arrowPrefab = archerE1.Data.Skills.Select(s => s.ProjectilePrefab).First(p => p != null);
            var oldArrow = spawner1.Get(arrowPrefab);
            var oldArrowEntityId = oldArrow.GetEntityId();
            oldArrow.LaunchStraight(new Vector2(5f, 26f), new Vector2(-5f, 0f), 10, 1f, archerE1.gameObject);

            zombieE1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player1.gameObject));
            archerE1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player1.gameObject));
            yield return null;
            Assert.IsTrue(gate1.IsOpen);

            PlayModeTestHelper.PositionPlayerAtFeet(player1, new Vector2(72f, 0f));
            yield return new WaitForFixedUpdate();
            player1.Health.TakeDamage(new DamageInfo(30, 1, 0f, 0f, false, null));
            Assert.IsTrue(chest1.TryInteract());
            Assert.IsTrue(chest1.IsConsumed);

            // Finish run 1
            if (winFirstRun)
                exit1.TrySubmitForCollider(player1.GetComponent<BoxCollider2D>(), Time.frameCount);
            else
                player1.Health.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null));

            yield return PlayModeTestHelper.WaitUntil(() => run1.IsEnded && panel1.IsVisible, 2f, $"[P07/{label}] Run1 did not finish.");

            // Double-click ReplayButton -> only ONE reload triggers
            int replayCount = 0;
            run1.ReplayRequested += () => replayCount++;
            panel1.ReplayButton.onClick.Invoke();
            panel1.ReplayButton.onClick.Invoke();
            Assert.AreEqual(1, replayCount, $"[P07/{label}] Double-click must trigger exactly 1 reload.");

            // Wait until new LevelRunController instance replaces run1
            yield return PlayModeTestHelper.WaitUntil(() =>
            {
                var current = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
                return current != null && current != run1;
            }, 10f, $"[P07/{label}] Scene did not reload new LevelRunController.");

            var run2 = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player2 = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var gate2 = UnityEngine.Object.FindAnyObjectByType<EncounterGate>();
            var chest2 = UnityEngine.Object.FindAnyObjectByType<HealingChest>();
            var panel2 = UnityEngine.Object.FindAnyObjectByType<ResultPanel>();

            // Wait for fresh intro to complete
            yield return PlayModeTestHelper.WaitForPlaying(run2);

            // Verify clean reset of entire run
            Vector2 feet2 = PlayModeTestHelper.GetPlayerFeet(player2);
            Assert.AreEqual(3f, feet2.x, 0.25f, $"[P07/{label}/Reset] Player spawn X.");
            Assert.AreEqual(0f, feet2.y, 0.25f, $"[P07/{label}/Reset] Player spawn Y.");
            Assert.AreEqual(player2.Health.Max, player2.Health.Current, $"[P07/{label}/Reset] Full HP.");
            Assert.IsFalse(panel2.IsVisible, $"[P07/{label}/Reset] Panel hidden.");
            Assert.IsTrue(chest2.HasReward, $"[P07/{label}/Reset] I3 reward restored.");
            Assert.IsFalse(gate2.IsOpen, $"[P07/{label}/Reset] I4 closed.");
            Assert.IsTrue(gate2.GateBarrierRoot.activeInHierarchy, $"[P07/{label}/Reset] I4 barrier active.");

            var enemies2 = UnityEngine.Object.FindObjectsByType<EnemyController>();
            Assert.AreEqual(34, enemies2.Length, $"[P07/{label}/Reset] All 34 enemies restored.");
            foreach (var e in enemies2)
            {
                Assert.IsFalse(e.enabled, $"[P07/{label}/Reset] {e.name} dormant.");
                Assert.AreEqual(e.Data.MaxHealth, e.GetComponent<Health>().Current, $"[P07/{label}/Reset] {e.name} full HP.");
            }

            var zones2 = UnityEngine.Object.FindObjectsByType<EncounterZone>();
            Assert.AreEqual(12, zones2.Length);
            Assert.IsTrue(zones2.All(z => !z.IsActivated), $"[P07/{label}/Reset] All 12 zones dormant.");

            // Old projectile from run 1 no longer exists
            Assert.IsTrue(oldArrow == null, $"[P07/{label}/Reset] Old projectile from previous run must be destroyed.");
            var liveProjectiles = UnityEngine.Object.FindObjectsByType<Projectile>();
            Assert.IsFalse(liveProjectiles.Any(p => p.GetEntityId() == oldArrowEntityId),
                $"[P07/{label}/Reset] Old projectile entity must not survive scene reload.");

            // Complete second run cleanly to verify no duplicate subscriptions
            int run2FinishedCount = 0;
            run2.Finished += _ => run2FinishedCount++;
            player2.Health.TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, null));
            yield return PlayModeTestHelper.WaitUntil(() => run2.IsEnded && panel2.IsVisible, 2f, $"[P07/{label}/Run2] Did not settle.");
            yield return null;
            Assert.AreEqual(1, run2FinishedCount, $"[P07/{label}/Run2] Finished must fire exactly once.");
            Assert.AreEqual(1, panel2.ShowCount, $"[P07/{label}/Run2] Panel must show exactly once.");
        }
    }
}
