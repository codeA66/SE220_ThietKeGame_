using System.Collections.Generic;
using DeadCells.Combat;
using DeadCells.Player.StateMachine;
using NUnit.Framework;
using UnityEngine;

namespace DeadCells.NhaGiamGiengChuong.Tests.Unit
{
    public sealed class SwitchDoorTests
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
        public void U17_SwitchDoor_RejectsOutsideDeadOrEnded_AndOpensBarrierAtomicallyOnceWhenAliveInsideTrigger()
        {
            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            var playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.6f);
            playerCol.offset = new Vector2(0f, 0.8f);
            var health = playerGo.AddComponent<Health>();
            health.Initialize(100, 0f);

            var run = Track(new GameObject("Run")).AddComponent<LevelRunController>();
            run.Configure(null, health);

            var doorGo = Track(new GameObject("SD_1_EastVault"));
            doorGo.transform.position = new Vector3(144f, 0f, 0f);
            var trigger = doorGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.75f);
            trigger.size = new Vector2(2.5f, 1.5f);

            var barrierGo = Track(new GameObject("DoorBarrier"));
            barrierGo.transform.SetParent(doorGo.transform, false);
            var barrierCol = barrierGo.AddComponent<BoxCollider2D>();
            var barrierSr = barrierGo.AddComponent<SpriteRenderer>();

            var switchVisualGo = Track(new GameObject("SwitchVisual"));
            switchVisualGo.transform.SetParent(doorGo.transform, false);
            var switchSr = switchVisualGo.AddComponent<SpriteRenderer>();
            var offSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);
            var onSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 24f);

            var promptGo = Track(new GameObject("PromptIndicator"));
            promptGo.transform.SetParent(doorGo.transform, false);

            var sd = doorGo.AddComponent<SwitchDoor>();
            sd.Configure("SD_1", run, null, trigger, barrierGo, switchSr, offSprite, onSprite, null, promptGo, health, playerCol);

            int openEvents = 0;
            bool barrierOffAtEvent = false;
            sd.Opened += _ =>
            {
                openEvents++;
                barrierOffAtEvent = !barrierCol.gameObject.activeInHierarchy && !barrierSr.gameObject.activeInHierarchy;
            };

            // 1. Intro phase -> prompt hidden, interact rejected.
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Intro] Must reject before Playing.");
            Assert.IsFalse(sd.IsOpen);
            Assert.IsTrue(barrierGo.activeSelf);
            Assert.AreSame(offSprite, switchSr.sprite);

            // Enter Playing phase.
            run.TickIntro(new DummyPlayState(), 0f);
            sd.RefreshVisualState();
            Assert.IsTrue(sd.PromptVisible, "[U17/InTrigger] Prompt must show when alive and inside trigger.");

            // 2. Outside trigger -> rejected.
            playerGo.transform.position = new Vector3(130f, 0f, 0f);
            Physics2D.SyncTransforms();
            sd.RefreshVisualState();
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(sd.TryInteract(), "[U17/Outside] Must reject outside lever trigger.");
            Assert.IsFalse(sd.IsOpen);

            // 3. Return inside trigger and interact -> opens atomically once, swaps sprite, hides prompt.
            playerGo.transform.position = new Vector3(144f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.IsTrue(sd.TryInteract(), "[U17/ValidOpen] Expected true.");
            Assert.IsTrue(sd.IsOpen);
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(barrierOffAtEvent, "[U17/Atomic] Barrier collider and renderer must disable atomically before event.");
            Assert.IsFalse(barrierGo.activeSelf);
            Assert.AreSame(onSprite, switchSr.sprite);
            Assert.IsFalse(sd.PromptVisible);
            Assert.IsFalse(promptGo.activeSelf);

            // 4. Repeat interact -> rejected, remains open.
            Assert.IsFalse(sd.TryInteract(), "[U17/Repeat] Repeat press must return false.");
            Assert.AreEqual(1, openEvents);
            Assert.IsTrue(sd.IsOpen);

            // 5. Re-configuring restores barrier active state, re-enables barrier Collider2D, and resets sprite.
            sd.Configure("SD_1", run, null, trigger, barrierGo, switchSr, offSprite, onSprite, null, promptGo, health, playerCol);
            Assert.IsFalse(sd.IsOpen, "[U17/ReconfigureResetOpen]");
            Assert.IsTrue(barrierGo.activeSelf, "[U17/ReconfigureBarrierActive]");
            Assert.IsTrue(barrierCol.enabled, "[U17/ReconfigureColliderReenabled]");
            Assert.AreSame(offSprite, switchSr.sprite, "[U17/ReconfigureOffSprite]");
        }

        private GameObject Track(GameObject go)
        {
            _created.Add(go);
            return go;
        }
    }
}
