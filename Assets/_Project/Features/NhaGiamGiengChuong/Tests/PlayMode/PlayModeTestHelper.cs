using System;
using System.Collections;
using DeadCells.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DeadCells.NhaGiamGiengChuong.Tests.Play
{
    internal static class PlayModeTestHelper
    {
        public const string ScenePath = "Assets/_Project/Scenes/NhaGiamGiengChuong.unity";
        public const float DefaultIntroTimeout = 15f;

        public static IEnumerator LoadLevelScene()
        {
            Time.timeScale = 1f;
            AsyncOperation op;
#if UNITY_EDITOR
            op = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            op = SceneManager.LoadSceneAsync("NhaGiamGiengChuong", LoadSceneMode.Single);
#endif
            Assert.IsNotNull(op, "Failed to start async load of " + ScenePath);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!op.isDone)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail("Timed out loading scene: " + ScenePath);
                yield return null;
            }
            yield return null;
        }

        public static IEnumerator WaitForPlaying(LevelRunController run, float timeoutSeconds = DefaultIntroTimeout)
        {
            Assert.IsNotNull(run, "LevelRunController missing.");
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!run.IsPlaying)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out waiting for LevelRunController.IsPlaying (phase={run.Phase}, playerState={run.Player?.CurrentState?.GetType().Name}).");
                yield return null;
            }
        }

        public static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds, string timeoutMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(timeoutMessage);
                yield return null;
            }
        }

        public static void PositionPlayerAtFeet(PlayerController player, Vector2 feetPosition)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.position = feetPosition;
            player.transform.position = new Vector3(feetPosition.x, feetPosition.y, 0f);
            Physics2D.SyncTransforms();
        }

        public static Vector2 GetPlayerFeet(PlayerController player)
        {
            var col = player.GetComponent<BoxCollider2D>();
            return EncounterZone.GetColliderFeet(col);
        }
    }
}
