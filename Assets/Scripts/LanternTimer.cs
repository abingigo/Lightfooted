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
        Invoke(nameof(TurnOff), duration);
    }

    void TurnOff()
    {
        gameObject.SetActive(false);
        playerTrack.DropLantern();
    }
}
