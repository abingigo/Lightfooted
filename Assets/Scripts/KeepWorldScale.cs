using UnityEngine;

[ExecuteAlways]
public sealed class KeepWorldScale : MonoBehaviour
{
    [Tooltip("World scale kept whatever the parent's scale, in the editor and at play time.")]
    [SerializeField] Vector3 worldScale = Vector3.one;

    void Awake() => Apply();

    void Update()
    {
        if (!Application.isPlaying) Apply();
    }

    void Apply()
    {
        var parent = transform.parent;
        if (parent == null) return;
        var scale = parent.lossyScale;
        var wanted = new Vector3(Divide(worldScale.x, scale.x), Divide(worldScale.y, scale.y), Divide(worldScale.z, scale.z));
        if ((transform.localScale - wanted).sqrMagnitude > 1e-8f) transform.localScale = wanted;
    }

    static float Divide(float value, float by) => Mathf.Approximately(by, 0f) ? value : value / by;
}
