using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.VFX
{
    // How a burst of blood is thrown. Speeds in units/s, cone in degrees from the push direction
    // (positive = upward), sizes as drop radius in pixels.
    public readonly struct BloodBurst
    {
        public readonly int Count;
        public readonly float SpeedMin;
        public readonly float SpeedMax;
        public readonly float ConeMin;
        public readonly float ConeMax;
        public readonly float SizeMin;
        public readonly float SizeMax;

        public BloodBurst(int count, float speedMin, float speedMax, float coneMin, float coneMax, float sizeMin, float sizeMax)
        {
            Count = count;
            SpeedMin = speedMin;
            SpeedMax = speedMax;
            ConeMin = coneMin;
            ConeMax = coneMax;
            SizeMin = sizeMin;
            SizeMax = sizeMax;
        }

        public static readonly BloodBurst Light = new BloodBurst(8, 3.5f, 8f, -25f, 55f, 1.1f, 2.1f);
        public static readonly BloodBurst Heavy = new BloodBurst(15, 4.5f, 10.5f, -25f, 55f, 1.1f, 2.3f);
        public static readonly BloodBurst Gore = new BloodBurst(60, 6f, 17f, -35f, 75f, 1.1f, 3.6f);
        /// <summary>Blood thrown every which way: fired once to each side, hard along the floor and up and over.</summary>
        public static readonly BloodBurst Splatter = new BloodBurst(90, 3.5f, 17f, -20f, 125f, 1.1f, 3.8f);
    }

    // World-space blood drops that collide with the level. Each drop flies with gravity and air drag,
    // dragging a tapered red trail along its real path. A drop that hits a floor splats into a flat
    // stain; one that hits a wall sticks and runs down it, leaving a streak; one that hits a ceiling
    // drops off it. Stains linger, then fade. Everything is drawn as one mesh, snapped to the 32 px grid.
    public class BloodDrops : MonoBehaviour
    {
        private const float Px = 1f / 32f;
        private const float Gravity = 30f;
        private const float Drag = 1.6f;
        private const int TrailLength = 7;
        private const float MaxFlightTime = 3f;
        private const float SlideSpeed = 0.6f;
        private const float SlideTime = 0.5f;
        private const float StainTime = 2.5f;
        private const float FadeTime = 0.6f;
        private const int MaxDrops = 500;
        private const int SortingOrder = 8;
        private const float SurfaceOffset = 0.01f;

        private const int Flying = 0;
        private const int Sliding = 1;
        private const int Stained = 2;

        // A pure red palette: almost no blue in it, so it reads as blood and not pink.
        private static readonly Color32 BloodBrightSrgb = new Color32(190, 10, 14, 255);
        private static readonly Color32 BloodSrgb = new Color32(138, 6, 12, 255);
        private static readonly Color32 BloodDarkSrgb = new Color32(80, 3, 8, 255);
        private static readonly Color32 BloodShineSrgb = new Color32(230, 84, 72, 255);

        private static BloodDrops s_instance;

        private readonly List<Drop> _active = new List<Drop>();
        private readonly Stack<Drop> _free = new Stack<Drop>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();
        private Mesh _mesh;
        // The palette as it goes into the mesh: vertex colours are not converted to linear for us.
        private Color32 _bright;
        private Color32 _mid;
        private Color32 _dark;
        private Color32 _shine;
        private bool _meshDirty;

        private sealed class Drop
        {
            public readonly Vector2[] Trail = new Vector2[TrailLength];
            public Vector2 Position;
            public Vector2 Velocity;
            public float Radius;
            public float Age;
            public float StateTime;
            public int State;
            public int TrailCount;
            public LayerMask Collision;
            public Vector2 StainMin;
            public Vector2 StainMax;
            public float SlideTop;
        }

        /// <summary>
        /// Throws a burst of blood from origin. direction is the push direction (-1 left, 1 right);
        /// tilt rotates the whole cone in degrees. collision is what the drops land on.
        /// </summary>
        public static void Emit(Vector2 origin, int direction, in BloodBurst burst, LayerMask collision, float tilt = 0f)
        {
            if (s_instance == null)
                s_instance = new GameObject("BloodDrops").AddComponent<BloodDrops>();
            s_instance.Spawn(origin, direction < 0 ? -1 : 1, burst, collision, tilt);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_instance = null;

        private void Awake()
        {
            _bright = RuntimeSprites.ToRenderSpace(BloodBrightSrgb);
            _mid = RuntimeSprites.ToRenderSpace(BloodSrgb);
            _dark = RuntimeSprites.ToRenderSpace(BloodDarkSrgb);
            _shine = RuntimeSprites.ToRenderSpace(BloodShineSrgb);

            _mesh = new Mesh { name = "BloodDrops" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            // The default sprite material, so the drops are lit like every other sprite effect.
            RuntimeSprites.Prepare(gameObject.AddComponent<MeshRenderer>(), 0, SortingOrder);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void Spawn(Vector2 origin, int direction, in BloodBurst burst, LayerMask collision, float tilt)
        {
            for (int i = 0; i < burst.Count && _active.Count < MaxDrops; i++)
            {
                Drop drop = _free.Count > 0 ? _free.Pop() : new Drop();
                float angle = (Random.Range(burst.ConeMin, burst.ConeMax) + tilt) * Mathf.Deg2Rad;
                float speed = Random.Range(burst.SpeedMin, burst.SpeedMax);
                drop.Position = origin + Random.insideUnitCircle * (2f * Px);
                drop.Velocity = new Vector2(Mathf.Cos(angle) * direction, Mathf.Sin(angle)) * speed;
                drop.Radius = Random.Range(burst.SizeMin, burst.SizeMax);
                drop.Age = 0f;
                drop.StateTime = 0f;
                drop.State = Flying;
                drop.TrailCount = 0;
                drop.Collision = collision;
                _active.Add(drop);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Drop drop = _active[i];
                drop.Age += dt;
                drop.StateTime += dt;

                if (drop.State == Flying)
                    Fly(drop, dt);
                else if (drop.TrailCount > 0)
                    drop.TrailCount--; // the trail catches up once the drop stops

                if (drop.State == Sliding)
                    Slide(drop, dt);

                bool expired = drop.State == Stained
                    ? drop.StateTime >= StainTime + FadeTime
                    : drop.Age >= MaxFlightTime;
                if (expired)
                {
                    _active.RemoveAt(i);
                    _free.Push(drop);
                }
            }
            _meshDirty = true;
        }

        private void LateUpdate()
        {
            if (!_meshDirty)
                return;
            _meshDirty = false;
            BuildMesh();
        }

        private void Fly(Drop drop, float dt)
        {
            if (dt <= 0f)
                return;
            drop.Velocity.y -= Gravity * dt;
            drop.Velocity /= 1f + Drag * dt;
            Vector2 next = drop.Position + drop.Velocity * dt;

            PushTrail(drop);
            RaycastHit2D hit = Physics2D.Linecast(drop.Position, next, drop.Collision);
            if (hit.collider == null)
            {
                drop.Position = next;
                return;
            }

            Vector2 point = hit.point + hit.normal * SurfaceOffset;
            drop.Position = point;
            if (hit.normal.y > 0.5f)
            {
                // Floor: splat sideways into a one-pixel-high stain.
                float halfWidth = (drop.Radius * 2f + Random.Range(1f, 3f)) * Px * 0.5f;
                drop.StainMin = new Vector2(point.x - halfWidth, point.y - SurfaceOffset);
                drop.StainMax = new Vector2(point.x + halfWidth, point.y - SurfaceOffset + Px);
                SetState(drop, Stained);
            }
            else if (Mathf.Abs(hit.normal.x) > 0.5f)
            {
                // Wall: stick and run down it.
                drop.Velocity = Vector2.zero;
                drop.SlideTop = point.y;
                SetState(drop, Sliding);
            }
            else
            {
                // Ceiling: lose the upward speed and fall off it.
                drop.Velocity = new Vector2(drop.Velocity.x * 0.2f, 0f);
            }
        }

        private void Slide(Drop drop, float dt)
        {
            float slowdown = 1f - Mathf.Clamp01(drop.StateTime / SlideTime);
            drop.Position.y -= SlideSpeed * slowdown * dt;
            if (drop.StateTime < SlideTime)
                return;
            float halfWidth = Mathf.Max(Px, drop.Radius * Px) * 0.5f;
            drop.StainMin = new Vector2(drop.Position.x - halfWidth, drop.Position.y);
            drop.StainMax = new Vector2(drop.Position.x + halfWidth, drop.SlideTop);
            SetState(drop, Stained);
        }

        private static void SetState(Drop drop, int state)
        {
            drop.State = state;
            drop.StateTime = 0f;
        }

        private static void PushTrail(Drop drop)
        {
            for (int k = Mathf.Min(drop.TrailCount, TrailLength - 1); k > 0; k--)
                drop.Trail[k] = drop.Trail[k - 1];
            drop.Trail[0] = drop.Position;
            drop.TrailCount = Mathf.Min(drop.TrailCount + 1, TrailLength);
        }

        private void BuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();

            foreach (Drop drop in _active)
            {
                if (drop.State == Stained)
                {
                    float fade = 1f - Mathf.Clamp01((drop.StateTime - StainTime) / FadeTime);
                    AddRect(drop.StainMin, drop.StainMax, WithAlpha(_mid, fade));
                    continue;
                }

                float size = drop.Radius * 2f * Px;
                AddTrail(drop, size);

                if (drop.State == Sliding)
                {
                    float halfWidth = Mathf.Max(Px, drop.Radius * Px) * 0.5f;
                    AddRect(new Vector2(drop.Position.x - halfWidth, drop.Position.y),
                        new Vector2(drop.Position.x + halfWidth, drop.SlideTop), _mid);
                }

                // Head: a rounded pixel blob (two overlapping rectangles) with a highlight.
                Vector2 head = Snap(drop.Position);
                float half = Mathf.Max(Px, Mathf.Round(size / Px) * Px) * 0.5f;
                float inset = half > Px ? Px * 0.5f : 0f;
                AddRect(head - new Vector2(half, half - inset), head + new Vector2(half, half - inset), _bright);
                AddRect(head - new Vector2(half - inset, half), head + new Vector2(half - inset, half), _bright);
                if (drop.Radius >= 1.5f)
                {
                    Vector2 shine = head + new Vector2(-inset, half - Px);
                    AddRect(shine, shine + new Vector2(Px, Px), _shine);
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        // A ribbon from the head back through the recorded trail, tapering to a point and darkening.
        private void AddTrail(Drop drop, float size)
        {
            int points = drop.TrailCount + 1;
            if (points < 2)
                return;
            Vector2 previous = drop.Position;
            float previousWidth = size * 0.85f;
            Color32 previousColor = _mid;
            for (int k = 0; k < drop.TrailCount; k++)
            {
                Vector2 current = drop.Trail[k];
                float along = (k + 1) / (float)points;
                float width = size * 0.85f * Mathf.Pow(1f - along, 0.9f);
                Color32 color = along < 0.45f ? _mid : _dark;
                Vector2 segment = current - previous;
                if (segment.sqrMagnitude > 1e-8f)
                {
                    Vector2 normal = new Vector2(-segment.y, segment.x).normalized;
                    AddQuad(previous + normal * previousWidth * 0.5f, previous - normal * previousWidth * 0.5f,
                        current - normal * width * 0.5f, current + normal * width * 0.5f, previousColor, color);
                }
                previous = current;
                previousWidth = Mathf.Max(width, Px * 0.5f);
                previousColor = color;
            }
        }

        private void AddRect(Vector2 min, Vector2 max, Color32 color)
        {
            AddQuad(new Vector2(min.x, max.y), new Vector2(min.x, min.y),
                new Vector2(max.x, min.y), new Vector2(max.x, max.y), color, color);
        }

        // a and b share colorStart, c and d share colorEnd.
        private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 colorStart, Color32 colorEnd)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            _colors.Add(colorStart);
            _colors.Add(colorStart);
            _colors.Add(colorEnd);
            _colors.Add(colorEnd);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 1);
            _triangles.Add(start);
            _triangles.Add(start + 3);
            _triangles.Add(start + 2);
        }

        private static Vector2 Snap(Vector2 position) =>
            new Vector2(Mathf.Round(position.x / Px) * Px, Mathf.Round(position.y / Px) * Px);

        private static Color32 WithAlpha(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));
    }
}
