

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class WebAnalytics
{
    const string DeathFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdOccewU9SNPT3bMvfZjSJDJ-bvJ527IyhYUQNdcmNWljPYZA/formResponse";

    const string DeathSession = "entry.340425386";
    const string DeathLevel   = "entry.1079181997";
    const string DeathLastX   = "entry.1310378247";
    const string DeathLastY   = "entry.81663229";
    const string DeathCause   = "entry.238411755";
    const string DeathX  = "entry.1312775574";
    const string DeathY  = "entry.835351499";

    const string CompletionFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdBduAA7oLFym9Y3c_YriDZAblMv7E7gHEUPIrfiIQLy_c9gw/formResponse";
    const string CompSession = "entry.1199672331";
    const string CompLevel   = "entry.732495175";
    const string CompTime    = "entry.1069351944";
    const string CompCoin    = "entry.30866952";
    const string CompDeaths  = "entry.1016662765";

    const string ConsentKey = "analytics_consent";

    public static bool SendFromEditor = false;

    static readonly string SessionId = System.DateTime.Now.Ticks.ToString();
    static readonly HashSet<string> warned = new HashSet<string>();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void FormSubmit(string url, string body);
#endif

    public static bool HasConsent => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    public static void SetConsent(bool granted)
    {
        PlayerPrefs.SetInt(ConsentKey, granted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Once per death.
    public static void ReportDeath(string level, string cause, Vector2 lastPlatform, Vector2 deathPosition)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            Pair(DeathSession, SessionId),
            Pair(DeathLevel,   NotEmpty(level)),
            Pair(DeathLastX,   Number(lastPlatform.x)),
            Pair(DeathLastY,   Number(lastPlatform.y)),
            Pair(DeathCause,   NotEmpty(cause)),
            Pair(DeathX,       Number(deathPosition.x)),
            Pair(DeathY,       Number(deathPosition.y))
        };
        Send(DeathFormUrl, "death", fields);
    }

    // Once per completed level.
    public static void ReportCompletion(string level, float seconds, string coinStatus, int deathsBeforeCompleting)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            Pair(CompSession, SessionId),
            Pair(CompLevel,   NotEmpty(level)),
            Pair(CompTime,    Number(seconds)),
            Pair(CompCoin,    NotEmpty(coinStatus)),
            Pair(CompDeaths,  deathsBeforeCompleting.ToString(CultureInfo.InvariantCulture))
        };
        Send(CompletionFormUrl, "completion", fields);
    }

    static void Send(string url, string label, List<KeyValuePair<string, string>> fields)
    {
#if UNITY_EDITOR
        Debug.Log("[Analytics] " + label + ": " + Describe(fields));
        if (!SendFromEditor) return;
#endif

        if (!HasConsent) return;

        if (url.Contains("FORM_ID"))
        {
            if (warned.Add(label))
                Debug.LogWarning("[Analytics] The " + label + " form address in WebAnalytics.cs is still the placeholder, so nothing is being sent for it.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        FormSubmit(url, EncodeBody(fields));
#else
        WWWForm form = new WWWForm();
        foreach (KeyValuePair<string, string> f in fields) form.AddField(f.Key, f.Value);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        UnityWebRequestAsyncOperation op = www.SendWebRequest();
        op.completed += _ =>
        {
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[Analytics] " + label + " form upload failed: " + www.error);
            www.Dispose();
        };
#endif
    }

    static KeyValuePair<string, string> Pair(string key, string value)
    {
        return new KeyValuePair<string, string>(key, value);
    }

    static string NotEmpty(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? "unknown" : text;
    }

    static string Number(float value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    static string EncodeBody(List<KeyValuePair<string, string>> fields)
    {
        var sb = new StringBuilder();
        foreach (KeyValuePair<string, string> f in fields)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(UnityWebRequest.EscapeURL(f.Key)).Append('=').Append(UnityWebRequest.EscapeURL(f.Value));
        }
        return sb.ToString();
    }

    static string Describe(List<KeyValuePair<string, string>> fields)
    {
        var sb = new StringBuilder();
        foreach (KeyValuePair<string, string> f in fields)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(f.Key).Append('=').Append(f.Value);
        }
        return sb.ToString();
    }
}