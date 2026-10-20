using System;
using System.Collections;
using System.IO;
using Core;
using Lightfooted.Analytics;
using LightPlatformer.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public interface IAnalyticsService : IService
{
    bool ReachCheckpoint(string checkpointId);
}

[DefaultExecutionOrder(-900)]
public sealed class AnalyticsService : Service<IAnalyticsService>, IAnalyticsService
{
    public const string DesktopEndpoint = "http://127.0.0.1:8765/api/events";
    public string Endpoint { get; private set; } = DesktopEndpoint;
    public static AnalyticsService Instance { get; private set; }
    public AttemptRecorder Recorder { get; private set; }
    public IEventQueue Queue { get; private set; }
    public string DeliveryStatus { get; private set; } = "Starting analytics";
    readonly System.Collections.Generic.List<AnalyticsEvent> earlyEvents = new System.Collections.Generic.List<AnalyticsEvent>();
    [Serializable] public sealed class Configuration { public string mode; public string endpoint; public string dataset; }
    public bool DownloadMode { get; private set; } = true;
    public string Dataset { get; private set; } = "alpha-2026-10-20";
    string displayedStatus;
    int displayedCount = -1;
    GameMenuController menus;
    string currentLevel;
    double previousTime;
    bool wasActive, focused = true, suspended;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("Analytics service").AddComponent<AnalyticsService>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (!ServiceRegistry.TryGet<IAnalyticsService>(out var registered) || !ReferenceEquals(registered, this)) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if !UNITY_WEBGL || UNITY_EDITOR
        string root = Environment.GetEnvironmentVariable("LIGHTFOOTED_ANALYTICS_DIR");
        if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(Application.persistentDataPath, "Analytics", "outbox");
        Queue = new LocalEventQueue(root);
#endif
        Recorder = new AttemptRecorder(Guid.NewGuid().ToString("N"), Application.version, Application.platform.ToString(), data => {
            if (Queue == null) earlyEvents.Add(data); else Queue.Enqueue(data);
        });
        previousTime = Time.realtimeSinceStartupAsDouble;
        focused = Application.isFocused || Application.isBatchMode;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void Start() { AttachMenus(); Reconcile(); StartCoroutine(InitializeDelivery()); }

