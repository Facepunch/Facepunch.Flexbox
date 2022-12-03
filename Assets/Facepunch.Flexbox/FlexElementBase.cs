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

        [Tooltip("Absolute elements act as the root container for any number of flex elements.")]
        public bool IsAbsolute;

        [Tooltip("Automatically resize an absolute element to match the size of its children.")]
        public bool AutoSizeX, AutoSizeY;

        protected bool IsDirty, IsDoingLayout;
        protected float PrefWidth, PrefHeight;
        protected readonly List<IFlexNode> Children = new List<IFlexNode>();

        protected virtual bool IsReversed => false;

#if UNITY_EDITOR
        private const DrivenTransformProperties ControlledProperties = DrivenTransformProperties.AnchoredPosition |
                                                                       DrivenTransformProperties.SizeDelta |
                                                                       DrivenTransformProperties.Anchors |
                                                                       DrivenTransformProperties.Pivot |
                                                                       DrivenTransformProperties.Rotation;
        protected DrivenRectTransformTracker DrivenTracker = new DrivenRectTransformTracker();
#endif

        internal void PerformLayout()
        {
            var rectTransform = (RectTransform)transform;

            var rect = rectTransform.rect;
            var width = rect.width;
            var height = rect.height;

            var nonAbsoluteRootOverride = !IsAbsolute && FlexUtility.IsPrefabRoot(gameObject);
            var autoSizeX = AutoSizeX || nonAbsoluteRootOverride;
            var autoSizeY = AutoSizeY || nonAbsoluteRootOverride;

            var node = (IFlexNode)this;
            node.MeasureHorizontal();
            node.LayoutHorizontal(autoSizeX ? PrefWidth : width, autoSizeY ? PrefHeight : height);
            node.MeasureVertical();
            node.LayoutVertical(autoSizeX ? PrefWidth : width, autoSizeY ? PrefHeight : height);

            IsDoingLayout = true;
            try
            {
                if (autoSizeX)
                {
                    //Debug.Log($"w={_prefWidth}");
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PrefWidth);

#if UNITY_EDITOR
                    DrivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
#endif
                }

                if (autoSizeY)
                {
                    //Debug.Log($"h={_prefHeight}");
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, PrefHeight);

#if UNITY_EDITOR
                    DrivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
#endif
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

        private void SetupTransform()
        {
            if (!IsAbsolute)
            {
                var rt = (RectTransform)transform;
                rt.localRotation = Quaternion.identity;
                rt.pivot = new Vector2(0, 1); // top left
                rt.anchorMin = new Vector2(0, 1); // top left
                rt.anchorMax = new Vector2(0, 1); // top left
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

        void IFlexNode.MeasureHorizontal()
        {
#if UNITY_EDITOR
            DrivenTracker.Clear();
#endif

            Children.Clear();
            foreach (var child in new FlexChildEnumerable(this, IsReversed))
            {
                Children.Add(child);

#if UNITY_EDITOR
                DrivenTracker.Add(this, child.Transform, ControlledProperties);
#endif
            }

            MeasureHorizontalImpl();
        }

        void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
        {
            IsDoingLayout = true;

            SetupTransform();

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
            preferredWidth = PrefWidth;
            preferredHeight = PrefHeight;
        }
        #endregion

        protected override void OnEnable()
        {
            SetupTransform();
            SetLayoutDirty(true);
        }

        protected override void OnDisable()
        {
            SetLayoutDirty(true);

#if UNITY_EDITOR
            DrivenTracker.Clear();
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
