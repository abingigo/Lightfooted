using System;

namespace Core
{
    public interface IChallengeCoinService : IService
    {
        int CoinsInLevel { get; }
        int CollectedInLevel { get; }
        int FoundIn(string scenePath);
        event Action ProgressChanged;
    }
}