    IEnumerator InitializeDelivery()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var config = new Configuration { mode = "download", dataset = Dataset };
        using (var request = UnityWebRequest.Get(Application.streamingAssetsPath + "/analytics-config.json"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                try { config = JsonUtility.FromJson<Configuration>(request.downloadHandler.text) ?? config; }
                catch (ArgumentException) { DeliveryStatus = "Invalid analytics configuration"; }
            }
        }
        Dataset = string.IsNullOrWhiteSpace(config.dataset) ? Dataset : config.dataset;
        DownloadMode = config.mode != "server";
        string key = "lightfooted.analytics." + Dataset + ".outbox.v1";
        Queue = new BrowserEventQueue(() => BrowserAnalyticsBridge.Read(key), value => BrowserAnalyticsBridge.Write(key, value));
        foreach (var data in earlyEvents) Queue.Enqueue(data);
        earlyEvents.Clear();
        Endpoint = null;
        if (!DownloadMode) try
        {
            var address = new Uri(new Uri(Application.absoluteURL), config.endpoint);
            bool loopback = address.IsLoopback;
            if (address.Scheme != "https" && !(loopback && address.Scheme == "http"))
                throw new ArgumentException("Remote analytics endpoints must use HTTPS.");
            Endpoint = address.AbsoluteUri;
        }
        catch (Exception)
        {
            Endpoint = null; DeliveryStatus = "Analytics endpoint is invalid; events remain queued";
            yield break;
        }
        string dashboard = Application.streamingAssetsPath + "/playtest-graphs.html";
        BrowserAnalyticsBridge.Attach(gameObject.name, Endpoint, key, dashboard);
#else
        DownloadMode = Environment.GetEnvironmentVariable("LIGHTFOOTED_ANALYTICS_E2E") != "1";
#endif
        if (DownloadMode)
        {
            DeliveryStatus = "Data stays in this browser. Download it after playing, then open the graphs.";
            yield break;
        }
        yield return Deliver();
    }

    void AttachMenus()
    {
        if (menus != null || GameMenuController.Instance == null) return;
        menus = GameMenuController.Instance;
        menus.ViewChanged += MenuChanged;
        menus.SceneLoadRequested += LoadRequested;
    }

    bool CanPlay => focused && !suspended && Time.timeScale > 0 && menus != null && menus.State == MenuState.Playing;

    void Advance()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        Recorder.Advance(Math.Max(0, now - previousTime), wasActive);
        previousTime = now;
        wasActive = CanPlay;
    }

    void Update()
    {
        AttachMenus(); Advance(); Reconcile();
        if (Queue == null) return;
        string status = Queue.LastError ?? DeliveryStatus;
        if (status == displayedStatus && displayedCount == Queue.Count) return;
        displayedStatus = status; displayedCount = Queue.Count;
        BrowserAnalyticsBridge.Status(status, Queue.Count);
    }

    public PlaytestExport CaptureExport()
    {
        Advance();
        return PlaytestExport.Capture(Queue, Dataset);
    }

    public void BrowserExportRequested(string unused)
    {
        try
        {
            if (menus != null && menus.State == MenuState.Playing) menus.Pause();
            var snapshot = CaptureExport();
            string filename = "lightfooted-playtest-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json";
            BrowserAnalyticsBridge.Download(snapshot.ToJson(), filename);
            DeliveryStatus = "Download requested. Saved data is kept; importing it twice will not add duplicates.";
        }
        catch (Exception error)
        {
            DeliveryStatus = "Could not download playtest data: " + error.Message;
        }
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) { Advance(); Reconcile(); } }

    void MenuChanged()
    {
        Advance();
        if (menus.State == MenuState.GameOver) Recorder.End("died");
        else if (menus.State == MenuState.Victory) Recorder.End("completed");
        else if (menus.State == MenuState.LevelSelect) Recorder.End("left_level");
        Reconcile();
    }

    void Reconcile()
    {
        var scene = SceneManager.GetActiveScene();
        bool playable = LevelCatalog.FromPaths(new[] { scene.path }).Count == 1;
        if (Recorder.IsRunning && currentLevel != scene.path) Recorder.End("left_level");
        if (playable && menus != null && menus.State == MenuState.Playing && !Recorder.IsRunning)
        {
            currentLevel = scene.path;
            Recorder.Begin(currentLevel);
            previousTime = Time.realtimeSinceStartupAsDouble;
        }
        wasActive = CanPlay && Recorder.IsRunning;
    }

    void LoadRequested(string path)
    {
        Advance();
        Recorder.End(path == currentLevel ? "restarted" : "left_level");
    }

    public bool ReachCheckpoint(string checkpointId)
    {
        Advance();
        return CanPlay && Recorder.Reach(checkpointId);
    }

    // Headless playtests have no foreground window. Desktop players still exclude background time.
    void OnApplicationFocus(bool value) { if (Recorder == null) return; Advance(); focused = value || Application.isBatchMode; wasActive = CanPlay; }
    void OnApplicationPause(bool value) { if (Recorder == null) return; Advance(); suspended = value; wasActive = CanPlay; }
    void OnApplicationQuit() { if (Recorder == null) return; Advance(); Recorder.End("quit"); Queue?.Persist(); }

    public void BrowserVisibilityChanged(string value)
    {
        Advance(); suspended = value == "hidden"; wasActive = CanPlay;
    }

    public void BrowserPageHide(string value)
    {
        Advance();
        if (value == "quit") Recorder.End("quit");
        suspended = true; wasActive = false; Queue?.Persist();
    }

    IEnumerator Deliver()
    {
        while (true)
        {
            Queue.Persist();
            var batch = Queue.Peek();
            if (batch.Length > 0)
            {
                using (var request = new UnityWebRequest(Endpoint, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EventBatch { events = batch })));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 5;
                    yield return request.SendWebRequest();
                    bool accepted = false;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        EventReceipt receipt = null;
                        try { receipt = JsonUtility.FromJson<EventReceipt>(request.downloadHandler.text); }
                        catch (ArgumentException) { }
                        if (receipt?.accepted_ids != null && receipt.accepted_ids.Length == batch.Length)
                        {
                            var expected = new System.Collections.Generic.HashSet<string>();
                            foreach (var data in batch) expected.Add(data.event_id);
                            accepted = expected.SetEquals(receipt.accepted_ids) && Queue.Acknowledge(receipt.accepted_ids);
                        }
                    }
                    DeliveryStatus = accepted ? "Saved to analytics database" : "Collector unavailable or rejected batch; events remain queued";
                }
            }
            yield return new WaitForSecondsRealtime(3);
        }
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (menus != null) { menus.ViewChanged -= MenuChanged; menus.SceneLoadRequested -= LoadRequested; }
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }
}
