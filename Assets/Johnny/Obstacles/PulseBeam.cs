using LightPlatformer.UI;
using TMPro;
using UnityEngine;

namespace LightPlatformer.Obstacles
{
    public enum BeamPhase { Safe, Warning, Active }

    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PulseBeam : MonoBehaviour
    {
        [Min(0.1f)] public float safeDuration = 4f;
        [Min(0.1f)] public float warningDuration = 0.85f;
        [Min(0.1f)] public float activeDuration = 1.75f;
        [Min(0)] public float phaseOffset;
        public SpriteRenderer beamVisual;
        public TMP_Text stateLabel;
        public BeamPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float SafeTimeRemaining => Phase == BeamPhase.Safe ? Mathf.Max(0, safeDuration - CycleTime) : 0;
        float CycleTime => Mathf.Repeat(Elapsed + phaseOffset, safeDuration + warningDuration + activeDuration);
        Collider2D hazard;

        void Awake() { hazard = GetComponent<BoxCollider2D>(); }
        void OnEnable() { ResetCycle(); }
        public void ResetCycle() { Elapsed = 0; Refresh(); }
        void OnValidate()
        {
            safeDuration = Mathf.Max(0.1f, safeDuration);
            warningDuration = Mathf.Max(0.1f, warningDuration);
            activeDuration = Mathf.Max(0.1f, activeDuration);
        }
        void Update()
        {
            if (GameMenuController.GameplayInputBlocked) return;
            Elapsed += Time.deltaTime;
            Refresh();
        }
        void Refresh()
        {
            float time = CycleTime;
            Phase = time < safeDuration ? BeamPhase.Safe : time < safeDuration + warningDuration ? BeamPhase.Warning : BeamPhase.Active;
            if (beamVisual != null)
                beamVisual.color = Phase == BeamPhase.Safe ? new Color32(128, 157, 178, 35)
                    : Phase == BeamPhase.Warning ? new Color32(255, 207, 115, 180) : new Color32(255, 117, 111, 255);
            if (stateLabel != null)
            {
                stateLabel.text = Phase == BeamPhase.Safe ? "SAFE" : Phase == BeamPhase.Warning ? "GET CLEAR" : "STOP";
                stateLabel.color = Phase == BeamPhase.Safe ? new Color32(165, 234, 203, 255)
                    : Phase == BeamPhase.Warning ? new Color32(255, 207, 115, 255) : new Color32(255, 117, 111, 255);
            }
        }
        void OnTriggerEnter2D(Collider2D other) { Check(other); }
        void OnTriggerStay2D(Collider2D other) { Check(other); }
        void Check(Collider2D other)
        {
            // Trigger stays enabled in all phases: someone waiting inside the beam
            // must be hit when it turns on, even without another Enter callback.
            if (Phase == BeamPhase.Active) HazardContact.Hurt(other, hazard);
        }
    }
}
