using System;
using UnityEngine;
using UnityEngine.Events;

namespace Collectables
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))] // please set to is_trigger
    public sealed class Collectable : MonoBehaviour
    {
        [SerializeField] private CollectDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Runs after this is collected, e.g. to play a sound.")]
        [SerializeField] private UnityEvent onCollected = new();
        
        private Sprite defaultSprite;
        private bool collected;
        
        public CollectDefinition Definition => definition;
        public bool IsCollected => collected;
        public event Action<Collectable> Collected;

        private void Reset() => spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        private void OnEnable() => collected = false;
        private void Start() => ApplyVisuals();
        private void Awake()
        {
            if (spriteRenderer != null) defaultSprite = spriteRenderer.sprite;
        }
        public void Initialize(CollectDefinition newDefinition)
        {
            definition = newDefinition;
            ApplyVisuals();
        }
        private void ApplyVisuals()
        {
            if (spriteRenderer == null) return;
            var sprite = definition != null ? definition.Sprite : null;
            spriteRenderer.sprite = sprite != null ? sprite : defaultSprite;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || definition == null) return;

            var body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent(out Collector collector)) return;
            if (!definition.TryApply(collector, this)) return;

            collected = true;
            gameObject.SetActive(false); 
            collector.NotifyCollected(definition, this);
            Collected?.Invoke(this);
            onCollected.Invoke();
        }
    }
}
