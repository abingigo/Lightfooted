using System.Linq;
using UnityEditor;

namespace Core.Editor
{
    internal sealed class SceneReferencePathSync : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!moved.Any(path => path.EndsWith(".unity"))) return;
            EditorApplication.delayCall += SyncAll;
        }

        private static void SyncAll()
        {
            foreach (var id in AssetDatabase.FindAssets("t:" + nameof(LevelList)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelList>(AssetDatabase.GUIDToAssetPath(id));
                if (asset == null) continue;

                var serialized = new SerializedObject(asset);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.type != nameof(SceneReference)) continue;
                    var guid = property.FindPropertyRelative("guid").stringValue;
                    var path = property.FindPropertyRelative("path");
                    var current = string.IsNullOrEmpty(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid);
                    if (current.Length > 0 && path.stringValue != current) path.stringValue = current;
                }

                if (serialized.ApplyModifiedPropertiesWithoutUndo()) AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
    }
}
