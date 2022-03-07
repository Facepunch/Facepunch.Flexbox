using UnityEditor;
using TMPro.EditorUtilities;

[CustomEditor(typeof(FlexText))]
[CanEditMultipleObjects]
public class FlexTextEditor : TMP_EditorPanelUI
{
    private SerializedProperty _grow;
    private SerializedProperty _minWidth, _maxWidth;
    private SerializedProperty _minHeight, _maxHeight;

    protected override void OnEnable()
    {
        base.OnEnable();

        _grow = serializedObject.FindProperty("Grow");
        _minWidth = serializedObject.FindProperty("MinWidth");
        _maxWidth = serializedObject.FindProperty("MaxWidth");
        _minHeight = serializedObject.FindProperty("MinHeight");
        _maxHeight = serializedObject.FindProperty("MaxHeight");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();
        
        EditorGUILayout.PropertyField(_grow);
        EditorGUILayout.PropertyField(_minWidth);
        EditorGUILayout.PropertyField(_maxWidth);
        EditorGUILayout.PropertyField(_minHeight);
        EditorGUILayout.PropertyField(_maxHeight);

        serializedObject.ApplyModifiedProperties();
    }
}
