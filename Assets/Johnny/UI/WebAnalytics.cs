using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class WebAnalytics
{
    const string MeasurementId = "G-VGZFRN1EQG";
    const string ConsentKey = "analytics_consent";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void AnalyticsStart(string measurementId);
    [DllImport("__Internal")] static extern void AnalyticsTrackEvent(string name, string json);
#endif

    public static bool HasConsent => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    public static void SetConsent(bool granted)
    {
        PlayerPrefs.SetInt(ConsentKey, granted ? 1 : 0);
        PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
        if (granted) AnalyticsStart(MeasurementId);
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (HasConsent) AnalyticsStart(MeasurementId);
#endif
    }

    // Usage: WebAnalytics.Track("level_completed", ("level", "Tutorial"), ("seconds", 42.5f));
    public static void Track(string eventName, params (string key, object value)[] data)
    {
        string json = BuildJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!HasConsent) return;
        AnalyticsTrackEvent(eventName, json);
#else
        Debug.Log("[Analytics] " + eventName + " " + json);
#endif
    }

    static string BuildJson((string key, object value)[] data)
    {
        var sb = new StringBuilder("{");
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Escape(data[i].key)).Append("\":");

            object v = data[i].value;
            if (v is bool b) sb.Append(b ? "true" : "false");
            else if (v is int || v is long || v is float || v is double)
                sb.Append(System.Convert.ToString(v, CultureInfo.InvariantCulture));
            else sb.Append('"').Append(Escape(v == null ? "" : v.ToString())).Append('"');
        }
        return sb.Append('}').ToString();
    }

    static string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
