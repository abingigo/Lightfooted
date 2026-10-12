using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

namespace LightPlatformer.UI
{
    public sealed class LevelEntry
    {
        public string ScenePath { get; }
        public string Title { get; }
        public string Description { get; }

        public LevelEntry(string path)
        {
            ScenePath = path;
            var name = Path.GetFileNameWithoutExtension(path);
            Title = name == "TutorialLevel" ? "Tutorial" : Nicify(name);
            Description = name == "TutorialLevel"
                ? "Learn to move, jump, dash, and use light to stay solid."
                : name == "ObstacleLab" ? "Jump spikes, time pulsing beams, and keep moving across a crumbling bridge."
                : name == "LightPlatformLab" ? "Aim light to hold a moving platform, release it, and ride across the gap."
                : "Explore a new challenge of light and shadow.";
        }

        static string Nicify(string value)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '_' || c == '-') { result.Append(' '); continue; }
                if (i > 0 && char.IsUpper(c) && char.IsLower(value[i - 1])) result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }
    }

    public static class LevelCatalog
    {
        public const string MenuPath = "Assets/Scenes/Johnny/LevelSelect.unity";

        public static List<LevelEntry> ReadBuildLevels()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                paths.Add(SceneUtility.GetScenePathByBuildIndex(i));
            return FromPaths(paths);
        }

        public static List<LevelEntry> FromPaths(IEnumerable<string> paths)
        {
            var result = new List<LevelEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;
                var normalized = path.Replace('\\', '/');
                if (normalized == MenuPath || normalized.Contains("/Tests/") || normalized.Contains("/Testing/")) continue;
                if (Path.GetFileNameWithoutExtension(normalized).StartsWith("InitTestScene", StringComparison.Ordinal)) continue;
                if (seen.Add(normalized)) result.Add(new LevelEntry(normalized));
            }
            return result;
        }
    }
}
