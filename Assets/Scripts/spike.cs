using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class spike : MonoBehaviour
{
    public TMP_Text text;

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

        text.text = "Game Over!";
        Time.timeScale =  0f;
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