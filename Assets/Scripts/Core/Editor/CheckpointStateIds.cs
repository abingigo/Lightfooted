using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Editor
{
    [InitializeOnLoad]
    internal static class CheckpointStateIds
    {
        static CheckpointStateIds() => EditorSceneManager.sceneSaving += (scene, _) => Assign(scene);

        [MenuItem("Lightfooted/Assign Checkpoint IDs In Build Levels")]
        private static void AssignBuildLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();

            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled) continue;
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                int changed = Assign(scene);
                if (changed > 0) EditorSceneManager.SaveScene(scene);
                Debug.Log($"Checkpoint IDs: {entry.path} ({changed} updated)");
            }

            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static int Assign(Scene scene)
        {
            int changed = 0;
            var used = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var state in root.GetComponentsInChildren<CheckpointState>(true))
            {
                var serialized = new SerializedObject(state);
                var id = serialized.FindProperty("id");
                string wanted = StableId(state);
                if (wanted == null || used.Contains(wanted))
                    wanted = !string.IsNullOrEmpty(id.stringValue) && !used.Contains(id.stringValue)
                        ? id.stringValue
                        : Guid.NewGuid().ToString("N");

                used.Add(wanted);
                if (id.stringValue == wanted) continue;
                id.stringValue = wanted;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed++;
            }

            return changed;
        }

        private static string StableId(UnityEngine.Object target)
        {
            var global = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return global.targetObjectId == 0 ? null : $"{global.targetObjectId:x}-{global.targetPrefabId:x}";
        }
    }
}
