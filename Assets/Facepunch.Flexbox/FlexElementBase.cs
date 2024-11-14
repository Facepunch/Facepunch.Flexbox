using Facepunch.Flexbox.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Facepunch.Flexbox
{
    public abstract class FlexElementBase : UIBehaviour, IFlexNode
    {
        [Tooltip("Controls the initial size of the element before factoring in grow/shrink.")]
        public FlexLength Basis;

        [Min(0), Tooltip("How much this flex element should grow relative to its siblings.")]
        public int Grow = 0;

        [Min(0), Tooltip("How much this flex element should shrink relative to its siblings.")]
        public int Shrink = 1;

        [Tooltip("Optionally override the parent's cross axis alignment for this element.")]
        public FlexAlignSelf AlignSelf;

        [Tooltip("The minimum allowed dimensions of this flex element.")]
        public FlexLength MinWidth, MaxWidth;

        [Tooltip("The maximum allowed dimensions of this flex element.")]
        public FlexLength MinHeight, MaxHeight;

        [Tooltip("Overrides for the preferred dimensions of this flex element. Useful for things like images which would normally have a preferred size of zero.")]
        public FlexValue OverridePreferredWidth, OverridePreferredHeight;

        [Tooltip("Absolute elements act as the root container for any number of flex elements.")]
        public bool IsAbsolute;

        [Tooltip("Automatically resize an absolute element to match the size of its children.")]
        public bool AutoSizeX, AutoSizeY;

        protected bool IsDirty, IsDoingLayout;
        protected float PrefWidth, PrefHeight;
        protected readonly List<IFlexNode> Children = new List<IFlexNode>();

        protected virtual bool IsReversed => false;

#if UNITY_EDITOR
        public const DrivenTransformProperties ControlledProperties = DrivenTransformProperties.AnchoredPosition | 
                                                                      DrivenTransformProperties.SizeDelta | 
                                                                      DrivenTransformProperties.Anchors | 
                                                                      DrivenTransformProperties.Pivot | 
                                                                      DrivenTransformProperties.Rotation;
        private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

        internal void PerformLayout()
        {
            var rectTransform = (RectTransform)transform;

            var rect = rectTransform.rect;
            var width = rect.width;
            var height = rect.height;

            var nonAbsoluteRootOverride = !IsAbsolute && FlexUtility.IsPrefabRoot(gameObject);
            var autoSizeX = (IsAbsolute && AutoSizeX) || nonAbsoluteRootOverride;
            var autoSizeY = (IsAbsolute && AutoSizeY) || nonAbsoluteRootOverride;

            var node = (IFlexNode)this;
            node.MeasureHorizontal();
            node.LayoutHorizontal(autoSizeX ? PrefWidth : width, autoSizeY ? PrefHeight : height);
            node.MeasureVertical();
            node.LayoutVertical(autoSizeX ? PrefWidth : width, autoSizeY ? PrefHeight : height);

            IsDoingLayout = true;
            try
            {
#if UNITY_EDITOR
                _drivenTracker.Clear();
#endif
                
                if (autoSizeX)
                {
#if UNITY_EDITOR
                    _drivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
#endif

                    //Debug.Log($"w={_prefWidth}");
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PrefWidth);
                }

                if (autoSizeY)
                {
#if UNITY_EDITOR
                    _drivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
#endif

                    //Debug.Log($"h={_prefHeight}");
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, PrefHeight);
                }
            }
            finally
            {
                IsDoingLayout = false;
            }
        }

        public void SetLayoutDirty(bool force = false)
        {
            if (!force && (IsDoingLayout || !IsActive()))
            {
                return;
            }

            IsDirty = true;
            
            var parent = transform.parent;
            if (IsAbsolute || parent == null || !parent.TryGetComponent<IFlexNode>(out var parentNode))
            {
                FlexLayoutManager.EnqueueLayout(this);
            }
            else
            {
                parentNode.SetLayoutDirty(force);
            }
        }
        
        protected abstract void MeasureHorizontalImpl();
        protected abstract void LayoutHorizontalImpl(float maxWidth, float maxHeight);
        protected abstract void MeasureVerticalImpl();
        protected abstract void LayoutVerticalImpl(float maxWidth, float maxHeight);
        
        #region IFlexNode
        RectTransform IFlexNode.Transform => (RectTransform)transform;
        bool IFlexNode.IsActive => IsActive();
        bool IFlexNode.IsAbsolute => IsAbsolute;
        bool IFlexNode.IsDirty => IsDirty;
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
            if (!IsAbsolute)
            {
                var rectTransform = (RectTransform)transform;
                
#if UNITY_EDITOR
                _drivenTracker.Clear();
                _drivenTracker.Add(this, rectTransform, ControlledProperties);
#endif
                
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.pivot = new Vector2(0, 1); // top left
                rectTransform.anchorMin = new Vector2(0, 1); // top left
                rectTransform.anchorMax = new Vector2(0, 1); // top left
            }
        }

        void IFlexNode.MeasureHorizontal()
        {
            Children.Clear();
            foreach (var child in new FlexChildEnumerable(this, IsReversed))
            {
                Children.Add(child);
                child.SetupTransform();
            }

            MeasureHorizontalImpl();
        }

        void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
        {
            IsDoingLayout = true;
            
            try
            {
                LayoutHorizontalImpl(maxWidth, maxHeight);
            }
            finally
            {
                IsDoingLayout = false;
            }
        }

        void IFlexNode.MeasureVertical()
        {
            MeasureVerticalImpl();
        }

        void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
        {
            IsDoingLayout = true;

            try
            {
                LayoutVerticalImpl(maxWidth, maxHeight);
                IsDirty = false;
            }
            finally
            {
                IsDoingLayout = false;
            }
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
            preferredWidth = Mathf.Clamp(OverridePreferredWidth.GetOrDefault(PrefWidth), MinWidth.GetValueOrDefault(0), MaxWidth.GetValueOrDefault(float.PositiveInfinity));
            preferredHeight = Mathf.Clamp(OverridePreferredHeight.GetOrDefault(PrefHeight), MinHeight.GetValueOrDefault(0), MaxHeight.GetValueOrDefault(float.PositiveInfinity));
        }
        #endregion

        protected override void OnEnable()
        {
            SetLayoutDirty(true);
        }

        protected override void OnDisable()
        {
            SetLayoutDirty(true);

#if UNITY_EDITOR
            _drivenTracker.Clear();
#endif
        }

        protected override void OnRectTransformDimensionsChange() => SetLayoutDirty();

        protected override void OnBeforeTransformParentChanged() => SetLayoutDirty();

        protected override void OnTransformParentChanged() => SetLayoutDirty();

        protected virtual void OnTransformChildrenChanged() => SetLayoutDirty();

#if UNITY_EDITOR
        protected override void OnValidate() => SetLayoutDirty(true);
#endif

        protected static ref T Pick<T>(bool value, ref T ifTrue, ref T ifFalse)
        {
            if (value)
            {
                return ref ifTrue;
            }

            return ref ifFalse;
        }

        protected static float CalculateLengthValue(in FlexLength length, float fillValue, float defaultValue)
        {
            if (!length.HasValue)
            {
                return defaultValue;
            }

            return length.Unit == FlexUnit.Percent
                ? (length.Value / 100f) * fillValue
                : length.Value;
        }
    }
}
