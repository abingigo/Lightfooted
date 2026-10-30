using LightPlatformer.UI;
using UnityEngine;

namespace LightPlatformer.Obstacles
{
    // The player's large trigger is a light sensor, not its hurtbox. Check the
    // actual box geometry, including when darkness disables the solid collider.
    public static class HazardContact
    {
        static readonly Collider2D[] hits = new Collider2D[32];

        public static bool TryPlayerBox(Collider2D other, out BoxCollider2D box)
        {
            box = null;
            var body = other.attachedRigidbody;
            if (body == null || !body.CompareTag("Player")) return false;
            box = body.GetComponent<BoxCollider2D>();
            return box != null;
        }

        public static void Geometry(BoxCollider2D box, out Vector2 center, out Vector2 size, out float angle)
        {
            center = box.transform.TransformPoint(box.offset);
            var scale = box.transform.lossyScale;
            size = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y));
            angle = box.transform.eulerAngles.z;
        }

        public static bool BodyTouches(Collider2D other, Collider2D hazard)
        {
            if (!TryPlayerBox(other, out var box)) return false;
            Geometry(box, out var center, out var size, out var angle);
            int count = Physics2D.OverlapBox(center, size, angle, new ContactFilter2D().NoFilter(), hits);
            for (int i = 0; i < count; i++) if (hits[i] == hazard) return true;
            return false;
        }

        public static void Hurt(Collider2D other, Collider2D hazard, string caller = "unknown")
        {
            if (!GameMenuController.GameplayInputBlocked && BodyTouches(other, hazard))
                GameMenuController.NotifyGameOver(caller);
        }

        public static bool PlayerOccupies(BoxCollider2D area)
        {
            Geometry(area, out var center, out var size, out var angle);
            int count = Physics2D.OverlapBox(center, size + Vector2.one * 0.04f, angle, new ContactFilter2D().NoFilter(), hits);
            var areaBounds = new Bounds(center, size);
            for (int i = 0; i < count; i++)
            {
                if (!TryPlayerBox(hits[i], out var player)) continue;
                Geometry(player, out var playerCenter, out var playerSize, out _);
                if (areaBounds.Intersects(new Bounds(playerCenter, playerSize))) return true;
            }
            return false;
        }
    }
}
