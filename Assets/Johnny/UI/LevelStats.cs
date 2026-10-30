using System.Collections.Generic;
using UnityEngine;

public static class LevelStats
{
    public static float AttemptSeconds { get; set; }

    public static bool CoinCollected { get; set; }

    static readonly HashSet<string> levelsWithCoin = new HashSet<string>();
    static readonly Dictionary<string, int> deaths = new Dictionary<string, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        AttemptSeconds = 0f;
        CoinCollected = false;
        levelsWithCoin.Clear();
        deaths.Clear();
    }

    public static void RegisterCoinLevel(string level)
    {
        levelsWithCoin.Add(level);
    }

    public static string CoinStatus(string level)
    {
        if (!levelsWithCoin.Contains(level)) return "no coin in level";
        return CoinCollected ? "yes" : "no";
    }

    public static void RecordDeath(string level)
    {
        deaths.TryGetValue(level, out int count);
        deaths[level] = count + 1;
        CoinCollected = false;
    }

    public static int DeathsIn(string level)
    {
        deaths.TryGetValue(level, out int count);
        return count;
    }

    public static void ClearLevel(string level)
    {
        deaths.Remove(level);
        CoinCollected = false;
    }

    // Called whenever a level is (re)loaded.
    public static void StartAttempt()
    {
        AttemptSeconds = 0f;
        CoinCollected = false;
    }
}