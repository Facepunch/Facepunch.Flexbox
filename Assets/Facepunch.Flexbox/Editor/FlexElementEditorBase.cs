using Facepunch.Flexbox.Utility;
using UnityEditor;

namespace Facepunch.Flexbox
{
    public abstract class FlexElementEditorBase : Editor
    {
        private SerializedProperty _basis;
        private SerializedProperty _grow;
        private SerializedProperty _shrink;
        private SerializedProperty _alignSelf;
        private SerializedProperty _isAbsolute;
        private SerializedProperty _autoSizeX, _autoSizeY;
        private SerializedProperty _minWidth, _maxWidth;
        private SerializedProperty _minHeight, _maxHeight;
        private SerializedProperty _overridePreferredWidth, _overridePreferredHeight;

        public virtual void OnEnable()
        {
            _basis = serializedObject.FindProperty("Basis");
            _grow = serializedObject.FindProperty("Grow");
            _shrink = serializedObject.FindProperty("Shrink");
            _alignSelf = serializedObject.FindProperty("AlignSelf");
            _isAbsolute = serializedObject.FindProperty("IsAbsolute");
            _autoSizeX = serializedObject.FindProperty("AutoSizeX");
            _autoSizeY = serializedObject.FindProperty("AutoSizeY");
            _minWidth = serializedObject.FindProperty("MinWidth");
            _maxWidth = serializedObject.FindProperty("MaxWidth");
            _minHeight = serializedObject.FindProperty("MinHeight");
            _maxHeight = serializedObject.FindProperty("MaxHeight");
            _overridePreferredWidth = serializedObject.FindProperty("OverridePreferredWidth");
            _overridePreferredHeight = serializedObject.FindProperty("OverridePreferredHeight");
        }

        protected abstract void LayoutSection();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            LayoutSection();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_isAbsolute);

            if (!serializedObject.isEditingMultipleObjects && !_isAbsolute.boolValue)
            {
                var elem = (FlexElementBase)serializedObject.targetObject;
                if (!FlexUtility.IsPrefabRoot(elem.gameObject))
                {
                    var parentObj = elem.transform.parent;
                    if (parentObj == null || !parentObj.TryGetComponent<FlexElementBase>(out _))
                    {
                        EditorGUILayout.HelpBox("This element has no parent FlexElement. It should probably be marked as absolute.", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("If this prefab will not spawn parented to another FlexElement then IsAbsolute should be enabled.", MessageType.Info);
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
                    EditorGUILayout.PropertyField(_basis);
                    EditorGUILayout.PropertyField(_grow);
                    EditorGUILayout.PropertyField(_shrink);
                    EditorGUILayout.PropertyField(_alignSelf);
                    EditorGUILayout.PropertyField(_minWidth);
                    EditorGUILayout.PropertyField(_maxWidth);
                    EditorGUILayout.PropertyField(_minHeight);
                    EditorGUILayout.PropertyField(_maxHeight);
                    EditorGUILayout.PropertyField(_overridePreferredWidth);
                    EditorGUILayout.PropertyField(_overridePreferredHeight);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
