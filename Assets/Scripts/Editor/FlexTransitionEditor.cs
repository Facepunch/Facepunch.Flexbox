﻿using System;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

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

    private static void DrawTransition(Rect position, SerializedProperty property)
    {
        var lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        var targetProperty = property.FindPropertyRelative("Property");
        EditorGUI.PropertyField(lineRect, targetProperty);
        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

        var objectProp = property.FindPropertyRelative("Object");

        GUIContent label;
        Type type;
        var useColor = false;

        var propertyType = (FlexTransition.TransitionProperty)targetProperty.intValue;
        if (propertyType == FlexTransition.TransitionProperty.ImageColor)
        {
            label = new GUIContent("Image");
            type = typeof(Image);
            useColor = true;
        }
        else if (propertyType == FlexTransition.TransitionProperty.TextColor)
        {
            label = new GUIContent("Text");
            type = typeof(TMP_Text);
            useColor = true;
        }
        else if (propertyType == FlexTransition.TransitionProperty.CanvasAlpha)
        {
            label = new GUIContent("CanvasGroup");
            type = typeof(CanvasGroup);
        }
        else
        {
            label = new GUIContent("Element");
            type = typeof(FlexElement);
        }

        objectProp.objectReferenceValue = EditorGUI.ObjectField(lineRect, label, objectProp.objectReferenceValue, type, true);
        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;

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
