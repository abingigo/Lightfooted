using System;
using System.IO;
using System.Linq;
using LightPlatformer.Obstacles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightPlatformLabSetup
{
    public const string ScenePath = "Assets/Scenes/Johnny/LightPlatformLab.unity";
    const string Root = "Assets/Johnny/Obstacles";
    static Sprite square, triangle;
    static Material material;
    static readonly Color Mint = new Color32(165, 234, 203, 255);
    static readonly Color Amber = new Color32(255, 207, 115, 255);
    static readonly Color Muted = new Color32(161, 177, 193, 255);

    [MenuItem("Tools/Johnny UI/Create light platform demonstration level")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || SceneManager.GetSceneByPath(ScenePath).isLoaded)
            throw new InvalidOperationException("Stop Play mode and close LightPlatformLab before regenerating it.");
        var previous = SceneManager.GetActiveScene();
        if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
            throw new InvalidOperationException("Open a saved scene first; unsaved scenes are preserved.");
        square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Square.png");
        triangle = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Triangle.png");
        material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/ObstacleUnlit.mat");
        if (square == null || triangle == null || material == null)
            throw new InvalidOperationException("Import Johnny's obstacle art before generating this demonstration.");
        var mode = Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root = new GameObject("LightStoppingPlatform"); root.layer = LayerMask.NameToLayer("Ground");
            var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(3, 0.4f); box.offset = new Vector2(0, -0.2f);
            var platform = root.AddComponent<LightStoppingPlatform>();
            platform.surface = Visual("Surface", root.transform, box.offset, box.size, Mint, 3);
            platform.stateLabel = Label("MOVING", Vector2.up * 0.95f, new Vector2(4, 0.75f), 2.2f, Mint, root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/LightStoppingPlatform.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(10, 0, 0);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            player.name = "Player"; player.tag = "Player"; player.transform.position = new Vector3(3, 0.65f, 0);
            player.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.GetComponent<SpriteRenderer>().sharedMaterial = material; player.GetComponent<SpriteRenderer>().color = Mint;
            foreach (var timer in player.GetComponentsInChildren<LanternTimer>(true)) timer.gameObject.SetActive(false);

            var camera = new GameObject("Light platform camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 6;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(17, 22, 29, 255);
            camera.transform.position = new Vector3(10, 2.8f, -10);
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.player = player.transform; follow.minX = 0; follow.maxX = 32; follow.offset = new Vector3(0, 0, -10);

            // Training lights cover the player but leave the platform slab clear.
            // The controllable cone supplies the platform's brake light.
            Coverage("Overhead player training light", new Vector2(16, 5.15f), new Vector2(34, 10));
            Coverage("Recovery player training light", new Vector2(16, -3.55f), new Vector2(18, 6));
            Visual("Overhead light wash", null, new Vector2(16, 3.15f), new Vector2(32, 6), new Color32(30, 45, 48, 75), -5);
            Floor("Start ledge", 0, 8, 0);
            Floor("Recovery floor", 7, 24, -2.5f);
            Floor("Recovery step", 21.5f, 23, -1.25f);
            Floor("Exit ledge", 22, 32, 0);
            Visual("Travel rail", null, new Vector2(15, -0.7f), new Vector2(10, 0.05f), Muted, -1);

            var light = new GameObject("Controllable brake light"); light.tag = "Light";
            light.transform.position = new Vector3(15, 5, 0);
            var cone = light.AddComponent<PolygonCollider2D>(); cone.isTrigger = true;
            cone.points = new[] { Vector2.zero, new Vector2(-1.2f, 6), new Vector2(1.2f, 6) };
            var control = light.AddComponent<ControllableLight>();
            var wash = Visual("Visible brake cone", light.transform, new Vector2(0, 3), new Vector2(2.4f, 6), new Color32(255, 207, 115, 70), 0);
            wash.sprite = triangle; wash.transform.localRotation = Quaternion.Euler(0, 0, 180);
            var emitter = Visual("Emitter", light.transform, Vector2.zero, new Vector2(0.85f, 0.65f), Amber, 4);
            var clickTarget = emitter.gameObject.AddComponent<BoxCollider2D>();
            clickTarget.isTrigger = true;
            control.clickCollider = clickTarget;
            var feedback = emitter.gameObject.AddComponent<LightPlatformer.Demos.BrakeLightFeedback>();
            feedback.emitter = emitter; feedback.controller = control;
            feedback.stateLabel = Label("CLICK TO SELECT", new Vector2(15, 6.1f), new Vector2(7, 0.7f), 1.8f, Amber);

            Label("LIGHT PLATFORM LAB", new Vector2(5, 6.8f), new Vector2(10, 1), 3.1f, Mint);
            Label("A / D: move. Space: jump.\nJump onto the platform and ride it.\nOverhead light keeps you solid.", new Vector2(5, 5.1f), new Vector2(9, 2.3f), 2.1f, Muted);
            Label("YOU CONTROL THE BRAKE", new Vector2(15, 7), new Vector2(10.5f, 1), 2.6f, Amber);
            Label("Click the amber lamp, then press F.\nTwo turns aim down; two more release.\nLight contact = HELD BY LIGHT.", new Vector2(15, 3.4f), new Vector2(10.5f, 2.2f), 1.95f, Muted);
            Label("Missed the ride? Use the safe lower path.\nJump up the step on the right.", new Vector2(15, -1.35f), new Vector2(14, 1.3f), 2.1f, Muted);
            Label("EXIT", new Vector2(29, 4.5f), new Vector2(6, 1), 3.2f, Mint);
            var goal = new GameObject("Light platform exit"); goal.transform.position = new Vector3(30, 1.2f, 0);
            var door = goal.AddComponent<BoxCollider2D>(); door.size = new Vector2(1.3f, 2.4f); door.isTrigger = true;
            goal.AddComponent<Victory>();
            Visual("Exit", goal.transform, Vector2.zero, door.size, Mint, 1);
            Visual("Door inset", goal.transform, Vector2.zero, new Vector2(0.85f, 2), new Color32(29, 61, 54, 255), 2);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
        var scenes = EditorBuildSettings.scenes.ToList();
        int index = scenes.FindIndex(item => item.path == ScenePath);
        if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        else scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("LIGHT_PLATFORM_LAB_CREATED: reusable platform and separate demonstration.");
    }

    static void Coverage(string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name); obj.tag = "Light"; obj.transform.position = position;
        var box = obj.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = size;
    }
    static void Floor(string name, float start, float end, float top)
    {
        var obj = new GameObject(name); obj.layer = LayerMask.NameToLayer("Ground");
        obj.transform.position = new Vector3((start + end) / 2, top - 0.4f, 0);
        var box = obj.AddComponent<BoxCollider2D>(); box.size = new Vector2(end - start, 0.8f);
        Visual("Floor", obj.transform, Vector2.zero, box.size, new Color32(66, 83, 101, 255), 0);
        Visual("Lit edge", obj.transform, new Vector2(0, 0.36f), new Vector2(end - start, 0.08f), new Color32(104, 170, 148, 255), 1);
    }
    static SpriteRenderer Visual(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = square;
        renderer.color = color; renderer.sharedMaterial = material; renderer.sortingOrder = order; return renderer;
    }
    static TMP_Text Label(string value, Vector2 position, Vector2 size, float fontSize, Color color, Transform parent = null)
    {
        var obj = new GameObject("Sign " + value, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        var text = obj.AddComponent<TextMeshPro>(); text.text = value; text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize; text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = size; text.textWrappingMode = TextWrappingModes.Normal;
        text.GetComponent<MeshRenderer>().sortingOrder = 10; return text;
    }
}
