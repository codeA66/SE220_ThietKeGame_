using UnityEngine;

namespace DeadCells.Core
{
    /// <summary>
    /// Computes accurate world-space AABBs for 2D colliders without requiring
    /// Physics2D.SyncTransforms or an enabled collider, and includes BoxCollider2D.edgeRadius.
    /// </summary>
    public static class ColliderBoundsUtility
    {
        public static Bounds GetWorldBounds(Collider2D collider)
        {
            if (collider == null)
                return default;

            if (collider is BoxCollider2D box)
                return GetBoxWorldBounds(box);

            if (collider is CircleCollider2D circle)
                return GetCircleWorldBounds(circle);

            if (collider is CapsuleCollider2D capsule)
                return GetCapsuleWorldBounds(capsule);

            return collider.bounds;
        }

        private static Bounds GetBoxWorldBounds(BoxCollider2D box)
        {
            Transform t = box.transform;
            Vector3 center = t.TransformPoint(box.offset);
            float edgeExpand = Mathf.Max(0f, box.edgeRadius) * 2f;
            Vector3 right = t.TransformVector(new Vector3(box.size.x, 0f, 0f));
            Vector3 up = t.TransformVector(new Vector3(0f, box.size.y, 0f));
            float width = Mathf.Abs(right.x) + Mathf.Abs(up.x) + edgeExpand;
            float height = Mathf.Abs(right.y) + Mathf.Abs(up.y) + edgeExpand;
            return new Bounds(center, new Vector3(width, height, 1f));
        }

        private static Bounds GetCircleWorldBounds(CircleCollider2D circle)
        {
            Transform t = circle.transform;
            Vector3 center = t.TransformPoint(circle.offset);
            Vector3 scale = t.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            float diameter = Mathf.Max(0f, circle.radius * 2f * maxScale);
            return new Bounds(center, new Vector3(diameter, diameter, 1f));
        }

        private static Bounds GetCapsuleWorldBounds(CapsuleCollider2D capsule)
        {
            Transform t = capsule.transform;
            Vector3 center = t.TransformPoint(capsule.offset);
            Vector3 scale = t.lossyScale;
            float scaledX = Mathf.Abs(capsule.size.x * scale.x);
            float scaledY = Mathf.Abs(capsule.size.y * scale.y);

            float diameter;
            float major;
            Vector3 localAxis;
            if (capsule.direction == CapsuleDirection2D.Horizontal)
            {
                diameter = scaledY;
                major = Mathf.Max(diameter, scaledX);
                localAxis = Vector3.right;
            }
            else
            {
                diameter = scaledX;
                major = Mathf.Max(diameter, scaledY);
                localAxis = Vector3.up;
            }

            float halfSpan = (major - diameter) * 0.5f;
            Vector3 axisDir = t.TransformDirection(localAxis);
            float width = 2f * Mathf.Abs(axisDir.x) * halfSpan + diameter;
            float height = 2f * Mathf.Abs(axisDir.y) * halfSpan + diameter;
            return new Bounds(center, new Vector3(width, height, 1f));
        }
    }
}
