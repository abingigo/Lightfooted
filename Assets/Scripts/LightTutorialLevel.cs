using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-specific rules for the battery and throwable-light teaching level.
public class LightTutorialLevel : MonoBehaviour
{
    [SerializeField] float cameraSmoothSpeed = 6f;
    [SerializeField] Vector2 goalPosition = new Vector2(4.3f, 7.25f);
    [SerializeField] float goalRadius = 0.85f;

    PlayerController player;
    Camera sceneCamera;
    BatteryPoweredLight batteryLight;
    ThrowableLight throwableLight;
    Collider2D[] steps;
    bool[] unlocked;
    bool batteryUsed;
    bool completed;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        batteryLight = FindFirstObjectByType<BatteryPoweredLight>();
        throwableLight = FindFirstObjectByType<ThrowableLight>();
        sceneCamera = Camera.main;

        steps = new Collider2D[3];
        unlocked = new bool[3];
        for (int i = 0; i < steps.Length; i++)
        {
            GameObject step = GameObject.Find("Step " + (i + 1));
            if (step != null) steps[i] = step.GetComponent<Collider2D>();
        }

        if (player == null || batteryLight == null || throwableLight == null || sceneCamera == null ||
            steps[0] == null || steps[1] == null || steps[2] == null)
            Debug.LogError("LightTutorialLevel: a required player, light, platform, or camera is missing.", this);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Restart();
            return;
        }
        if (player == null || batteryLight == null || throwableLight == null) return;

        batteryUsed |= batteryLight.HasBattery;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == null) continue;
            bool litByBattery = i == 0 && batteryLight.IsPowered;
            bool litByThrowable = LitByThrownLight(steps[i]);
            unlocked[i] |= litByBattery || litByThrowable;
            steps[i].enabled = litByBattery || litByThrowable ||
                               (unlocked[i] && PlayerHoldingLightOn(steps[i]));
        }

        if (completed) return;
        if (player.transform.position.y < -4f)
        {
            Restart();
            return;
        }

        if (batteryUsed && throwableLight.HasBeenThrown &&
            Vector2.Distance(player.transform.position, goalPosition) < goalRadius)
        {
            completed = true;
            Time.timeScale = 0f;
        }
    }

    bool LitByThrownLight(Collider2D step)
    {
        Collider2D range = throwableLight.LightRange;
        return throwableLight.HasBeenThrown && !throwableLight.IsCarried && range != null && range.enabled &&
               range.OverlapPoint(step.transform.position);
    }

    bool PlayerHoldingLightOn(Collider2D step)
    {
        if (!throwableLight.IsCarried) return false;
        Vector2 playerPosition = player.transform.position;
        Vector2 stepPosition = step.transform.position;
        float halfWidth = step.transform.lossyScale.x * 0.5f;
        return Mathf.Abs(playerPosition.x - stepPosition.x) < halfWidth &&
               playerPosition.y > stepPosition.y + 0.35f &&
               playerPosition.y < stepPosition.y + 1.7f;
    }

    void LateUpdate()
    {
        if (sceneCamera == null || player == null) return;
        Vector3 desired = new Vector3(0f, Mathf.Clamp(player.transform.position.y + 1.2f, 2f, 5.5f), -10f);
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, desired,
            cameraSmoothSpeed * Time.unscaledDeltaTime);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGUI()
    {
        if (player == null || batteryLight == null || throwableLight == null) return;

        string instruction;
        if (completed) instruction = "LEVEL COMPLETE!  Press T to replay.";
        else if (!batteryLight.HasBattery) instruction = "R: pick up the battery. Press R again beside the blue lamp.";
        else if (!throwableLight.HasBeenThrown && !throwableLight.IsCarried)
            instruction = "E: pick up the orange light near the start.";
        else if (player.transform.position.y < 1.7f) instruction = "Battery light powers Step 1. Use A/D and Space to jump.";
        else if (!unlocked[1])
            instruction = "Q: throw the light toward Step 2. It lights the dark platform.";
        else if (player.transform.position.y < 3.7f)
            instruction = "Jump to Step 2, then press E to catch the light again.";
        else if (!unlocked[2])
            instruction = "E: catch the light again. Q: throw it toward Step 3.";
        else if (player.transform.position.y < 5.8f)
            instruction = "Jump to the lit Step 3.";
        else instruction = "Reach the green goal on the highest platform.";

        GUI.Box(new Rect(14f, 14f, 660f, 72f), "LIGHT TUTORIAL");
        GUI.Label(new Rect(28f, 40f, 630f, 22f), instruction);
        GUI.Label(new Rect(28f, 62f, 630f, 20f), "A/D move  |  Space jump  |  E catch  |  Q throw  |  R battery  |  T restart");

        if (sceneCamera == null) return;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == null) continue;
            Vector3 screen = sceneCamera.WorldToScreenPoint(steps[i].transform.position + Vector3.up * 0.65f);
            if (screen.z > 0f)
                GUI.Label(new Rect(screen.x - 32f, Screen.height - screen.y, 80f, 22f), "STEP " + (i + 1));
        }
    }
}
