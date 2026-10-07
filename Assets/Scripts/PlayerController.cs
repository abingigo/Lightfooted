using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float acceleration = 50f;
    public float jumpForce = 8f;
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;

    Rigidbody2D rb;
    bool isGrounded;
    public bool isDashing = false;
    bool isCoolDownReady = true;
    float moveX;
    float moveY;
    bool dashReady = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 1f;
    [SerializeField] private float dashCooldown = 1f;

    bool jumpQueued;
    bool dashQueued;
    Vector2 dashDirection;

    PlayerLightTracker playerLightTracker;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerLightTracker = GetComponent<PlayerLightTracker>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        moveX = Input.GetAxisRaw("Horizontal");
        moveY = 0;
        if (Input.GetButton("Jump"))
        {
            moveY = 1;
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            jumpQueued = true;
        }

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            dashReady = true;
        }

        if (isDashing)
        {
            return;
        }

        if (isGrounded)
        {
            isCoolDownReady = true;
        }

        if (Input.GetKey(KeyCode.LeftShift) && (moveX != 0 || moveY != 0) && dashReady && isCoolDownReady)
        {
            dashReady = false;
            isDashing = true;

            float dirX = rb.linearVelocity.x != 0f ? Mathf.Sign(rb.linearVelocity.x)
                       : moveX != 0f ? Mathf.Sign(moveX)
                       : 1f;
            dashDirection = new Vector2(dirX, 0f);

            dashQueued = true;

            Invoke(nameof(StopDash), dashDuration);

            if (!isGrounded)
            {
                isCoolDownReady = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (dashQueued)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(dashDirection * dashSpeed * rb.mass, ForceMode2D.Impulse);
            dashQueued = false;
        }

        if (isDashing)
        {
            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            return;
        }

        float targetVelocityX = moveX * moveSpeed;
        float velocityDiff = targetVelocityX - rb.linearVelocity.x;
        rb.AddForce(new Vector2(velocityDiff * acceleration, 0f), ForceMode2D.Force);

        if (jumpQueued)
        {
            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector2.up * jumpForce * rb.mass, ForceMode2D.Impulse);
            jumpQueued = false;
        }
    }

    private void StopDash()
    {
        isDashing = false;
        playerLightTracker.UpdateCollision();
    }

    private void ResetCooldown()
    {
        isCoolDownReady = true;
    }
}