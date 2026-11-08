using Lighting;
using UnityEngine;

/// Fades the player's sprites while they're out of the light, so it's clear they've stopped being solid.
public class PlayerLightFade : MonoBehaviour
{
    [SerializeField] LightExposure exposure;

    [SerializeField] SpriteRenderer[] sprites = { };

    [SerializeField, Range(0f, 1f)] float darkAlpha = 0.4f;

    [SerializeField, Min(0.01f)] float fadeSeconds = 0f;

    float[] baseAlphas;
    float fade;

    void Reset() => exposure = GetComponent<LightExposure>();

    void Awake()
    {
        if (exposure == null) exposure = GetComponent<LightExposure>();
        if (sprites == null || sprites.Length == 0) sprites = GetComponentsInChildren<SpriteRenderer>(true);

        baseAlphas = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            baseAlphas[i] = sprites[i] != null ? sprites[i].color.a : 1f;
    }

    void LateUpdate()
    {
        bool lit = exposure == null || exposure.IsExposed;
        float target = lit ? 0f : 1f;
        if (Mathf.Approximately(fade, target)) return;

        fade = Mathf.MoveTowards(fade, target, Time.deltaTime / fadeSeconds);
        float strength = Mathf.Lerp(1f, darkAlpha, fade);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color color = sprites[i].color;
            color.a = baseAlphas[i] * strength;
            sprites[i].color = color;
        }
    }
}
