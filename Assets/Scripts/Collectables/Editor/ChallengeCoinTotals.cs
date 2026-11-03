using Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Collectables.Editor
{
    [InitializeOnLoad]
    internal static class ChallengeCoinTotals
    {
        static ChallengeCoinTotals() => EditorSceneManager.sceneSaving += (scene, _) => Record(scene);

        [MenuItem("Lightfooted/Count Challenge Coins In Listed Levels")]
        private static void CountListedLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();

            foreach (var guid in AssetDatabase.FindAssets("t:LevelList"))
            {
                var list = AssetDatabase.LoadAssetAtPath<LevelList>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var level in list.Levels)
                {
                    if (string.IsNullOrEmpty(level.ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(level.ScenePath) == null) continue;
                    var scene = EditorSceneManager.OpenScene(level.ScenePath, OpenSceneMode.Single);
                    Debug.Log($"Challenge coins: {level.ScenePath} has {Record(scene)}");
                }
            }

            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static int Record(Scene scene)
        {
            int total = 0;
            foreach (var root in scene.GetRootGameObjects())
                total += root.GetComponentsInChildren<ChallengeCoin>(true).Length;

            foreach (var guid in AssetDatabase.FindAssets("t:LevelList"))
            {
                var list = AssetDatabase.LoadAssetAtPath<LevelList>(AssetDatabase.GUIDToAssetPath(guid));
                if (list == null) continue;
                var serialized = new SerializedObject(list);
                var levels = serialized.FindProperty("levels");
                bool changed = false;
                for (int i = 0; i < levels.arraySize; i++)
                {
                    var level = levels.GetArrayElementAtIndex(i);
                    if (level.FindPropertyRelative("scene.path").stringValue != scene.path) continue;
                    var coins = level.FindPropertyRelative("coinTotal");
                    if (coins.intValue == total) continue;
                    coins.intValue = total;
                    changed = true;
                }

                if (!changed) continue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(list);
            }

            return total;
        }
    }
}
