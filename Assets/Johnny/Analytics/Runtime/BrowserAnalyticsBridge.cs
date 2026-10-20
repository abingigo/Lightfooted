using System;
using System.Runtime.InteropServices;

public static class BrowserAnalyticsBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern string LF_AnalyticsRead(string key);
    [DllImport("__Internal")] static extern int LF_AnalyticsWrite(string key, string value);
    [DllImport("__Internal")] static extern void LF_AnalyticsAttach(string receiver, string endpoint, string key);
#endif
    public static string Read(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return LF_AnalyticsRead(key);
#else
        return null;
#endif
    }
    public static bool Write(string key, string value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return LF_AnalyticsWrite(key, value) == 1;
#else
        return false;
#endif
    }
    public static void Attach(string receiver, string endpoint, string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LF_AnalyticsAttach(receiver, endpoint, key);
#endif
    }
}
