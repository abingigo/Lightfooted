using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelStats
{
    static IGameService Game => ServiceRegistry.TryGet(out IGameService game) ? game : null;
    static IChallengeCoinService Coins => ServiceRegistry.TryGet(out IChallengeCoinService coins) ? coins : null;

    public static float AttemptSeconds => Game?.AttemptSeconds ?? 0f;

    public static bool CoinCollected
    {
        get => coinCollected || (Coins?.CollectedInLevel ?? 0) > 0;
        set => coinCollected = value;
    }

    public static int CoinsCollected => collectedCoins.Count + (Coins?.CollectedInLevel ?? 0);
    public static int CoinTotal => coins.Count + (Coins?.CoinsInLevel ?? 0);
    public static event Action CoinProgressChanged;

    // Picked coins stay in the total after their objects are destroyed.
    static readonly HashSet<int> coins = new HashSet<int>();
    static readonly HashSet<int> collectedCoins = new HashSet<int>();
    static readonly HashSet<string> levelsWithCoin = new HashSet<string>();
    static IGameService watched;
    static IChallengeCoinService watchedCoins;
    static bool coinCollected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        CoinCollected = false;
        coins.Clear();
        collectedCoins.Clear();
        CoinProgressChanged = null;
        levelsWithCoin.Clear();
        watched = null;
        watchedCoins = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void WatchLevels()
    {
        var challengeCoins = Coins;
        if (challengeCoins != null && !ReferenceEquals(challengeCoins, watchedCoins))
        {
            if (watchedCoins != null) watchedCoins.ProgressChanged -= RaiseCoinProgress;
            watchedCoins = challengeCoins;
            challengeCoins.ProgressChanged += RaiseCoinProgress;
        }

        var game = Game;
        if (game == null || ReferenceEquals(game, watched)) return;
        if (watched != null) watched.LevelStarted -= LevelStarted;
        watched = game;
        game.LevelStarted += LevelStarted;
    }

    static void LevelStarted(Scene scene) => ResetCoinProgress();

    static void RaiseCoinProgress() => CoinProgressChanged?.Invoke();

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
        if (!levelsWithCoin.Contains(level) && (Coins?.CoinsInLevel ?? 0) == 0) return "no coin in level";
        return CoinCollected ? "yes" : "no";
    }

    public static void RecordDeath(string level)
    {
        CoinCollected = false;
    }

    public static int DeathsIn(string level) => Game?.DeathsIn(level) ?? 0;

    public static void ClearLevel(string level)
    {
        CoinCollected = false;
    }

    // Called whenever a level is (re)loaded.
    public static void StartAttempt()
    {
        ResetCoinProgress();
    }
}
