using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FlexLength))]
[CanEditMultipleObjects]
public class FlexLengthPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var hasValue = property.FindPropertyRelative("HasValue");
        var value = property.FindPropertyRelative("Value");

        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var toggleRect = new Rect(position.x, position.y, 10, position.height);
        var valueRect = new Rect(position.x + 17, position.y, position.width - 17, position.height);

        EditorGUI.PropertyField(toggleRect, hasValue, GUIContent.none);

        if (!hasValue.hasMultipleDifferentValues && hasValue.boolValue)
        {
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);
        }

        EditorGUI.EndProperty();
    }
}
