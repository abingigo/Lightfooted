using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

public class GhostChase : MonoBehaviour
{
    public Transform player;
    public Light2D flashlight;
    public float chaseRange = 8f;
    public float moveSpeed = 3f;


    
    public float stopDistance = 0.5f;
    public TMP_Text text;
    public string message = "Game Over!";

    Rigidbody2D rb;
    SpriteRenderer sprite;
    PolygonCollider2D lightArea;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
            sprite = GetComponent<SpriteRenderer>();

        lightArea = flashlight.GetComponent<PolygonCollider2D>();
    }

    void FixedUpdate()
    {
        if (flashlight.isActiveAndEnabled && lightArea.OverlapPoint(rb.position))
        {
            rb.linearVelocity = Vector2.zero;



            return;
        }

        Vector2 direction = (Vector2)player.position - rb.position;
        float distance = direction.magnitude;

        if (distance > stopDistance && distance <= chaseRange)
        {
            rb.linearVelocity = direction.normalized *  moveSpeed;

            sprite.flipX = direction.x < 0;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            text.text = message;
             Time.timeScale = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
             Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}