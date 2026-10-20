using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AnalyticsWebBuild
{
    [MenuItem("Tools/Johnny Analytics/Build Alpha WebGL")]
    public static void BuildAlpha() { Build(false); }
    public static void BuildQa() { Build(true); }

    static void Build(bool qa)
    {
        string output = qa ? "tmp/AnalyticsWebReview/qa/play" : "tmp/AnalyticsWebReview/alpha/play";
        Directory.CreateDirectory(output);
        var scenes = EditorBuildSettings.scenes.Where(item => item.enabled).Select(item => item.path).ToList();
        if (qa) { scenes.Remove(AnalyticsLabSetup.ScenePath); scenes.Insert(0, AnalyticsLabSetup.ScenePath); }
        else scenes.Remove(AnalyticsLabSetup.ScenePath);
        string version = PlayerSettings.bundleVersion;
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        try
        {
            PlayerSettings.bundleVersion = qa ? "alpha-2026-10-20-qa" : "alpha-2026-10-20";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            var report = BuildPipeline.BuildPlayer(scenes.ToArray(), output, BuildTarget.WebGL, qa ? BuildOptions.Development : BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed: " + report.summary.result);
            string page = Path.Combine(output, "index.html");
            string html = File.ReadAllText(page);
            html = html.Replace("<body>", "<body><p style=\"margin:10px;text-align:center;font:14px system-ui\">Lightfooted Alpha · Playtest records level time, progress, and outcomes. <a href=\"/analytics/\">View playtest graphs</a></p>");
            File.WriteAllText(page, html);
            Debug.Log("ANALYTICS_WEBGL_BUILD_SUCCEEDED " + output + " " + report.summary.totalSize);
        }
        finally
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
        }
    }
}
