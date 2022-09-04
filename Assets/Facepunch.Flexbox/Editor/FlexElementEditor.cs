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
    private SerializedProperty _autoSizeX, _autoSizeY;
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
        _autoSizeX = serializedObject.FindProperty("AutoSizeX");
        _autoSizeY = serializedObject.FindProperty("AutoSizeY");
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

        if (!serializedObject.isEditingMultipleObjects && !_isAbsolute.boolValue)
        {
            var elem = (FlexElement)serializedObject.targetObject;
            var parentObj = elem.transform.parent;
            if (parentObj == null || !parentObj.TryGetComponent<FlexElement>(out _))
            {
                EditorGUILayout.HelpBox("This element has no parent FlexElement, it should probably be marked as absolute.", MessageType.Warning);
            }
        }

        if (!_isAbsolute.hasMultipleDifferentValues)
        {
            if (_isAbsolute.boolValue)
            {
                EditorGUILayout.PropertyField(_autoSizeX);
                EditorGUILayout.PropertyField(_autoSizeY);
            }
            else
            {
                EditorGUILayout.PropertyField(_grow);
                EditorGUILayout.PropertyField(_shrink);
                EditorGUILayout.PropertyField(_minWidth);
                EditorGUILayout.PropertyField(_maxWidth);
                EditorGUILayout.PropertyField(_minHeight);
                EditorGUILayout.PropertyField(_maxHeight);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
