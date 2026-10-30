using UnityEngine;

public class PlayerDeathContext : MonoBehaviour
{
    public static PlayerDeathContext Current { get; private set; }

    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundCheckRadius = 0.15f;

    public Vector2 LastGroundedPosition { get; private set; }

    void Awake()
    {
        Current = this;
        LastGroundedPosition = transform.position;
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Update()
    {
        if (groundCheck == null) return;

        if (Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask))
        {
            LastGroundedPosition = transform.position;
        }
    }
}