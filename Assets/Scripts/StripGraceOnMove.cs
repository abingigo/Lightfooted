using Lighting;
using UnityEngine;

[DefaultExecutionOrder(-1001)]
[RequireComponent(typeof(PlayerController))]
public class StripGraceOnMove : MonoBehaviour
{
    [Tooltip("The exposure whose coyote time ends when the player dashes or jumps (the body's).")]
    [SerializeField] LightExposure exposure;

    PlayerController controller;
    bool wasDashing;
    bool jumped;

    void Reset() => exposure = GetComponent<LightExposure>();

    void Awake() => controller = GetComponent<PlayerController>();

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (Input.GetButtonDown("Jump") && controller.IsGrounded && !controller.isDashing) jumped = true;
    }

    void FixedUpdate()
    {
        bool dashing = controller.isDashing;
        if (exposure != null && (jumped || (dashing && !wasDashing))) exposure.ClearGrace();
        jumped = false;
        wasDashing = dashing;
    }

    void OnDisable()
    {
        jumped = false;
        wasDashing = false;
    }
}
