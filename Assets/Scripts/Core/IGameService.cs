using System;
using UnityEngine.SceneManagement;

namespace Core
{
    public interface IGameService : IService
    {
        Scene CurrentLevel { get; }
        LevelList Levels { get; }

        event Action<Scene> LevelStarted;
        event Action<Scene> LevelEnded;
    }
}
