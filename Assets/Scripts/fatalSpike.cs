using UnityEngine;
using LightPlatformer.UI;

public class fatalspike : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")|| other.isTrigger) return; //use this for now ig lol
        

        GameMenuController.NotifyGameOver();
    }
}