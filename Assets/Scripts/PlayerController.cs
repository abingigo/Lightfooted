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
    [Tooltip("If set, the ground check is a box of this size instead of a circle. Match the feet's width.")]
    public Vector2 groundCheckSize;

    Rigidbody2D rb;
    bool isGrounded;
    public bool IsGrounded => isGrounded;
    public bool isDashing = false;
    bool isCoolDownReady = true;
    readonly ContactPoint2D[] dashSupportContact = new ContactPoint2D[1];
    float moveX;
    float moveY;
    bool dashReady = true;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 1f;
    [SerializeField] private float dashCooldown = 1f;
    [Tooltip("Cancel gravity while dashing so the dash stays level instead of sinking each physics step.")]
    [SerializeField] private bool holdDashHeight;

    bool jumpQueued;
    bool dashQueued;
    Vector2 dashDirection;

    PlayerLightTracker playerLightTracker;

    /// One frame of controls.
    public struct InputFrame
    {
        public float horizontal;
        public bool jumpHeld;
        public bool jumpPressed;
        public bool dashHeld;
    }

    /// When set, the controller follows this instead of the keyboard and keeps moving during
    /// gameplay holds. Called once per frame. The tutorial's ghost demo uses it.
    public System.Func<InputFrame> scriptedInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerLightTracker = GetComponent<PlayerLightTracker>();
    }

    void Update()
    {
        if (scriptedInput == null && LightPlatformer.UI.GameMenuController.GameplayInputBlocked)
        {
            ClearQueuedInput();
            return;
        }
        InputFrame input = scriptedInput != null ? scriptedInput() : ReadKeyboard();

        isGrounded = groundCheckSize.x > 0f && groundCheckSize.y > 0f
            ? Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundMask)
            : Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

        moveX = input.horizontal;
        moveY = 0;
        if (input.jumpHeld)
        {
            moveY = 1;
        }

        if (input.jumpPressed && isGrounded && !isDashing)
        {
            jumpQueued = true;
        }

        if (!input.dashHeld)
        {
            dashReady = true;
        }

        if (isDashing)
        {
            return;
        }

        bool hasDashSupport = HasDashSupport();
        if (hasDashSupport)
        {
            isCoolDownReady = true;
        }

        if (input.dashHeld && (moveX != 0 || moveY != 0) && dashReady && isCoolDownReady)
        {
            dashReady = false;
            isDashing = true;

            float dirX = rb.linearVelocity.x != 0f ? Mathf.Sign(rb.linearVelocity.x)
                       : moveX != 0f ? Mathf.Sign(moveX)
                       : 1f;
            dashDirection = new Vector2(dirX, 0f);

            dashQueued = true;

            Invoke(nameof(StopDash), dashDuration);

            if (!hasDashSupport)
            {
                isCoolDownReady = false;
            }
        }
    }

    bool HasDashSupport()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundMask);
        float supportAngle = Physics2D.gravity.y * rb.gravityScale <= 0f ? 90f : 270f;
        filter.SetNormalAngle(supportAngle - 45f, supportAngle + 45f);
        return rb.GetContacts(filter, dashSupportContact) > 0;
    }
    
    static InputFrame ReadKeyboard() => new InputFrame
    {
        horizontal = Input.GetAxisRaw("Horizontal"),
        jumpHeld = Input.GetButton("Jump"),
        jumpPressed = Input.GetButtonDown("Jump"),
        dashHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
    };

    void FixedUpdate()
    {
        if (scriptedInput == null && LightPlatformer.UI.GameMenuController.GameplayInputBlocked)
        {
            ClearQueuedInput();
            return;
        }
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
            if (holdDashHeight) rb.AddForce(-Physics2D.gravity * (rb.gravityScale * rb.mass), ForceMode2D.Force);
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

    void ClearQueuedInput()
    {
        moveX = moveY = 0;
        jumpQueued = false;
        if (dashQueued)
        {
            dashQueued = false;
            isDashing = false;
            CancelInvoke(nameof(StopDash));
        }
    }

    private void StopDash()
    {
        isDashing = false;
        if (playerLightTracker != null) playerLightTracker.UpdateCollision();
    }

    private void ResetCooldown()
    {
        isCoolDownReady = true;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = !Application.isPlaying ? Color.yellow : isGrounded ? Color.green : Color.red;
        if (groundCheckSize.x > 0f && groundCheckSize.y > 0f)
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        else
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
