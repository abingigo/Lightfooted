using UnityEngine;

namespace LightPlatformer.UI
{
    public interface ILanternStatus
    {
        Transform Anchor { get; }
        float RemainingSeconds { get; }
        float DurationSeconds { get; }
    }
}
