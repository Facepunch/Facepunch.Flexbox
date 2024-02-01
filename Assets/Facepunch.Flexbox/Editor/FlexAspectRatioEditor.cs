using UnityEditor;
using TMPro.EditorUtilities;

namespace Facepunch.Flexbox
{
    [CustomEditor(typeof(FlexAspectRatio))]
    [CanEditMultipleObjects]
    public class FlexAspectRatioEditor : Editor
    {
        private SerializedProperty _basis;
        private SerializedProperty _grow;
        private SerializedProperty _shrink;
        private SerializedProperty _alignSelf;
        private SerializedProperty _minWidth, _maxWidth;
        private SerializedProperty _minHeight, _maxHeight;
        private SerializedProperty _aspectRatio;

        protected void OnEnable()
        {
            _basis = serializedObject.FindProperty("Basis");
            _grow = serializedObject.FindProperty("Grow");
            _shrink = serializedObject.FindProperty("Shrink");
            _alignSelf = serializedObject.FindProperty("AlignSelf");
            _minWidth = serializedObject.FindProperty("MinWidth");
            _maxWidth = serializedObject.FindProperty("MaxWidth");
            _minHeight = serializedObject.FindProperty("MinHeight");
            _maxHeight = serializedObject.FindProperty("MaxHeight");
            _aspectRatio = serializedObject.FindProperty("AspectRatio");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_basis);
            EditorGUILayout.PropertyField(_grow);
            EditorGUILayout.PropertyField(_shrink);
            EditorGUILayout.PropertyField(_alignSelf);
            EditorGUILayout.PropertyField(_minWidth);
            EditorGUILayout.PropertyField(_maxWidth);
            EditorGUILayout.PropertyField(_minHeight);
            EditorGUILayout.PropertyField(_maxHeight);
            EditorGUILayout.PropertyField(_aspectRatio);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
