using UnityEngine;

namespace DeadCells.Core.Audio
{
    /// <summary>
    /// Pooled 2D SFX dispatcher with pitch jitter and a subtle looping dungeon ambient bed.
    /// Null-safe when no scene instance exists (e.g. unit tests).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatAudioHub : MonoBehaviour
    {
        private const int PoolSize = 8;
        private const float AmbientVolume = 0.18f;

        private static CombatAudioHub _instance;

        private AudioSource[] _sfxPool;
        private AudioSource _ambientSource;
        private int _nextIndex;
        private float _ambientMultiplier = 1f;
        private float _ambientFadeStart = 1f;
        private float _ambientFadeTarget = 1f;
        private float _ambientFadeDuration;
        private float _ambientFadeElapsed;
        private bool _isFadingAmbient;

        public static float AmbientMultiplier => _instance != null ? _instance._ambientMultiplier : 1f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _sfxPool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var child = new GameObject($"SFX_{i}");
                child.transform.SetParent(transform, false);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                _sfxPool[i] = src;
            }

            var ambientGo = new GameObject("AmbientLoop");
            ambientGo.transform.SetParent(transform, false);
            _ambientSource = ambientGo.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.volume = AmbientVolume;
            _ambientSource.clip = ProceduralSfxLibrary.GetAmbientLoopClip();
            if (Application.isPlaying && _ambientSource.clip != null)
                _ambientSource.Play();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _ambientMultiplier = 1f;
                _isFadingAmbient = false;
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isFadingAmbient)
                TickAmbientFade(Time.unscaledDeltaTime);
        }

        public static void SetAmbientMultiplier(float multiplier)
        {
            if (_instance == null)
                return;
            _instance._isFadingAmbient = false;
            _instance.ApplyAmbientMultiplier(multiplier);
        }

        public static void FadeAmbient(float targetMultiplier, float duration)
        {
            if (_instance == null)
                return;
            float clampedTarget = Mathf.Clamp01(targetMultiplier);
            if (duration <= 0f)
            {
                SetAmbientMultiplier(clampedTarget);
                return;
            }

            _instance._ambientFadeStart = _instance._ambientMultiplier;
            _instance._ambientFadeTarget = clampedTarget;
            _instance._ambientFadeDuration = duration;
            _instance._ambientFadeElapsed = 0f;
            _instance._isFadingAmbient = true;
        }

        public void TickAmbientFade(float deltaTime)
        {
            if (!_isFadingAmbient || deltaTime <= 0f)
                return;

            _ambientFadeElapsed += deltaTime;
            float t = Mathf.Clamp01(_ambientFadeElapsed / _ambientFadeDuration);
            ApplyAmbientMultiplier(Mathf.Lerp(_ambientFadeStart, _ambientFadeTarget, t));
            if (t >= 1f)
                _isFadingAmbient = false;
        }

        private void ApplyAmbientMultiplier(float multiplier)
        {
            _ambientMultiplier = Mathf.Clamp01(multiplier);
            if (_ambientSource != null)
                _ambientSource.volume = AmbientVolume * _ambientMultiplier;
        }

        public static void Play(SfxId id, float volumeScale = 1f)
        {
            if (_instance == null)
                return;
            _instance.PlayInternal(id, volumeScale);
        }

        private void PlayInternal(SfxId id, float volumeScale)
        {
            if (_sfxPool == null || _sfxPool.Length == 0)
                return;
            AudioClip clip = ProceduralSfxLibrary.GetClip(id);
            if (clip == null)
                return;

            AudioSource src = _sfxPool[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _sfxPool.Length;
            if (src == null)
                return;

            src.clip = clip;
            src.pitch = Random.Range(0.93f, 1.07f);
            src.volume = Mathf.Clamp01(volumeScale);
            src.Play();
        }
    }
}
