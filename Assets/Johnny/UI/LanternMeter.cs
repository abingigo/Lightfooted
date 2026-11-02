using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LightPlatformer.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class LanternMeter : MonoBehaviour
    {
        static readonly Color Frame = new Color32(94, 125, 125, 255);
        static readonly Color Warning = new Color32(255, 202, 112, 255);
        ILanternStatus source;
        Object sourceObject;
        RectTransform root;
        RectTransform canvas;
        RectTransform fill;
        CanvasGroup visibility;
        Image border;
        Image fillImage;
        TMP_Text label;
        Camera worldCamera;
        int shownSeconds = -1;

        public void Bind(ILanternStatus lantern)
        {
            source = lantern;
            sourceObject = lantern as Object;
            root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(132, 44);
            canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            visibility = gameObject.AddComponent<CanvasGroup>();
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
            border = gameObject.AddComponent<Image>();
            border.raycastTarget = false;
            border.color = Frame;
            Box(root, "Panel", 128, 40, 0, 0, new Color32(17, 22, 29, 240));
            var textRect = Box(root, "Time left", 120, 24, 0, 7);
            label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var track = Box(root, "Time bar", 112, 7, 0, -11, new Color32(53, 65, 76, 255));
            fill = Box(track, "Time fill", 0, 0, 0, 0, new Color32(165, 234, 203, 255));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fillImage = fill.GetComponent<Image>();
            UpdateDisplay();
        }

        void LateUpdate() => UpdateDisplay();

        void UpdateDisplay()
        {
            if (visibility == null) return;
            visibility.alpha = 0f;
            if (sourceObject == null || source.Anchor == null) return;
            var menu = GameMenuController.Instance;
            if (menu == null || menu.State != MenuState.Playing) return;
            float remaining = source.RemainingSeconds;
            if (remaining <= 0f || source.DurationSeconds <= 0f) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;
            var viewport = worldCamera.WorldToViewportPoint(source.Anchor.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return;

            var screen = worldCamera.WorldToScreenPoint(source.Anchor.position + Vector3.up * 0.9f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var position);
            position.x = Mathf.Clamp(position.x, canvas.rect.xMin + 74, canvas.rect.xMax - 74);
            position.y = Mathf.Clamp(position.y, canvas.rect.yMin + 30, canvas.rect.yMax - 90);
            root.anchoredPosition = position;
            fill.anchorMax = new Vector2(Mathf.Clamp01(remaining / source.DurationSeconds), 1f);
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds != shownSeconds) { label.text = "Lantern " + seconds + "s"; shownSeconds = seconds; }
            bool lowTime = remaining <= 1.5f;
            fillImage.color = lowTime ? Warning : new Color32(165, 234, 203, 255);
            // Pulse only the border. The light stays on until its timer ends.
            border.color = lowTime ? Color.Lerp(Frame, Warning, 0.65f + 0.2f * Mathf.Sin(Time.time * 5f)) : Frame;
            visibility.alpha = 1f;
        }

        static RectTransform Box(Transform parent, string name, float width, float height, float x, float y, Color? color = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            if (color.HasValue)
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.color = color.Value;
                image.raycastTarget = false;
            }
            return rect;
        }
    }
}
