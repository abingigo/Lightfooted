using UnityEngine;

public class CheckpointProgress : MonoBehaviour
{
    public static int deathTimes;
    public static bool reachedCheckpoint;


    public static Vector3 savePos;
    public static string saveScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRun()//death time countar
    {
        deathTimes = 0;
        reachedCheckpoint = false;
    }

    void Start()
    {
        if (reachedCheckpoint &&saveScene ==  gameObject.scene.path)
        {
            transform.position = savePos;
        }
    }
}