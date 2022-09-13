using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(FlexTransition), true)]
internal class FlexTransitionEditor : Editor
{
    private const int TransitionFieldCount = 6;
    private const float LineSpacing = 2;

    private ReorderableList _transitions;

    public void OnEnable()
    {
        _transitions = new ReorderableList(serializedObject, serializedObject.FindProperty("Transitions"), false, true, true, true);
        _transitions.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Transitions");
        _transitions.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = _transitions.serializedProperty.GetArrayElementAtIndex(index);
            DrawTransition(rect, element);
        };
        _transitions.elementHeightCallback = index =>
        {
            var length = _transitions.serializedProperty.arraySize;
            if (length == 0) return EditorGUIUtility.singleLineHeight;

            var height = EditorGUIUtility.singleLineHeight * TransitionFieldCount + (TransitionFieldCount - 1) * LineSpacing;
            return index < length - 1 ? height + EditorGUIUtility.singleLineHeight : height;
        };
        _transitions.onAddCallback = list =>
        {
            list.serializedProperty.arraySize++;
            list.index = list.serializedProperty.arraySize - 1;
            var newElem = list.serializedProperty.GetArrayElementAtIndex(list.index);
            newElem.FindPropertyRelative("Duration").floatValue = 0.25f;
            newElem.FindPropertyRelative("Ease").intValue = (int)LeanTweenType.linear;
        };
    }

    private void DrawTransition(Rect position, SerializedProperty property)
    {
        var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var targetProperty = property.FindPropertyRelative("Property");
        EditorGUI.PropertyField(lineRect, targetProperty);
        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

        var useColor = false;

        var propertyType = (FlexTransition.TransitionProperty)targetProperty.intValue;
        if (propertyType == FlexTransition.TransitionProperty.ImageColor)
        {
            var imageProp = property.FindPropertyRelative("Image");
            EditorGUI.PropertyField(lineRect, imageProp);
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

            useColor = true;
        }
        else if (propertyType == FlexTransition.TransitionProperty.TextColor)
        {
            var textProp = property.FindPropertyRelative("Text");
            EditorGUI.PropertyField(lineRect, textProp);
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
            
            useColor = true;
        }
        else if (propertyType == FlexTransition.TransitionProperty.CanvasAlpha)
        {
            var canvasProp = property.FindPropertyRelative("CanvasGroup");
            EditorGUI.PropertyField(lineRect, canvasProp);
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }
        else
        {
            var elementProp = property.FindPropertyRelative("Element");
            EditorGUI.PropertyField(lineRect, elementProp);
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        if (useColor)
        {
            var fromColor = property.FindPropertyRelative("FromColor");
            EditorGUI.PropertyField(lineRect, fromColor, new GUIContent("From"));
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

            var toColor = property.FindPropertyRelative("ToColor");
            EditorGUI.PropertyField(lineRect, toColor, new GUIContent("To"));
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }
        else
        {
            var fromFloat = property.FindPropertyRelative("FromFloat");
            EditorGUI.PropertyField(lineRect, fromFloat, new GUIContent("From"));
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

            var toFloat = property.FindPropertyRelative("ToFloat");
            EditorGUI.PropertyField(lineRect, toFloat, new GUIContent("To"));
            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        var duration = property.FindPropertyRelative("Duration");
        EditorGUI.PropertyField(lineRect, duration);
        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        
        var ease = property.FindPropertyRelative("Ease");
        EditorGUI.PropertyField(lineRect, ease);
        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _transitions.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
