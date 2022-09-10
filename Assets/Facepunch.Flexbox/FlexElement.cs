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
    public RectOffset Padding;

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
    private float _minWidth, _minHeight;
    private float _maxWidth, _maxHeight;
    private float _prefWidth, _prefHeight;
    private float _contentPrefWidth, _contentPrefHeight;
    private int _growSum, _shrinkSum;
    private readonly List<IFlexNode> _children = new List<IFlexNode>();
    private readonly List<float> _childSizes = new List<float>();

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
        ref var minSize = ref Pick(horizontal, ref _minWidth, ref _minHeight);
        ref var maxSize = ref Pick(horizontal, ref _maxWidth, ref _maxHeight);
        ref var contentPrefSize = ref Pick(horizontal, ref _contentPrefWidth, ref _contentPrefHeight);
        ref var prefSize = ref Pick(horizontal, ref _prefWidth, ref _prefHeight);
        var padding = horizontal
            ? Padding.left + Padding.right
            : Padding.top + Padding.bottom;

        var mainAxisMinSize = 0f;
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
            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var hasFixedWidth = !float.IsPositiveInfinity(childMaxWidth) && childMinWidth >= childMaxWidth;
            var hasFixedHeight = !float.IsPositiveInfinity(childMaxHeight) && childMinHeight >= childMaxHeight;
            var isFlexible = horizontal ? !hasFixedWidth : !hasFixedHeight;

            if (isFlexible)
            {
                growSum += child.Grow;
                shrinkSum += child.Shrink;
            }

            var gap = first ? 0f : Gap;
            if (horizontal)
            {
                mainAxisMinSize += (childMinWidth * childScaleX) + gap;
                mainAxisPreferredSize += (childPreferredWidth * childScaleX) + gap;
            }
            else
            {
                mainAxisMinSize += (childMinHeight * childScaleY) + gap;
                mainAxisPreferredSize += (childPreferredHeight * childScaleY) + gap;
            }

            if (first)
            {
                first = false;
            }
        }

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;
            minSize = maxSize = horizontal ? rect.width : rect.height;

            if ((horizontal && AutoSizeX) || (!horizontal && AutoSizeY))
            {
                minSize = 0;
                maxSize = float.PositiveInfinity;
            }
        }
        else
        {
            minSize = (horizontal ? MinWidth : MinHeight).GetValueOrDefault(mainAxisMinSize + padding);
            maxSize = (horizontal ? MaxWidth : MaxHeight).GetValueOrDefault(float.PositiveInfinity);
            if (minSize > maxSize) minSize = maxSize;
        }

        contentPrefSize = Mathf.Max(mainAxisPreferredSize, mainAxisMinSize);
        prefSize = Mathf.Clamp(contentPrefSize + padding, minSize, maxSize);

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
        var prefMainSize = horizontal ? _contentPrefWidth : _contentPrefHeight;

        var growthAllowance = Mathf.Max(innerSize - prefMainSize, 0);
        var shrinkAllowance = Mathf.Max(prefMainSize - innerSize, 0);

        var actualMainSize = prefMainSize;
        if (_growSum > 0 && growthAllowance > 0) actualMainSize = innerSize;
        else if (_shrinkSum > 0 && shrinkAllowance > 0) actualMainSize = innerSize;

        //Debug.Log($"({name}) main setup: w={maxWidth} h={maxHeight} inner={innerSize} pref={prefMainSize} grow={growthAllowance} shrink={shrinkAllowance}", this);
        
        SizingChildren.Clear();
        foreach (var child in _children)
        {
            SizingChildren.Add(child);
        }

        _childSizes.Clear();
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

                child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
                child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
                child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

                var childMinMain = horizontal ? childMinWidth : childMinHeight;
                var childMaxMain = horizontal ? childMaxWidth : childMaxHeight;
                var childPrefMain = horizontal ? childPreferredWidth : childPreferredHeight;
                var childFlexible = childMinMain < childMaxMain;

                if (_childSizes.Count == i)
                {
                    var startingMainSize = Mathf.Max(childPrefMain, childMinMain);
                    _childSizes.Add(startingMainSize);
                }

                var finishedFlexing = true;
                var mainSize = _childSizes[i];

                if (growthAllowance > 0 && child.Grow > 0 && childFlexible)
                {
                    finishedFlexing = TakeGrowth(ref mainSize, childMaxMain);

                    bool TakeGrowth(ref float value, float maxValue)
                    {
                        var growPotential = ((float)child.Grow / growSum) * growthAllowance;
                        var growAmount = Mathf.Clamp(maxValue - value, 0, growPotential);
                        value += growAmount;
                        growthAllowance -= growAmount;
                        growSum -= child.Grow;
                        return growAmount <= float.Epsilon;
                    }
                }
                else if (shrinkAllowance > 0 && child.Shrink > 0 && childFlexible)
                {
                    finishedFlexing = TakeShrink(ref mainSize, childMinMain);

                    bool TakeShrink(ref float value, float minValue)
                    {
                        var shrinkPotential = ((float)child.Shrink / shrinkSum) * shrinkAllowance;
                        var shrinkAmount = Mathf.Clamp(value - minValue, 0, shrinkPotential);
                        value -= shrinkAmount;
                        shrinkAllowance -= shrinkAmount;
                        shrinkSum -= child.Shrink;
                        return shrinkAmount <= float.Epsilon;
                    }
                }

                _childSizes[i] = mainSize;

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
            var mainSize = _childSizes[i];
            
            if (horizontal) child.LayoutHorizontal(mainSize, float.PositiveInfinity);
            else child.LayoutVertical(float.PositiveInfinity, mainSize);

            //Debug.Log($"({name}) main: min={childMinMain} max={childMaxMain} pref={childPrefMain} clamped={clampedMainSize}", child.Transform);

            var childRt = child.Transform;

            var childSizeDelta = childRt.sizeDelta;
            childRt.sizeDelta = horizontal
                ? new Vector2(mainSize, childSizeDelta.y)
                : new Vector2(childSizeDelta.x, mainSize);

            var childAnchoredPos = childRt.anchoredPosition;
            childRt.anchoredPosition = horizontal
                ? new Vector2(mainAxisOffset, childAnchoredPos.y)
                : new Vector2(childAnchoredPos.x, mainAxisOffset);

            mainAxisOffset += horizontal
                ? mainSize + Gap
                : -mainSize - Gap;
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
        ref var minSize = ref Pick(horizontal, ref _minHeight, ref _minWidth);
        ref var maxSize = ref Pick(horizontal, ref _maxHeight, ref _maxWidth);
        ref var contentPrefSize = ref Pick(horizontal, ref _contentPrefHeight, ref _contentPrefWidth);
        ref var prefSize = ref Pick(horizontal, ref _prefHeight, ref _prefWidth);
        var padding = horizontal
            ? Padding.left + Padding.right
            : Padding.top + Padding.bottom;

        var crossAxisMinSize = 0f;
        var crossAxisPreferredSize = 0f;
        foreach (var child in _children)
        {
            if (child.IsDirty)
            {
                if (horizontal) child.MeasureVertical();
                else child.MeasureHorizontal();
            }
            
            child.GetScale(out var childScaleX, out var childScaleY);
            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            if (horizontal)
            {
                crossAxisMinSize = Mathf.Max(crossAxisMinSize, childMinHeight * childScaleY);
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredHeight * childScaleY);
            }
            else
            {
                crossAxisMinSize = Mathf.Max(crossAxisMinSize, childMinWidth * childScaleX);
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredWidth * childScaleX);
            }
        }

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;
            minSize = maxSize = horizontal ? rect.height : rect.width;

            if ((!horizontal && AutoSizeX) || (horizontal && AutoSizeY))
            {
                minSize = 0;
                maxSize = float.PositiveInfinity;
            }
        }
        else
        {
            minSize = (horizontal ? MinHeight : MinWidth).GetValueOrDefault(crossAxisMinSize + padding);
            maxSize = (horizontal ? MaxHeight : MaxWidth).GetValueOrDefault(float.PositiveInfinity);
            if (minSize > maxSize) minSize = maxSize;
        }

        contentPrefSize = Mathf.Max(crossAxisPreferredSize, crossAxisMinSize);
        prefSize = Mathf.Clamp(contentPrefSize + padding, minSize, maxSize);

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
            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var childAlign = child.AlignSelf.GetValueOrDefault(AlignItems);
            var childMinCross = horizontal ? childMinHeight : childMinWidth;
            var childMaxCross = horizontal ? childMaxHeight : childMaxWidth;
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
                        : innerSize - Padding.right - childWidth;
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

    void IFlexNode.GetCalculatedMinSize(out float minWidth, out float minHeight)
    {
        minWidth = _minWidth;
        minHeight = _minHeight;
    }

    void IFlexNode.GetCalculatedMaxSize(out float maxWidth, out float maxHeight)
    {
        maxWidth = _maxWidth;
        maxHeight = _maxHeight;
    }

    void IFlexNode.GetPreferredSize(out float preferredWidth, out float preferredHeight)
    {
        preferredWidth = _prefWidth;
        preferredHeight = _prefHeight;
    }
    #endregion

    protected override void OnEnable()
    {
        if (Padding == null)
        {
            Padding = new RectOffset();
        }

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
