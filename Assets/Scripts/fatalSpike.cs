using UnityEngine;
using LightPlatformer.Obstacles;

public class fatalSpike : MonoBehaviour
{
    Collider2D col;
    void Awake()
    {
        col =  GetComponent<Collider2D>();
    }

    void OnTriggerStay2D (Collider2D other)
    {
        HazardContact.Hurt(other, col, "spike");
    }
}