using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadCells.Combat;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.Player;
using DeadCells.Player.StateMachine.States;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class Round9PlayTests : InputTestFixture
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
        public IEnumerator P09_Round9_StairsH45SmoothWalk_ChainFlushDismountNoAirGrabAndSDescend_AndEnemyPassThrough()
        {
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = Object.FindAnyObjectByType<LevelRunController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(run, "[P09] LevelRunController missing.");
            Assert.IsNotNull(player, "[P09] PlayerController missing.");
            yield return PlayModeTestHelper.WaitForPlaying(run);

            // 1. 45-degree Stairs_H smooth walk up, mid-slope idle hold with zero slide, finish climb, and walk down
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(20f, 8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Press(_keyboard.aKey);
            float midSlopeDeadline = Time.realtimeSinceStartup + 4f;
            while (PlayModeTestHelper.GetPlayerFeet(player).x > 14.5f)
            {
                Assert.IsFalse(player.CurrentState is PlayerJumpState,
                    "[P09/StairsUpMid] Walking up 45-degree Stairs_H must not require or trigger Jump.");
                if (Time.realtimeSinceStartup > midSlopeDeadline)
                {
                    Release(_keyboard.aKey);
                    Assert.Fail($"[P09/StairsUpMid] Timed out walking up Stairs_H to mid-slope. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");
                }
                yield return null;
            }
            Release(_keyboard.aKey);

            // Let PlayerIdleState settle naturally on the 45-degree slope, then hold idle 0.35s -> zero downhill drift
            yield return PlayModeTestHelper.WaitUntil(
                () => player.Context.IsOnSlope && player.Context.Velocity.sqrMagnitude < 0.01f,
                1.5f,
                $"[P09/StairsIdleSettle] Player did not settle idle on 45-degree slope. feet={PlayModeTestHelper.GetPlayerFeet(player)}, isOnSlope={player.Context.IsOnSlope}, vel={player.Context.Velocity}");
            yield return new WaitForFixedUpdate();
            yield return null;

            Vector2 slopeStart = PlayModeTestHelper.GetPlayerFeet(player);
            yield return new WaitForSeconds(0.35f);
            Vector2 slopeAfter = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.Less(Mathf.Abs(slopeAfter.x - slopeStart.x), 0.08f,
                $"[P09/StairsIdle] Player slid horizontally on 45-degree slope while idle: start={slopeStart}, after={slopeAfter}.");
            Assert.Less(Mathf.Abs(slopeAfter.y - slopeStart.y), 0.08f,
                $"[P09/StairsIdle] Player slid vertically on 45-degree slope while idle: start={slopeStart}, after={slopeAfter}.");

            // Resume walking up to Room J top landing (x < 7, y = 16.0 ± 0.15)
            Press(_keyboard.aKey);
            float climbStairsDeadline = Time.realtimeSinceStartup + 5f;
            while (PlayModeTestHelper.GetPlayerFeet(player).x >= 7f)
            {
                Assert.IsFalse(player.CurrentState is PlayerJumpState,
                    "[P09/StairsUp] Walking up 45-degree Stairs_H must not require or trigger Jump.");
                if (Time.realtimeSinceStartup > climbStairsDeadline)
                {
                    Release(_keyboard.aKey);
                    Assert.Fail($"[P09/StairsUp] Timed out walking up Stairs_H to Room J. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");
                }
                yield return null;
            }
            Release(_keyboard.aKey);
            yield return new WaitForFixedUpdate();
            yield return null;

            Vector2 topLandingFeet = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.AreEqual(16.0f, topLandingFeet.y, 0.15f,
                $"[P09/StairsUp] Expected Room J top landing y=16.0 ± 0.15, got {topLandingFeet}.");

            // Walk down Stairs_H to bottom landing (x > 19, y = 8.0 ± 0.15)
            Press(_keyboard.dKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => PlayModeTestHelper.GetPlayerFeet(player).x > 19f,
                5f,
                $"[P09/StairsDown] Timed out walking down Stairs_H. feet={PlayModeTestHelper.GetPlayerFeet(player)}");
            Release(_keyboard.dKey);
            yield return new WaitForFixedUpdate();
            yield return null;
            Vector2 bottomLandingFeet = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.AreEqual(8.0f, bottomLandingFeet.y, 0.15f,
                $"[P09/StairsDown] Expected bottom landing y=8.0 ± 0.15, got {bottomLandingFeet}.");

            // 2. Chain I1_Chain_C flush dismount + no W air re-grab + S crouch-descend (no Space)
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(46f, 0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            bool enteredClimb = false;
            Press(_keyboard.wKey);
            float chainDeadline = Time.realtimeSinceStartup + 6f;
            while (true)
            {
                bool isClimbing = player.CurrentState is PlayerClimbState;
                if (isClimbing)
                    enteredClimb = true;
                if (enteredClimb && !isClimbing && player.Context.IsGrounded && PlayModeTestHelper.GetPlayerFeet(player).y >= 7.8f)
                    break;
                if (Time.realtimeSinceStartup > chainDeadline)
                {
                    Release(_keyboard.wKey);
                    Assert.Fail($"[P09/ChainTop] Timed out dismounting I1_Chain_C onto OneWay_C_Top. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");
                }
                yield return null;
            }
            Assert.IsTrue(enteredClimb, "[P09/ChainTop] Must enter PlayerClimbState on I1_Chain_C.");

            Vector2 dismountFeet = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.AreEqual(8.0f, dismountFeet.y, 0.12f,
                $"[P09/ChainFlush] Top dismount must settle flush on deck y=8.0 ± 0.12 (no floating at 8.5), got {dismountFeet}.");

            // Continuing to hold W on OneWay_C_Top must NOT re-grab the chain in air
            float holdWEnd = Time.realtimeSinceStartup + 0.14f;
            while (Time.realtimeSinceStartup < holdWEnd)
            {
                Assert.IsTrue(player.Context.IsGrounded, "[P09/NoAirRegrabHoldW] Must remain grounded on OneWay_C_Top.");
                Assert.IsFalse(player.CurrentState is PlayerClimbState, "[P09/NoAirRegrabHoldW] Holding W at deck top must not re-grab chain.");
                yield return null;
            }
            Release(_keyboard.wKey);
            yield return null;

            // Fresh W press while standing on OneWay_C_Top must also stay grounded
            Press(_keyboard.wKey);
            float tapWEnd = Time.realtimeSinceStartup + 0.14f;
            while (Time.realtimeSinceStartup < tapWEnd)
            {
                Assert.IsTrue(player.Context.IsGrounded, "[P09/NoAirRegrabTapW] Must remain grounded on OneWay_C_Top.");
                Assert.IsFalse(player.CurrentState is PlayerClimbState, "[P09/NoAirRegrabTapW] Pressing W on deck top must not re-grab chain.");
                yield return null;
            }
            Release(_keyboard.wKey);
            yield return null;

            // Hold S (without Space) -> enters PlayerClimbState and descends below y = 7.4f
            Press(_keyboard.sKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => player.CurrentState is PlayerClimbState,
                1.2f,
                $"[P09/ChainDescend] Holding S above I1_Chain_C must enter PlayerClimbState without Space. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");
            yield return new WaitForSeconds(0.30f);
            Vector2 descendFeet = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.IsTrue(player.CurrentState is PlayerClimbState,
                $"[P09/ChainDescend] Expected PlayerClimbState while descending, got {player.CurrentState?.GetType().Name}.");
            Assert.Less(descendFeet.y, 7.4f,
                $"[P09/ChainDescend] Expected feet.y < 7.4 after 0.30s descend, got {descendFeet}.");
            Release(_keyboard.sKey);
            yield return null;

            // Exit PlayerClimbState cleanly on flat Stairs_H top [6..10]@16 (outside Zone J1) before dropping into J1
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(8f, 16f));
            yield return new WaitForFixedUpdate();
            yield return null;

            // 3. Enemy pass-through (Enemy_Shield_J) + rear melee damage
            var zoneJ1 = Object.FindObjectsByType<EncounterZone>().Single(z => z.ZoneId == "J1");
            var shieldJ = Object.FindObjectsByType<EnemyController>().Single(e => e.name == "Enemy_Shield_J");
            var shieldHealth = shieldJ.GetComponent<Health>();

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(-4f, 18.5f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(zoneJ1.IsActivated, "[P09/ZoneJ1] Zone J1 must activate.");
            Assert.IsTrue(shieldJ.enabled, "[P09/ZoneJ1] Enemy_Shield_J must wake.");

            yield return PlayModeTestHelper.WaitUntil(
                () => player.Context.IsGrounded && Mathf.Abs(PlayModeTestHelper.GetPlayerFeet(player).y - 16.0f) <= 0.15f,
                0.8f,
                $"[P09/EnemyPassThrough] Player must fall through Enemy_Shield_J onto floor y=16.0 ± 0.15 within 0.8s (not stand on head at ~17.6). feet={PlayModeTestHelper.GetPlayerFeet(player)}");

            var shieldSr = shieldJ.GetComponentInChildren<SpriteRenderer>();
            int shieldFacing = shieldSr != null && shieldSr.flipX ? -1 : 1;
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(shieldJ.transform.position.x - shieldFacing * 1.15f, 16f));
            yield return new WaitForFixedUpdate();
            yield return null;

            var faceKey = shieldFacing > 0 ? _keyboard.dKey : _keyboard.aKey;
            Press(faceKey);
            yield return null;
            Release(faceKey);
            yield return null;

            int shieldHpBefore = shieldHealth.Current;
            yield return TapAttack();
            yield return PlayModeTestHelper.WaitUntil(
                () => shieldHealth.Current < shieldHpBefore,
                1.0f,
                $"[P09/ShieldRearSlash] Slashing Enemy_Shield_J from behind must deal damage. hp={shieldHealth.Current}/{shieldHpBefore}");
        }

        [UnityTest]
        public IEnumerator P10_Round9_HackAndSlashFeel_PersistentLunge_ContinuousCombo_KnockbackPop_RoomNGold_AndPromenadeExit()
        {
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = Object.FindAnyObjectByType<LevelRunController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var wallet = Object.FindAnyObjectByType<GoldWallet>();
            var panel = Object.FindAnyObjectByType<ResultPanel>();
            Assert.IsNotNull(run, "[P10] LevelRunController missing.");
            Assert.IsNotNull(player, "[P10] PlayerController missing.");
            Assert.IsNotNull(wallet, "[P10] GoldWallet missing.");
            Assert.IsNotNull(panel, "[P10] ResultPanel missing.");
            yield return PlayModeTestHelper.WaitForPlaying(run);

            // 1. Persistent lunge + continuous 1 -> 2 -> 3 -> 1 combo + single StrikePerformed per swing
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Press(_keyboard.dKey);
            yield return null;
            Release(_keyboard.dKey);
            yield return null;

            var swingStates = new List<string>();
            player.Context.Combat.StrikePerformed += (strike, _, _) => swingStates.Add(strike.AnimState);

            float startX = PlayModeTestHelper.GetPlayerFeet(player).x;
            yield return TapAttack();
            yield return PlayModeTestHelper.WaitUntil(() => swingStates.Count >= 1, 1.0f, "[P10/Combo] Hit 1 StrikePerformed did not fire.");

            // Buffer Hit 2 immediately, then sample Hit 1 forward lunge across full 0.095s lunge duration before Hit 2 strikes
            yield return TapAttack();
            yield return new WaitForSeconds(0.10f);
            float hit1Dx = PlayModeTestHelper.GetPlayerFeet(player).x - startX;
            Assert.Greater(hit1Dx, 0.22f, $"[P10/Lunge] Hit 1 persistent forward lunge must carry dx > 0.22u, got {hit1Dx:F3}.");

            yield return WaitWhileAttackingUntil(() => swingStates.Count >= 2, 1.0f, player, "[P10/Combo] Hit 2 failed or dropped to Idle.");
            yield return TapAttack();
            yield return WaitWhileAttackingUntil(() => swingStates.Count >= 3, 1.0f, player, "[P10/Combo] Hit 3 failed or dropped to Idle.");
            yield return TapAttack();
            yield return WaitWhileAttackingUntil(() => swingStates.Count >= 4, 1.0f, player, "[P10/Combo3To1] Buffered Hit 4 must chain directly from Hit 3 back to Hit 1 without dropping to Idle.");

            // Wait past ActiveHitWindow (0.065s) to confirm StrikePerformed fires strictly once per swing
            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual(4, swingStates.Count,
                $"[P10/SingleSwingEvent] Expected exactly 4 StrikePerformed events for 4 swings, got {swingStates.Count} ({string.Join(", ", swingStates)}).");
            CollectionAssert.AreEqual(new[] { "AttackA", "AttackB", "AttackC", "AttackA" }, swingStates,
                "[P10/ComboOrder] Expected continuous 1 -> 2 -> 3 -> 1 combo sequence.");

            yield return PlayModeTestHelper.WaitUntil(
                () => !(player.CurrentState is PlayerAttackState),
                1.0f,
                "[P10/ComboEnd] Player did not exit PlayerAttackState after finishing combo.");

            float totalComboDx = PlayModeTestHelper.GetPlayerFeet(player).x - startX;
            Assert.Greater(totalComboDx, 0.90f,
                $"[P10/ComboForwardAdvance] Continuous 4-hit combo must advance total dx > 0.90u, got {totalComboDx:F3}.");

            // 2. Knockback horizontal push + upward pop (vy > 1.0f) on Enemy_Zombie_B
            var zombieB = Object.FindObjectsByType<EnemyController>().Single(e => e.name == "Enemy_Zombie_B");
            var zombieHealth = zombieB.GetComponent<Health>();
            var zombieRb = zombieB.GetComponent<Rigidbody2D>();

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(27.4f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(zombieB.enabled, "[P10/ZoneB1] Enemy_Zombie_B must wake in Zone B1.");

            Press(_keyboard.dKey);
            yield return null;
            Release(_keyboard.dKey);
            yield return null;

            float maxZombieVx = 0f;
            float maxZombieVy = 0f;
            void RecordZombieHit(Health victim, DamageInfo _)
            {
                if (victim != zombieHealth)
                    return;
                maxZombieVx = Mathf.Max(maxZombieVx, Mathf.Abs(zombieRb.linearVelocity.x));
                maxZombieVy = Mathf.Max(maxZombieVy, zombieRb.linearVelocity.y);
            }

            player.Context.Combat.TargetHit += RecordZombieHit;
            try
            {
                int zombieHpBefore = zombieHealth.Current;
                yield return TapAttack();
                float knockDeadline = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < knockDeadline)
                {
                    maxZombieVx = Mathf.Max(maxZombieVx, Mathf.Abs(zombieRb.linearVelocity.x));
                    maxZombieVy = Mathf.Max(maxZombieVy, zombieRb.linearVelocity.y);
                    if (zombieHealth.Current < zombieHpBefore && maxZombieVx > 1.0f && maxZombieVy > 1.0f)
                        break;
                    yield return null;
                }
                Assert.Less(zombieHealth.Current, zombieHpBefore, "[P10/ZombieHit] Slash must damage Enemy_Zombie_B.");
                Assert.Greater(maxZombieVx, 1.0f, $"[P10/KnockbackX] Expected horizontal knockback > 1.0 u/s, got {maxZombieVx:F2}.");
                Assert.Greater(maxZombieVy, 1.0f, $"[P10/KnockbackPopY] Expected upward knockback pop vy > 1.0 u/s, got {maxZombieVy:F2}.");
            }
            finally
            {
                player.Context.Combat.TargetHit -= RecordZombieHit;
            }

            yield return PlayModeTestHelper.WaitUntil(() => Time.timeScale >= 0.99f, 1.0f, "[P10/HitStopResume] HitStop did not restore timeScale.");

            // 3. Room N LoreTablet & 4-coin GoldCache_N payoff (>= 100 gold)
            var northLore = Object.FindObjectsByType<LoreTablet>(FindObjectsInactive.Include)
                .Single(t => t.name == "I19_LoreTablet_NorthGallery");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(152.5f, 8f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(northLore.TryInteract(), "[P10/RoomNLore] I19_LoreTablet_NorthGallery.TryInteract() must open lore banner.");
            Assert.IsTrue(northLore.IsReading, "[P10/RoomNLore] Expected IsReading == true.");
            Assert.IsNotNull(northLore.LoreBannerRoot, "[P10/RoomNLore] Missing LoreBannerRoot.");
            Assert.IsTrue(northLore.LoreBannerRoot.activeInHierarchy, "[P10/RoomNLore] LoreBannerRoot must be active.");

            int goldBefore = wallet.Balance;
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(159f, 8f));
            yield return PlayModeTestHelper.WaitUntil(
                () => wallet.Balance - goldBefore >= 100,
                3.5f,
                $"[P10/RoomNGold] Expected GoldWallet.Balance to gain >= 100 from GoldCache_N, gained {wallet.Balance - goldBefore}.");
            Assert.GreaterOrEqual(wallet.Balance - goldBefore, 100);

            // 4. Room T Promenade Exit: locked while Shield_T1 & Grenadier_T1 live, unlocks on both deaths, settles Win
            var promenadeExit = Object.FindObjectsByType<SecondaryBiomeExitTrigger>(FindObjectsInactive.Include)
                .Single(e => e.name == "I18_PromenadeExitTrigger");
            var shieldT1 = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include)
                .Single(e => e.name == "Enemy_Shield_T1");
            var grenadierT1 = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include)
                .Single(e => e.name == "Enemy_Grenadier_T1");

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(126f, 40f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsFalse(promenadeExit.IsUnlocked, "[P10/PromenadeLocked] Must stay locked while guardians live.");
            Assert.IsFalse(promenadeExit.TrySubmitForCollider(player.GetComponent<BoxCollider2D>(), Time.frameCount),
                "[P10/PromenadeLockedSubmit] Locked exit must reject win submission.");

            Assert.IsTrue(
                shieldT1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 0, 0f, 0.5f, false, player.gameObject)),
                "[P10/KillShieldT1]");
            yield return null;
            Assert.IsFalse(promenadeExit.IsUnlocked, "[P10/PromenadeHalfLocked] Must stay locked until both guardians die.");

            Assert.IsTrue(
                grenadierT1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)),
                "[P10/KillGrenadierT1]");
            yield return null;
            Assert.IsTrue(promenadeExit.IsUnlocked, "[P10/PromenadeUnlocked] Must unlock after both guardians die.");
            Assert.IsFalse(run.IsEnded, "[P10/PromenadeNoAutoWin] Standing at unlocked Promenade exit must wait for [E].");
            Assert.IsTrue(promenadeExit.PromptVisible, "[P10/PromenadePromptVisible] Unlocked Promenade exit must show [E] prompt.");

            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);

            yield return PlayModeTestHelper.WaitUntil(() => run.IsEnded && panel.IsVisible, 2f, "[P10/PromenadeWinSettle]");
            Assert.AreEqual(LevelOutcome.Win, run.Outcome);
            Assert.AreEqual(ResultPanel.WinTitle, panel.TitleText.text);
        }

        private IEnumerator TapAttack()
        {
            Press(_keyboard.enterKey);
            yield return null;
            Release(_keyboard.enterKey);
        }

        private static IEnumerator WaitWhileAttackingUntil(
            System.Func<bool> predicate,
            float timeoutSeconds,
            PlayerController player,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                Assert.IsTrue(player.CurrentState is PlayerAttackState,
                    $"{failureMessage} Dropped out of PlayerAttackState into {player.CurrentState?.GetType().Name}.");
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"{failureMessage} (timed out)");
                yield return null;
            }
        }
    }
}
