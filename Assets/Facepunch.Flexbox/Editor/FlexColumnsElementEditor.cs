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
        private SerializedProperty _verticalFill;
        private SerializedProperty _padding;
        private SerializedProperty _horizontalSpacing;
        private SerializedProperty _verticalSpacing;

        public override void OnEnable()
        {
            base.OnEnable();

            _fixedColumnCount = serializedObject.FindProperty("FixedColumnCount");
            _columnCount = serializedObject.FindProperty("ColumnCount");
            _columnMinWidth = serializedObject.FindProperty("ColumnMinWidth");
            _verticalFill = serializedObject.FindProperty("VerticalFill");
            _padding = serializedObject.FindProperty("Padding");
            _horizontalSpacing = serializedObject.FindProperty("HorizontalSpacing");
            _verticalSpacing = serializedObject.FindProperty("VerticalSpacing");
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

            EditorGUILayout.PropertyField(_verticalFill);
            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_horizontalSpacing);
            EditorGUILayout.PropertyField(_verticalSpacing);
        }
    }
}
