using Lighting;
using UnityEngine;

[DefaultExecutionOrder(-1001)]
[RequireComponent(typeof(PlayerController))]
public class StripGraceOnMove : MonoBehaviour
{
    [Tooltip("The exposure that gets no coyote time while the player dashes, or after a jump (the body's).")]
    [SerializeField] LightExposure exposure;

    PlayerController controller;
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
        if (exposure != null && (jumped || controller.isDashing)) exposure.ClearGrace();
        jumped = false;
    }

    void OnDisable() => jumped = false;
}
