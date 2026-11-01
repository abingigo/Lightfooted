using UnityEngine;

public class LanternTimer : MonoBehaviour, LightPlatformer.UI.ILanternStatus
{
    public float duration = 5f;
    PlayerLightTracker playerTrack;
    float expiresAt;

    public Transform Anchor => playerTrack != null ? playerTrack.transform : transform;
    public float RemainingSeconds => isActiveAndEnabled ? Mathf.Max(0f, expiresAt - Time.time) : 0f;
    public float DurationSeconds { get; private set; }

    void Awake()
    {
        playerTrack = GetComponentInParent<PlayerLightTracker>();
    }

    void OnEnable()
    {
        ResetTimer();
    }

    void OnDisable()
    {
        CancelInvoke(nameof(TurnOff));
    }

    public void ResetTimer()
    {
        CancelInvoke(nameof(TurnOff));
        DurationSeconds = duration;
        expiresAt = Time.time + duration;
        Invoke(nameof(TurnOff), duration);
    }

    void TurnOff()
    {
        gameObject.SetActive(false);
        if (playerTrack != null) playerTrack.DropLantern();
    }
}
