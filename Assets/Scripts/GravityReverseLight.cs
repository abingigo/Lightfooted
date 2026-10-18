using UnityEngine;

public class GravityReverseLight : MonoBehaviour
{
    Rigidbody2D playerRb;
    float normalGravity;
    public float gravityScale = 3f;

    void Start()
    {
        playerRb = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
        normalGravity =  playerRb.gravityScale;
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerRb.gravityScale = -normalGravity / gravityScale;
    }

    void OnTriggerExit2D(Collider2D other)
    {
       if (!other.CompareTag("Player")) return;

        playerRb.gravityScale = normalGravity;
    }
}