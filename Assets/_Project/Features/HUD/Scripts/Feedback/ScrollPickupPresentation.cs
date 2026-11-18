using System.Collections;
using DeadCells.Combat;
using DeadCells.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DeadCells.HUD
{
    public sealed class ScrollPickupPresentation : MonoBehaviour
    {
        // Times are relative to the start of the pickup sequence, sampled from the reference.
        private const float BreakAt = 0.36f;
        private const float BurstAt = 1.01f;
        private const float RevealDuration = 1.93f;

        [SerializeField] private SpriteRenderer _playerSprite;
        [SerializeField] private SpriteRenderer _pose;
        [SerializeField] private Sprite[] _carryFrames;
        [SerializeField] private SpriteRenderer _scroll;
        [SerializeField] private SpriteRenderer _halo;
        [SerializeField] private SpriteRenderer[] _rays;
        [SerializeField] private SpriteRenderer[] _stars;
        [SerializeField] private Light2D _light;
        [SerializeField] private Canvas _gameplayHud;
        [SerializeField] private GameObject _statsStrip;
        [SerializeField] private RectTransform[] _bars;
        [SerializeField] private RawImage _blur;
        [SerializeField] private CanvasGroup _toast;
        [SerializeField] private Text _toastText;
        [SerializeField] private Image _toastIcon;
        [SerializeField] private Sprite[] _statIcons;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _glass;
        [SerializeField] private AudioClip _power;

        private PowerScrollPickup _pickup;
        private Texture2D _snapshot;
        private bool _playerWasVisible;
        private bool _hudWasVisible;
        private bool _stripWasVisible;
        private float _toastUntil;

        public IEnumerator Play(PowerScrollPickup pickup)
        {
            _pickup = pickup;
            Vector3 origin = pickup.IconPosition;
            _playerWasVisible = _playerSprite.enabled;
            _hudWasVisible = _gameplayHud.enabled;
            _stripWasVisible = _statsStrip.activeSelf;
            _gameplayHud.enabled = false;
            _statsStrip.SetActive(false);
            _pose.transform.SetPositionAndRotation(_playerSprite.transform.position, _playerSprite.transform.rotation);
            _pose.flipX = _playerSprite.flipX;
            _pose.transform.localScale = _playerSprite.transform.lossyScale;
            var properties = new MaterialPropertyBlock();
            _playerSprite.GetPropertyBlock(properties);
            _pose.SetPropertyBlock(properties);

            bool broken = false;
            bool burst = false;
            for (float elapsed = 0f; elapsed < RevealDuration; elapsed += Time.unscaledDeltaTime)
            {
                SetBars(Mathf.InverseLerp(0f, 0.17f, elapsed) *
                    (1f - Mathf.InverseLerp(RevealDuration - 0.20f, RevealDuration, elapsed)));
                if (elapsed >= BreakAt && !broken)
                {
                    broken = true;
                    pickup.Reveal(true);
                    _audio.PlayOneShot(_glass, 0.82f);
                    _playerSprite.enabled = false;
                    _pose.enabled = true;
                    _scroll.enabled = true;
                }
                if (elapsed >= BurstAt && !burst)
                {
                    burst = true;
                    _audio.PlayOneShot(_power, 0.78f);
                }
                if (broken)
                    RenderReveal(elapsed, origin);
                yield return null;
            }

            SetBars(0f);
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                yield return new WaitForEndOfFrame();
                _snapshot = ScreenCapture.CaptureScreenshotAsTexture();
            }
            _blur.texture = _snapshot;
            _blur.enabled = _snapshot != null;
            RestoreHud();
        }

        public void Finish(bool completed)
        {
            if (_pickup != null)
            {
                if (!completed)
                    _pickup.Reveal(false);
                _pickup = null;
                if (_playerSprite != null)
                    _playerSprite.enabled = _playerWasVisible;
                RestoreHud();
            }

            SetBars(0f);
            Hide(_pose);
            Hide(_scroll);
            Hide(_halo);
            if (_light != null)
                _light.enabled = false;
            foreach (var ray in _rays)
                Hide(ray);
            foreach (var star in _stars)
                Hide(star);
            if (_blur != null)
            {
                _blur.enabled = false;
                _blur.texture = null;
            }
            if (_snapshot != null)
                Destroy(_snapshot);
            _snapshot = null;
        }

        public void Confirm(CombatStat stat)
        {
            _toastText.text = $"{stat} đã tăng!";
            _toastText.color = stat switch
            {
                CombatStat.Brutality => new Color(1f, 0.53f, 0.55f),
                CombatStat.Tactics => new Color(0.8f, 0.64f, 1f),
                _ => new Color(0.52f, 0.93f, 0.64f)
            };
            _toastIcon.sprite = _statIcons[(int)stat - 1];
            _toastUntil = Time.unscaledTime + 1.25f;
            _toast.alpha = 1f;
        }

        private void Update() =>
            _toast.alpha = Mathf.Clamp01((_toastUntil - Time.unscaledTime) / 0.28f);

        private void RenderReveal(float elapsed, Vector3 origin)
        {
            int index = Mathf.Min((int)((elapsed - BreakAt) * 20f), _carryFrames.Length - 1);
            _pose.sprite = _carryFrames[index];
            float lift = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BreakAt, BurstAt, elapsed));
            var hand = _pose.transform.position + new Vector3(_pose.flipX ? -0.05f : 0.05f, 2.13f, 0f);
            _scroll.transform.position = Vector3.Lerp(origin, hand, lift);
            float flare = Mathf.InverseLerp(BurstAt - 0.1f, BurstAt + 0.05f, elapsed) *
                Mathf.Lerp(1f, 0.28f, Mathf.InverseLerp(BurstAt + 0.05f, RevealDuration, elapsed));
            _halo.enabled = _light.enabled = flare > 0f;
            _halo.transform.position = _light.transform.position = hand;
            _halo.transform.localScale = Vector3.one * (0.22f + flare * 0.50f);
            _halo.color = new Color(0.45f, 0.77f, 1f, flare * 0.85f);
            _light.intensity = flare * 1.25f;
            for (int i = 0; i < _rays.Length; i++)
            {
                var ray = _rays[i];
                ray.enabled = flare > 0f;
                ray.transform.position = hand;
                ray.transform.rotation = Quaternion.Euler(0f, 0f, i * 36f + 8f);
                ray.transform.localScale = new Vector3(0.5f + flare * (1.18f - i * 0.11f), 0.17f, 1f);
                ray.color = new Color(0.70f, 0.89f, 1f, flare * 0.92f);
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                float speed = 1.15f + (i % 3) * 0.4f;
                float travel = Mathf.Repeat((elapsed - BreakAt) * speed + i * 0.083f, 1f);
                float angle = i * 2.4f;
                Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                star.enabled = true;
                star.transform.position = Vector3.Lerp(origin, hand, lift * 0.85f) + velocity * (0.18f + travel * 1.1f);
                star.color = new Color(0.85f, 0.96f, 1f, (1f - travel) * 0.8f);
            }
        }

        private void RestoreHud()
        {
            if (_gameplayHud != null)
                _gameplayHud.enabled = _hudWasVisible;
            if (_statsStrip != null)
                _statsStrip.SetActive(_stripWasVisible);
        }

        private static void Hide(SpriteRenderer sprite)
        {
            if (sprite != null)
                sprite.enabled = false;
        }

        private void SetBars(float amount)
        {
            foreach (var bar in _bars)
                if (bar != null)
                    bar.sizeDelta = new Vector2(0f, amount * 56f);
        }
    }
}
