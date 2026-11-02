using UnityEngine;

namespace Core
{
    public abstract class CheckpointState : MonoBehaviour
    {
        [Tooltip("Assigned automatically when the scene is saved. Matches this object across a checkpoint reload.")]
        [SerializeField]
        private string id;

        public string Id => id;

        public abstract string Capture();

        public abstract void Restore(string data);

        public virtual void RestoreConsumed()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
