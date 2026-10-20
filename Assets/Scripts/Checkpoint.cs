using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {





        if (!other.CompareTag("Player") ||!(other is CircleCollider2D))
        {
            return;
        }

        CheckpointProgress.savePos  = transform.position;
            CheckpointProgress.saveScene = gameObject.scene.path;
        CheckpointProgress.reachedCheckpoint = true;
    }
}