using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(ControllableLight))]
public sealed class SelectionTint : MonoBehaviour
{
    [Tooltip("Light colour while this light is selected.")]
    [SerializeField] Color selectedColor = new(0.3f, 0.85f, 1f, 1f);

    [Tooltip("Light to tint. Found on this object if left empty.")]
    [SerializeField] Light2D light2D;

    ControllableLight controller;
    Color normalColor;
    bool tinted;

    void Awake()
    {
        controller = GetComponent<ControllableLight>();
        if (light2D == null) light2D = GetComponent<Light2D>();
        if (light2D != null) normalColor = light2D.color;
    }

    void LateUpdate()
    {
        if (light2D == null || controller.IsSelected == tinted) return;
        tinted = controller.IsSelected;
        light2D.color = tinted ? selectedColor : normalColor;
    }
}
