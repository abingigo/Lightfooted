using Lighting;
using UnityEngine;

[DefaultExecutionOrder(-1001)]
[RequireComponent(typeof(PlayerController))]
public class DashLightGrace : MonoBehaviour
{
    [Tooltip("The exposure whose grace countdown pauses while dashing (the body's).")]
    [SerializeField] LightExposure exposure;

    [Tooltip("Physics steps of grace after a dash that ends while still exposed (50 steps = 1 s).")]
    [SerializeField, Min(0)] int graceAfterDash = 15;

    PlayerController controller;
    bool wasDashing;

    void Reset() => exposure = GetComponent<LightExposure>();

    void Awake() => controller = GetComponent<PlayerController>();

    void FixedUpdate()
    {
        if (exposure == null) return;

        bool dashing = controller.isDashing;
        if (wasDashing && !dashing) exposure.RefillGrace(graceAfterDash);
        exposure.GracePaused = dashing;
        wasDashing = dashing;
    }

    void OnDisable()
    {
        if (exposure != null) exposure.GracePaused = false;
        wasDashing = false;
    }
}