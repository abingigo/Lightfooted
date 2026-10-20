// using UnityEngine;
using UnityEngine;

public class LanternPickup : MonoBehaviour
{
    public GameObject playerLantern;
    public Collider2D platform;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!playerLantern.activeSelf)
        {
            playerLantern.SetActive(true);
        }
        else
        {
            playerLantern.GetComponent<LanternTimer>().ResetTimer();
        }

        var tracker = other.GetComponentInParent<PlayerLightTracker>();
        if (tracker != null)
        {
            tracker.hasLantern = true;
            tracker.UpdateCollision();
        }
        platform.isTrigger = false;
        Destroy(gameObject);
    }
}