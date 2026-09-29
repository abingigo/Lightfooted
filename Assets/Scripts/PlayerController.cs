using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
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
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            dashReady = true;
        }
        if (isDashing)
        {
            return;
        }
        if(isGrounded == true){
            isCoolDownReady = true;
        }
        Vector2 v = rb.linearVelocity;
        v.x = moveX * moveSpeed;
        rb.linearVelocity = v;

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;
            moveY = 1;
        }
        // getKey is always active when the leftshift is pressed, while getKeydown is active only at the frame when shift is pressed
        if (Input.GetKey(KeyCode.LeftShift) && (moveX != 0 || moveY != 0) && (dashReady == true) && (isCoolDownReady == true))
            {
                dashReady = false;
                isDashing = true;
                //isCoolDownReady = false;
                Vector2 dashDirection =  new Vector2(rb.linearVelocity.x * 2f, rb.linearVelocity.y).normalized;
                rb.linearVelocity = dashDirection * dashSpeed;

                Invoke(nameof(StopDash), dashDuration);
                //you dont want players to cheat by keep dashing by when they are not grounded(they can fly by doing this trick)
                if(isGrounded == false){// now they cant dash again unless they touched the grass.
                    isCoolDownReady = false;
                }
                //Invoke(nameof(ResetCooldown), dashDuration + dashCooldown);
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
