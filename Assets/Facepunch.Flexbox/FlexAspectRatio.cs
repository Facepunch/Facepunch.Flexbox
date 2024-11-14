using UnityEngine;
using UnityEngine.Profiling;

namespace Facepunch.Flexbox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FlexAspectRatio : MonoBehaviour, IFlexNode
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

        [Tooltip("The aspect ratio to constrain to - X:Y.")]
        public Vector2 AspectRatio = new Vector2(16, 9);

        private float _preferredWidth, _preferredHeight;

#if UNITY_EDITOR
        private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

        protected void OnEnable()
        {
            SetLayoutDirty();
        }

        protected void OnDisable()
        {
            SetLayoutDirty();

#if UNITY_EDITOR
            _drivenTracker.Clear();
#endif
        }

        public void SetLayoutDirty()
        {
            var parent = transform.parent;
            if (parent != null && parent.TryGetComponent<IFlexNode>(out var parentNode) && parentNode.IsActive)
            {
                parentNode.SetLayoutDirty();
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            SetLayoutDirty();
        }
#endif

        #region IFlexNode

        RectTransform IFlexNode.Transform => (RectTransform)transform;
        bool IFlexNode.IsActive => isActiveAndEnabled;
        bool IFlexNode.IsAbsolute => false;
        bool IFlexNode.IsDirty => true;
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
            if (!force && !isActiveAndEnabled)
            {
                return;
            }

            SetLayoutDirty();
        }

        void IFlexNode.MeasureHorizontal()
        {
            Profiler.BeginSample(nameof(IFlexNode.MeasureHorizontal), this);

            _preferredWidth = MinWidth.HasValue && MinWidth.Unit == FlexUnit.Pixels ? MinWidth.Value : 1;
            _preferredHeight = MinHeight.HasValue && MinHeight.Unit == FlexUnit.Pixels ? MinHeight.Value : 1;

            Profiler.EndSample();
        }

        void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
        {
        }

        void IFlexNode.MeasureVertical()
        {
            Profiler.BeginSample(nameof(IFlexNode.MeasureVertical), this);

            var aspect = AspectRatio.x > 0 && AspectRatio.y > 1
                ? AspectRatio.x / AspectRatio.y
                : 1;
            var rt = (RectTransform)transform;
            var size = rt.sizeDelta;

            _preferredHeight = size.x / aspect;

            Profiler.EndSample();
        }

        void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
        {
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
