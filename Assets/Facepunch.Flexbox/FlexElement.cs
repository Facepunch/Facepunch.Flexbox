using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class FlexElement : UIBehaviour, IFlexNode
{
    private static readonly List<IFlexNode> SizingChildren = new List<IFlexNode>();

    [Tooltip("The direction to layout children in. This determines which axis is the main axis.")]
    public FlexDirection FlexDirection = FlexDirection.Row;

    [Tooltip("Where to start laying out children on the main axis.")]
    public FlexJustify JustifyContent = FlexJustify.Start;

    [Tooltip("How to align child flex elements on the cross axis.")]
    public FlexAlign AlignItems = FlexAlign.Stretch;

    [Tooltip("Spacing to add from this elements borders to where children are laid out.")]
    public FlexPadding Padding;

    [Min(0), Tooltip("Spacing to add between each child flex item.")]
    public float Gap = 0;

    [Min(0), Tooltip("How much this flex element should grow relative to its siblings.")]
    public int Grow = 0;

    [Tooltip("Optionally override the parent's cross axis alignment for this element.")]
    public FlexAlignSelf AlignSelf;

    [Min(0), Tooltip("How much this flex element should shrink relative to its siblings.")]
    public int Shrink = 1;

    [Tooltip("Absolute elements act as the root container for any number of flex elements.")]
    public bool IsAbsolute;

    [Tooltip("Automatically resize an absolute element to match the size of its children.")]
    public bool AutoSizeX, AutoSizeY;

    [Tooltip("The minimum allowed dimensions of this flex element.")]
    public FlexLength MinWidth, MaxWidth;

    [Tooltip("The maximum allowed dimensions of this flex element.")]
    public FlexLength MinHeight, MaxHeight;

    private bool _isDirty;
    private bool _isDoingLayout;
    private float _prefWidth, _prefHeight;
    private int _growSum, _shrinkSum;
    private readonly List<IFlexNode> _children = new List<IFlexNode>();
    private ChildSizingParameters[] _childSizes = Array.Empty<ChildSizingParameters>();

    private struct ChildSizingParameters
    {
        public float Size;
        public float MinSize;
        public float MaxSize;
        public bool IsFlexible;
        public float Scale;
    }

#if UNITY_EDITOR
    private const DrivenTransformProperties ControlledProperties = DrivenTransformProperties.AnchoredPosition |
                                                                   DrivenTransformProperties.SizeDelta |
                                                                   DrivenTransformProperties.Anchors |
                                                                   DrivenTransformProperties.Pivot |
                                                                   DrivenTransformProperties.Rotation;
    private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

    private bool IsHorizontal => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse;
    private bool IsReversed => FlexDirection == FlexDirection.RowReverse || FlexDirection == FlexDirection.ColumnReverse;

    public void SetLayoutDirty(bool force = false)
    {
        if (!force && (_isDoingLayout || !IsActive()))
        {
            return;
        }

        _isDirty = true;

        var parent = transform.parent;
        if (parent == null || !parent.TryGetComponent<IFlexNode>(out var parentNode))
        {
            FlexLayoutManager.EnqueueLayout(this);
        }
        else
        {
            parentNode.SetLayoutDirty(force);
        }
    }

    internal void PerformLayout()
    {
        var rectTransform = (RectTransform)transform;

        var rect = rectTransform.rect;
        var width = rect.width;
        var height = rect.height;

        var node = (IFlexNode)this;
        node.MeasureHorizontal();
        node.LayoutHorizontal(AutoSizeX ? _prefWidth : width, AutoSizeY ? _prefHeight : height);
        node.MeasureVertical();
        node.LayoutVertical(AutoSizeX ? _prefWidth : width, AutoSizeY ? _prefHeight : height);

        _isDoingLayout = true;
        try
        {
            if (AutoSizeX)
            {
                //Debug.Log($"w={_prefWidth}");
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _prefWidth);

#if UNITY_EDITOR
                _drivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
#endif
            }

            if (AutoSizeY)
            {
                //Debug.Log($"h={_prefHeight}");
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _prefHeight);

#if UNITY_EDITOR
                _drivenTracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
#endif
            }
        }
        finally
        {
            _isDoingLayout = false;
        }
    }

    private void MeasureMainAxis()
    {
        Profiler.BeginSample(nameof(MeasureMainAxis), this);

        var horizontal = IsHorizontal;
        ref var prefSize = ref Pick(horizontal, ref _prefWidth, ref _prefHeight);
        var padding = horizontal
            ? Padding.left + Padding.right
            : Padding.top + Padding.bottom;

        var mainAxisPreferredSize = 0f;
        var growSum = 0;
        var shrinkSum = 0;
        var first = true;
        foreach (var child in _children)
        {
            if (child.IsDirty)
            {
                if (horizontal) child.MeasureHorizontal();
                else child.MeasureVertical();
            }

            child.GetScale(out var childScaleX, out var childScaleY);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var childMinSize = horizontal ? child.MinWidth : child.MinHeight;
            var childMaxSize = horizontal ? child.MaxWidth : child.MaxHeight;
            var hasFixedSize = childMinSize.HasValue && childMaxSize.HasValue &&
                               childMinSize.Unit == childMaxSize.Unit &&
                               childMinSize.Value >= childMaxSize.Value;
            if (!hasFixedSize)
            {
                growSum += child.Grow;
                shrinkSum += child.Shrink;
            }

            var gap = first ? 0f : Gap;
            if (horizontal)
            {
                mainAxisPreferredSize += (childPreferredWidth * childScaleX) + gap;
            }
            else
            {
                mainAxisPreferredSize += (childPreferredHeight * childScaleY) + gap;
            }

            if (first)
            {
                first = false;
            }
        }
        
        var minSize = horizontal ? MinWidth : MinHeight;
        var maxSize = horizontal ? MaxWidth : MaxHeight;
        var minClamp = minSize.HasValue && minSize.Unit == FlexUnit.Pixels ? minSize.Value : 0;
        var maxClamp = maxSize.HasValue && maxSize.Unit == FlexUnit.Pixels ? maxSize.Value : float.PositiveInfinity;

        prefSize = Mathf.Clamp(mainAxisPreferredSize + padding, minClamp, maxClamp);

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;

            if (horizontal && !AutoSizeX)
            {
                prefSize = rect.width;
            }
            else if (!horizontal && !AutoSizeY)
            {
                prefSize = rect.height;
            }
        }

        _growSum = growSum;
        _shrinkSum = shrinkSum;

        Profiler.EndSample();
    }

    private void LayoutMainAxis(float maxWidth, float maxHeight)
    {
        Profiler.BeginSample(nameof(LayoutMainAxis), this);

        var horizontal = IsHorizontal;
        var reversed = IsReversed;

        var innerSize = horizontal
            ? maxWidth - Padding.left - Padding.right
            : maxHeight - Padding.top - Padding.bottom;

        SizingChildren.Clear();
        if (_childSizes.Length < _children.Count) Array.Resize(ref _childSizes, _children.Count);
        
        var prefMainContentSize = 0f;
        var first = true;
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            ref var childParams = ref _childSizes[i];

            var childMinMain = CalculateLengthValue(horizontal ? child.MinWidth : child.MinHeight, innerSize, 0);
            var childMaxMain = CalculateLengthValue(horizontal ? child.MaxWidth : child.MaxHeight, innerSize, float.PositiveInfinity);
            var childFlexible = childMinMain < childMaxMain;

            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);
            var childPrefMain = horizontal ? childPreferredWidth : childPreferredHeight;

            child.GetScale(out var childScaleX, out var childScaleY);
            var childScaleMain = horizontal ? childScaleX : childScaleY;
            
            var startingMainSize = Mathf.Clamp(childPrefMain, childMinMain, childMaxMain);

            childParams.Size = startingMainSize;
            childParams.MinSize = childMinMain;
            childParams.MaxSize = childMaxMain;
            childParams.IsFlexible = childFlexible;
            childParams.Scale = childScaleMain;

            SizingChildren.Add(childFlexible ? child : null);

            prefMainContentSize += startingMainSize * childScaleMain;

            if (first)
            {
                first = false;
            }
            else
            {
                prefMainContentSize += Gap;
            }
        }

        var growthAllowance = Mathf.Max(innerSize - prefMainContentSize, 0);
        var shrinkAllowance = Mathf.Max(prefMainContentSize - innerSize, 0);

        var actualMainSize = prefMainContentSize;
        if (_growSum > 0 && growthAllowance > 0) actualMainSize = innerSize;
        else if (_shrinkSum > 0 && shrinkAllowance > 0) actualMainSize = innerSize;

        //Debug.Log($"({name}) main setup: w={maxWidth} h={maxHeight} inner={innerSize} pref={(horizontal ? _prefWidth : _prefHeight)} grow={growthAllowance} shrink={shrinkAllowance}", this);
        
        while (SizingChildren.Exists(n => n != null))
        {
            var growSum = _growSum;
            var shrinkSum = _shrinkSum;

            for (var i = 0; i < SizingChildren.Count; i++)
            {
                var child = SizingChildren[i];
                if (child == null)
                {
                    continue;
                }

                ref var childParams = ref _childSizes[i];

                var finishedFlexing = true;

                if (growthAllowance > 0 && child.Grow > 0 && childParams.IsFlexible)
                {
                    finishedFlexing = TakeGrowth(ref childParams.Size, childParams.MaxSize, childParams.Scale);

                    bool TakeGrowth(ref float value, float maxValue, float scale)
                    {
                        var growPotential = ((float)child.Grow / growSum) * growthAllowance;
                        var growAmount = Mathf.Clamp(maxValue - value, 0, growPotential);
                        value += growAmount / scale;
                        growthAllowance -= growAmount;
                        growSum -= child.Grow;
                        return growAmount <= float.Epsilon;
                    }
                }
                else if (shrinkAllowance > 0 && child.Shrink > 0 && childParams.IsFlexible)
                {
                    finishedFlexing = TakeShrink(ref childParams.Size, childParams.MinSize, childParams.Scale);

                    bool TakeShrink(ref float value, float minValue, float scale)
                    {
                        var shrinkPotential = ((float)child.Shrink / shrinkSum) * shrinkAllowance;
                        var shrinkAmount = Mathf.Clamp(value - minValue, 0, shrinkPotential);
                        value -= shrinkAmount / scale;
                        shrinkAllowance -= shrinkAmount;
                        shrinkSum -= child.Shrink;
                        return shrinkAmount <= float.Epsilon;
                    }
                }

                if (finishedFlexing)
                {
                    SizingChildren[i] = null;
                }
            }
        }

        var mainAxisOffset = GetMainAxisStart(horizontal, reversed);
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            ref var childParams = ref _childSizes[i];
            
            if (horizontal) child.LayoutHorizontal(childParams.Size, float.PositiveInfinity);
            else child.LayoutVertical(float.PositiveInfinity, childParams.Size);

            //Debug.Log($"({name}) main: min={childMinMain} max={childMaxMain} pref={childPrefMain} clamped={clampedMainSize}", child.Transform);

            child.GetScale(out var childScaleX, out var childScaleY);
            var scaledMainSize = childParams.Size * (horizontal ? childScaleX : childScaleY);

            var childRt = child.Transform;

            var childSizeDelta = childRt.sizeDelta;
            childRt.sizeDelta = horizontal
                ? new Vector2(childParams.Size, childSizeDelta.y)
                : new Vector2(childSizeDelta.x, childParams.Size);

            var childAnchoredPos = childRt.anchoredPosition;
            childRt.anchoredPosition = horizontal
                ? new Vector2(mainAxisOffset, childAnchoredPos.y)
                : new Vector2(childAnchoredPos.x, mainAxisOffset);

            mainAxisOffset += horizontal
                ? scaledMainSize + Gap
                : -scaledMainSize - Gap;
        }

        Profiler.EndSample();

        float GetMainAxisStart(bool isHorizontal, bool isReversed)
        {
            switch (JustifyContent)
            {
                case FlexJustify.Start:
                    return isHorizontal
                        ? (isReversed ? innerSize - actualMainSize + Padding.left : Padding.left)
                        : -(isReversed ? innerSize - actualMainSize + Padding.top : Padding.top);
                case FlexJustify.End:
                    return isHorizontal
                        ? (isReversed ? Padding.left : innerSize - actualMainSize + Padding.left)
                        : -(isReversed ? Padding.top : innerSize - actualMainSize + Padding.top);
                case FlexJustify.Center:
                    return isHorizontal
                        ? ((innerSize - actualMainSize) / 2) + Padding.left
                        : -((innerSize - actualMainSize) / 2) - Padding.top;
                default:
                    throw new NotSupportedException(JustifyContent.ToString());
            }
        }
    }

    private void MeasureCrossAxis()
    {
        Profiler.BeginSample(nameof(MeasureCrossAxis), this);

        var horizontal = IsHorizontal;
        ref var prefSize = ref Pick(horizontal, ref _prefHeight, ref _prefWidth);
        var padding = horizontal
            ? Padding.left + Padding.right
            : Padding.top + Padding.bottom;

        var crossAxisPreferredSize = 0f;
        foreach (var child in _children)
        {
            if (child.IsDirty)
            {
                if (horizontal) child.MeasureVertical();
                else child.MeasureHorizontal();
            }
            
            child.GetScale(out var childScaleX, out var childScaleY);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            if (horizontal)
            {
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredHeight * childScaleY);
            }
            else
            {
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredWidth * childScaleX);
            }
        }

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;
            prefSize = horizontal ? rect.height : rect.width;
        }
        else
        {
            var minSize = horizontal ? MinHeight : MinWidth;
            var maxSize = horizontal ? MaxHeight : MaxWidth;
            var minClamp = minSize.HasValue && minSize.Unit == FlexUnit.Pixels ? minSize.Value : 0;
            var maxClamp = maxSize.HasValue && maxSize.Unit == FlexUnit.Pixels ? maxSize.Value : float.PositiveInfinity;

            prefSize = Mathf.Clamp(crossAxisPreferredSize + padding, minClamp, maxClamp);
        }

        Profiler.EndSample();
    }

    private void LayoutCrossAxis(float maxWidth, float maxHeight)
    {
        Profiler.BeginSample(nameof(LayoutCrossAxis), this);

        var horizontal = IsHorizontal;

        var innerSize = horizontal
            ? maxHeight - Padding.top - Padding.bottom
            : maxWidth - Padding.left - Padding.right;

        //Debug.Log($"({name}) cross setup: w={maxWidth} h={maxHeight} inner={innerSize}", this);

        foreach (var child in _children)
        {
            child.GetScale(out var childScaleX, out var childScaleY);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var childAlign = child.AlignSelf.GetValueOrDefault(AlignItems);
            var childMinCross = CalculateLengthValue(horizontal ? child.MinHeight : child.MinWidth, innerSize, 0);
            var childMaxCross = CalculateLengthValue(horizontal ? child.MaxHeight : child.MaxWidth, innerSize, float.PositiveInfinity);
            var childPrefCross = horizontal ? childPreferredHeight : childPreferredWidth;
            var crossSize = childAlign == FlexAlign.Stretch ? innerSize : childPrefCross;
            var clampedCrossSize = Mathf.Clamp(Mathf.Min(crossSize, innerSize), childMinCross, childMaxCross);
            
            var layoutMaxWidth = horizontal ? float.PositiveInfinity : clampedCrossSize;
            var layoutMaxHeight = horizontal ? clampedCrossSize : float.PositiveInfinity;
            
            if (horizontal) child.LayoutVertical(layoutMaxWidth, layoutMaxHeight);
            else child.LayoutHorizontal(layoutMaxWidth, layoutMaxHeight);

            //Debug.Log($"({name}) cross: min={childMinCross} max={childMaxCross} pref={childPrefCross} clamped={clampedCrossSize}", child.Transform);

            var crossAxis = GetCrossAxis(childAlign, horizontal, layoutMaxWidth * childScaleX, layoutMaxHeight * childScaleY);

            var childRt = child.Transform;
            
            var childSizeDelta = childRt.sizeDelta;
            childRt.sizeDelta = horizontal
                ? new Vector2(childSizeDelta.x, clampedCrossSize)
                : new Vector2(clampedCrossSize, childSizeDelta.y);
            
            var childAnchoredPos = childRt.anchoredPosition;
            childRt.anchoredPosition = horizontal
                ? new Vector2(childAnchoredPos.x, crossAxis)
                : new Vector2(crossAxis, childAnchoredPos.y);
        }

        Profiler.EndSample();

        float GetCrossAxis(FlexAlign align, bool isHorizontal, float childWidth, float childHeight)
        {
            switch (align)
            {
                case FlexAlign.Start:
                case FlexAlign.Stretch:
                    return isHorizontal
                        ? -Padding.top
                        : Padding.left;
                case FlexAlign.End:
                    return isHorizontal
                        ? -innerSize - Padding.top + childHeight
                        : innerSize + Padding.left - childWidth;
                case FlexAlign.Center:
                    return isHorizontal
                        ? -((innerSize / 2) - (childHeight / 2) + Padding.top)
                        : (innerSize / 2) - (childWidth / 2) + Padding.left;
                default:
                    throw new NotSupportedException(AlignItems.ToString());
            }
        }
    }

    private static ref T Pick<T>(bool value, ref T ifTrue, ref T ifFalse)
    {
        if (value)
        {
            return ref ifTrue;
        }

        return ref ifFalse;
    }

    private static float CalculateLengthValue(in FlexLength length, float fillValue, float defaultValue)
    {
        if (!length.HasValue)
        {
            return defaultValue;
        }

        return length.Unit == FlexUnit.Percent
            ? (length.Value / 100f) * fillValue
            : length.Value;
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

    #region IFlexNode
    RectTransform IFlexNode.Transform => (RectTransform)transform;
    bool IFlexNode.IsActive => IsActive();
    bool IFlexNode.IsAbsolute => IsAbsolute;
    bool IFlexNode.IsDirty => _isDirty;
    FlexLength IFlexNode.MinWidth => MinWidth;
    FlexLength IFlexNode.MaxWidth => MaxWidth;
    FlexLength IFlexNode.MinHeight => MinHeight;
    FlexLength IFlexNode.MaxHeight => MaxHeight;
    int IFlexNode.Grow => Grow;
    int IFlexNode.Shrink => Shrink;
    FlexAlignSelf IFlexNode.AlignSelf => AlignSelf;

    void IFlexNode.MeasureHorizontal()
    {
#if UNITY_EDITOR
        _drivenTracker.Clear();
#endif

        _children.Clear();
        foreach (var child in new FlexChildEnumerable(this, IsReversed))
        {
            _children.Add(child);

#if UNITY_EDITOR
            _drivenTracker.Add(this, child.Transform, ControlledProperties);
#endif
        }

        if (IsHorizontal) MeasureMainAxis();
        else MeasureCrossAxis();
    }

    void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
    {
        _isDoingLayout = true;
        
        SetupTransform();

        try
        {
            if (IsHorizontal) LayoutMainAxis(maxWidth, maxHeight);
            else LayoutCrossAxis(maxWidth, maxHeight);
        }
        finally
        {
            _isDoingLayout = false;
        }
    }

    void IFlexNode.MeasureVertical()
    {
        if (IsHorizontal) MeasureCrossAxis();
        else MeasureMainAxis();
    }

    void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
    {
        _isDoingLayout = true;

        try
        {
            if (IsHorizontal) LayoutCrossAxis(maxWidth, maxHeight);
            else LayoutMainAxis(maxWidth, maxHeight);

            _isDirty = false;
        }
        finally
        {
            _isDoingLayout = false;
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
        preferredWidth = _prefWidth;
        preferredHeight = _prefHeight;
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
        _drivenTracker.Clear();
#endif
    }

    protected override void OnRectTransformDimensionsChange() => SetLayoutDirty();

    protected override void OnTransformParentChanged() => SetLayoutDirty();

    protected virtual void OnTransformChildrenChanged() => SetLayoutDirty();

#if UNITY_EDITOR
    protected override void OnValidate() => SetLayoutDirty();
#endif
}
