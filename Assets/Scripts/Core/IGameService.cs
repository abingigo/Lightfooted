using System;
using UnityEngine.SceneManagement;

namespace Core
{
    public interface IGameService : IService
    {
        Scene CurrentLevel { get; }

        event Action<Scene> LevelStarted;
        event Action<Scene> LevelEnded;
    }
}
