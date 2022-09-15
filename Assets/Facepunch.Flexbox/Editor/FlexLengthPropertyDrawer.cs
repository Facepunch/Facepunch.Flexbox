using UnityEditor;
using UnityEngine;

namespace Facepunch.Flexbox
{
    [CustomPropertyDrawer(typeof(FlexLength))]
    [CanEditMultipleObjects]
    public class FlexLengthPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hasValue = property.FindPropertyRelative("HasValue");
            var value = property.FindPropertyRelative("Value");
            var unit = property.FindPropertyRelative("Unit");

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            const int toggleWidth = 17;
            const int unitWidth = 35;
            var toggleRect = new Rect(position.x, position.y, 10, position.height);
            var valueRect = new Rect(position.x + toggleWidth, position.y, position.width - toggleWidth - unitWidth, position.height);
            var unitRect = new Rect(position.x + toggleWidth + valueRect.width + 2, position.y, unitWidth, position.height);

            EditorGUI.PropertyField(toggleRect, hasValue, GUIContent.none);

            if (!hasValue.hasMultipleDifferentValues && hasValue.boolValue)
            {
                EditorGUI.PropertyField(valueRect, value, GUIContent.none);
                EditorGUI.PropertyField(unitRect, unit, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }
    }
}
