using UnityEditor;

namespace Facepunch.Flexbox
{
    [CustomEditor(typeof(FlexColumnsElement))]
    [CanEditMultipleObjects]
    public class FlexColumnsElementEditor : FlexElementEditorBase
    {
        private SerializedProperty _fixedColumnCount;
        private SerializedProperty _columnCount;
        private SerializedProperty _columnMinWidth;
        private SerializedProperty _padding;
        private SerializedProperty _gap;

        public override void OnEnable()
        {
            base.OnEnable();

            _fixedColumnCount = serializedObject.FindProperty("FixedColumnCount");
            _columnCount = serializedObject.FindProperty("ColumnCount");
            _columnMinWidth = serializedObject.FindProperty("ColumnMinWidth");
            _padding = serializedObject.FindProperty("Padding");
            _gap = serializedObject.FindProperty("Gap");
        }

        protected override void LayoutSection()
        {
            EditorGUILayout.PropertyField(_fixedColumnCount);
            if (!_fixedColumnCount.hasMultipleDifferentValues)
            {
                if (_fixedColumnCount.boolValue)
                {
                    EditorGUILayout.PropertyField(_columnCount);
                }
                else
                {
                    EditorGUILayout.PropertyField(_columnMinWidth);
                }
            }

            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_gap);
        }
    }
}
