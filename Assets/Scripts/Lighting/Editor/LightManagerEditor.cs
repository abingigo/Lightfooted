using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lighting.EditorTools
{
    [CustomEditor(typeof(LightManager))]
    public sealed class LightManagerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var grace = serializedObject.FindProperty("graceSteps");
            var info = new HelpBox("", HelpBoxMessageType.None);

            void Refresh(SerializedProperty p) =>
                info.text =
                    $"{p.intValue} steps = {p.intValue * Time.fixedDeltaTime:0.###} s at {1f / Time.fixedDeltaTime:0} Hz";

            var field = root.Q<PropertyField>("PropertyField:graceSteps");
            if (field != null) field.parent.Insert(field.parent.IndexOf(field) + 1, info);
            else root.Add(info);

            Refresh(grace);
            root.TrackPropertyValue(grace, Refresh);
            return root;
        }
    }
}