using System;
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

        bool ReportDeath(string cause);
        bool ReportGoalReached();

        GameState State { get; }
        bool IsGameplayActive { get; }
        Scene CurrentLevel { get; }
        LevelList Levels { get; }

        event Action<GameState, GameState> StateChanged;
        event Action<Scene> LevelStarted;
        event Action<Scene> LevelEnded;
        event Action<string> DeathReported;
        event Action GoalReached;
        event Action<float> LoadingProgress;
    }
}
