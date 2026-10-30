using UnityEngine;

public class GameEndText : MonoBehaviour
{
    public float EndY = -20f;
    bool hasEnded;

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (!hasEnded && transform.position.y < EndY)
        {
            hasEnded = true;
            LightPlatformer.UI.GameMenuController.NotifyGameOver("fall");
        }
    }
}