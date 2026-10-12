using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightPlatformer.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameUiSetup
{
    [MenuItem("Tools/Johnny UI/Create or update menu scene")]
    public static void CreateMenuScene()
    {
        // Never save or edit the currently open team scene.
        var previous = EditorSceneManager.GetSceneManagerSetup();
        Directory.CreateDirectory("Assets/Scenes/Johnny");
        var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Menu camera").AddComponent<Camera>();
        camera.backgroundColor = new Color32(17, 22, 29, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.orthographic = true;
        EditorSceneManager.SaveScene(menu, LevelCatalog.MenuPath);
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(LevelCatalog.MenuPath, true) };
        scenes.AddRange(EditorBuildSettings.scenes.Where(scene => scene.path != LevelCatalog.MenuPath));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.Refresh();
        if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
        Debug.Log("Johnny UI menu ready. Tutorial scene unchanged.");
    }

    public static void BuildPreview()
    {
        Directory.CreateDirectory("tmp/UIBuild");
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "tmp/UIBuild/Lightfooted.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        var result = BuildPipeline.BuildPlayer(options);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("UI preview build failed: " + result.summary.result);
        Debug.Log("UI_PREVIEW_BUILD_SUCCEEDED");
    }
}
