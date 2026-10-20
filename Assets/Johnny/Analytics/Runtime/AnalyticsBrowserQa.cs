#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using LightPlatformer.UI;
using UnityEngine;

// This runner is stripped from release builds and activates only on an explicit QA URL.
public sealed class AnalyticsBrowserQa : MonoBehaviour
{
    string state = "Waiting for game focus";
    bool done, driving, jumped;
    Rigidbody2D player;
    PlayerController movement;
    readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartIfRequested()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer && Application.absoluteURL.Contains("analytics-qa=route"))
            new GameObject("Browser analytics QA").AddComponent<AnalyticsBrowserQa>();
    }
    IEnumerator Start()
    {
        while (AnalyticsService.Instance?.Queue == null || GameMenuController.Instance?.State != MenuState.Playing || !Application.isFocused)
            yield return null;
        yield return null; yield return null;
        state = "Running actual movement and checkpoint physics";
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        movement = player.GetComponent<PlayerController>(); movement.enabled = false;
        driving = true;
        bool paused = false;
        float deadline = Time.realtimeSinceStartup + 30;
        while (GameMenuController.Instance.State == MenuState.Playing && Time.realtimeSinceStartup < deadline)
        {
            if (!paused && player.position.x > 12.8f)
            {
                GameMenuController.Instance.Pause();
                yield return new WaitForSecondsRealtime(0.5f);
                GameMenuController.Instance.Resume(); paused = true;
                yield return null; yield return null;
            }
            yield return null;
        }
        driving = false;
        state = GameMenuController.Instance.State == MenuState.Victory && jumped && paused ? "ROUTE PASSED" : "ROUTE FAILED";
        done = true;
    }
    void FixedUpdate()
    {
        if (!driving || GameMenuController.Instance.State != MenuState.Playing) return;
        bool jump = !jumped && player.position.x >= 14.5f && Physics2D.Raycast(player.position, Vector2.down, 0.7f, LayerMask.GetMask("Ground"));
        if (jump) { jumped = true; Debug.Log("WEBGL_QA_JUMP " + player.position); }
        movement.GetType().GetField("moveX", flags).SetValue(movement, 1f);
        movement.GetType().GetField("jumpQueued", flags).SetValue(movement, jump);
        movement.SendMessage("FixedUpdate");
    }
    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(20, 50, 650, 40), "WEBGL QA · " + state, style);
        if (done && AnalyticsService.Instance != null)
            GUI.Box(new Rect(20, 95, 650, 40), "Pending: " + (AnalyticsService.Instance.Queue?.Count ?? -1) + " · " + AnalyticsService.Instance.DeliveryStatus, style);
    }
}
#endif
