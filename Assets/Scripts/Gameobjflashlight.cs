using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    public GameObject Gameobjflashlight;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Gameobjflashlight.SetActive(true);
        Destroy(gameObject);
    }
}