using System.Collections;
using Collectables;
using TMPro;
using UnityEngine;

public sealed class PickupMessage : MonoBehaviour
{
    [Tooltip("Text to show. Can start inactive; it is shown and hidden again. Keep this component on an active object, e.g. the Canvas.")]
    [SerializeField] TMP_Text text;

    [Tooltip("Seconds the message stays on screen.")]
    [SerializeField, Min(0.1f)] float duration = 2f;

    Collector collector;
    Coroutine showing;

    void Start()
    {
        collector = FindAnyObjectByType<Collector>();
        if (collector != null) collector.Collected += OnCollected;
        else Debug.LogWarning($"{name}: PickupMessage found no Collector (player) in the scene.", this);
    }

    void OnDestroy()
    {
        if (collector != null) collector.Collected -= OnCollected;
    }

    void OnCollected(CollectDefinition definition) => Show(definition.PickupText);

    public void Show(string value)
    {
        if (text == null || string.IsNullOrEmpty(value) || !isActiveAndEnabled) return;
        if (showing != null) StopCoroutine(showing);
        showing = StartCoroutine(Display(value));
    }

    IEnumerator Display(string value)
    {
        text.text = value;
        text.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        text.gameObject.SetActive(false);
        showing = null;
    }
}
