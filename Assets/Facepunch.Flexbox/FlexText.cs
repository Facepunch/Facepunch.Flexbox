using UnityEngine;
using UnityEngine.Profiling;

namespace Facepunch.Flexbox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FlexText : TMPro.TextMeshProUGUI, IFlexNode
    {
        [Tooltip("Controls the initial size of the element before factoring in grow/shrink.")]
        public FlexLength Basis;

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

            SetLayoutDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SetLayoutDirty();

#if UNITY_EDITOR
            _drivenTracker.Clear();
#endif
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

        #region IFlexNode

        RectTransform IFlexNode.Transform => (RectTransform)transform;
        bool IFlexNode.IsActive => isActiveAndEnabled;
        bool IFlexNode.IsAbsolute => false;
        bool IFlexNode.IsDirty => _isDirty;
        FlexLength IFlexNode.MinWidth => MinWidth;
        FlexLength IFlexNode.MaxWidth => MaxWidth;
        FlexLength IFlexNode.MinHeight => MinHeight;
        FlexLength IFlexNode.MaxHeight => MaxHeight;
        FlexLength IFlexNode.Basis => Basis;
        int IFlexNode.Grow => Grow;
        int IFlexNode.Shrink => Shrink;
        FlexAlignSelf IFlexNode.AlignSelf => AlignSelf;

        void IFlexNode.SetupTransform()
        {
            var rectTransform = (RectTransform)transform;

#if UNITY_EDITOR
            _drivenTracker.Clear();
            _drivenTracker.Add(this, rectTransform, FlexElementBase.ControlledProperties);
#endif

            rectTransform.localRotation = Quaternion.identity;
            rectTransform.pivot = new Vector2(0, 1); // top left
            rectTransform.anchorMin = new Vector2(0, 1); // top left
            rectTransform.anchorMax = new Vector2(0, 1); // top left
        }

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

            var maxWidth = MaxWidth.GetValueOrDefault(float.PositiveInfinity);
            var maxHeight = MaxHeight.GetValueOrDefault(float.PositiveInfinity);
            var preferredSize = GetPreferredValues(maxWidth, maxHeight);
            _preferredWidth = Mathf.Max(preferredSize.x, MinWidth.GetValueOrDefault(0));
            _preferredHeight = Mathf.Max(preferredSize.y, MinHeight.GetValueOrDefault(0));

            //Debug.Log($"text horizontal prefW={_preferredWidth} prefH={_preferredHeight}");

            Profiler.EndSample();
        }

        void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
        {
        }

        void IFlexNode.MeasureVertical()
        {
            Profiler.BeginSample(nameof(IFlexNode.MeasureVertical), this);

            var rt = (RectTransform)transform;
            var size = rt.sizeDelta;

            var maxHeight = MaxHeight.GetValueOrDefault(float.PositiveInfinity);
            var preferredSize = GetPreferredValues(size.x, maxHeight);
            _preferredWidth = Mathf.Max(preferredSize.x, MinWidth.GetValueOrDefault(0));
            _preferredHeight = Mathf.Max(preferredSize.y, MinHeight.GetValueOrDefault(0));

            //Debug.Log($"text vertical w={size.x} prefW={_preferredWidth} prefH={_preferredHeight}");

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
