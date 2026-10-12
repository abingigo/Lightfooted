using UnityEngine;
using TMPro;

public class fatalspike : MonoBehaviour
{
    public TMP_Text text;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.transform.root.CompareTag("Player")) return;

        text.text = "Game Over!";
        Time.timeScale = 0f;
    }
}