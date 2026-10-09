using UnityEditor;
using UnityEngine;

namespace magus.dialogue.editor
{
    /// <summary>Shows only the DialogueEntry fields its Kind actually uses - DialogueEntry is
    /// one concrete class with a Kind discriminator (see its summary), so without this every
    /// entry shows every field. Unused fields keep their serialized values; they're just
    /// hidden.</summary>
    [CustomPropertyDrawer(typeof(DialogueEntry))]
    public class DialogueEntryDrawer : PropertyDrawer
    {
        private static readonly string[] TextFields = { "CharacterId", "Expression", "Text" };
        private static readonly string[] ChoiceFields = { "Options", "VariableKey" };
        private static readonly string[] TextInputFields = { "CharacterId", "Text", "VariableKey", "DefaultValue", "MaxLength" };

        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var kind = property.FindPropertyRelative("Kind");
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            var header = new GUIContent($"{label.text} ({kind.enumDisplayNames[kind.enumValueIndex]})");
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, header, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                line.y += line.height + Spacing;
                EditorGUI.PropertyField(line, kind);
                line.y += line.height + Spacing;

                foreach (var name in GetVisibleFields(kind))
                {
                    var field = property.FindPropertyRelative(name);
                    var height = EditorGUI.GetPropertyHeight(field, true);
                    EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, height), field, true);
                    line.y += height + Spacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            var kind = property.FindPropertyRelative("Kind");
            height += EditorGUIUtility.singleLineHeight + Spacing;

            foreach (var name in GetVisibleFields(kind))
            {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + Spacing;
            }

            return height;
        }

        private static string[] GetVisibleFields(SerializedProperty kind)
        {
            return (DialogueEntryKind)kind.enumValueIndex switch
            {
                DialogueEntryKind.Choice => ChoiceFields,
                DialogueEntryKind.TextInput => TextInputFields,
                _ => TextFields
            };
        }
    }
}
