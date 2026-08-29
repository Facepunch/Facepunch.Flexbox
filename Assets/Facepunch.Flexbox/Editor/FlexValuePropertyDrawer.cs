using UnityEditor;
using UnityEngine;

namespace Facepunch.Flexbox
{
    [CustomPropertyDrawer(typeof(FlexValue))]
    [CanEditMultipleObjects]
    public class FlexValuePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hasValue = property.FindPropertyRelative("HasValue");
            var value = property.FindPropertyRelative("Value");

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            const int toggleWidth = 17;
            var toggleRect = new Rect(position.x, position.y, 10, position.height);
            var valueRect = new Rect(position.x + toggleWidth, position.y, position.width - toggleWidth, position.height);

            EditorGUI.PropertyField(toggleRect, hasValue, GUIContent.none);

            if (!hasValue.hasMultipleDifferentValues && hasValue.boolValue)
            {
                EditorGUI.BeginChangeCheck();

                // massive hack: temporarily shrink labelWidth so the blank label takes less space
                // we need a FloatField to support the value scrubbing 
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 10f;
                float newVal = EditorGUI.FloatField(valueRect, new GUIContent(" "), value.floatValue);
                EditorGUIUtility.labelWidth = oldWidth;

                if (EditorGUI.EndChangeCheck())
                {
                    value.floatValue = newVal;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
