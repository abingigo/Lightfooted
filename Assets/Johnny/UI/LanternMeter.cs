using UnityEngine;
using UnityEngine.UI;

namespace LightPlatformer.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class LanternMeter : MonoBehaviour
    {
        static readonly Color Warning = new Color32(255, 202, 112, 255);
        ILanternStatus source;
        Object sourceObject;
        RectTransform root;
        RectTransform canvas;
        RectTransform fill;
        CanvasGroup visibility;
        Image fillImage;
        Camera worldCamera;

        public void Bind(ILanternStatus lantern)
        {
            source = lantern;
            sourceObject = lantern as Object;
            root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(112, 7);
            canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            visibility = gameObject.AddComponent<CanvasGroup>();
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
            var track = Box(root, "Time bar", 112, 7, 0, 0, new Color32(53, 65, 76, 255));
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
            var halfSize = root.rect.size * 0.5f;
            position.x = Mathf.Clamp(position.x, canvas.rect.xMin + halfSize.x + 8, canvas.rect.xMax - halfSize.x - 8);
            position.y = Mathf.Clamp(position.y, canvas.rect.yMin + halfSize.y + 8, canvas.rect.yMax - halfSize.y - 64);
            root.anchoredPosition = position;
            fill.anchorMax = new Vector2(Mathf.Clamp01(remaining / source.DurationSeconds), 1f);
            bool lowTime = remaining <= 1.5f;
            fillImage.color = lowTime ? Warning : new Color32(165, 234, 203, 255);
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
