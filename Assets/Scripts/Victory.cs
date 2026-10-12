using UnityEngine;
using TMPro;

public class Victory : MonoBehaviour
{

    public TMP_Text text;
    public string message = "You win!";
    bool hasEnded;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if(!hasEnded && other.CompareTag("Player"))
        {
            hasEnded = true;
            if (text != null) text.text = message;
            Time.timeScale = 0f;
            LightPlatformer.UI.GameMenuController.NotifyVictory();
        }
    }
}
