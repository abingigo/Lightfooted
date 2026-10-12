using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightPlatformer.Obstacles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ObstacleLabSetup
{
    public const string ScenePath = "Assets/Scenes/Johnny/ObstacleLab.unity";
    const string Root = "Assets/Johnny/Obstacles";
    static Sprite square, triangle;
    static Material material;
    static readonly Color Mint = new Color32(165, 234, 203, 255);
    static readonly Color Muted = new Color32(161, 177, 193, 255);
    static readonly Color Coral = new Color32(255, 117, 111, 255);

    [MenuItem("Tools/Johnny UI/Create obstacle demonstration level")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before generating the obstacle level.");
        var existing = SceneManager.GetSceneByPath(ScenePath);
        if (existing.isLoaded) throw new InvalidOperationException("Close ObstacleLab before regenerating it; this tool replaces that scene.");
        var previous = SceneManager.GetActiveScene();
        if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
            throw new InvalidOperationException("Open a saved scene before generating the lab. Unsaved Untitled scenes are preserved.");
        EnsureLightTag();
        Directory.CreateDirectory(Root + "/Art");
        Directory.CreateDirectory(Root + "/Prefabs");
        square = PrimitiveSprite("Square", false);
        triangle = PrimitiveSprite("Triangle", true);
        material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/ObstacleUnlit.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, Root + "/Art/ObstacleUnlit.mat");
        }
        // Additive generation leaves the team's currently open scenes untouched.
        // A command-line Unity launch starts with its own unsaved empty scene.
        var mode = Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        SceneManager.SetActiveScene(scene);
        try
        {
            var spikes = SavePrefab(CreateSpikes(), "Spikes");
            var beam = SavePrefab(CreateBeam(), "PulseBeam");
            var crumble = SavePrefab(CreateCrumble(), "CrumblePlatform");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab"));
            player.name = "Player"; player.tag = "Player"; player.transform.position = new Vector3(3, 0.65f, 0);
            player.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.GetComponent<SpriteRenderer>().sharedMaterial = material;
            player.GetComponent<SpriteRenderer>().color = Mint;
            // This lab uses fixed training lights, not the timed portable lantern.
            foreach (var timer in player.GetComponentsInChildren<LanternTimer>(true)) timer.gameObject.SetActive(false);

            var camera = new GameObject("Lab camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 6;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(17, 22, 29, 255);
            camera.transform.position = new Vector3(10, 2.8f, -10);
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.player = player.transform; follow.minX = 0; follow.maxX = 65;
            follow.offset = new Vector3(0, 0, -10);

            var light = new GameObject("Steady training light coverage");
            light.tag = "Light";
            light.transform.position = new Vector3(32.5f, 2, 0);
            var coverage = light.AddComponent<BoxCollider2D>(); coverage.isTrigger = true; coverage.size = new Vector2(69, 24);
            Visual("Training light backdrop", null, new Vector2(32.5f, 2), new Vector2(65, 10), new Color32(30, 45, 48, 100), -5);

            Floor("Approach and spike floor", 0, 23, 0);
            Floor("Beam waiting floor", 23, 42, 0);
            Floor("Recovery floor", 41, 54, -2.5f);
            Floor("Exit floor", 52.6f, 65, 0);
            Floor("Recovery step", 52.8f, 54, -1.25f);

            Label("OBSTACLE LAB", new Vector2(5, 6.8f), new Vector2(9, 1), 3.5f, Mint);
            Label("Three quick challenges.\nA / D to move. Space to jump.\nTraining lights stay on here.", new Vector2(5, 5.35f), new Vector2(9, 2), 2.3f, Muted);
            Label("01 / SPIKES", new Vector2(14, 4.7f), new Vector2(9, 1), 3.2f, Coral);
            Label("Jump over red spikes.\nRed hazards still hurt in darkness.", new Vector2(14, 3.55f), new Vector2(9, 1.6f), 2.3f, Muted);
            Place(spikes, "Spikes A", new Vector2(11, 0));
            Place(spikes, "Spikes B", new Vector2(17, 0));

            Label("02 / PULSE BEAMS", new Vector2(29, 7), new Vector2(15, 1), 3.2f, Coral);
            Label("Cross while SAFE. Amber warns; red means stop.\nWait on solid ground and watch the rhythm.", new Vector2(29, 6), new Vector2(15, 1.5f), 2.3f, Muted);
            Place(beam, "Pulse beam A", new Vector2(27, 2.2f));
            Place(beam, "Pulse beam B", new Vector2(33, 2.2f)).GetComponent<PulseBeam>().phaseOffset = 3.1f;
            Label("WAIT", new Vector2(25.1f, 0.2f), new Vector2(2, 0.5f), 1.9f, Mint);
            Label("WAIT", new Vector2(31.1f, 0.2f), new Vector2(2, 0.5f), 1.9f, Mint);

            Label("03 / CRUMBLE BRIDGE", new Vector2(47, 7), new Vector2(14, 1), 3.2f, new Color32(255, 207, 115, 255));
            Label("Keep moving when a tile says MOVE.\nTiles return after a short rest.", new Vector2(47, 6), new Vector2(14, 1.5f), 2.3f, Muted);
            for (int i = 0; i < 4; i++) Place(crumble, "Crumble tile " + (i + 1), new Vector2(43.2f + i * 2.6f, 0));
            Label("Missed a tile? This lower path is safe.\nJump up the step at the end.", new Vector2(47, -0.95f), new Vector2(12, 1.25f), 2.1f, Muted);

            var goal = new GameObject("Lab exit"); goal.transform.position = new Vector3(61.5f, 1.2f, 0);
            var door = goal.AddComponent<BoxCollider2D>(); door.size = new Vector2(1.3f, 2.4f); door.isTrigger = true;
            goal.AddComponent<Victory>();
            Visual("Exit", goal.transform, Vector2.zero, door.size, Mint, 1);
            Visual("Door inset", goal.transform, Vector2.zero, new Vector2(0.85f, 2), new Color32(29, 61, 54, 255), 2);
            Label("NICE WORK", new Vector2(59.5f, 5), new Vector2(10, 1), 3.1f, Mint);
            Label("Reach the green door to finish.", new Vector2(59.5f, 4), new Vector2(10, 1), 2.2f, Muted);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
            }
        }
        var scenes = EditorBuildSettings.scenes.ToList();
        int index = scenes.FindIndex(item => item.path == ScenePath);
        if (index < 0) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        else scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("OBSTACLE_LAB_CREATED: three prefabs, separate scene, tutorial untouched.");
    }

    static void EnsureLightTag()
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = manager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == "Light") return;
        tags.InsertArrayElementAtIndex(tags.arraySize); tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Light";
        manager.ApplyModifiedPropertiesWithoutUndo();
    }

    static Sprite PrimitiveSprite(string name, bool pointed)
    {
        string path = Root + "/Art/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                texture.SetPixel(x, y, !pointed || Mathf.Abs(x - 31.5f) <= (63 - y) * 0.5f ? Color.white : Color.clear);
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static GameObject CreateSpikes()
    {
        var root = new GameObject("Spikes");
        var collider = root.AddComponent<PolygonCollider2D>(); collider.isTrigger = true; collider.pathCount = 3;
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 0.7f;
            collider.SetPath(i, new[] { new Vector2(x - 0.34f, 0), new Vector2(x, 0.75f), new Vector2(x + 0.34f, 0) });
            var visual = Visual("Spike " + (i + 1), root.transform, new Vector2(x, 0.375f), new Vector2(0.68f, 0.75f), Coral, 2);
            visual.sprite = triangle;
        }
        root.AddComponent<SpikeHazard>(); return root;
    }
    static GameObject CreateBeam()
    {
        var root = new GameObject("PulseBeam");
        var collider = root.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(0.36f, 4.4f);
        var behavior = root.AddComponent<PulseBeam>();
        behavior.beamVisual = Visual("Beam", root.transform, Vector2.zero, collider.size, new Color32(128, 157, 178, 35), 2);
        Visual("Top emitter", root.transform, new Vector2(0, 2.28f), new Vector2(1, 0.16f), Muted, 3);
        Visual("Base emitter", root.transform, new Vector2(0, -2.12f), new Vector2(0.9f, 0.16f), Muted, 3);
        behavior.stateLabel = Label("SAFE", new Vector2(0, 2.8f), new Vector2(4, 0.8f), 2.5f, Mint, root.transform);
        return root;
    }
    static GameObject CreateCrumble()
    {
        var root = new GameObject("CrumblePlatform"); root.layer = LayerMask.NameToLayer("Ground");
        var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(2.4f, 0.36f); collider.offset = new Vector2(0, -0.18f);
        var behavior = root.AddComponent<CrumblePlatform>();
        behavior.surface = Visual("Surface", root.transform, collider.offset, collider.size, new Color32(198, 176, 130, 255), 2);
        behavior.stateLabel = Label("", new Vector2(0, 0.9f), new Vector2(2.5f, 0.7f), 2.1f, new Color32(255, 207, 115, 255), root.transform);
        return root;
    }
    static GameObject SavePrefab(GameObject root, string name)
    {
        var result = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root); return result;
    }
    static GameObject Place(GameObject prefab, string name, Vector2 position)
    {
        var result = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        result.name = name; result.transform.position = position; return result;
    }
    static void Floor(string name, float start, float end, float top)
    {
        var root = new GameObject(name); root.layer = LayerMask.NameToLayer("Ground");
        root.transform.position = new Vector3((start + end) / 2, top - 0.4f, 0);
        var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(end - start, 0.8f);
        Visual("Floor", root.transform, Vector2.zero, collider.size, new Color32(66, 83, 101, 255), 0);
        Visual("Lit edge", root.transform, new Vector2(0, 0.36f), new Vector2(end - start, 0.08f), new Color32(104, 170, 148, 255), 1);
    }
    static SpriteRenderer Visual(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var sprite = obj.AddComponent<SpriteRenderer>(); sprite.sprite = square; sprite.color = color;
        sprite.sharedMaterial = material; sprite.sortingOrder = order; return sprite;
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
