using LightPlatformer.UI;
using TMPro;
using UnityEngine;

namespace LightPlatformer.Obstacles
{
    public enum CrumbleState { Stable, Warning, Absent }

    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CrumblePlatform : MonoBehaviour
    {
        [Min(0.1f)] public float collapseDelay = 0.8f;
        [Min(0.1f)] public float returnDelay = 3f;
        public SpriteRenderer surface;
        public TMP_Text stateLabel;
        public CrumbleState State { get; private set; }
        public float Elapsed { get; private set; }
        BoxCollider2D solid;
        Color originalColor;

        void Awake()
        {
            solid = GetComponent<BoxCollider2D>();
            originalColor = surface != null ? surface.color : Color.white;
        }
        void OnEnable() { ResetPlatform(); }
        void OnValidate() { collapseDelay = Mathf.Max(0.1f, collapseDelay); returnDelay = Mathf.Max(0.1f, returnDelay); }
        public void ResetPlatform()
        {
            State = CrumbleState.Stable; Elapsed = 0;
            if (solid != null) solid.enabled = true;
            if (surface != null) surface.color = originalColor;
            if (stateLabel != null) stateLabel.text = "";
        }
        void OnCollisionEnter2D(Collision2D collision) { CheckLanding(collision); }
        void OnCollisionStay2D(Collision2D collision) { CheckLanding(collision); }
        void CheckLanding(Collision2D collision)
        {
            if (State != CrumbleState.Stable || GameMenuController.GameplayInputBlocked) return;
            if (!HazardContact.TryPlayerBox(collision.collider, out var box) || !box.enabled) return;
            if (box.bounds.center.y <= solid.bounds.max.y || box.bounds.min.y < solid.bounds.max.y - 0.15f) return;
            State = CrumbleState.Warning; Elapsed = 0;
            if (stateLabel != null) stateLabel.text = "MOVE";
        }
        void Update()
        {
            if (GameMenuController.GameplayInputBlocked || State == CrumbleState.Stable) return;
            Elapsed += Time.deltaTime;
            if (State == CrumbleState.Warning)
            {
                if (surface != null) surface.color = Color.Lerp(originalColor, new Color32(255, 167, 109, 255), Mathf.Clamp01(Elapsed / collapseDelay));
                if (Elapsed < collapseDelay) return;
                State = CrumbleState.Absent; Elapsed = 0; solid.enabled = false;
                if (surface != null) surface.color = new Color32(165, 184, 193, 35);
                if (stateLabel != null) stateLabel.text = "...";
            }
            else if (Elapsed >= returnDelay && !HazardContact.PlayerOccupies(solid)) ResetPlatform();
        }
    }
}
