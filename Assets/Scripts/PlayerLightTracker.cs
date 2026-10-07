using System.Collections.Generic;
using UnityEngine;

public class PlayerLightTracker : MonoBehaviour
{
    public Collider2D collisionBox;
    public bool hasLantern = false; //to check if player has a lantern.
    public string lightTag = "Light";

    readonly List<Collider2D> lightsInside = new List<Collider2D>();

    PlayerController playerController;

    void Awake()
    {
        if (collisionBox == null)
            Debug.LogWarning("PlayerLightTracker: Collision Box not assigned in the Inspector.");

        playerController = GetComponent<PlayerController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(lightTag)) return;
        if (!lightsInside.Contains(other))
            lightsInside.Add(other);

        UpdateCollision();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(lightTag)) return;
        lightsInside.Remove(other);

        UpdateCollision();
    }

    public void UpdateCollision()
    {
        if (collisionBox == null) return;

        collisionBox.enabled = hasLantern || lightsInside.Count > 0;
    }

    public void DropLantern()
    {
        hasLantern = false;
        UpdateCollision();
    }
}