using Core;
using UnityEngine;

public class CheckpointProgress : MonoBehaviour
{
    static IGameService Game => ServiceRegistry.TryGet(out IGameService game) ? game : null;

    public static int deathTimes => Game?.SessionDeaths ?? 0;

    public static bool reachedCheckpoint
    {
        get => Game?.HasCheckpoint ?? false;
        set { if (!value) Game?.DiscardCheckpoint(); }
    }

    public static Vector3 savePos => Game?.CheckpointPosition ?? Vector3.zero;
    public static string saveScene => Game?.CheckpointScene;

    public static bool HasCheckpointForScene(string scenePath) => Game?.HasCheckpointFor(scenePath) ?? false;
}
