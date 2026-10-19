using UnityEngine;

public class LanternTimer : MonoBehaviour
{
    public float duration = 5f;
    PlayerLightTracker playerTrack;

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
        Invoke(nameof(TurnOff), duration);
    }

    void TurnOff()
    {
        gameObject.SetActive(false);
        if (playerTrack != null) playerTrack.DropLantern();
    }
}