using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Coin : MonoBehaviour
{
    [Tooltip("A TextMeshPro - Text (UI) object in your Canvas. Leave its GameObject unchecked (inactive) so it starts hidden.")]
    public TMP_Text text;

    public string message = "Coin collected!";

    [Tooltip("How long the message stays on screen.")]
    public float messageDuration = 2f;

    static int latestMessage;

    bool collected;

    void Start()
    {
        LevelStats.RegisterCoinLevel(SceneManager.GetActiveScene().name);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.transform.root.CompareTag("Player")) return;

        collected = true;
        LevelStats.CoinCollected = true;
        StartCoroutine(Collect());
    }

    IEnumerator Collect()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        int myMessage = ++latestMessage;
        if (text != null)
        {
            text.text = message;
            text.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(messageDuration);

        if (text != null && myMessage == latestMessage)
            text.gameObject.SetActive(false);

        Destroy(gameObject);
    }
}