using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace magus.editor
{
    /// <summary>Draws a [SerializeReference, SubclassPicker] field (or each element of such a
    /// list) as a type dropdown plus the chosen instance's fields. The dropdown lists every
    /// concrete, non-UnityEngine.Object type assignable to the field's declared type.</summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public class SubclassPickerDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Use [SubclassPicker] with [SerializeReference].");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var value = property.managedReferenceValue;

            var labelWidth = EditorGUIUtility.labelWidth;
            var foldoutRect = new Rect(line.x, line.y, labelWidth, line.height);
            var buttonRect = new Rect(line.x + labelWidth, line.y, line.width - labelWidth, line.height);

            if (value != null)
            {
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            }
            else
            {
                EditorGUI.LabelField(foldoutRect, label);
            }

            var typeLabel = new GUIContent(value != null ? ObjectNames.NicifyVariableName(value.GetType().Name) : "(None)");
            if (EditorGUI.DropdownButton(buttonRect, typeLabel, FocusType.Keyboard))
            {
                ShowTypeMenu(property, buttonRect);
            }

            if (value != null && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                line.y += line.height + Spacing;

                foreach (var child in GetChildren(property))
                {
                    var height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, height), child, true);
                    line.y += height + Spacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ManagedReference
                || property.managedReferenceValue == null
                || !property.isExpanded)
            {
                return height;
            }

            foreach (var child in GetChildren(property))
            {
                height += EditorGUI.GetPropertyHeight(child, true) + Spacing;
            }

            return height;
        }

        private static System.Collections.Generic.IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            var end = property.GetEndProperty();
            var child = property.Copy();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                yield return child.Copy();
            }
        }

        private static void ShowTypeMenu(SerializedProperty property, Rect buttonRect)
        {
            var baseType = GetFieldType(property);
            if (baseType == null)
            {
                return;
            }

            // Captured by path, not by SerializedProperty - the property object can be stale
            // by the time the menu callback runs.
            var serializedObject = property.serializedObject;
            var path = property.propertyPath;
            var current = property.managedReferenceValue?.GetType();

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(None)"), current == null, () => Assign(serializedObject, path, null));

            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var t = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name)), current == t, () => Assign(serializedObject, path, t));
            }

            menu.DropDown(buttonRect);
        }

        private static void Assign(SerializedObject serializedObject, string path, Type type)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(path);
            if (property == null)
            {
                return;
            }

            property.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
            property.isExpanded = type != null;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>managedReferenceFieldTypename is "AssemblyName Namespace.TypeName".</summary>
        private static Type GetFieldType(SerializedProperty property)
        {
            var parts = property.managedReferenceFieldTypename.Split(' ');
            if (parts.Length != 2)
            {
                return null;
            }

            return Type.GetType($"{parts[1]}, {parts[0]}");
        }
    }
}
