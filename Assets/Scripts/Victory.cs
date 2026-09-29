using UnityEngine;
using TMPro;

public class Victory : MonoBehaviour
{

    public TMP_Text text;
    public string message = "You win!";

    public void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Player")
        {
            text.text = message;
            Time.timeScale = 0f;
        }
    }
}
