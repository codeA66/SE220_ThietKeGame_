using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.VFX
{
    // How a burst of sparks is thrown. Speeds in units/s, cone in degrees from the flight direction
    // (positive = upward), lifetimes in seconds, sizes as spark radius in pixels, flash as the radius of
    // the glow popped at the impact point in units.
    public readonly struct SparkBurst
    {
        public readonly int Count;
        public readonly float SpeedMin;
        public readonly float SpeedMax;
        public readonly float ConeMin;
        public readonly float ConeMax;
        public readonly float LifeMin;
        public readonly float LifeMax;
        public readonly float SizeMin;
        public readonly float SizeMax;
        public readonly float Bounce;
        public readonly float FlashRadius;
        /// <summary>Scales gravity: 1 is normal, 0 floats, below 0 drifts upward.</summary>
        public readonly float GravityScale;
        /// <summary>Draws the sparks in plain white and grey instead of the warm pale palette.</summary>
        public readonly bool Monotone;

        public SparkBurst(int count, float speedMin, float speedMax, float coneMin, float coneMax, float lifeMin,
            float lifeMax, float sizeMin, float sizeMax, float bounce, float flashRadius, float gravityScale = 1f,
            bool monotone = false)
        {
            Count = count;
            SpeedMin = speedMin;
            SpeedMax = speedMax;
            ConeMin = coneMin;
            ConeMax = coneMax;
            LifeMin = lifeMin;
            LifeMax = lifeMax;
            SizeMin = sizeMin;
            SizeMax = sizeMax;
            Bounce = bounce;
            FlashRadius = flashRadius;
            GravityScale = gravityScale;
            Monotone = monotone;
        }

        /// <summary>A blade or arrow ringing off a shield: a spray of hot sparks and a bright flash.</summary>
        public static readonly SparkBurst Shield = new SparkBurst(10, 4f, 10f, -20f, 70f, 0.28f, 0.55f, 0.8f, 1.4f, 0.5f, 0.5f);

        /// <summary>White motes that float up off the body, no flash and no bounce. Plain white and grey.</summary>
        public static readonly SparkBurst Ascend = new SparkBurst(1, 9f, 14f, 87f, 93f, 0.55f, 0.85f, 1.5f, 2.3f, 0f, 0f, 0f, true);

        /// <summary>One pale white mote drifting slowly up and away, like a soul leaving. No gravity, no bounce.</summary>
        public static readonly SparkBurst Soul = new SparkBurst(1, 1.4f, 3f, 72f, 108f, 1f, 1.7f, 1.3f, 2.1f, 0f, 0f, 0f, true);
    }

    // World-space sparks that collide with the level. Each spark flies with gravity and air drag, bounces off
    // floors and walls losing energy, and drags a tapered trail that cools from white to pale yellow to soft amber.
    // Every spark has a soft halo and every burst starts with a bright flash, drawn unlit so the glow stays
    // strong in dark rooms (a baked stand-in for post-process bloom, which is off in this project).
    // Everything is two meshes: the glow and the cores with their trails.
    public class Sparks : MonoBehaviour
    {
        private const float Px = 1f / 32f;
        private const float Gravity = 26f;
        private const float Drag = 0.9f;
        private const float WallFriction = 0.75f;
        private const int MaxBounces = 3;
        private const int TrailLength = 6;
        private const float FlashTime = 0.12f;
        private const int MaxSparks = 400;
        private const int CoreSortingOrder = 13;
        private const int GlowSortingOrder = 12;

        private static readonly Color32 HeadWhite = new Color32(255, 255, 248, 255);
        private static readonly Color32 HeadYellow = new Color32(255, 247, 200, 255);
        private static readonly Color32 HeadAmber = new Color32(255, 222, 160, 255);
        private static readonly Color32 HeadEmber = new Color32(255, 200, 150, 255);
        private static readonly Color32 GlowPale = new Color32(255, 238, 180, 255);
        private static readonly Color32 FlashWarm = new Color32(255, 246, 205, 255);
        // The monotone palette: plain white cooling to light grey, for effects that must stay one colour.
        private static readonly Color32 MonoHead = new Color32(255, 255, 255, 255);
        private static readonly Color32 MonoMid = new Color32(238, 238, 238, 255);
        private static readonly Color32 MonoDim = new Color32(196, 196, 196, 255);
        private static readonly Color32 MonoGlow = new Color32(255, 255, 255, 255);

        private static Sparks s_instance;

        private readonly List<Spark> _active = new List<Spark>();
        private readonly Stack<Spark> _free = new Stack<Spark>();
        private readonly List<Flash> _flashes = new List<Flash>();
        private readonly List<Vector3> _coreVertices = new List<Vector3>();
        private readonly List<Color32> _coreColors = new List<Color32>();
        private readonly List<int> _coreTriangles = new List<int>();
        private readonly List<Vector3> _glowVertices = new List<Vector3>();
        private readonly List<Color32> _glowColors = new List<Color32>();
        private readonly List<Vector2> _glowUvs = new List<Vector2>();
        private readonly List<int> _glowTriangles = new List<int>();
        private Mesh _coreMesh;
        private Mesh _glowMesh;
        private Color32 _white;
        private Color32 _yellow;
        private Color32 _amber;
        private Color32 _ember;
        private Color32 _glow;
        private Color32 _flash;
        private Color32 _monoHead;
        private Color32 _monoMid;
        private Color32 _monoDim;
        private Color32 _monoGlow;

        private sealed class Spark
        {
            public readonly Vector2[] Trail = new Vector2[TrailLength];
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Age;
            public float Life;
            public float Bounce;
            public int Bounces;
            public int TrailCount;
            public bool IsResting;
            public float GravityScale;
            public bool Monotone;
            public LayerMask Collision;
        }

        private struct Flash
        {
            public Vector2 Position;
            public float Radius;
            public float Age;
        }

        /// <summary>
        /// Throws a burst of sparks from origin. direction is the way they fly (-1 left, 1 right);
        /// tilt rotates the cone in degrees. collision is what they bounce off.
        /// </summary>
        public static void Emit(Vector2 origin, int direction, in SparkBurst burst, LayerMask collision, float tilt = 0f)
        {
            if (s_instance == null)
                s_instance = new GameObject("Sparks").AddComponent<Sparks>();
            s_instance.Spawn(origin, direction < 0 ? -1 : 1, burst, collision, tilt);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_instance = null;

        private void Awake()
        {
            _white = RuntimeSprites.ToRenderSpace(HeadWhite);
            _yellow = RuntimeSprites.ToRenderSpace(HeadYellow);
            _amber = RuntimeSprites.ToRenderSpace(HeadAmber);
            _ember = RuntimeSprites.ToRenderSpace(HeadEmber);
            _glow = RuntimeSprites.ToRenderSpace(GlowPale);
            _flash = RuntimeSprites.ToRenderSpace(FlashWarm);
            _monoHead = RuntimeSprites.ToRenderSpace(MonoHead);
            _monoMid = RuntimeSprites.ToRenderSpace(MonoMid);
            _monoDim = RuntimeSprites.ToRenderSpace(MonoDim);
            _monoGlow = RuntimeSprites.ToRenderSpace(MonoGlow);

            _glowMesh = NewLayer("Glow", GlowSortingOrder, RuntimeSprites.GlowTexture);
            _coreMesh = NewLayer("Cores", CoreSortingOrder, null);
            if (_active.Count == 0 && _flashes.Count == 0)
                enabled = false;
        }

        private Mesh NewLayer(string layerName, int sortingOrder, Texture texture)
        {
            var mesh = new Mesh { name = layerName };
            mesh.MarkDynamic();
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            RuntimeSprites.Prepare(go.AddComponent<MeshRenderer>(), 0, sortingOrder, texture, unlit: true);
            return mesh;
        }

        private void OnDestroy()
        {
            if (_coreMesh != null)
                Destroy(_coreMesh);
            if (_glowMesh != null)
                Destroy(_glowMesh);
        }

        private void Spawn(Vector2 origin, int direction, in SparkBurst burst, LayerMask collision, float tilt)
        {
            if (burst.FlashRadius > 0f)
                _flashes.Add(new Flash { Position = origin, Radius = burst.FlashRadius, Age = 0f });

            for (int i = 0; i < burst.Count && _active.Count < MaxSparks; i++)
            {
                Spark spark = _free.Count > 0 ? _free.Pop() : new Spark();
                float angle = (Random.Range(burst.ConeMin, burst.ConeMax) + tilt) * Mathf.Deg2Rad;
                float speed = Random.Range(burst.SpeedMin, burst.SpeedMax);
                spark.Position = origin + Random.insideUnitCircle * (2f * Px);
                spark.Velocity = new Vector2(Mathf.Cos(angle) * direction, Mathf.Sin(angle)) * speed;
                spark.Size = Random.Range(burst.SizeMin, burst.SizeMax);
                spark.Life = Random.Range(burst.LifeMin, burst.LifeMax);
                spark.Bounce = burst.Bounce;
                spark.Age = 0f;
                spark.Bounces = 0;
                spark.TrailCount = 0;
                spark.IsResting = false;
                spark.GravityScale = burst.GravityScale;
                spark.Monotone = burst.Monotone;
                spark.Collision = collision;
                _active.Add(spark);
            }

            if (_active.Count > 0 || _flashes.Count > 0)
                enabled = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Spark spark = _active[i];
                spark.Age += dt;
                if (spark.Age >= spark.Life)
                {
                    _active.RemoveAt(i);
                    _free.Push(spark);
                    continue;
                }

                if (!spark.IsResting)
                    Fly(spark, dt);
                else if (spark.TrailCount > 0)
                    spark.TrailCount--; // the trail catches up once the spark stops
            }

            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                Flash flash = _flashes[i];
                flash.Age += dt;
                if (flash.Age >= FlashTime)
                    _flashes.RemoveAt(i);
                else
                    _flashes[i] = flash;
            }
        }

        private void LateUpdate()
        {
            BuildMeshes();
            if (_active.Count == 0 && _flashes.Count == 0)
                enabled = false;
        }

        private static void Fly(Spark spark, float dt)
        {
            if (dt <= 0f)
                return;
            spark.Velocity.y -= Gravity * spark.GravityScale * dt;
            spark.Velocity /= 1f + Drag * dt;
            Vector2 next = spark.Position + spark.Velocity * dt;

            PushTrail(spark);
            RaycastHit2D hit = Physics2D.Linecast(spark.Position, next, spark.Collision);
            if (hit.collider == null)
            {
                spark.Position = next;
                return;
            }

            // Bounce: keep a fraction of the speed along the surface normal, and most of it along the surface.
            spark.Position = hit.point + hit.normal * 0.01f;
            Vector2 reflected = Vector2.Reflect(spark.Velocity, hit.normal);
            float alongNormal = Vector2.Dot(reflected, hit.normal);
            Vector2 alongSurface = reflected - hit.normal * alongNormal;
            spark.Velocity = hit.normal * (alongNormal * spark.Bounce) + alongSurface * WallFriction;
            spark.Bounces++;
            if (spark.Bounces >= MaxBounces || spark.Velocity.sqrMagnitude < 1f)
            {
                spark.Velocity = Vector2.zero;
                spark.IsResting = true;
            }
        }

        private static void PushTrail(Spark spark)
        {
            for (int k = Mathf.Min(spark.TrailCount, TrailLength - 1); k > 0; k--)
                spark.Trail[k] = spark.Trail[k - 1];
            spark.Trail[0] = spark.Position;
            spark.TrailCount = Mathf.Min(spark.TrailCount + 1, TrailLength);
        }

        private void BuildMeshes()
        {
            _coreVertices.Clear();
            _coreColors.Clear();
            _coreTriangles.Clear();
            _glowVertices.Clear();
            _glowColors.Clear();
            _glowUvs.Clear();
            _glowTriangles.Clear();

            foreach (Flash flash in _flashes)
            {
                float t = flash.Age / FlashTime;
                float fade = 1f - t;
                // A wide warm bloom with a hot white heart, swelling as it fades.
                AddGlow(flash.Position, flash.Radius * (1.3f + 0.4f * t), WithAlpha(_flash, 0.45f * fade));
                AddGlow(flash.Position, flash.Radius * (0.7f + 0.15f * t), WithAlpha(_white, 0.8f * fade));
            }

            foreach (Spark spark in _active)
            {
                float t = spark.Age / spark.Life;
                float fade = 1f - t * t;
                Color32 head = spark.Monotone ? MonoHeadColor(t) : HeadColor(t);
                float size = spark.Size * Px * (1f - 0.35f * t);

                AddTrail(spark, size, head, fade, spark.Monotone ? _monoDim : _ember);
                AddSquare(spark.Position, Mathf.Max(Px, size * 1.1f), WithAlpha(head, 1f));
                AddGlow(spark.Position, size * 4f * (0.5f + 0.5f * fade), WithAlpha(spark.Monotone ? _monoGlow : _glow, 0.3f * fade));
            }

            Apply(_glowMesh, _glowVertices, _glowColors, _glowUvs, _glowTriangles);
            Apply(_coreMesh, _coreVertices, _coreColors, null, _coreTriangles);
        }

        // Plain white at birth, dimming to light grey: no hue at all.
        private Color32 MonoHeadColor(float t) =>
            t < 0.5f ? Color32.Lerp(_monoHead, _monoMid, t / 0.5f) : Color32.Lerp(_monoMid, _monoDim, (t - 0.5f) / 0.5f);

        // White-hot at birth, cooling through pale yellow to a soft amber.
        private Color32 HeadColor(float t)
        {
            if (t < 0.2f)
                return Color32.Lerp(_white, _yellow, t / 0.2f);
            if (t < 0.6f)
                return Color32.Lerp(_yellow, _amber, (t - 0.2f) / 0.4f);
            return Color32.Lerp(_amber, _ember, (t - 0.6f) / 0.4f);
        }

        // A ribbon from the head back through the recorded trail, tapering to a point and cooling.
        private void AddTrail(Spark spark, float size, Color32 head, float fade, Color32 tail)
        {
            if (spark.TrailCount < 1)
                return;
            Vector2 previous = spark.Position;
            float previousWidth = size;
            Color32 previousColor = WithAlpha(head, fade);
            for (int k = 0; k < spark.TrailCount; k++)
            {
                Vector2 current = spark.Trail[k];
                float along = (k + 1) / (float)(spark.TrailCount + 1);
                float width = size * (1f - along);
                Color32 color = WithAlpha(Color32.Lerp(head, tail, along), fade * (1f - along));
                Vector2 segment = current - previous;
                if (segment.sqrMagnitude > 1e-8f)
                {
                    Vector2 normal = new Vector2(-segment.y, segment.x).normalized;
                    AddCoreQuad(previous + normal * previousWidth * 0.5f, previous - normal * previousWidth * 0.5f,
                        current - normal * width * 0.5f, current + normal * width * 0.5f, previousColor, color);
                }
                previous = current;
                previousWidth = Mathf.Max(width, Px * 0.5f);
                previousColor = color;
            }
        }

        private void AddSquare(Vector2 center, float side, Color32 color)
        {
            float half = side * 0.5f;
            AddCoreQuad(new Vector2(center.x - half, center.y + half), new Vector2(center.x - half, center.y - half),
                new Vector2(center.x + half, center.y - half), new Vector2(center.x + half, center.y + half), color, color);
        }

        private void AddCoreQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 colorStart, Color32 colorEnd)
        {
            int start = _coreVertices.Count;
            _coreVertices.Add(a);
            _coreVertices.Add(b);
            _coreVertices.Add(c);
            _coreVertices.Add(d);
            _coreColors.Add(colorStart);
            _coreColors.Add(colorStart);
            _coreColors.Add(colorEnd);
            _coreColors.Add(colorEnd);
            _coreTriangles.Add(start);
            _coreTriangles.Add(start + 2);
            _coreTriangles.Add(start + 1);
            _coreTriangles.Add(start);
            _coreTriangles.Add(start + 3);
            _coreTriangles.Add(start + 2);
        }

        // A soft halo: one quad of the glow texture, centred on a point, radius in units.
        private void AddGlow(Vector2 center, float radius, Color32 color)
        {
            if (radius <= 0f || color.a == 0)
                return;
            int start = _glowVertices.Count;
            _glowVertices.Add(new Vector3(center.x - radius, center.y + radius));
            _glowVertices.Add(new Vector3(center.x - radius, center.y - radius));
            _glowVertices.Add(new Vector3(center.x + radius, center.y - radius));
            _glowVertices.Add(new Vector3(center.x + radius, center.y + radius));
            for (int i = 0; i < 4; i++)
                _glowColors.Add(color);
            _glowUvs.Add(new Vector2(0f, 1f));
            _glowUvs.Add(new Vector2(0f, 0f));
            _glowUvs.Add(new Vector2(1f, 0f));
            _glowUvs.Add(new Vector2(1f, 1f));
            _glowTriangles.Add(start);
            _glowTriangles.Add(start + 2);
            _glowTriangles.Add(start + 1);
            _glowTriangles.Add(start);
            _glowTriangles.Add(start + 3);
            _glowTriangles.Add(start + 2);
        }

        private static void Apply(Mesh mesh, List<Vector3> vertices, List<Color32> colors, List<Vector2> uvs, List<int> triangles)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            if (uvs != null)
                mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private static Color32 WithAlpha(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));
    }
}
