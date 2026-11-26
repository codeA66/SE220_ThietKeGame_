using DeadCells.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadCells.RunFlow
{
    [DisallowMultipleComponent]
    public sealed class RunSceneRestart : MonoBehaviour
    {
        [SerializeField] private DefeatScreen _screen;

        private bool _restarting;

        private void OnEnable()
        {
            _screen.RetryRequested += Restart;
            _screen.SetRetryAvailable(!_restarting);
        }

        private void OnDisable()
        {
            if (_screen == null)
                return;

            _screen.RetryRequested -= Restart;
            _screen.SetRetryAvailable(false);
        }

        private void Restart()
        {
            if (_restarting)
                return;

            int sceneIndex = gameObject.scene.buildIndex;
            if (sceneIndex < 0 || !Application.CanStreamedLevelBeLoaded(sceneIndex))
            {
                GameDebug.LogError("Add the run scene to the build scene list before playing.", LogTopic.Gameplay);
                return;
            }

            _restarting = true;
            _screen.SetRetryAvailable(false);
            if (SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single) == null)
            {
                _restarting = false;
                _screen.SetRetryAvailable(true);
                GameDebug.LogError("Could not start reloading the run scene.", LogTopic.Gameplay);
            }
        }
    }
}
