using UnityEngine;

public class LanternPickup : MonoBehaviour

{
    public GameObject playerLantern;
    public Collider2D platform;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerLantern.SetActive(true);
        other.GetComponentInParent<PlayerLightTracker>().hasLantern = true; //tracker now know player has the lantern
        other.GetComponentInParent<PlayerLightTracker>().collisionBox.enabled = true; // collision box will be set to TRUE as long as player has the lantern
        platform.isTrigger = false;
        Destroy(gameObject);
    }
}