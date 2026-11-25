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
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator P11_VaultA_BreakEnterFrameInspectScrollsAndReturn()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
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
            Assert.IsFalse(vaultRoom.activeSelf, "[P11/VaultInactiveAtRest]");
            Assert.IsTrue(parallaxRoot.activeSelf, "[P11/ParallaxActiveAtRest]");
            Assert.IsFalse(masonry.IsBroken, "[P11/MasonryIntact]");
            Assert.IsFalse(entry.IsUnlocked, "[P11/EntryLocked]");
            Assert.IsFalse(inscription.IsUnlocked, "[P11/InscriptionLocked]");

            // 2. Strike cracked masonry -> reveals fissure & unlocks entry
            Assert.IsTrue(masonry.WallHealth.TakeDamage(new DamageInfo(15, -1, 0f, 0f, false, player.gameObject)));
            yield return null;
            Assert.IsTrue(masonry.IsBroken, "[P11/MasonryBroken]");
            Assert.IsTrue(entry.IsUnlocked, "[P11/EntryUnlocked]");

            // 3. Tap real Keyboard [E] at fissure (Hold interaction action!) + verify rapid [E] mash during fade is ignored
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(0.75f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);

            yield return PlayModeTestHelper.WaitUntil(
                () => entry.Fader.IsTransitioning,
                1.0f,
                "[P11/RealETapStartedEntry] Tapping Keyboard E must trigger PassageTransition (Hold action uses .started).");
            // Mash [E] mid-fade: must be ignored
            Assert.IsFalse(entry.TryBeginTransition(), "[P11/ReentrantEBlocked]");

            yield return PlayModeTestHelper.WaitUntil(
                () => vaultRoom.activeSelf && !entry.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.0f,
                "[P11/EnterComplete] Fade transition into Room_VaultA did not finish.");

            Assert.IsFalse(parallaxRoot.activeSelf, "[P11/ParallaxHiddenInside]");
            Assert.AreEqual(new Rect(176f, 27f, 28f, 24f), camFollow.CurrentBounds, "[P11/VaultBounds]");
            Assert.AreEqual(180f, player.transform.position.x, 0.25f, "[P11/ArrivalX]");
            Assert.AreEqual(29f, player.transform.position.y, 0.25f, "[P11/ArrivalY]");
            Assert.IsTrue(pauseMenu.CanPause, "[P11/PauseAllowedBeforeTrigger]");
            Assert.LessOrEqual(CombatAudioHub.AmbientMultiplier, 0.15f, "[P11/AmbientDucked]");

            // 4. Airborne entry into framing trigger (x = 183.85, y = 30.8) -> locks controls, lands cleanly, walks to 190
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(183.85f, 30.8f));
            yield return new WaitForFixedUpdate();
            yield return null;

            yield return PlayModeTestHelper.WaitUntil(() => seq.IsPlaying, 1.5f, "[P11/SeqStarted]");
            Assert.IsTrue(player.IsInScriptedSequence, "[P11/ControlsLocked]");
            Assert.IsFalse(pauseMenu.CanPause, "[P11/PauseBlockedDuringSeq]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P11/PianoPlaying]");

            // 5. Wait for full unskippable sequence (~6.5s) to finish -> unlocks control, Pause, & inscription
            yield return PlayModeTestHelper.WaitUntil(
                () => !seq.IsPlaying && !player.IsInScriptedSequence,
                12.0f,
                "[P11/SeqFinished] Framing sequence did not finish cleanly.");

            Assert.IsTrue(seq.HasPlayed, "[P11/SeqMarkedPlayed]");
            Assert.AreEqual(188.8f, player.transform.position.x, 0.35f, "[P11/PlayerStopX]");
            Assert.AreEqual(29f, player.transform.position.y, 0.25f, "[P11/PlayerGroundedAfterAirborneTrigger]");
            Assert.IsFalse(camFollow.IsFocusOverrideActive, "[P11/CameraRestored]");
            Assert.IsTrue(pauseMenu.CanPause, "[P11/PauseRestored]");
            Assert.IsTrue(inscription.IsUnlocked, "[P11/InscriptionUnlocked]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P11/PianoContinuesAfterSeq]");

            // 6. Tap real Keyboard [E] at inscription beneath tapestry, then walk away to auto-close
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(190.0f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;
            Assert.IsTrue(inscription.IsReading, "[P11/RealETapOpensQuote]");
            Assert.IsTrue(inscription.BannerRoot.activeSelf);

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(199.6f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsFalse(inscription.IsReading, "[P11/InscriptionAutoClose]");
            Assert.IsFalse(inscription.BannerRoot.activeSelf);

            // 7. Pick up I20_Scroll_VaultA1 via real PowerScrollInteraction + ScrollUpgradeMenu -> piano & VaultA bounds stay intact
            var scroll1 = GameObject.Find("I20_Scroll_VaultA1").GetComponent<PowerScrollPickup>();
            var scroll2 = GameObject.Find("I21_Scroll_VaultA2").GetComponent<PowerScrollPickup>();
            var scrollMenu = Object.FindAnyObjectByType<DeadCells.HUD.ScrollUpgradeMenu>(FindObjectsInactive.Include);
            var scrollInteraction = Object.FindAnyObjectByType<PowerScrollInteraction>();
            Assert.IsNotNull(scrollInteraction, "[P11/PowerScrollInteraction]");
            Assert.IsTrue(scroll1.IsAvailable, "[P11/Scroll1Available]");
            Assert.IsTrue(scroll2.IsAvailable, "[P11/Scroll2Available]");
            Assert.IsTrue(scrollInteraction.Tick(new Vector2(199.6f, 29f), true, true, player.RunStats), "[P11/BeginScrollChoice]");
            yield return null;
            scrollMenu.ChooseBrutality();
            yield return null;
            Assert.IsFalse(scroll1.IsAvailable, "[P11/Scroll1Consumed]");
            Assert.AreEqual(new Rect(176f, 27f, 28f, 24f), camFollow.CurrentBounds, "[P11/BoundsIntactAfterScroll]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P11/PianoStillPlayingAfterScroll]");

            // 8. Tap real Keyboard [E] at return fissure -> restores Room_VaultA inactive, parallax active, main bounds & ambient
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(178.5f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);

            yield return PlayModeTestHelper.WaitUntil(
                () => !vaultRoom.activeSelf && !exit.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.5f,
                "[P11/ExitComplete] Fade transition back to Room A did not finish.");

            Assert.IsTrue(parallaxRoot.activeSelf, "[P11/ParallaxRestored]");
            Assert.AreEqual(new Rect(-104f, -20f, 340f, 86f), camFollow.CurrentBounds, "[P11/MainBoundsRestored]");
            Assert.AreEqual(1.2f, player.transform.position.x, 0.25f, "[P11/ReturnX]");
            yield return PlayModeTestHelper.WaitUntil(() => CombatAudioHub.AmbientMultiplier >= 0.95f, 1.5f, "[P11/AmbientRestored]");

            // 9. Re-enter VaultA -> hides parallax, ducks ambient, fades piano in, keeps inscription unlocked, NO replay on trigger
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(0.75f, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return PlayModeTestHelper.WaitUntil(
                () => vaultRoom.activeSelf && !entry.Fader.IsTransitioning && !player.IsInScriptedSequence,
                4.0f,
                "[P11/ReEnterComplete]");

            Assert.IsFalse(parallaxRoot.activeSelf, "[P11/SecondVisitParallaxHidden]");
            Assert.LessOrEqual(CombatAudioHub.AmbientMultiplier, 0.15f, "[P11/SecondVisitAmbientDucked]");
            Assert.IsTrue(seq.ChamberAudio.isPlaying, "[P11/SecondVisitPianoPlaying]");
            Assert.IsTrue(inscription.IsUnlocked, "[P11/SecondVisitInscriptionStaysUnlocked]");

            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(183.85f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
            Assert.IsFalse(seq.IsPlaying, "[P11/NoReplayOnSecondVisit]");
            Assert.IsFalse(player.IsInScriptedSequence, "[P11/FreeControlOnSecondVisit]");

            // Clean exit at end of test so TearDown doesn't mask ambient leak
            PlayModeTestHelper.PositionPlayerAtFeet(player, new Vector2(178.5f, 29f));
            yield return new WaitForFixedUpdate();
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return PlayModeTestHelper.WaitUntil(() => !vaultRoom.activeSelf && !exit.Fader.IsTransitioning, 4.5f, "[P11/FinalExit]");
            yield return PlayModeTestHelper.WaitUntil(() => CombatAudioHub.AmbientMultiplier >= 0.95f, 1.5f, "[P11/FinalAmbientRestored]");
        }
    }
}
