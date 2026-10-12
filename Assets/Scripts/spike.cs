using System.Collections.Generic;
using UnityEngine;
using LightPlatformer.UI;

public class spike : MonoBehaviour
{

    SpriteRenderer sprite;
    Collider2D colli;
    List<Collider2D> touching = new List<Collider2D>();

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();

        colli = GetComponent<Collider2D>();
    }
    void Update()
    {
        sprite.enabled = InLight();
    }

    void OnTriggerStay2D( Collider2D other)
    {
        if (!other.CompareTag("Player") || !InLight()) return;

        GameMenuController.NotifyGameOver();
    }

    bool InLight()
    {
        colli.Overlap(new ContactFilter2D { useTriggers = true }, touching);

        foreach (Collider2D thing in touching)
        {
            if (thing.CompareTag("Light")) return true;
        }

        return false;
    }
}