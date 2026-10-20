using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Lighting.EditorTools
{
    // Default inspector, but the local profile greys out while a shared profile overrides it.
    [CustomEditor(typeof(LightSource))]
    public sealed class LightSourceEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var local = root.Q<PropertyField>("PropertyField:profile");
            var shared = serializedObject.FindProperty("sharedProfile");

            void Refresh(SerializedProperty p) => local?.SetEnabled(p.objectReferenceValue == null);
            Refresh(shared);
            root.TrackPropertyValue(shared, Refresh);
            return root;
        }
    }
}