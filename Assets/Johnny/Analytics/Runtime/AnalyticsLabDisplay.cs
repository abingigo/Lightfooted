using UnityEngine;

public sealed class AnalyticsLabDisplay : MonoBehaviour
{
    void OnGUI()
    {
        var service = AnalyticsService.Instance;
        if (service == null || service.Recorder == null || service.Queue == null) return;
        if (LightPlatformer.UI.GameMenuController.Instance?.State != LightPlatformer.UI.MenuState.Playing) return;
        float scale = Mathf.Clamp(Screen.height / 720f, 0.85f, 1.5f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14 * scale), wordWrap = true };
        GUILayout.BeginArea(new Rect(16, Screen.height - 132 * scale, Mathf.Min(720 * scale, Screen.width - 32), 116 * scale), GUI.skin.box);
        GUILayout.Label($"ANALYTICS  |  Active: {service.Recorder.ActiveSeconds:F1}s  |  Elapsed: {service.Recorder.ElapsedSeconds:F1}s", style);
        GUILayout.Label($"Pending: {service.Queue.Count}  |  {service.DeliveryStatus}", style);
        GUILayout.Label("Cross blue markers A and B. Jump the red block. Reach the green exit.", style);
        GUILayout.Label("Escape pauses. Restart starts a new attempt. Markers do not change respawn.", style);
        if (!string.IsNullOrEmpty(service.Queue.LastError)) GUILayout.Label("Storage: " + service.Queue.LastError, style);
        GUILayout.EndArea();
    }
}
