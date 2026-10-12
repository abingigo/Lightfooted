using UnityEngine;
using LightPlatformer.UI;

public class fatalspike : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.transform.root.CompareTag("Player")) return;

        GameMenuController.NotifyGameOver();
    }
}