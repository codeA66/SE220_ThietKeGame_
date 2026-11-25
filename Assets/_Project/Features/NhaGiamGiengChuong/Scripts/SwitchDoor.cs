using System;
using System.Collections;
using DeadCells.CameraSystem;
using DeadCells.Combat;
using DeadCells.Core.Audio;
using DeadCells.Player;
using DeadCells.VFX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadCells.NhaGiamGiengChuong
{
    /// <summary>
    /// Interactive lever/switch door that opens a linked barrier atomically once when a living Player
    /// interacts inside the lever trigger during the Playing phase, playing pedestal press feedback,
    /// activation sparks, stone dust, camera rumble, and an animated vertical gate retraction.
    /// </summary>
    public sealed class SwitchDoor : MonoBehaviour
    {
        [SerializeField] private string _doorId;
        [SerializeField] private LevelRunController _run;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BoxCollider2D _playerCollider;
        [SerializeField] private BoxCollider2D _switchTrigger;
        [SerializeField] private GameObject _doorBarrierRoot;
        [SerializeField] private SpriteRenderer _switchRenderer;
        [SerializeField] private Sprite _switchOffSprite;
        [SerializeField] private Sprite _switchOnSprite;
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private GameObject _promptIndicator;
        [SerializeField] private CameraFollow2D _camera;
        [SerializeField] private VfxSpawner _vfx;
        [SerializeField] private VfxDefinition _openVfx;

        private static Material s_burstMaterial;
        private InputAction _interactAction;
        private bool _isOpen;
        private bool _promptVisible;
        private Coroutine _openAnimCoroutine;
        private bool _hasCapturedBaseVisuals;
        private Vector3 _switchBaseScale = Vector3.one;
        private Color _switchBaseColor = Color.white;
        private Vector3 _doorBasePos;
        private SpriteRenderer[] _doorRenderers = Array.Empty<SpriteRenderer>();
        private Color[] _doorBaseColors = Array.Empty<Color>();

        public string DoorId => _doorId;
        public bool IsOpen => _isOpen;
        public bool IsAnimatingOpen => _openAnimCoroutine != null;
        public bool PromptVisible => _promptVisible;
        public GameObject DoorBarrierRoot => _doorBarrierRoot;
        public BoxCollider2D SwitchTrigger => _switchTrigger;
        public GameObject PromptIndicator => _promptIndicator;
        public InputActionAsset InputActions => _inputActions;

        public event Action<SwitchDoor> Opened;

        public void Configure(
            string doorId,
            LevelRunController run,
            PlayerController player,
            BoxCollider2D switchTrigger,
            GameObject doorBarrierRoot,
            SpriteRenderer switchRenderer = null,
            Sprite switchOffSprite = null,
            Sprite switchOnSprite = null,
            InputActionAsset inputActions = null,
            GameObject promptIndicator = null,
            Health playerHealth = null,
            BoxCollider2D playerCollider = null)
        {
            UnsubscribeInput();
            StopOpenAnimationAndResetVisuals();
            _hasCapturedBaseVisuals = false;
            _doorId = doorId;
            _run = run;
            _player = player;
            _switchTrigger = switchTrigger;
            _doorBarrierRoot = doorBarrierRoot;
            _switchRenderer = switchRenderer;
            _switchOffSprite = switchOffSprite;
            _switchOnSprite = switchOnSprite;
            _inputActions = inputActions;
            _promptIndicator = promptIndicator;
            _playerHealth = playerHealth;
            _playerCollider = playerCollider;
            _isOpen = false;

            if (_doorBarrierRoot != null)
            {
                foreach (var barrierCol in _doorBarrierRoot.GetComponentsInChildren<Collider2D>(true))
                {
                    if (barrierCol != null)
                        barrierCol.enabled = true;
                }
                _doorBarrierRoot.SetActive(true);
            }
            if (_switchRenderer != null && _switchOffSprite != null)
                _switchRenderer.sprite = _switchOffSprite;

            if (isActiveAndEnabled)
                SubscribeInput();
            RefreshVisualState();
        }

        private void Awake()
        {
            if (_switchTrigger == null)
                _switchTrigger = GetComponent<BoxCollider2D>();

            if (_doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(!_isOpen);

            ApplySwitchSprite();
            RefreshVisualState();
        }

        private void OnEnable()
        {
            SubscribeInput();
            RefreshVisualState();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            StopOpenAnimationAndResetVisuals();
            if (_isOpen && _doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            RefreshVisualState();
        }

        public void RefreshVisualState()
        {
            var health = ResolvePlayerHealth();
            var col = ResolvePlayerCollider();
            bool inTrigger = IsColliderInTrigger(col);

            _promptVisible = !_isOpen
                && _run != null && _run.IsPlaying
                && !IsInputBlocked()
                && health != null && health.IsAlive
                && inTrigger;

            if (_promptIndicator != null)
                _promptIndicator.SetActive(_promptVisible);
        }

        public bool TryInteract()
        {
            if (_isOpen)
                return false;

            if (_run == null || !_run.IsPlaying || IsInputBlocked())
                return false;

            var health = ResolvePlayerHealth();
            if (health == null || !health.IsAlive)
                return false;

            var col = ResolvePlayerCollider();
            if (!IsColliderInTrigger(col))
                return false;

            _isOpen = true;
            ApplySwitchSprite();
            RefreshVisualState();

            if (_doorBarrierRoot != null)
            {
                bool hasBarrierBounds = TryCaptureBarrierBounds(_doorBarrierRoot, out Bounds barrierBounds);
                // Always unlock physics passage immediately on frame 0
                foreach (var barrierCol in _doorBarrierRoot.GetComponentsInChildren<Collider2D>(true))
                {
                    if (barrierCol != null)
                        barrierCol.enabled = false;
                }
                if (hasBarrierBounds)
                {
                    Physics2D.SyncTransforms();
                    DeadCells.HUD.MinimapHud.NotifyWorldGeometryChanged(barrierBounds);
                }
            }

            if (_camera != null)
                _camera.Shake(0.14f, 0.22f);
            if (_vfx != null && _openVfx != null)
            {
                _vfx.Play(_openVfx, transform.position + Vector3.up * 0.9f);
                if (_doorBarrierRoot != null)
                    _vfx.Play(_openVfx, _doorBarrierRoot.transform.position);
            }
            CombatAudioHub.Play(SfxId.SwitchDoorOpen);

            if (Application.isPlaying && isActiveAndEnabled && _doorBarrierRoot != null && _doorBarrierRoot.activeInHierarchy)
            {
                CaptureBaseVisualState();
                _openAnimCoroutine = StartCoroutine(AnimateSwitchAndDoorOpenRoutine());
                Opened?.Invoke(this);
            }
            else
            {
                if (_doorBarrierRoot != null)
                    _doorBarrierRoot.SetActive(false);
                Opened?.Invoke(this);
            }
            return true;
        }

        private void CaptureBaseVisualState()
        {
            if (_hasCapturedBaseVisuals)
                return;
            _hasCapturedBaseVisuals = true;
            if (_switchRenderer != null)
            {
                _switchBaseScale = _switchRenderer.transform.localScale;
                _switchBaseColor = _switchRenderer.color;
            }
            if (_doorBarrierRoot != null)
            {
                _doorBasePos = _doorBarrierRoot.transform.position;
                _doorRenderers = _doorBarrierRoot.GetComponentsInChildren<SpriteRenderer>(true);
                _doorBaseColors = new Color[_doorRenderers.Length];
                for (int i = 0; i < _doorRenderers.Length; i++)
                    _doorBaseColors[i] = _doorRenderers[i] != null ? _doorRenderers[i].color : Color.white;
            }
            else
            {
                _doorRenderers = Array.Empty<SpriteRenderer>();
                _doorBaseColors = Array.Empty<Color>();
            }
        }

        private void StopOpenAnimationAndResetVisuals()
        {
            if (_openAnimCoroutine != null)
            {
                StopCoroutine(_openAnimCoroutine);
                _openAnimCoroutine = null;
            }
            if (!_hasCapturedBaseVisuals)
                return;

            if (_switchRenderer != null)
            {
                _switchRenderer.transform.localScale = _switchBaseScale;
                _switchRenderer.color = _switchBaseColor;
            }
            if (_doorBarrierRoot != null)
                _doorBarrierRoot.transform.position = _doorBasePos;
            for (int i = 0; i < _doorRenderers.Length; i++)
            {
                if (_doorRenderers[i] != null && i < _doorBaseColors.Length)
                    _doorRenderers[i].color = _doorBaseColors[i];
            }
        }

        private IEnumerator AnimateSwitchAndDoorOpenRoutine()
        {
            SpawnActivationBurst(transform.position + Vector3.up * 0.95f, new Color(0.45f, 0.95f, 1.0f, 1f), 14, 2.8f, 0.4f);
            if (_doorBarrierRoot != null)
                SpawnActivationBurst(_doorBarrierRoot.transform.position + Vector3.down * 1.3f, new Color(0.74f, 0.70f, 0.58f, 1f), 18, 3.6f, 1.3f);

            Transform swTr = _switchRenderer != null ? _switchRenderer.transform : null;
            var flashColor = new Color(0.75f, 1.0f, 1.0f, 1f);
            Transform doorTr = _doorBarrierRoot != null ? _doorBarrierRoot.transform : null;

            const float switchPressDuration = 0.22f;
            const float rumbleDuration = 0.11f;
            const float slideDuration = 0.42f;
            const float totalDuration = rumbleDuration + slideDuration;
            const float slideHeight = 3.6f;

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                // 1. Switch pedestal press-bounce & cyan activation glow
                if (swTr != null && elapsed <= switchPressDuration)
                {
                    float st = Mathf.Clamp01(elapsed / switchPressDuration);
                    float dip = Mathf.Sin(st * Mathf.PI);
                    swTr.localScale = new Vector3(
                        _switchBaseScale.x * (1f + 0.14f * dip),
                        _switchBaseScale.y * (1f - 0.22f * dip),
                        _switchBaseScale.z);
                    if (_switchRenderer != null)
                        _switchRenderer.color = Color.Lerp(_switchBaseColor, flashColor, dip);
                }
                else if (swTr != null)
                {
                    swTr.localScale = _switchBaseScale;
                    if (_switchRenderer != null)
                        _switchRenderer.color = _switchBaseColor;
                }

                // 2. Door barrier rumble followed by smooth vertical retraction into ceiling header
                if (doorTr != null)
                {
                    if (elapsed < rumbleDuration)
                    {
                        float jitter = Mathf.Sin(elapsed * 95f) * 0.065f;
                        doorTr.position = _doorBasePos + new Vector3(jitter, 0f, 0f);
                    }
                    else
                    {
                        float dt = Mathf.Clamp01((elapsed - rumbleDuration) / slideDuration);
                        float ease = dt * dt * (3f - 2f * dt);
                        float microShake = (1f - dt) * Mathf.Sin(elapsed * 70f) * 0.025f;
                        doorTr.position = _doorBasePos + new Vector3(microShake, ease * slideHeight, 0f);
                        if (dt > 0.65f)
                        {
                            float fade = 1f - Mathf.Clamp01((dt - 0.65f) / 0.35f);
                            for (int i = 0; i < _doorRenderers.Length; i++)
                            {
                                if (_doorRenderers[i] == null || i >= _doorBaseColors.Length)
                                    continue;
                                Color bc = _doorBaseColors[i];
                                _doorRenderers[i].color = new Color(bc.r, bc.g, bc.b, bc.a * fade);
                            }
                        }
                    }
                }

                yield return null;
            }

            _openAnimCoroutine = null;
            StopOpenAnimationAndResetVisuals();
            if (_doorBarrierRoot != null)
                _doorBarrierRoot.SetActive(false);
        }

        private static void SpawnActivationBurst(Vector3 pos, Color color, int count, float speed, float gravity)
        {
            var go = new GameObject("SwitchDoorBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.gravityModifier = gravity;
            main.startColor = color;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                int vfxLayer = SortingLayer.NameToID("VFX");
                if (vfxLayer != 0)
                    rend.sortingLayerID = vfxLayer;
                rend.sortingOrder = 12;
                if (s_burstMaterial == null)
                {
                    Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                    if (unlit != null)
                    {
                        s_burstMaterial = new Material(unlit)
                        {
                            name = "Mat_SwitchDoorBurst_Runtime",
                            hideFlags = HideFlags.HideAndDontSave,
                        };
                    }
                }
                if (s_burstMaterial != null)
                    rend.sharedMaterial = s_burstMaterial;
            }
            ps.Play();
            Destroy(go, 0.85f);
        }

        public bool IsColliderInTrigger(Collider2D candidate)
        {
            if (candidate == null || _switchTrigger == null)
                return false;

            Bounds triggerBounds = GetWorldBounds(_switchTrigger);
            Bounds actorBounds = GetWorldBounds(candidate);

            return actorBounds.max.x >= triggerBounds.min.x && actorBounds.min.x <= triggerBounds.max.x
                && actorBounds.max.y >= triggerBounds.min.y && actorBounds.min.y <= triggerBounds.max.y;
        }

        private void ApplySwitchSprite()
        {
            if (_switchRenderer == null)
                return;

            Sprite target = _isOpen ? _switchOnSprite : _switchOffSprite;
            if (target != null)
                _switchRenderer.sprite = target;
        }

        internal static bool TryCaptureBarrierBounds(GameObject root, out Bounds combined)
        {
            combined = default;
            if (root == null)
                return false;

            bool hasAny = false;
            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null)
                    continue;

                Bounds bounds = GetWorldBounds(col);
                if (!hasAny)
                {
                    combined = bounds;
                    hasAny = true;
                }
                else
                {
                    combined.Encapsulate(bounds);
                }
            }

            return hasAny;
        }

        private static Bounds GetWorldBounds(Collider2D collider)
        {
            if (collider is BoxCollider2D box)
            {
                Vector3 center = box.transform.TransformPoint(box.offset);
                Vector3 scale = box.transform.lossyScale;
                Vector3 size = new Vector3(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y),
                    1f);
                return new Bounds(center, size);
            }

            return collider.bounds;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        private void SubscribeInput()
        {
            if (_inputActions == null || _interactAction != null)
                return;

            var map = _inputActions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (map == null)
                return;

            _interactAction = map.FindAction(_interactActionName, throwIfNotFound: false);
            if (_interactAction != null)
                _interactAction.started += OnInteractStarted;
        }

        private void UnsubscribeInput()
        {
            if (_interactAction == null)
                return;

            _interactAction.started -= OnInteractStarted;
            _interactAction = null;
        }

        private PlayerController ResolvePlayer() =>
            _player != null ? _player : (_run != null ? _run.Player : null);

        private bool IsInputBlocked()
        {
            var p = ResolvePlayer();
            return p != null && ((p.RunStats != null && p.RunStats.BlocksGameplayInput) || p.IsInScriptedSequence);
        }

        private BoxCollider2D ResolvePlayerCollider()
        {
            if (_playerCollider != null)
                return _playerCollider;
            var p = ResolvePlayer();
            return p != null ? p.GetComponent<BoxCollider2D>() : null;
        }

        private Health ResolvePlayerHealth()
        {
            if (_playerHealth != null)
                return _playerHealth;
            var p = ResolvePlayer();
            if (p != null)
                return p.Health != null ? p.Health : p.GetComponent<Health>();
            return _run != null ? _run.PlayerHealth : null;
        }
    }
}
