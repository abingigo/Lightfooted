using System;
using System.Collections.Generic;
using System.IO;
using Core;
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

        public LevelEntry(string path, string title, string description) : this(path)
        {
            if (!string.IsNullOrWhiteSpace(title)) Title = title;
            if (!string.IsNullOrWhiteSpace(description)) Description = description;
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
            if (TryGetLevelList(out var list)) return FromLevelList(list);

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

        public static LevelEntry Describe(string path)
        {
            if (TryGetLevelList(out var list))
                foreach (var level in list.Levels)
                    if (level.ScenePath == path) return new LevelEntry(path, level.Title, level.Description);
            return new LevelEntry(path);
        }

        public static string CoinSummary(string path)
        {
            if (!TryGetLevelList(out var list)) return "";
            foreach (var level in list.Levels)
            {
                if (level.ScenePath != path || level.CoinTotal <= 0) continue;
                int found = ServiceRegistry.TryGet(out IChallengeCoinService coins) ? coins.FoundIn(path) : 0;
                return $"Coins found {found}/{level.CoinTotal}";
            }
            return "";
        }

        static bool TryGetLevelList(out LevelList list)
        {
            list = ServiceRegistry.TryGet(out IGameService game) ? game.Levels : null;
            return list != null && list.Levels.Count > 0;
        }

        static List<LevelEntry> FromLevelList(LevelList list)
        {
            var result = new List<LevelEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var level in list.Levels)
                if (level.IsPlayable && seen.Add(level.ScenePath))
                    result.Add(new LevelEntry(level.ScenePath, level.Title, level.Description));
            return result;
        }
    }
}
