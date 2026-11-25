using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.VFX
{
    // World-space chunks of floor thrown up by a shockwave. Each chunk waits for its turn, pops up, tumbles
    // under gravity, bounces off the floor and walls losing energy, comes to rest, lingers, then shrinks away.
    // Chunks have a lighter top edge so they read as stone. Drawn as one lit mesh, so they sit in the scene's
    // lighting like the sprites do.
    public class Debris : MonoBehaviour
    {
        private const float Px = 1f / 32f;
        private const float Gravity = 32f;
        private const float BounceKept = 0.35f;
        private const float FloorFriction = 0.6f;
        private const int MaxBounces = 3;
        private const float RestTime = 0.7f;
        private const float ShrinkTime = 0.25f;
        private const int MaxChunks = 200;
        private const int SortingOrder = 3;

        // The same front the baked shockwave sprite draws, in units: where it starts, how far it travels.
        private const float FrontStart = 8f * Px;
        private const float FrontTravel = 74f * Px;

        // Sampled from the Prison floor tiles (floorStamp / ground): very dark slate-teal, with the lighter
        // blue-grey accents the tiles use. The light face stays a little brighter so a chunk still reads.
        private static readonly Color32 StoneDark = new Color32(18, 26, 26, 255);
        private static readonly Color32 StoneMid = new Color32(38, 48, 52, 255);
        private static readonly Color32 StoneLight = new Color32(70, 86, 94, 255);

        private static Debris s_instance;

        private readonly List<Chunk> _active = new List<Chunk>();
        private readonly Stack<Chunk> _free = new Stack<Chunk>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();
        private Mesh _mesh;
        private Color32 _dark;
        private Color32 _mid;
        private Color32 _light;

        private sealed class Chunk
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Angle;
            public float Spin;
            public float Delay;
            public float RestedFor;
            public int Bounces;
            public int Tone;
            public bool IsResting;
            public LayerMask Collision;
        }

        /// <summary>
        /// Chunks popping up along the floor to both sides of origin as a shockwave front passes them.
        /// duration is how long the front takes to reach its full distance (seconds).
        /// </summary>
        public static void Shockwave(Vector2 origin, LayerMask collision, float duration, int count = 18)
        {
            if (s_instance == null)
                s_instance = new GameObject("Debris").AddComponent<Debris>();
            s_instance.SpawnShockwave(origin, collision, duration, count);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_instance = null;

        private void Awake()
        {
            _dark = RuntimeSprites.ToRenderSpace(StoneDark);
            _mid = RuntimeSprites.ToRenderSpace(StoneMid);
            _light = RuntimeSprites.ToRenderSpace(StoneLight);

            _mesh = new Mesh { name = "Debris" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            RuntimeSprites.Prepare(gameObject.AddComponent<MeshRenderer>(), 0, SortingOrder);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void SpawnShockwave(Vector2 origin, LayerMask collision, float duration, int count)
        {
            for (int i = 0; i < count && _active.Count < MaxChunks; i++)
            {
                Chunk chunk = _free.Count > 0 ? _free.Pop() : new Chunk();
                float side = i % 2 == 0 ? -1f : 1f;
                float along = (i + Random.value) / count;
                float distance = Mathf.Lerp(0.45f, FrontStart + FrontTravel - 0.1f, along);

                // The front is eased out (cubic), so invert that to find when it reaches this distance:
                // the chunk waits until then, so it pops right as the front passes.
                float reached = Mathf.Clamp01((distance - FrontStart) / FrontTravel);
                float arrival = 1f - Mathf.Pow(1f - reached, 1f / 3f);

                // Mostly small bits, a middle group, and a few heavy slabs. Heavier chunks are thrown less high,
                // spin slower and look darker.
                float roll = Random.value;
                float size = roll < 0.18f ? Random.Range(5.5f, 8.5f)
                    : roll < 0.5f ? Random.Range(3.2f, 5f)
                    : Random.Range(1.5f, 3f);
                float heavy = Mathf.InverseLerp(1.5f, 8.5f, size);

                chunk.Position = origin + new Vector2(side * distance, 0.05f);
                chunk.Velocity = new Vector2(side * Random.Range(0.4f, 1.8f), Random.Range(3.4f, 6.4f) * Mathf.Lerp(1.15f, 0.65f, heavy));
                chunk.Size = size;
                chunk.Angle = Random.Range(0f, 360f);
                chunk.Spin = Random.Range(-540f, 540f) * Mathf.Lerp(1f, 0.35f, heavy);
                chunk.Delay = arrival * duration;
                chunk.RestedFor = 0f;
                chunk.Bounces = 0;
                chunk.Tone = heavy > 0.5f || Random.value < 0.4f ? 1 : 0;
                chunk.IsResting = false;
                chunk.Collision = collision;
                _active.Add(chunk);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Chunk chunk = _active[i];
                if (chunk.Delay > 0f)
                {
                    chunk.Delay -= dt;
                    continue;
                }

                if (!chunk.IsResting)
                    Fly(chunk, dt);
                else
                {
                    chunk.RestedFor += dt;
                    if (chunk.RestedFor >= RestTime + ShrinkTime)
                    {
                        _active.RemoveAt(i);
                        _free.Push(chunk);
                    }
                }
            }
        }

        private void LateUpdate() => BuildMesh();

        private static void Fly(Chunk chunk, float dt)
        {
            if (dt <= 0f)
                return;
            chunk.Velocity.y -= Gravity * dt;
            Vector2 next = chunk.Position + chunk.Velocity * dt;
            chunk.Angle += chunk.Spin * dt;

            RaycastHit2D hit = Physics2D.Linecast(chunk.Position, next, chunk.Collision);
            if (hit.collider == null)
            {
                chunk.Position = next;
                return;
            }

            chunk.Position = hit.point + hit.normal * 0.01f;
            Vector2 reflected = Vector2.Reflect(chunk.Velocity, hit.normal);
            float alongNormal = Vector2.Dot(reflected, hit.normal);
            Vector2 alongSurface = reflected - hit.normal * alongNormal;
            chunk.Velocity = hit.normal * (alongNormal * BounceKept) + alongSurface * FloorFriction;
            chunk.Spin *= 0.5f;
            chunk.Bounces++;
            if (chunk.Bounces >= MaxBounces || chunk.Velocity.sqrMagnitude < 0.6f)
            {
                chunk.Velocity = Vector2.zero;
                chunk.Spin = 0f;
                chunk.IsResting = true;
            }
        }

        private void BuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();

            foreach (Chunk chunk in _active)
            {
                if (chunk.Delay > 0f)
                    continue;
                float shrink = chunk.IsResting
                    ? 1f - Mathf.Clamp01((chunk.RestedFor - RestTime) / ShrinkTime)
                    : 1f;
                float half = chunk.Size * Px * 0.5f * shrink;
                if (half <= 0f)
                    continue;

                Color32 body = chunk.Tone == 0 ? _mid : _dark;
                AddRotatedSquare(chunk.Position, half, chunk.Angle, body);
                // A lighter, smaller face shifted toward the top edge, so the chunk reads as lit stone.
                Vector2 up = new Vector2(-Mathf.Sin(chunk.Angle * Mathf.Deg2Rad), Mathf.Cos(chunk.Angle * Mathf.Deg2Rad));
                AddRotatedSquare(chunk.Position + up * (half * 0.28f), half * 0.62f, chunk.Angle, _light);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void AddRotatedSquare(Vector2 center, float half, float angleDegrees, Color32 color)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * half;
            Vector2 up = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * half;
            int start = _vertices.Count;
            _vertices.Add(center - right + up);
            _vertices.Add(center - right - up);
            _vertices.Add(center + right - up);
            _vertices.Add(center + right + up);
            for (int i = 0; i < 4; i++)
                _colors.Add(color);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 1);
            _triangles.Add(start);
            _triangles.Add(start + 3);
            _triangles.Add(start + 2);
        }
    }
}
