using UnityEngine;
using UnityEngine.SceneManagement;
using LightPlatformer.UI;
using TMPro;

// Scene-specific rules for the battery and throwable-light teaching level.
public class LightTutorialLevel : MonoBehaviour
{
    [SerializeField] float cameraSmoothSpeed = 6f;
    [SerializeField] Vector2 goalPosition = new Vector2(4.3f, 7.25f);
    [SerializeField] float goalRadius = 0.85f;
    [SerializeField] TMP_FontAsset hintFont;

    PlayerController player;
    Camera sceneCamera;
    BatteryPoweredLight batteryLight;
    ThrowableLight throwableLight;
    Collider2D[] steps;
    Collider2D[] sceneColliders;
    bool[] unlocked;
    bool batteryUsed;
    bool completed;
    TextMeshProUGUI hintText;
    RectTransform hintRect;
    bool? hintUsesDarkText;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        batteryLight = FindFirstObjectByType<BatteryPoweredLight>();
        throwableLight = FindFirstObjectByType<ThrowableLight>();
        sceneCamera = Camera.main;
        sceneColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

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

        CreateHint();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Restart();
            return;
        }
        if (player == null || batteryLight == null || throwableLight == null ||
            GameMenuController.Instance == null || GameMenuController.Instance.State != MenuState.Playing) return;

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
            GameMenuController.NotifyVictory();
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
        UpdateHint();
    }

    void CreateHint()
    {
        GameObject canvasObject = new GameObject("Tutorial hints", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        GameObject textObject = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        hintRect = textObject.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
        hintRect.pivot = new Vector2(0f, 1f);
        hintRect.sizeDelta = new Vector2(500f, 72f);
        hintText = textObject.GetComponent<TextMeshProUGUI>();
        hintText.font = hintFont != null ? hintFont : TMP_Settings.defaultFontAsset;
        hintText.fontSize = 25f;
        hintText.fontStyle = FontStyles.Normal;
        hintText.color = Color.white;
        hintText.outlineColor = Color.black;
        hintText.outlineWidth = 0.18f;
        hintText.alignment = TextAlignmentOptions.TopLeft;
        hintText.enableWordWrapping = true;
        hintText.raycastTarget = false;
        hintText.enabled = false;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateHint()
    {
        if (hintText == null) return;
        if (player == null || batteryLight == null || throwableLight == null ||
            sceneCamera == null || GameMenuController.Instance == null ||
            GameMenuController.Instance.State != MenuState.Playing || steps == null ||
            steps[0] == null || steps[1] == null || steps[2] == null)
        {
            hintText.enabled = false;
            return;
        }

        string instruction;
        Vector3 hintPosition;
        if (!batteryLight.HasBattery)
        {
            BatteryItem battery = FindFirstObjectByType<BatteryItem>();
            bool carried = battery != null && !battery.IsAvailable;
            instruction = carried ? "Press R beside the blue lamp." : "Press R to pick up the battery.";
            hintPosition = carried ? batteryLight.transform.position :
                battery != null ? battery.transform.position : player.transform.position;
        }
        else if (!throwableLight.HasBeenThrown && !throwableLight.IsCarried)
        {
            instruction = "Press E to pick up the orange light.";
            hintPosition = throwableLight.transform.position;
        }
        else if (player.transform.position.y < 1.7f)
        {
            instruction = "Use A/D and Space to jump onto the lit platform.";
            hintPosition = steps[0].transform.position;
        }
        else if (!unlocked[1])
        {
            instruction = "Press Q to throw the light toward this platform.";
            hintPosition = steps[1].transform.position;
        }
        else if (player.transform.position.y < 3.7f)
        {
            instruction = "Jump across, then press E to catch the light.";
            hintPosition = steps[1].transform.position;
        }
        else if (!unlocked[2])
        {
            instruction = "Catch with E, then throw with Q toward this platform.";
            hintPosition = steps[2].transform.position;
        }
        else if (player.transform.position.y < 5.8f)
        {
            instruction = "Jump to the highest lit platform.";
            hintPosition = steps[2].transform.position;
        }
        else
        {
            instruction = "Reach the green goal.";
            hintPosition = goalPosition;
        }

        Vector3 screen = sceneCamera.WorldToScreenPoint(hintPosition + Vector3.up * 0.9f);
        if (screen.z <= 0f || screen.y < 0f || screen.y > Screen.height)
        {
            hintText.enabled = false;
            return;
        }
        const float width = 500f;
        const float height = 72f;
        float x = Mathf.Clamp(screen.x - width * 0.5f, 8f, Mathf.Max(8f, Screen.width - width - 8f));
        float y = Mathf.Clamp(Screen.height - screen.y - height, 100f, Screen.height - height - 8f);
        hintRect.anchoredPosition = new Vector2(x, -y);
        bool brightBackground = IsHintOverLight(x, y, width, height, screen.z);
        if (hintUsesDarkText != brightBackground)
        {
            hintText.color = brightBackground ? new Color(0.06f, 0.08f, 0.13f) : Color.white;
            hintText.outlineColor = brightBackground ? Color.white : Color.black;
            hintUsesDarkText = brightBackground;
        }
        hintText.text = instruction;
        hintText.enabled = true;
    }

    bool IsHintOverLight(float x, float y, float width, float height, float depth)
    {
        float screenY = Screen.height - y - height * 0.5f;
        for (int i = 0; i < 3; i++)
        {
            float screenX = x + width * (0.15f + i * 0.3f);
            Vector3 world = sceneCamera.ScreenToWorldPoint(new Vector3(screenX, screenY, depth));
            foreach (Collider2D hit in sceneColliders)
                if (hit != null && hit.enabled && hit.CompareTag("Light") && hit.OverlapPoint(world)) return true;
        }
        return false;
    }
}
