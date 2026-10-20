using System;
using System.IO;
using System.Linq;
using LightPlatformer.Obstacles;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnalyticsLabSetup
{
    public const string ScenePath = "Assets/Scenes/Johnny/AnalyticsLab.unity";
    static Sprite square;
    static Material unlit;

    [MenuItem("Tools/Johnny Analytics/Create checkpoint lab")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("AnalyticsLab already exists. Open the existing scene.");
        var previous = SceneManager.GetActiveScene();
        if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
            throw new InvalidOperationException("Save or close the untitled scene before generating the lab.");
        var mode = Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        SceneManager.SetActiveScene(scene);
        try
        {
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Johnny/Obstacles/Art/Square.png");
            unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Johnny/Obstacles/Art/ObstacleUnlit.mat");
            var camera = new GameObject("Analytics camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 6.8f;
            camera.transform.position = new Vector3(10.5f, 3.5f, -10);
            camera.backgroundColor = new Color32(18, 26, 36, 255); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AnalyticsLabDisplay>();
            var floor = Block("Floor", new Vector2(11, -0.5f), new Vector2(29, 1), new Color32(62, 83, 103, 255));
            floor.layer = LayerMask.NameToLayer("Ground"); floor.AddComponent<BoxCollider2D>();
            var light = new GameObject("Steady training light"); light.tag = "Light";
            light.transform.position = new Vector3(11, 3, 0);
            var coverage = light.AddComponent<BoxCollider2D>(); coverage.isTrigger = true; coverage.size = new Vector2(35, 18);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            player.transform.position = new Vector3(0, 1, 0);
            player.GetComponent<SpriteRenderer>().sharedMaterial = unlit;
            player.GetComponent<SpriteRenderer>().color = Color.white;
            foreach (var timer in player.GetComponentsInChildren<LanternTimer>(true)) timer.gameObject.SetActive(false);
            Checkpoint("gate-a", 5, "A / FIRST MARKER");
            Checkpoint("gate-b", 12, "B / SECOND MARKER");
            var goal = Block("Green exit", new Vector2(21, 1.1f), new Vector2(1, 2.2f), new Color32(134, 232, 183, 255));
            goal.AddComponent<BoxCollider2D>().isTrigger = true; goal.AddComponent<Victory>();
            var danger = Block("Red test hazard", new Vector2(16.5f, 0.3f), new Vector2(1, 0.6f), new Color32(241, 112, 113, 255));
            var hazard = danger.AddComponent<PolygonCollider2D>(); hazard.isTrigger = true;
            hazard.SetPath(0, new[] { new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, -0.5f) });
            danger.AddComponent<SpikeHazard>();
            Label("CHECKPOINT TIMING LAB", new Vector2(10.5f, 7), 3.4f);
            Label("A / D move  |  Space jump the red block  |  Escape pause", new Vector2(10.5f, 5.8f), 2.2f);
            Label("START", new Vector2(0, 2.8f), 2.4f);
            Label("JUMP", new Vector2(16.5f, 2.8f), 2.4f);
            Label("FINISH", new Vector2(21, 3.5f), 2.4f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("ANALYTICS_LAB_CREATED " + ScenePath);
    }

    static GameObject Block(string name, Vector2 position, Vector2 size, Color color)
    {
        var root = new GameObject(name); root.transform.position = position;
        var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = new Vector3(size.x, size.y, 1);
        var sprite = visual.AddComponent<SpriteRenderer>(); sprite.sprite = square; sprite.sharedMaterial = unlit; sprite.color = color;
        // Colliders use the root scale, matching the visible bounds.
        root.transform.localScale = new Vector3(size.x, size.y, 1); visual.transform.localScale = Vector3.one;
        return root;
    }

    static void Checkpoint(string id, float x, string title)
    {
        var root = Block("Checkpoint " + id, new Vector2(x, 1.8f), new Vector2(0.4f, 3.6f), new Color32(104, 191, 238, 110));
        root.AddComponent<BoxCollider2D>().isTrigger = true;
        root.AddComponent<AnalyticsCheckpoint>().checkpointId = id;
        Label(title, new Vector2(x, 4.4f), 2.3f);
        Directory.CreateDirectory("Assets/Johnny/Analytics/Prefabs");
        if (id == "gate-a") PrefabUtility.SaveAsPrefabAsset(root, "Assets/Johnny/Analytics/Prefabs/AnalyticsCheckpoint.prefab");
    }

    static void Label(string message, Vector2 position, float size)
    {
        var root = new GameObject(message, typeof(RectTransform)); root.transform.position = position;
        var text = root.AddComponent<TextMeshPro>(); text.text = message; text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = new Color32(210, 230, 242, 255);
        text.rectTransform.sizeDelta = new Vector2(25, 1.2f); text.GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    public static void BuildPreview()
    {
        Directory.CreateDirectory("tmp/AnalyticsReview/Build");
        var scenes = new[] { ScenePath, LightPlatformer.UI.LevelCatalog.MenuPath };
        var report = BuildPipeline.BuildPlayer(scenes, "tmp/AnalyticsReview/Build/LightfootedAnalytics.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Analytics preview build failed: " + report.summary.result);
        Debug.Log("ANALYTICS_BUILD_SUCCEEDED " + report.summary.totalSize);
    }
}
