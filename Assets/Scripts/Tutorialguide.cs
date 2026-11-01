using TMPro;
using UnityEngine;

/// Walks the player through the controls one step at a time. A step's message
/// appears once the player reaches its marker, and the guide moves on only when
/// the player actually does the thing: move, jump, dash, or jump-dash.
///
/// Put it on any object in the tutorial scene, then fill in the steps in the Inspector.
[DefaultExecutionOrder(100)]
public class TutorialGuide : MonoBehaviour
{
    public enum Goal
    {
        MoveSideways, // walk a few units away from where the step began
        Jump,         // press jump while standing on the ground
        GroundDash,   // start a dash while standing on the ground
        JumpDash,     // start a dash while in the air
        None          // just show the message (use for the last step only)
    }

    [System.Serializable]
    public class Step
    {
        [TextArea] public string message;
        public Goal goal;

        [Tooltip("Optional. The message appears once the player's X passes this object's X. Leave empty to show it right away.")]
        public Transform showWhenPlayerPasses;
    }

    public PlayerController player;

    [Tooltip("A TextMeshPro - Text (UI) object. It's cleared while no step is showing.")]
    public TMP_Text text;

    public Step[] steps;

    [Tooltip("How far the player must walk to finish the move step.")]
    public float moveDistance = 3f;

    [Tooltip("Short message shown when a step is completed.")]
    public string successMessage = "Nice!";
    public float successSeconds = 1f;

    [Tooltip("If on, steps the player already completed are skipped after a death or restart.")]
    public bool rememberProgress = true;

    [Header("Follow the player")]
    [Tooltip("Keeps the message just above the player so it never leaves the screen.")]
    public bool followPlayer = true;
    public Vector3 followOffset = new Vector3(0f, 2.5f, 0f);

    [Tooltip("Optional. If set, the message stays at this object's height and only follows the player sideways. Place it below the level.")]
    public Transform fixedHeight;

    [Tooltip("How far (in world units) to keep the message from the screen edges. About half the message's width works well.")]
    public float edgePadding = 4f;

    // Kept between scene reloads so a retry doesn't repeat what the player already learned.
    static int savedStep;

    Camera cam;

    int index;
    bool stepShowing;
    float moveStartX;
    float successTimer;
    bool wasDashing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        savedStep = 0;
    }

    void Start()
    {
        cam = Camera.main;
        index = rememberProgress ? Mathf.Min(savedStep, steps.Length) : 0;
        SetText("");
    }

    void Update()
    {
        if (player == null || steps == null || index >= steps.Length) return;

        // Don't count key presses made in the pause menu or during loading.
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked)
        {
            wasDashing = player.isDashing;
            return;
        }

        // Show the "Nice!" message for a moment before the next step appears.
        if (successTimer > 0f)
        {
            successTimer -= Time.deltaTime;
            if (successTimer > 0f) return;
            SetText("");
        }

        Step step = steps[index];

        // Wait until the player reaches this step's marker.
        if (!stepShowing)
        {
            if (step.showWhenPlayerPasses != null &&
                player.transform.position.x < step.showWhenPlayerPasses.position.x)
            {
                wasDashing = player.isDashing;
                return;
            }

            stepShowing = true;
            moveStartX = player.transform.position.x;
            SetText(step.message);
        }

        bool dashStarted = player.isDashing && !wasDashing;
        wasDashing = player.isDashing;

        bool done = false;
        switch (step.goal)
        {
            case Goal.MoveSideways:
                done = Mathf.Abs(player.transform.position.x - moveStartX) >= moveDistance;
                break;
            case Goal.Jump:
                done = player.IsGrounded && Input.GetButtonDown("Jump");
                break;
            case Goal.GroundDash:
                done = dashStarted && player.IsGrounded;
                break;
            case Goal.JumpDash:
                done = dashStarted && !player.IsGrounded;
                break;
        }

        if (done) Advance();
    }

    // Moves the message to just above the player every frame. This runs after the
    // camera has moved (see DefaultExecutionOrder), so it never lags behind.
    void LateUpdate()
    {
        if (!followPlayer || player == null || text == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 target = player.transform.position + followOffset;
        if (fixedHeight != null) target.y = fixedHeight.position.y;

        // Keep the message inside what the camera can see, so it isn't cut off
        // when the player is near the edge of the screen.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth + edgePadding;
        float maxX = camPos.x + halfWidth - edgePadding;
        if (minX <= maxX) target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, camPos.y - halfHeight + 1f, camPos.y + halfHeight - 1f);

        // A screen-space canvas works in pixels; anything else works in world units.
        Canvas canvas = text.canvas;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            text.transform.position = cam.WorldToScreenPoint(target);
        else
            text.transform.position = new Vector3(target.x, target.y, text.transform.position.z);
    }

    void Advance()
    {
        index++;
        stepShowing = false;
        if (rememberProgress) savedStep = index;

        successTimer = successSeconds;
        SetText(successMessage);
    }

    void SetText(string value)
    {
        if (text != null) text.text = value;
    }
}