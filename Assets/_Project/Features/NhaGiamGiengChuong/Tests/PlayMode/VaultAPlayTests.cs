using System.Collections;
using System.Linq;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    public sealed class VaultAPlayTests : InputTestFixture
    {
        public override void TearDown()
        {
            Time.timeScale = 1f;
            CombatAudioHub.SetAmbientMultiplier(1f);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P11_VaultA_BreakEnterFrameInspectScrollsAndReturn()
        {
            yield return PlayModeTestHelper.LoadLevelScene();

            var run = Object.FindAnyObjectByType<LevelRunController>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var camFollow = Object.FindAnyObjectByType<CameraFollow2D>();
            var pauseMenu = Object.FindAnyObjectByType<PauseMenuController>();
            var masonry = Object.FindAnyObjectByType<CrackedMasonry>(FindObjectsInactive.Include);
            var passages = Object.FindObjectsByType<PassageTransition>(FindObjectsInactive.Include);
            var entry = passages.Single(p => p.name == "I22_VaultA_Entry");
            var exit = passages.Single(p => p.name == "I23_VaultA_Exit");
            var seq = Object.FindAnyObjectByType<ChamberFramingSequence>(FindObjectsInactive.Include);
            var inscription = Object.FindAnyObjectByType<WallInscription>(FindObjectsInactive.Include);
            var roomsRoot = GameObject.Find("Rooms");
            var vaultRoom = roomsRoot.transform.Find("Room_VaultA").gameObject;
            var parallaxRoot = GameObject.Find("Prison_Parallax_R5");

            yield return PlayModeTestHelper.WaitForPlaying(run);

            // 1. Initial rest state: VaultA inactive, parallax active, masonry intact, entry locked
            Assert.IsFalse(vaultRoom.activeSelf, "[P09/VaultInactiveAtRest]");
            Assert.IsTrue(parallaxRoot.activeSelf, "[P09/ParallaxActiveAtRest]");
            Assert.IsFalse(masonry.IsBroken, "[P09/MasonryIntact]");
            Assert.IsFalse(entry.IsUnlocked, "[P09/EntryLocked]");
            Assert.IsFalse(inscription.IsUnlocked, "[P09/InscriptionLocked]");

            // 2. Strike cracked masonry -> reveals fissure & unlocks entry
            Assert.IsTrue(masonry.WallHealth.TakeDamage(new DamageInfo(15, -1, 0f, 0f, false, player.gameObject)));
            yield return null;
            Assert.IsTrue(masonry.IsBroken, "[P09/MasonryBroken]");
            Assert.IsTrue(entry.IsUnlocked, "[P09/EntryUnlocked]");

            // 3. Enter fissure -> black fade, Room_VaultA active, parallax hidden, bounds swapped, free walk corridor
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(0.75f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(entry.TryBeginTransition(), "[P09/BeginEnter]");

            yield return PlayModeTestHelper.WaitUntil(
                () => vaultRoom.activeSelf && !entry.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.0f,
                "[P09/EnterComplete] Fade transition into Room_VaultA did not finish.");

            Assert.IsFalse(parallaxRoot.activeSelf, "[P09/ParallaxHiddenInside]");
            Assert.AreEqual(new Rect(176f, 27f, 28f, 24f), camFollow.CurrentBounds, "[P09/VaultBounds]");
            Assert.AreEqual(180f, player.transform.position.x, 0.25f, "[P09/ArrivalX]");
            Assert.AreEqual(29f, player.transform.position.y, 0.25f, "[P09/ArrivalY]");
            Assert.IsTrue(pauseMenu.CanPause, "[P09/PauseAllowedBeforeTrigger]");
            Assert.LessOrEqual(CombatAudioHub.AmbientMultiplier, 0.15f, "[P09/AmbientDucked]");

            // 4. Step across framing sequence trigger at x = 183.85 -> locks controls, blocks Pause, starts piano & camera focus
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(183.85f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;

            yield return PlayModeTestHelper.WaitUntil(() => seq.IsPlaying, 1.5f, "[P09/SeqStarted]");
            Assert.IsTrue(player.IsInScriptedSequence, "[P09/ControlsLocked]");
            Assert.IsFalse(pauseMenu.CanPause, "[P09/PauseBlockedDuringSeq]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P09/PianoPlaying]");

            // 5. Wait for full unskippable sequence (~6.5s) to finish -> unlocks control, Pause, & inscription
            yield return PlayModeTestHelper.WaitUntil(
                () => !seq.IsPlaying && !player.IsInScriptedSequence,
                12.0f,
                "[P09/SeqFinished] Framing sequence did not finish cleanly.");

            Assert.IsTrue(seq.HasPlayed, "[P09/SeqMarkedPlayed]");
            Assert.AreEqual(190f, player.transform.position.x, 0.35f, "[P09/PlayerStopX]");
            Assert.IsFalse(camFollow.IsFocusOverrideActive, "[P09/CameraRestored]");
            Assert.IsTrue(pauseMenu.CanPause, "[P09/PauseRestored]");
            Assert.IsTrue(inscription.IsUnlocked, "[P09/InscriptionUnlocked]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P09/PianoContinuesAfterSeq]");

            // 6. Inspect inscription quote beneath tapestry, then walk away to auto-close
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(191.5f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(inscription.TryToggleReading(), "[P09/InscriptionOpen]");
            Assert.IsTrue(inscription.IsReading);
            Assert.IsTrue(inscription.BannerRoot.activeSelf);

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(198.5f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsFalse(inscription.IsReading, "[P09/InscriptionAutoClose]");
            Assert.IsFalse(inscription.BannerRoot.activeSelf);

            // 7. Verify both VaultA PowerScrollPickups are reachable & available
            var scroll1 = GameObject.Find("I20_Scroll_VaultA1").GetComponent<PowerScrollPickup>();
            var scroll2 = GameObject.Find("I21_Scroll_VaultA2").GetComponent<PowerScrollPickup>();
            Assert.IsTrue(scroll1.IsAvailable, "[P09/Scroll1Available]");
            Assert.IsTrue(scroll2.IsAvailable, "[P09/Scroll2Available]");
            Assert.IsTrue(scroll1.TryGetDistance(new Vector2(198.5f, 29f), out _), "[P09/Scroll1Reachable]");
            Assert.IsTrue(scroll2.TryGetDistance(new Vector2(201.2f, 29f), out _), "[P09/Scroll2Reachable]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P09/PianoStillPlayingNearScrolls]");

            // 8. Exit back to Room A via return fissure -> restores Room_VaultA inactive, parallax active, main bounds & ambient
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(178.5f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(exit.TryBeginTransition(), "[P09/BeginExit]");

            yield return PlayModeTestHelper.WaitUntil(
                () => !vaultRoom.activeSelf && !exit.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.5f,
                "[P09/ExitComplete] Fade transition back to Room A did not finish.");

            Assert.IsTrue(parallaxRoot.activeSelf, "[P09/ParallaxRestored]");
            Assert.AreEqual(new Rect(-104f, -20f, 340f, 86f), camFollow.CurrentBounds, "[P09/MainBoundsRestored]");
            Assert.AreEqual(1.2f, player.transform.position.x, 0.25f, "[P09/ReturnX]");
            yield return PlayModeTestHelper.WaitUntil(() => CombatAudioHub.AmbientMultiplier >= 0.95f, 1.5f, "[P09/AmbientRestored]");

            // 9. Re-enter VaultA & cross trigger again -> one-shot guard ensures sequence does NOT replay
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(0.75f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(entry.TryBeginTransition(), "[P09/ReEnter]");
            yield return PlayModeTestHelper.WaitUntil(
                () => vaultRoom.activeSelf && !entry.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.0f,
                "[P09/ReEnterComplete]");

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(183.85f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
            Assert.IsFalse(seq.IsPlaying, "[P09/NoReplayOnSecondVisit]");
            Assert.IsFalse(player.IsInScriptedSequence, "[P09/FreeControlOnSecondVisit]");
        }
    }
}
