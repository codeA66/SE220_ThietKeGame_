using System;
using System.Collections;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class PilotPlayTests : InputTestFixture
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
        public IEnumerator P01_ClimbWellI1ToFloorD_AndDropBackToFloorC()
        {
            string gate = Environment.GetEnvironmentVariable("NGGC_QA_GATE");
            if (string.IsNullOrEmpty(gate))
                gate = "Q1";
            if (gate != "Q1" && gate != "Q-M1")
                Assert.Fail($"Invalid NGGC_QA_GATE='{gate}'. Expected Q-M1 or Q1.");

            yield return PlayModeTestHelper.LoadLevelScene();

            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(run, "[P01] LevelRunController missing.");
            Assert.IsNotNull(player, "[P01] PlayerController missing.");

            yield return PlayModeTestHelper.WaitForPlaying(run);

            Vector2 spawnFeet = PlayModeTestHelper.GetPlayerFeet(player);
            if (gate == "Q-M1")
            {
                Assert.AreEqual(46f, spawnFeet.x, 0.25f, "[P01/Q-M1] Initial spawn X expected ~46.");
                Assert.AreEqual(0f, spawnFeet.y, 0.25f, "[P01/Q-M1] Initial spawn Y expected ~0.");
            }
            else
            {
                Assert.AreEqual(3f, spawnFeet.x, 0.25f, "[P01/Q1] Initial spawn X expected ~3.");
                Assert.AreEqual(0f, spawnFeet.y, 0.25f, "[P01/Q1] Initial spawn Y expected ~0.");
                // One-time setup positioning at base of I1 for Q1 pilot route check
                PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(46f, 0f));
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            // 1. Hold Up (W) at x=46 to climb chain I1 past rest platform y=4.5 up past top of chain (y=8.5) so Climb exits onto y=8
            bool passedRest = false;
            bool enteredClimb = false;
            Press(_keyboard.wKey);
            float climbDeadline = Time.realtimeSinceStartup + 8f;
            while (true)
            {
                float y = PlayModeTestHelper.GetPlayerFeet(player).y;
                bool isClimbing = player.CurrentState is DeadCells.Player.StateMachine.States.PlayerClimbState;
                if (isClimbing)
                    enteredClimb = true;
                if (y > 4.6f)
                    passedRest = true;
                if (enteredClimb && !isClimbing && y >= 8.0f)
                    break;
                if (Time.realtimeSinceStartup > climbDeadline)
                    Assert.Fail($"[P01/ClimbUp] Timed out climbing I1. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");
                yield return null;
            }
            Release(_keyboard.wKey);
            Assert.IsTrue(enteredClimb, "[P01/ClimbUp] Must enter PlayerClimbState on I1.");
            Assert.IsTrue(passedRest, "[P01/ClimbUp] Must pass y=4.5 rest platform.");

            // Settle onto y=8 top of C
            yield return PlayModeTestHelper.WaitUntil(
                () => player.Context.IsGrounded && Mathf.Abs(PlayModeTestHelper.GetPlayerFeet(player).y - 8f) < 0.25f,
                3f,
                $"[P01/LandTopC] Failed to land on y=8. feet={PlayModeTestHelper.GetPlayerFeet(player)}");

            // 2. Walk Left (A) through C<->D opening x=42 onto solid Floor D (x < 39)
            Press(_keyboard.aKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => PlayModeTestHelper.GetPlayerFeet(player).x < 39f,
                4f,
                $"[P01/WalkIntoD] Blocked crossing x=42 into D. feet={PlayModeTestHelper.GetPlayerFeet(player)}");
            Release(_keyboard.aKey);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.AreEqual(8f, PlayModeTestHelper.GetPlayerFeet(player).y, 0.25f, "[P01/OnFloorD] Solid Floor D must support Player at y~8.");

            // 3. Walk Right (D) back to I1 at x=46
            Press(_keyboard.dKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => PlayModeTestHelper.GetPlayerFeet(player).x >= 45.7f,
                4f,
                $"[P01/ReturnToC] Blocked returning to x=46. feet={PlayModeTestHelper.GetPlayerFeet(player)}");
            Release(_keyboard.dKey);
            yield return new WaitForFixedUpdate();
            yield return null;

            // 4. Drop down from y=8 using Down (S) + Jump (Space), repeating if landing on rest platform y=4.5, down to y=0
            float dropDeadline = Time.realtimeSinceStartup + 10f;
            while (PlayModeTestHelper.GetPlayerFeet(player).y > 0.3f || !player.Context.IsGrounded)
            {
                if (Time.realtimeSinceStartup > dropDeadline)
                    Assert.Fail($"[P01/DropDown] Timed out dropping to floor C y=0. feet={PlayModeTestHelper.GetPlayerFeet(player)}, state={player.CurrentState?.GetType().Name}");

                if (player.Context.IsGrounded && PlayModeTestHelper.GetPlayerFeet(player).y > 0.5f)
                {
                    float startY = PlayModeTestHelper.GetPlayerFeet(player).y;
                    Press(_keyboard.sKey);
                    yield return PlayModeTestHelper.WaitUntil(
                        () => player.CurrentState is DeadCells.Player.StateMachine.States.PlayerCrouchState,
                        2f,
                        $"[P01/DropDown/Crouch] Did not enter PlayerCrouchState at y={startY:F2}.");

                    Press(_keyboard.spaceKey);
                    yield return PlayModeTestHelper.WaitUntil(
                        () => player.CurrentState is DeadCells.Player.StateMachine.States.PlayerFallState,
                        2f,
                        $"[P01/DropDown/StartFall] TryDropThrough did not enter PlayerFallState at y={startY:F2}.");

                    // Release Jump first while still holding Down, let 1 frame clear JumpPressed, then release Down
                    Release(_keyboard.spaceKey);
                    yield return null;
                    yield return null;
                    Release(_keyboard.sKey);

                    // Wait for Player to drop below startY and settle on lower platform/floor
                    yield return PlayModeTestHelper.WaitUntil(
                        () => PlayModeTestHelper.GetPlayerFeet(player).y < startY - 0.5f && player.Context.IsGrounded,
                        4f,
                        $"[P01/DropDown/Fall] Failed to land below y={startY:F2}.");
                }
                else
                {
                    yield return null;
                }
            }

            // The loop above gates on 0.3, wider than the assertion's tolerance, so it can exit while the
            // Rigidbody2D interpolation is still trailing the body by up to one fixed step (~0.4 u at the
            // 20 u/s fall clamp). GetPlayerFeet reads the interpolated transform, so sample one settled step later.
            yield return new WaitForFixedUpdate();
            yield return null;

            Vector2 finalFeet = PlayModeTestHelper.GetPlayerFeet(player);
            Assert.AreEqual(0f, finalFeet.y, 0.25f, $"[P01/LandBottomC] Expected y~0, measured={finalFeet}.");
            Assert.GreaterOrEqual(finalFeet.x, 42f, "[P01/LandBottomC] Must land inside C.");
            Assert.LessOrEqual(finalFeet.x, 56f, "[P01/LandBottomC] Must land inside C.");
        }
    }
}
