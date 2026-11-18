using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.VFX
{
    // A tapered streak behind a fast object (arrows). While Emitting, it records where the object has
    // been for the last few hundredths of a second and draws a ribbon from there to the object, thinning
    // and fading toward the tail. When emitting stops, the tail catches up and the streak shrinks away.
    // The mesh lives on its own unparented object so it stays in world space; it is cleared whenever the
    // owner is disabled (pooled and recalled).
    public class TrailRibbon : MonoBehaviour
    {
        private const int MaxPoints = 24;

        private readonly Vector3[] _points = new Vector3[MaxPoints];
        private readonly float[] _times = new float[MaxPoints];
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();
        private int _count;
        private Color32 _head;
        private Color32 _tail;
        private float _width;
        private float _duration;
        private Mesh _mesh;
        private GameObject _view;

        public bool Emitting { get; set; }

        /// <summary>Colours in sRGB; width in units at the head; duration is how long the tail lags.</summary>
        public void Setup(Color32 head, Color32 tail, float width, float duration, SpriteRenderer drawnBehind)
        {
            _head = RuntimeSprites.ToRenderSpace(head);
            _tail = RuntimeSprites.ToRenderSpace(tail);
            _width = width;
            _duration = duration;

            _mesh = new Mesh { name = "TrailRibbon" };
            _mesh.MarkDynamic();
            _view = new GameObject($"{name} Trail");
            _view.AddComponent<MeshFilter>().sharedMesh = _mesh;
            RuntimeSprites.Prepare(_view.AddComponent<MeshRenderer>(), drawnBehind.sortingLayerID, drawnBehind.sortingOrder - 1);
        }

        /// <summary>Changes colours (sRGB) and head width, e.g. per shot.</summary>
        public void SetStyle(Color32 head, Color32 tail, float width)
        {
            _head = RuntimeSprites.ToRenderSpace(head);
            _tail = RuntimeSprites.ToRenderSpace(tail);
            _width = width;
        }

        public void Clear()
        {
            _count = 0;
            if (_mesh != null)
                _mesh.Clear();
        }

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            if (_view != null)
                Destroy(_view);
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void LateUpdate()
        {
            if (_mesh == null)
                return;
            float now = Time.time;
            while (_count > 0 && now - _times[_count - 1] > _duration)
                _count--;

            Vector3 position = transform.position;
            if (Emitting && (_count == 0 || (position - _points[0]).sqrMagnitude > 1e-6f))
                Push(position, now);

            Rebuild(now);
        }

        private void Push(Vector3 position, float time)
        {
            for (int i = Mathf.Min(_count, MaxPoints - 1); i > 0; i--)
            {
                _points[i] = _points[i - 1];
                _times[i] = _times[i - 1];
            }
            _points[0] = position;
            _times[0] = time;
            _count = Mathf.Min(_count + 1, MaxPoints);
        }

        private void Rebuild(float now)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();

            for (int i = 0; i < _count - 1; i++)
            {
                Vector2 from = _points[i];
                Vector2 to = _points[i + 1];
                Vector2 segment = to - from;
                if (segment.sqrMagnitude < 1e-8f)
                    continue;
                Vector2 normal = new Vector2(-segment.y, segment.x).normalized;
                float ageFrom = Mathf.Clamp01((now - _times[i]) / _duration);
                float ageTo = Mathf.Clamp01((now - _times[i + 1]) / _duration);
                float widthFrom = _width * (1f - ageFrom) * 0.5f;
                float widthTo = _width * (1f - ageTo) * 0.5f;
                Color32 colorFrom = Fade(Color32.Lerp(_head, _tail, ageFrom), 1f - ageFrom);
                Color32 colorTo = Fade(Color32.Lerp(_head, _tail, ageTo), 1f - ageTo);

                int start = _vertices.Count;
                _vertices.Add(from + normal * widthFrom);
                _vertices.Add(from - normal * widthFrom);
                _vertices.Add(to - normal * widthTo);
                _vertices.Add(to + normal * widthTo);
                _colors.Add(colorFrom);
                _colors.Add(colorFrom);
                _colors.Add(colorTo);
                _colors.Add(colorTo);
                _triangles.Add(start);
                _triangles.Add(start + 2);
                _triangles.Add(start + 1);
                _triangles.Add(start);
                _triangles.Add(start + 3);
                _triangles.Add(start + 2);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private static Color32 Fade(Color32 color, float alpha) =>
            new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01(alpha)));
    }
}
