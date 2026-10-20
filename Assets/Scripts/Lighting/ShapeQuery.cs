using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
    public static class ShapeQuery
    {
        public static int Overlap(Collider2D shape, ContactFilter2D filter, List<Collider2D> results, float inset = 0f,
            Vector2 shift = default)
        {
            var t = shape.transform;
            Vector2 center = (Vector2)t.TransformPoint(shape.offset) + shift;
            Vector2 scale = t.lossyScale;
            scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            float angle = t.eulerAngles.z;

            switch (shape)
            {
                case BoxCollider2D box:
                    return Physics2D.OverlapBox(center, Shrink(Vector2.Scale(box.size, scale), inset), angle,
                        filter, results);
                case CapsuleCollider2D capsule:
                    return Physics2D.OverlapCapsule(center, Shrink(Vector2.Scale(capsule.size, scale), inset),
                        capsule.direction, angle, filter, results);
                case CircleCollider2D circle:
                    return Physics2D.OverlapCircle(center,
                        Mathf.Max(0f, circle.radius * Mathf.Max(scale.x, scale.y) - inset), filter, results);
                default:
                    if (inset <= 0f && shift == Vector2.zero && shape.enabled && shape.gameObject.activeInHierarchy)
                        return shape.Overlap(filter, results);
                    Debug.LogWarning($"ShapeQuery: a disabled or inset {shape.GetType().Name} isn't supported.", shape);
                    results.Clear();
                    return 0;
            }
        }

        private static Vector2 Shrink(Vector2 size, float inset) =>
            Vector2.Max(Vector2.zero, size - Vector2.one * (2f * inset));
    }
}