using UnityEngine;

public sealed class KeepWorldScale : MonoBehaviour
{
    [Tooltip("World scale kept at play time, whatever the parent's scale.")]
    [SerializeField] Vector3 worldScale = Vector3.one;

    void Awake()
    {
        var parent = transform.parent;
        if (parent == null) return;
        var scale = parent.lossyScale;
        transform.localScale = new Vector3(Divide(worldScale.x, scale.x), Divide(worldScale.y, scale.y), Divide(worldScale.z, scale.z));
    }

    static float Divide(float value, float by) => Mathf.Approximately(by, 0f) ? value : value / by;
}
