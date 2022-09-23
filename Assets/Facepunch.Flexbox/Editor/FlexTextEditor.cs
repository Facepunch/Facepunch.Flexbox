using UnityEditor;
using TMPro.EditorUtilities;

namespace Facepunch.Flexbox
{
    [CustomEditor(typeof(FlexText))]
    [CanEditMultipleObjects]
    public class FlexTextEditor : TMP_EditorPanelUI
    {
        private SerializedProperty _basis;
        private SerializedProperty _grow;
        private SerializedProperty _shrink;
        private SerializedProperty _alignSelf;
        private SerializedProperty _minWidth, _maxWidth;
        private SerializedProperty _minHeight, _maxHeight;

        protected override void OnEnable()
        {
            base.OnEnable();

            _basis = serializedObject.FindProperty("Basis");
            _grow = serializedObject.FindProperty("Grow");
            _shrink = serializedObject.FindProperty("Shrink");
            _alignSelf = serializedObject.FindProperty("AlignSelf");
            _minWidth = serializedObject.FindProperty("MinWidth");
            _maxWidth = serializedObject.FindProperty("MaxWidth");
            _minHeight = serializedObject.FindProperty("MinHeight");
            _maxHeight = serializedObject.FindProperty("MaxHeight");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(_basis);
            EditorGUILayout.PropertyField(_grow);
            EditorGUILayout.PropertyField(_shrink);
            EditorGUILayout.PropertyField(_alignSelf);
            EditorGUILayout.PropertyField(_minWidth);
            EditorGUILayout.PropertyField(_maxWidth);
            EditorGUILayout.PropertyField(_minHeight);
            EditorGUILayout.PropertyField(_maxHeight);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
