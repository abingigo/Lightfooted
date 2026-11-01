using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelStats
{
    public static float AttemptSeconds { get; set; }

    public static bool CoinCollected { get; set; }
    public static int CoinsCollected => collectedCoins.Count;
    public static int CoinTotal => coins.Count;
    public static event Action CoinProgressChanged;

    // Picked coins stay in the total after their objects are destroyed.
    static readonly HashSet<int> coins = new HashSet<int>();
    static readonly HashSet<int> collectedCoins = new HashSet<int>();
    static readonly HashSet<string> levelsWithCoin = new HashSet<string>();
    static readonly Dictionary<string, int> deaths = new Dictionary<string, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        AttemptSeconds = 0f;
        CoinCollected = false;
        coins.Clear();
        collectedCoins.Clear();
        CoinProgressChanged = null;
        levelsWithCoin.Clear();
        deaths.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void WatchSceneChanges()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetCoinProgress();
    }

    static void ResetCoinProgress()
    {
        CoinCollected = false;
        coins.Clear();
        collectedCoins.Clear();
        CoinProgressChanged?.Invoke();
    }

    public static void RegisterCoin(string level, int coinId)
    {
        RegisterCoinLevel(level);
        if (coins.Add(coinId)) CoinProgressChanged?.Invoke();
    }

    public static void CollectCoin(string level, int coinId)
    {
        RegisterCoin(level, coinId);
        CoinCollected = true;
        if (collectedCoins.Add(coinId)) CoinProgressChanged?.Invoke();
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
        ResetCoinProgress();
    }
}
