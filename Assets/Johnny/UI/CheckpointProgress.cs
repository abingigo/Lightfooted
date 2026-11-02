using UnityEngine;

public class CheckpointProgress : MonoBehaviour
{
    public static int deathTimes;
    public static bool reachedCheckpoint;


    public static Vector3 savePos;
    public static string saveScene;

    public static bool HasCheckpointForScene(string scenePath)
    {
        return reachedCheckpoint && saveScene == scenePath;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRun()//death time countar
    {
        deathTimes = 0;
        reachedCheckpoint = false;
    }

    void Start()
    {
        if (HasCheckpointForScene(gameObject.scene.path))
        {
            transform.position = savePos;
        }
    }
}
