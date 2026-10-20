using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lighting.EditorTools
{
    [CustomPropertyDrawer(typeof(LightProfile))]
    public sealed class LightProfileDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout { text = property.displayName, value = true };
            root.Add(new PropertyField(property.FindPropertyRelative("illuminates")));

            var effects = property.FindPropertyRelative("effects");
            var list = new VisualElement();
            root.Add(list);
            root.Add(new Button(() => ShowAddMenu(effects)) { text = "Add effect" });

            // Rebuild only when the count changes, so typing in a field doesn't rebuild (and lose focus).
            Rebuild(list, effects);
            list.TrackPropertyValue(effects.FindPropertyRelative("Array.size"), _ => Rebuild(list, effects));
            return root;
        }

        private static void Rebuild(VisualElement list, SerializedProperty effects)
        {
            list.Clear();
            for (int i = 0; i < effects.arraySize; i++)
            {
                var element = effects.GetArrayElementAtIndex(i);
                int index = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                string label = LabelFor(element.managedReferenceValue?.GetType());

                VisualElement body = element.hasVisibleChildren ? new PropertyField(element, label) : new Label(label);
                body.style.flexGrow = 1;
                if (body is PropertyField field) field.BindProperty(element);

                row.Add(body);
                row.Add(new Button(() => Remove(effects, index)) { text = "X" });
                list.Add(row);
            }
        }

        private static void ShowAddMenu(SerializedProperty effects)
        {
            effects.serializedObject.Update();
            var present = Enumerable.Range(0, effects.arraySize)
                .Select(i => effects.GetArrayElementAtIndex(i).managedReferenceValue?.GetType())
                .ToHashSet();

            var types = TypeCache.GetTypesDerivedFrom<LightEffect>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name);

            var menu = new GenericMenu();
            foreach (var type in types)
            {
                var content = new GUIContent(LabelFor(type));
                if (present.Contains(type))
                    menu.AddDisabledItem(content, true); // one of each: TryGet returns the first
                else menu.AddItem(content, false, () => Add(effects, type));
            }

            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("No effect types exist yet"));
            menu.ShowAsContext();
        }

        private static void Add(SerializedProperty effects, Type type)
        {
            effects.serializedObject.Update();
            int index = effects.arraySize++;
            effects.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
            effects.serializedObject.ApplyModifiedProperties();
        }

        private static void Remove(SerializedProperty effects, int index)
        {
            effects.serializedObject.Update();
            effects.DeleteArrayElementAtIndex(index);
            effects.serializedObject.ApplyModifiedProperties();
        }

        private static string LabelFor(Type type)
        {
            if (type == null) return "Missing effect (class deleted or renamed)";
            string name = type.Name.EndsWith("Effect") ? type.Name[..^"Effect".Length] : type.Name;
            return ObjectNames.NicifyVariableName(name);
        }
    }
}