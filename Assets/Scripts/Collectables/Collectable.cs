using UnityEngine;

namespace Collectables
{
    [RequireComponent(typeof(Collider2D))] // please set to is_trigger
    public abstract class Collectable : MonoBehaviour
    {
        [SerializeField] private string collectorTag = "Player";
        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag(collectorTag)) return;
            collected = true;
            OnCollected(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
            Destroy(gameObject);
        }
    
        protected abstract void OnCollected(GameObject collector);
    }
}
