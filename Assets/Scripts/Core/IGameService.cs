using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public interface IGameService : IService
    {
        bool LoadLevel(string scenePath);
        bool NextLevel();
        bool GoToMenu();
        bool Retry();
        bool RestartLevel();
        bool Pause();
        bool Resume();
        void DiscardCheckpoint();

        bool ReportDeath(string cause);
        bool ReportGoalReached();
        bool ReportCheckpoint(Vector3 respawnPoint);

        GameState State { get; }
        bool IsGameplayActive { get; }
        Scene CurrentLevel { get; }
        LevelList Levels { get; }

        bool HasCheckpoint { get; }
        string CheckpointScene { get; }
        Vector3 CheckpointPosition { get; }
        bool HasCheckpointFor(string scenePath);

        int SessionDeaths { get; }
        int DeathsIn(string level);
        float AttemptSeconds { get; }

        event Action<GameState, GameState> StateChanged;
        event Action<Scene> LevelStarted;
        event Action<Scene> LevelEnded;
        event Action<string> DeathReported;
        event Action GoalReached;
        event Action<Vector3> Respawned;
        event Action<float> LoadingProgress;
    }
}
