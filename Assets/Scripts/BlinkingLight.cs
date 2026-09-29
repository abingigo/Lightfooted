using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class BlinkingLight : MonoBehaviour
{
    public Light2D light2D;
    public bool IsOn { get; private set; } = true;
    public float waitTime = 0f;

    CircleCollider2D col;

    void Start()
    {
        col = GetComponent<CircleCollider2D>();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        yield return new WaitForSeconds(waitTime);
        while (true)
        {
            if (IsOn)
            {
                light2D.enabled = false;
                IsOn = false;
                col.enabled = false;
                yield return new WaitForSeconds(1f);
            }
            else
            {
                light2D.enabled = true;
                IsOn = true;
                col.enabled = true;
                yield return new WaitForSeconds(2f);
            }
        }
    }
}