using UnityEditor;

namespace Facepunch.Flexbox
{
    [CustomEditor(typeof(FlexElement))]
    [CanEditMultipleObjects]
    public class FlexElementEditor : FlexElementEditorBase
    {
        private SerializedProperty _flexDirection;
        private SerializedProperty _justifyContent;
        private SerializedProperty _alignItems;
        private SerializedProperty _padding;
        private SerializedProperty _gap;

        public override void OnEnable()
        {
            base.OnEnable();

            _flexDirection = serializedObject.FindProperty("FlexDirection");
            _justifyContent = serializedObject.FindProperty("JustifyContent");
            _alignItems = serializedObject.FindProperty("AlignItems");
            _padding = serializedObject.FindProperty("Padding");
            _gap = serializedObject.FindProperty("Gap");
        }

        protected override void LayoutSection()
        {
            EditorGUILayout.PropertyField(_flexDirection);
            EditorGUILayout.PropertyField(_justifyContent);
            EditorGUILayout.PropertyField(_alignItems);
            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_gap);
        }
    }
}
