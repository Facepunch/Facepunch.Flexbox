using UnityEditor;

[CustomEditor(typeof(FlexElement))]
[CanEditMultipleObjects]
public class FlexElementEditor : Editor
{
    private SerializedProperty _flexDirection;
    private SerializedProperty _justifyContent;
    private SerializedProperty _alignItems;
    private SerializedProperty _padding;
    private SerializedProperty _gap;
    private SerializedProperty _grow;
    private SerializedProperty _shrink;
    private SerializedProperty _isAbsolute;
    private SerializedProperty _overflowX, _overflowY;
    private SerializedProperty _minWidth, _maxWidth;
    private SerializedProperty _minHeight, _maxHeight;

    public void OnEnable()
    {
        _flexDirection = serializedObject.FindProperty("FlexDirection");
        _justifyContent = serializedObject.FindProperty("JustifyContent");
        _alignItems = serializedObject.FindProperty("AlignItems");
        _padding = serializedObject.FindProperty("Padding");
        _gap = serializedObject.FindProperty("Gap");
        _grow = serializedObject.FindProperty("Grow");
        _shrink = serializedObject.FindProperty("Shrink");
        _isAbsolute = serializedObject.FindProperty("IsAbsolute");
        _overflowX = serializedObject.FindProperty("OverflowX");
        _overflowY = serializedObject.FindProperty("OverflowY");
        _minWidth = serializedObject.FindProperty("MinWidth");
        _maxWidth = serializedObject.FindProperty("MaxWidth");
        _minHeight = serializedObject.FindProperty("MinHeight");
        _maxHeight = serializedObject.FindProperty("MaxHeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_flexDirection);
        EditorGUILayout.PropertyField(_justifyContent);
        EditorGUILayout.PropertyField(_alignItems);
        EditorGUILayout.PropertyField(_padding);
        EditorGUILayout.PropertyField(_gap);
        EditorGUILayout.PropertyField(_isAbsolute);

        if (!_isAbsolute.hasMultipleDifferentValues && !_isAbsolute.boolValue)
        {
            EditorGUILayout.PropertyField(_overflowX);
            EditorGUILayout.PropertyField(_overflowY);

            EditorGUILayout.PropertyField(_grow);
            EditorGUILayout.PropertyField(_shrink);
            EditorGUILayout.PropertyField(_minWidth);

            if (!_overflowX.hasMultipleDifferentValues && !_overflowX.boolValue)
            {
                EditorGUILayout.PropertyField(_maxWidth);
            }

            EditorGUILayout.PropertyField(_minHeight);

            if (!_overflowY.hasMultipleDifferentValues && !_overflowY.boolValue)
            {
                EditorGUILayout.PropertyField(_maxHeight);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
