using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class BlinkingLight : MonoBehaviour
{
    public Light2D light2D;
    public float onTime = 2f, offTime = 1f;
    public bool IsOn { get; private set; } = true;
    public float waitTime = 0f;

    [Tooltip("The light's area. Uses the collider on this object if empty; any shape works.")]
    [SerializeField] Collider2D col;

    [Tooltip("Whether the light is on during the Wait Time before its first switch.")]
    [SerializeField] bool startOn = true;

    void Start()
    {
        if (col == null) col = GetComponent<Collider2D>();
        IsOn = startOn;
        light2D.enabled = startOn;
        col.enabled = startOn;
        StartCoroutine(Blink());
    }

    // Each switch is scheduled from the level's start rather than from the previous switch, so frame
    // delays don't add up and lights with matching times keep switching on the same frame.
    IEnumerator Blink()
    {
        float next = Time.time + waitTime;
        var wait = new WaitUntil(() => Time.time >= next);
        yield return wait;
        while (true)
        {
            if (IsOn)
            {
                light2D.enabled = false;
                IsOn = false;
                col.enabled = false;
                next += offTime;
            }
            else
            {
                light2D.enabled = true;
                IsOn = true;
                col.enabled = true;
                next += onTime;
            }
            yield return wait;
        }
    }
}
