using System.Collections;
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
    public sealed class ChestPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P03_HealingChestI3_WorksViaRealKeyboardAndGamepadInputWithRatAlive()
        {
            // Variant 1: Keyboard E key
            yield return RunChestInputVariant("KeyboardE", useGamepad: false);

            // Variant 2: Gamepad buttonNorth
            yield return RunChestInputVariant("GamepadButtonNorth", useGamepad: true);
        }

        private IEnumerator RunChestInputVariant(string label, bool useGamepad)
        {
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = UnityEngine.Object.FindAnyObjectByType<LevelRunController>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var chest = UnityEngine.Object.FindAnyObjectByType<HealingChest>();
            var ratG = UnityEngine.Object.FindObjectsByType<EnemyController>()
                .Single(e => e.name == "Enemy_Rat_G");
            Assert.IsNotNull(chest, $"[P03/{label}] Missing I3 HealingChest.");

            yield return PlayModeTestHelper.WaitForPlaying(run);

            // Confirm Rat G is alive throughout
            Assert.IsTrue(ratG.GetComponent<Health>().IsAlive, $"[P03/{label}] Rat G must be alive.");

            // 1. Outside trigger zone: press Interact -> no effect
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(66f, 0f));
            yield return new WaitForFixedUpdate();
            yield return TapInteract(useGamepad);
            Assert.IsTrue(chest.HasReward, $"[P03/{label}/Outside] Reward untouched.");
            Assert.AreEqual(string.Empty, chest.LastFeedbackMessage);

            // 2. Inside trigger at full HP: press Interact -> "Đầy máu", reward preserved
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(72f, 0f));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(player.Health.Max, player.Health.Current);

            string feedback = null;
            int heals = 0;
            chest.FeedbackShown += m => feedback = m;
            chest.Healed += () => heals++;

            yield return TapInteract(useGamepad);
            Assert.AreEqual(HealingChest.FullHealthMessage, feedback, $"[P03/{label}/FullHP] Expected 'Đầy máu'.");
            Assert.IsTrue(chest.HasReward, $"[P03/{label}/FullHP] Reward must remain.");
            Assert.AreEqual(0, heals);

            // 3. Take damage inside trigger -> prompt appears; press Interact -> restores full HP once, prompt & glow off
            Assert.IsTrue(player.Health.TakeDamage(new DamageInfo(35, 1, 0f, 0f, false, null)));
            yield return null;
            Assert.IsTrue(chest.PromptVisible, $"[P03/{label}/Damaged] Prompt must be visible.");

            yield return TapInteract(useGamepad);
            Assert.AreEqual(player.Health.Max, player.Health.Current, $"[P03/{label}/Healed] Must restore to Max HP.");
            Assert.AreEqual(1, heals, $"[P03/{label}/Healed] Heal count expected=1.");
            Assert.IsTrue(chest.IsConsumed);
            Assert.IsFalse(chest.PromptVisible);
            Assert.IsFalse(chest.GlowVisible);
            Assert.IsTrue(ratG.GetComponent<Health>().IsAlive, $"[P03/{label}/RatAlive] Rat still alive.");

            // 4. Take damage again (after i-frames) and press Interact again -> stays damaged, no second heal
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(player.Health.TakeDamage(new DamageInfo(25, 1, 0f, 0f, false, null)));
            int expectedDamagedHp = player.Health.Current;
            Assert.Less(expectedDamagedHp, player.Health.Max);

            yield return TapInteract(useGamepad);
            Assert.AreEqual(expectedDamagedHp, player.Health.Current, $"[P03/{label}/SecondDamage] Must stay damaged.");
            Assert.AreEqual(1, heals);
        }

        private IEnumerator TapInteract(bool useGamepad)
        {
            // Ensure we are on DynamicUpdate before injecting InputSystem events
            yield return null;
            if (useGamepad)
            {
                Press(_gamepad.buttonNorth);
                yield return null;
                Release(_gamepad.buttonNorth);
                yield return null;
            }
            else
            {
                Press(_keyboard.eKey);
                yield return null;
                Release(_keyboard.eKey);
                yield return null;
            }
        }
    }
}
