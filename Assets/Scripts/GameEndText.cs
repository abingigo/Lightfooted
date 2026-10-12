using UnityEngine;
using TMPro;

public class GameEndText : MonoBehaviour
{
    public float EndY = -20f; 
    public TMP_Text text;
    public string message = "Game Over!";
    bool hasEnded;

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (!hasEnded && transform.position.y < EndY)
        {
            hasEnded = true;
            if (text != null) text.text = message;
            Time.timeScale = 0f;
            LightPlatformer.UI.GameMenuController.NotifyGameOver();
        }
    }
}
