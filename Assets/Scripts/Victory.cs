using UnityEngine;

public class Victory : MonoBehaviour
{
    bool hasEnded;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (!hasEnded && other.CompareTag("Player"))
        {
            hasEnded = true;
            LightPlatformer.UI.GameMenuController.NotifyVictory();
        }
    }
}