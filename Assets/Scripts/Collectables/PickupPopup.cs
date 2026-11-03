using TMPro;
using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class PickupPopup : MonoBehaviour
    {
        [Tooltip("Seconds before the popup is gone.")]
        [SerializeField, Min(0.1f)] private float duration = 1.2f;

        [Tooltip("World units the popup rises over its lifetime.")]
        [SerializeField] private float rise = 1f;

        private TMP_Text text;
        private Vector3 start;
        private float age;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            start = transform.position;
        }

        public void Show(string message) => text.text = message;

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = start + Vector3.up * (rise * eased);
            text.alpha = 1f - t * t;
        }
    }
}
