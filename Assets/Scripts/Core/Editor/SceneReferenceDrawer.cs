using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Core.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public sealed class SceneReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var guid = property.FindPropertyRelative("guid");
            var path = property.FindPropertyRelative("path");

            var root = new VisualElement();
            var field = new ObjectField(property.displayName) { objectType = typeof(SceneAsset), allowSceneObjects = false };
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            var warning = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            root.Add(field);
            root.Add(warning);

            void Refresh()
            {
                field.SetValueWithoutNotify(Load(guid.stringValue));
                var problem = Problem(guid.stringValue, path.stringValue);
                warning.text = problem ?? string.Empty;
                warning.style.display = problem == null ? DisplayStyle.None : DisplayStyle.Flex;
            }

            field.RegisterValueChangedCallback(evt =>
            {
                var assetPath = evt.newValue ? AssetDatabase.GetAssetPath(evt.newValue) : string.Empty;
                guid.stringValue = assetPath.Length > 0 ? AssetDatabase.AssetPathToGUID(assetPath) : string.Empty;
                path.stringValue = assetPath;
                property.serializedObject.ApplyModifiedProperties();
                Refresh();
            });

            root.TrackPropertyValue(property, _ => Refresh());
            root.RegisterCallback<AttachToPanelEvent>(_ => EditorBuildSettings.sceneListChanged += Refresh);
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorBuildSettings.sceneListChanged -= Refresh);
            Refresh();
            return root;
        }

        private static SceneAsset Load(string guid) =>
            string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));

        private static string Problem(string guid, string path)
        {
            if (string.IsNullOrEmpty(guid)) return "No scene set.";
            if (Load(guid) == null) return $"Scene is missing (was {path}).";
            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.guid.ToString() == guid))
                return "Not ticked in Build Settings, so it won't appear in game.";
            return null;
        }
    }
}
