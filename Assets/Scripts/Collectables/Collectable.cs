using System;
using UnityEngine;

namespace Collectables
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))] // please set to is_trigger
    public sealed class Collectable : MonoBehaviour
    {
        [SerializeField] private CollectDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        private Sprite defaultSprite;
        private bool collected;
        
        public CollectDefinition Definition => definition;
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
            if (!definition.TryApply(collector)) return;

            collected = true;
            gameObject.SetActive(false); 
            collector.NotifyCollected(definition);
            Collected?.Invoke(this);
        }
    }
}
