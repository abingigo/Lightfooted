using UnityEngine;
using TMPro;

public class GameEndText : MonoBehaviour
{
    public float EndY = -20f; 
    public TMP_Text text;
    public string message = "Game Over!";

    void Update()
    {
        if (transform.position.y < EndY)
        {
            text.text = message;
            Time.timeScale = 0f;
        }
    }
}
