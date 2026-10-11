using TMPro;
using UnityEngine;

namespace LightPlatformer.Demos
{
    public sealed class BrakeLightFeedback : MonoBehaviour
    {
        public ControllableLight controller;
        public SpriteRenderer emitter;
        public TMP_Text stateLabel;

        void Update()
        {
            bool selected = controller != null && controller.IsSelected;
            if (emitter != null)
                emitter.color = selected ? new Color32(165, 234, 203, 255) : new Color32(255, 207, 115, 255);
            if (stateLabel != null)
                stateLabel.text = selected ? "SELECTED / PRESS F" : "CLICK TO SELECT";
        }
    }
}
