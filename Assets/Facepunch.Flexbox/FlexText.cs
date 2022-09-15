using UnityEngine;
using UnityEngine.Profiling;

namespace Facepunch.Flexbox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FlexText : TMPro.TextMeshProUGUI, IFlexNode
    {
        [Min(0), Tooltip("How much this flex element should grow relative to its siblings.")]
        public int Grow = 1;

        [Min(0), Tooltip("How much this flex element should shrink relative to its siblings.")]
        public int Shrink = 1;

        [Tooltip("Optionally override the parent's cross axis alignment for this element.")]
        public FlexAlignSelf AlignSelf;

        [Tooltip("The minimum allowed dimensions of this flex element.")]
        public FlexLength MinWidth, MaxWidth;

        [Tooltip("The maximum allowed dimensions of this flex element.")]
        public FlexLength MinHeight, MaxHeight;

        private bool _isDirty;
        private float _preferredWidth, _preferredHeight;

#if UNITY_EDITOR
        private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            SetupTransform();
            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
#if UNITY_EDITOR
            _drivenTracker.Clear();
#endif

            base.OnDisable();
        }

        public override void SetLayoutDirty()
        {
            _isDirty = true;

            base.SetLayoutDirty();

            var parent = transform.parent;
            if (parent != null && parent.TryGetComponent<IFlexNode>(out var parentNode) && parentNode.IsActive)
            {
                parentNode.SetLayoutDirty();
            }
        }

        private void SetupTransform()
        {
            var rt = (RectTransform)transform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.pivot = new Vector2(0, 1); // top left
            rt.anchorMin = new Vector2(0, 1); // top left
            rt.anchorMax = new Vector2(0, 1); // top left
        }

        #region IFlexNode

        RectTransform IFlexNode.Transform => (RectTransform)transform;
        bool IFlexNode.IsActive => isActiveAndEnabled;
        bool IFlexNode.IsAbsolute => false;
        bool IFlexNode.IsDirty => _isDirty;
        FlexLength IFlexNode.MinWidth => MinWidth;
        FlexLength IFlexNode.MaxWidth => MaxWidth;
        FlexLength IFlexNode.MinHeight => MinHeight;
        FlexLength IFlexNode.MaxHeight => MaxHeight;
        int IFlexNode.Grow => Grow;
        int IFlexNode.Shrink => Shrink;
        FlexAlignSelf IFlexNode.AlignSelf => AlignSelf;

        void IFlexNode.SetLayoutDirty(bool force)
        {
            if (!force && !IsActive())
            {
                return;
            }

            SetLayoutDirty();
        }

        void IFlexNode.MeasureHorizontal()
        {
            Profiler.BeginSample(nameof(IFlexNode.MeasureHorizontal), this);

            // todo: use max width/height if we have any

            var preferredSize = GetPreferredValues();
            _preferredWidth = preferredSize.x;
            _preferredHeight = preferredSize.y;

            Profiler.EndSample();
        }

        void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
        {
            SetupTransform();
        }

        void IFlexNode.MeasureVertical()
        {
            Profiler.BeginSample(nameof(IFlexNode.MeasureVertical), this);

            var rt = (RectTransform)transform;
            var size = rt.sizeDelta;
            //Debug.Log($"text vertical w={size.x}");

            var preferredSize = GetPreferredValues(size.x, float.PositiveInfinity);
            _preferredWidth = preferredSize.x;
            _preferredHeight = preferredSize.y;

            Profiler.EndSample();
        }

        void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
        {
            _isDirty = false;
        }

        void IFlexNode.GetScale(out float scaleX, out float scaleY)
        {
            var rectTransform = (RectTransform)transform;
            var localScale = rectTransform.localScale;
            scaleX = localScale.x;
            scaleY = localScale.y;
        }

        void IFlexNode.GetPreferredSize(out float preferredWidth, out float preferredHeight)
        {
            preferredWidth = _preferredWidth;
            preferredHeight = _preferredHeight;
        }

        #endregion
    }
}
