using System.Collections;
using System.Linq;
using DeadCells.Combat;
using DeadCells.Currency;
using DeadCells.Enemies;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class ExpansionPlayTests : InputTestFixture
    {
        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P08_SwitchDoorTeleportGateLoreTabletGoldDropAndSecondarySewerExitWinFlow()
        {
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = Object.FindAnyObjectByType<LevelRunController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var wallet = Object.FindAnyObjectByType<GoldWallet>();
            var panel = Object.FindAnyObjectByType<ResultPanel>();
            var lore = Object.FindAnyObjectByType<LoreTablet>();
            var sewerExit = Object.FindAnyObjectByType<SecondaryBiomeExitTrigger>();
            var switchDoors = Object.FindObjectsByType<SwitchDoor>();
            var tpGates = Object.FindObjectsByType<TeleportGate>();

            yield return PlayModeTestHelper.WaitForPlaying(run);

            // 1. Teleport discovery gating at start: TP_0_Spawn cannot warp to undiscovered TP_1_EastVault
            var tp0 = tpGates.Single(t => t.name == "TP_0_Spawn");
            var tp1 = tpGates.Single(t => t.name == "TP_1_EastVault");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(15.5f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(tp0.IsDiscovered);
            Assert.IsFalse(tp1.IsDiscovered);
            Assert.IsFalse(tp0.TryTeleport(), "[P08/UndiscoveredReject] TP_0 must not warp to undiscovered TP_1.");

            // 2. Wake Zone M1, defeat Grenadier_M1 -> verifies GoldPickup drops on VFX sorting layer & credits GoldWallet
            Assert.AreEqual(0, wallet.Balance, "[P08/InitialGold]");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(82f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            var grenadierM1 = Object.FindObjectsByType<EnemyController>().Single(e => e.name == "Enemy_Grenadier_M1");
            Assert.IsTrue(grenadierM1.enabled, "[P08/ZoneM1] Enemy_Grenadier_M1 must wake in Zone M1.");
            Assert.IsTrue(grenadierM1.GetComponent<Health>().TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)));
            yield return null;
            var droppedCoin = Object.FindAnyObjectByType<GoldPickup>();
            Assert.IsNotNull(droppedCoin, "[P08/GoldDrop] Defeating enemy must spawn GoldPickup.");
            Assert.AreEqual("VFX", droppedCoin.GetComponentInChildren<SpriteRenderer>().sortingLayerName, "[P08/CoinVfxLayer]");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(92f, 3.5f));
            yield return PlayModeTestHelper.WaitUntil(() => wallet.Balance >= 20, 2.5f, "[P08/GoldCollected] GoldPickup must credit GoldWallet.");
            Assert.GreaterOrEqual(wallet.Balance, 20);

            // 3. Open SwitchDoor SD_1_EastVault at (144, 0), enter vault at (161, 0) to discover TP_1, warp back to TP_0 (15.5, 0)
            var sd1 = switchDoors.Single(d => d.name == "SD_1_EastVault");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(144f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(sd1.TryInteract(), "[P08/OpenSD1]");
            Assert.IsFalse(sd1.DoorBarrierRoot.activeInHierarchy);

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(161f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(tp1.IsDiscovered, "[P08/DiscoverTP1]");
            Assert.IsTrue(tp1.TryTeleport(), "[P08/TeleportTP1ToTP0]");
            Assert.AreEqual(15.5f, PlayModeTestHelper.GetPlayerFeet(player).x, 0.25f);

            // 4. Inspect LoreTablet I16 in Room S at (72, 24) and auto-close on walk away
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(72f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(lore.TryInteract(), "[P08/LoreOpen]");
            Assert.IsTrue(lore.IsReading);
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(50f, 24f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsFalse(lore.IsReading, "[P08/LoreAutoClose]");

            // 5. Toxic Sewers secondary exit in Room P: wake P1, kill Shield_P1 + Archer_P1, open SD_2_SewerExit, trigger win at (224, -15)
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(180f, -16f));
            yield return new WaitForFixedUpdate();
            yield return null;
            var enemies = Object.FindObjectsByType<EnemyController>();
            Assert.IsTrue(
                enemies.Single(e => e.name == "Enemy_Shield_P1").GetComponent<Health>()
                    .TakeDamage(new DamageInfo(9999, 0, 0f, 0.5f, false, player.gameObject)),
                "[P08/KillShieldP1] Directionless + stun damage must bypass front shield block.");
            Assert.IsTrue(
                enemies.Single(e => e.name == "Enemy_Archer_P1").GetComponent<Health>()
                    .TakeDamage(new DamageInfo(9999, 1, 0f, 0f, false, player.gameObject)),
                "[P08/KillArcherP1]");
            yield return null;

            var sd2 = switchDoors.Single(d => d.name == "SD_2_SewerExit");
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(206f, -16f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(sd2.TryInteract(), "[P08/OpenSD2]");
            Assert.IsTrue(sewerExit.IsUnlocked, "[P08/SewerExitUnlocked]");

            Assert.IsTrue(sewerExit.TrySubmitForCollider(player.GetComponent<BoxCollider2D>(), Time.frameCount), "[P08/SewerWinSubmit]");
            yield return PlayModeTestHelper.WaitUntil(() => run.IsEnded && panel.IsVisible, 2f, "[P08/SewerWinSettle]");
            Assert.AreEqual(LevelOutcome.Win, run.Outcome);
            Assert.AreEqual(ResultPanel.WinTitle, panel.TitleText.text);
        }
    }
}
