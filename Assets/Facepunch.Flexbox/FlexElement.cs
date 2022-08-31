using System;
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class FlexElement : UIBehaviour, IFlexNode
{
    public FlexDirection FlexDirection = FlexDirection.Row;
    public FlexJustify JustifyContent = FlexJustify.Start;
    public FlexAlign AlignItems = FlexAlign.Stretch;
    public RectOffset Padding;
    [Min(0)]
    public float Gap = 0;
    [Min(0)]
    public int Grow = 0;
    [Min(0)]
    public int Shrink = 1;
    public bool IsAbsolute;
    public FlexLength MinWidth, MaxWidth;
    public FlexLength MinHeight, MaxHeight;
    public bool OverflowX, OverflowY;

    private bool _isDirty;
    private bool _isDoingLayout;
    private float _minWidth, _minHeight;
    private float _maxWidth, _maxHeight;
    private float _prefWidth, _prefHeight;
    private float _contentPrefWidth, _contentPrefHeight;
    private int _growSum, _shrinkSum;

#if UNITY_EDITOR
    private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

    private bool IsHorizontal => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse;
    private bool IsReversed => FlexDirection == FlexDirection.RowReverse || FlexDirection == FlexDirection.ColumnReverse;

    protected override void Awake()
    {
        if (Padding == null)
        {
            Padding = new RectOffset();
        }

        SetupTransform();
    }

    public void SetLayoutDirty(bool force = false)
    {
        if (!force && (_isDoingLayout || !IsActive()))
        {
            return;
        }
        
#if !UNITY_EDITOR
        if (_isDirty)
        {
            return;
        }
#endif
        
        _isDirty = true;

#if UNITY_EDITOR
        SetupTransform();
#endif

        var parent = transform.parent;
        if (parent == null || !parent.TryGetComponent<IFlexNode>(out var parentNode) || !parentNode.IsActive)
        {
            FlexLayoutManager.EnqueueLayout(this);
        }
        else
        {
            parentNode.SetLayoutDirty();
        }
    }

    internal void PerformLayout()
    {
        var parentObj = transform.parent != null
            ? transform.parent.gameObject
            : gameObject;
        if (!parentObj.TryGetComponent<RectTransform>(out var parentRectTransform))
        {
            Debug.LogWarning("FlexElement has no parent or self RectTransform - cannot do layout!");
            _minWidth = _maxWidth = 0;
            _minHeight = _maxHeight = 0;
            _prefWidth = _contentPrefWidth = 0;
            _prefHeight = _contentPrefHeight = 0;
            _growSum = 0;
            _shrinkSum = 0;
            return;
        }

        var parentRect = parentRectTransform.rect;
        var width = parentRect.width;
        var height = parentRect.height;

        var node = (IFlexNode)this;
        node.Measure();
        node.LayoutHorizontal(width, height);
        node.LayoutVertical(width, height);
    }

    private void MeasureImpl()
    {
        var oldMinWidth = _minWidth;
        var oldMinHeight = _minHeight;
        var oldMaxWidth = _maxWidth;
        var oldMaxHeight = _maxHeight;
        var oldContentPrefWidth = _contentPrefWidth;
        var oldContentPrefHeight = _contentPrefHeight;
        var oldGrowSum = _growSum;
        var oldShrinkSum = _shrinkSum;

        var horizontal = IsHorizontal;
        var mainAxisMinSize = 0f;
        var crossAxisMinSize = 0f;
        var mainAxisPreferredSize = 0f;
        var crossAxisPreferredSize = 0f;
        var growSum = 0;
        var shrinkSum = 0;
        var first = true;
        foreach (var child in Children())
        {
            if (child.IsDirty)
            {
                child.Measure();
            }

            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            childPreferredWidth = Mathf.Clamp(childPreferredWidth, childMinWidth, childMaxWidth);
            childPreferredHeight = Mathf.Clamp(childPreferredHeight, childMinHeight, childMaxHeight);

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
                mainAxisMinSize += childMinWidth + gap;
                crossAxisMinSize = Mathf.Max(crossAxisMinSize, childMinHeight);

                mainAxisPreferredSize += childPreferredWidth + gap;
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredHeight);
            }
            else
            {
                mainAxisMinSize += childMinHeight + gap;
                crossAxisMinSize = Mathf.Max(crossAxisMinSize, childMinWidth);

                mainAxisPreferredSize += childPreferredHeight + gap;
                crossAxisPreferredSize = Mathf.Max(crossAxisPreferredSize, childPreferredWidth);
            }

            if (first)
            {
                first = false;
            }
        }

        var contentMinWidth = horizontal ? mainAxisMinSize : crossAxisMinSize;
        var contentMinHeight = horizontal ? crossAxisMinSize : mainAxisMinSize;

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;
            _minWidth = _maxWidth = rect.width;
            _minHeight = _maxHeight = rect.height;      
        }
        else
        {
            var calculatedMinWidth = Padding.left + contentMinWidth + Padding.right;
            var calculatedMinHeight = Padding.top + contentMinHeight + Padding.bottom;
            _minWidth = MinWidth.GetValueOrDefault(calculatedMinWidth);
            _minHeight = MinHeight.GetValueOrDefault(calculatedMinHeight);

            _maxWidth = MaxWidth.GetValueOrDefault(float.PositiveInfinity);
            _maxHeight = MaxHeight.GetValueOrDefault(float.PositiveInfinity);
        }

        if (OverflowX) _maxWidth = float.PositiveInfinity;
        if (OverflowY) _maxHeight = float.PositiveInfinity;

        _contentPrefWidth = Mathf.Max(horizontal ? mainAxisPreferredSize : crossAxisPreferredSize, contentMinWidth);
        _contentPrefHeight = Mathf.Max(horizontal ? crossAxisPreferredSize : mainAxisPreferredSize, contentMinHeight);

        _prefWidth = Mathf.Max(Padding.left + _contentPrefWidth + Padding.right, _minWidth);
        _prefHeight = Mathf.Max(Padding.top + _contentPrefHeight + Padding.bottom, _minHeight);

        _growSum = growSum;
        _shrinkSum = shrinkSum;

        if (!_isDirty && (_minWidth != oldMinWidth || _minHeight != oldMinHeight ||
            _maxWidth != oldMaxWidth || _maxHeight != oldMaxHeight ||
            _contentPrefWidth != oldContentPrefWidth || _contentPrefHeight != oldContentPrefHeight ||
            _growSum != oldGrowSum || _shrinkSum != oldShrinkSum))
        {
            SetLayoutDirty();
        }
    }

    private void LayoutMainAxis(float maxWidth, float maxHeight)
    {
        var horizontal = IsHorizontal;
        var reversed = IsReversed;

        var innerSize = horizontal
            ? maxWidth - Padding.left - Padding.right
            : maxHeight - Padding.top - Padding.bottom;
        var prefMainSize = horizontal ? _contentPrefWidth : _contentPrefHeight;

        var growSum = _growSum;
        var growthAllowance = Mathf.Max(innerSize - prefMainSize, 0);

        var shrinkSum = _shrinkSum;
        var shrinkAllowance = Mathf.Max(prefMainSize - innerSize, 0);

        var actualMainSize = prefMainSize;
        if (growSum > 0 && growthAllowance > 0) actualMainSize = innerSize;
        else if (shrinkSum > 0 && shrinkAllowance > 0) actualMainSize = innerSize;

        //Debug.Log($"({name}) main setup: w={maxWidth} h={maxHeight} inner={innerSize} pref={prefMainSize} grow={growthAllowance} shrink={shrinkAllowance}", this);

        var mainAxisOffset = GetMainAxisStart(horizontal, reversed);
        foreach (var child in Children(reversed))
        {
            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var childMinMain = horizontal ? childMinWidth : childMinHeight;
            var childMaxMain = horizontal ? childMaxWidth : childMaxHeight;
            var childPrefMain = horizontal ? childPreferredWidth : childPreferredHeight;
            var childFlexible = childMinMain < childMaxMain;

            var mainSize = Mathf.Max(childPrefMain, childMinMain);
            if (growthAllowance > 0 && child.Grow > 0 && childFlexible)
            {
                if (horizontal) TakeGrowth(ref mainSize, childMaxWidth);
                else TakeGrowth(ref mainSize, childMaxHeight);

                void TakeGrowth(ref float value, float maxValue)
                {
                    var growPotential = ((float)child.Grow / growSum) * growthAllowance;
                    var growAmount = Mathf.Clamp(maxValue - value, 0, growPotential);
                    value += growAmount;
                    growthAllowance -= growAmount;
                    growSum -= child.Grow;
                }
            }
            
            if (shrinkAllowance > 0 && child.Shrink > 0 && childFlexible)
            {
                if (horizontal) TakeShrink(ref mainSize, childMinWidth);
                else TakeShrink(ref mainSize, childMinHeight);

                void TakeShrink(ref float value, float minValue)
                {
                    var shrinkPotential = ((float)child.Shrink / shrinkSum) * shrinkAllowance;
                    var shrinkAmount = Mathf.Clamp(value - minValue, 0, shrinkPotential);
                    value -= shrinkAmount;
                    shrinkAllowance -= shrinkAmount;
                    shrinkSum -= child.Shrink;
                }
            }

            var clampedMainSize = Mathf.Clamp(mainSize, childMinMain, childMaxMain);

            //Debug.Log($"({name}) main: min={childMinMain} max={childMaxMain} pref={childPrefMain} clamped={clampedMainSize}", child.Transform);

            if (horizontal) child.LayoutHorizontal(clampedMainSize, float.PositiveInfinity);
            else child.LayoutVertical(float.PositiveInfinity, clampedMainSize);

            var childRt = child.Transform;

            var childSizeDelta = childRt.sizeDelta;
            childRt.sizeDelta = horizontal
                ? new Vector2(clampedMainSize, childSizeDelta.y)
                : new Vector2(childSizeDelta.x, clampedMainSize);

            var childAnchoredPos = childRt.anchoredPosition;
            childRt.anchoredPosition = horizontal
                ? new Vector2(mainAxisOffset, childAnchoredPos.y)
                : new Vector2(childAnchoredPos.x, mainAxisOffset);

            mainAxisOffset += horizontal
                ? clampedMainSize + Gap
                : -clampedMainSize - Gap;
        }

        _isDirty = false;

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

    private void LayoutCrossAxis(float maxWidth, float maxHeight)
    {
        var horizontal = IsHorizontal;
        var reversed = IsReversed;
        var stretchCross = AlignItems == FlexAlign.Stretch;

        var innerSize = horizontal
            ? maxHeight - Padding.top - Padding.bottom
            : maxWidth - Padding.left - Padding.right;

        //Debug.Log($"({name}) cross setup: w={maxWidth} h={maxHeight} inner={innerSize}", this);

        foreach (var child in Children(reversed))
        {
            child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
            child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
            child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

            var childMinCross = horizontal ? childMinHeight : childMinWidth;
            var childMaxCross = horizontal ? childMaxHeight : childMaxWidth;
            var childPrefCross = horizontal ? childPreferredHeight : childPreferredWidth;
            var crossSize = stretchCross ? innerSize : childPrefCross;
            var clampedCrossSize = Mathf.Clamp(crossSize, childMinCross, childMaxCross);

            var layoutMaxWidth = horizontal ? float.PositiveInfinity : clampedCrossSize;
            var layoutMaxHeight = horizontal ? clampedCrossSize : float.PositiveInfinity;
            if (horizontal) child.LayoutVertical(layoutMaxWidth, layoutMaxHeight);
            else child.LayoutHorizontal(layoutMaxWidth, layoutMaxHeight);

            //Debug.Log($"({name}) cross: min={childMinCross} max={childMaxCross} pref={childPrefCross} clamped={clampedCrossSize}", child.Transform);

            var crossAxis = GetCrossAxis(horizontal, layoutMaxWidth, layoutMaxHeight);

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

        float GetCrossAxis(bool isHorizontal, float childWidth, float childHeight)
        {
            switch (AlignItems)
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

    /*private void PerformLayoutImpl(float width, float height)
    {
        //Debug.Log($"{gameObject.name}: Doing layout");

        _isDoingLayout = true;

        try
        {
            if (!IsAbsolute)
            {
                var rt = (RectTransform)transform;

#if UNITY_EDITOR
                _drivenTracker.Clear();
                _drivenTracker.Add(this, rt, DrivenTransformProperties.All);
#endif

                rt.sizeDelta = new Vector2(width, height);
            }

            var horizontal = IsHorizontal;
            var reversed = IsReversed;
            var stretchCross = AlignItems == FlexAlign.Stretch;

            var innerWidth = width - Padding.left - Padding.right;
            var innerHeight = height - Padding.top - Padding.bottom;

            var growSum = _growSum;
            var growthAllowance = horizontal
                ? Mathf.Max(0, innerWidth - _contentWidth)
                : Mathf.Max(0, innerHeight - _contentHeight);

            var actualContentWidth = _contentWidth;
            var actualContentHeight = _contentHeight;
            if (growSum > 0 && growthAllowance > 0)
            {
                if (horizontal) actualContentWidth = Mathf.Min(innerWidth, _contentMaxWidth);
                else actualContentHeight = Mathf.Min(innerHeight, _contentMaxHeight);
            }

            var mainAxisOffset = GetMainAxisStart(horizontal, reversed);
            foreach (var child in Children(reversed))
            {
                child.GetCalculatedMinSize(out var childMinWidth, out var childMinHeight);
                child.GetCalculatedMaxSize(out var childMaxWidth, out var childMaxHeight);
                child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

                var childWidth = stretchCross && !horizontal
                    ? innerWidth
                    : childPreferredWidth;
                var childHeight = stretchCross && horizontal
                    ? innerHeight
                    : childPreferredHeight;

                if (growthAllowance > 0 && child.Grow > 0)
                {
                    if (horizontal) TakeGrowth(ref childWidth, childMaxWidth);
                    else TakeGrowth(ref childHeight, childMaxHeight);

                    void TakeGrowth(ref float value, float maxValue)
                    {
                        var growPotential = ((float)child.Grow / growSum) * growthAllowance;
                        var growAmount = Mathf.Clamp(maxValue - value, 0, growPotential);
                        value += growAmount;
                        growthAllowance -= growAmount;
                        growSum -= child.Grow;
                    }
                }

                childWidth = Mathf.Clamp(childWidth, childMinWidth, childMaxWidth);
                childHeight = Mathf.Clamp(childHeight, childMinHeight, childMaxHeight);

                //if (child._isDirty) // TODO: can we do this to avoid doing extra work?
                {
                    child.PerformLayout(childWidth, childHeight);
                }

                var crossAxis = GetCrossAxis(horizontal, childWidth, childHeight);
                var childRt = child.Transform;
                childRt.anchoredPosition = horizontal
                    ? new Vector2(mainAxisOffset, crossAxis)
                    : new Vector2(crossAxis, mainAxisOffset);

                mainAxisOffset += horizontal
                    ? childWidth + Gap
                    : -childHeight - Gap;
            }

            _isDirty = false;

            float GetMainAxisStart(bool isHorizontal, bool isReversed)
            {
                switch (JustifyContent)
                {
                    case FlexJustify.Start:
                        return isHorizontal
                            ? (isReversed ? innerWidth - actualContentWidth + Padding.left : Padding.left)
                            : -(isReversed ? innerHeight - actualContentHeight + Padding.top : Padding.top);
                    case FlexJustify.End:
                        return isHorizontal
                            ? (isReversed ? Padding.left : innerWidth - actualContentWidth + Padding.left)
                            : -(isReversed ? Padding.top : innerHeight - actualContentHeight + Padding.top);
                    case FlexJustify.Center:
                        return isHorizontal
                            ? ((innerWidth - actualContentWidth) / 2) + Padding.left
                            : -((innerHeight - actualContentHeight) / 2) - Padding.top;
                    default:
                        throw new NotSupportedException(JustifyContent.ToString());
                }
            }

            float GetCrossAxis(bool isHorizontal, float childWidth, float childHeight)
            {
                switch (AlignItems)
                {
                    case FlexAlign.Start:
                    case FlexAlign.Stretch:
                        return isHorizontal
                            ? -Padding.top
                            : Padding.left;
                    case FlexAlign.End:
                        return isHorizontal
                            ? -innerHeight - Padding.top + childHeight
                            : innerWidth - Padding.right - childWidth;
                    case FlexAlign.Center:
                        return isHorizontal
                            ? -((innerHeight / 2) - (childHeight / 2) + Padding.top)
                            : (innerWidth / 2) - (childWidth / 2) + Padding.left;
                    default:
                        throw new NotSupportedException(AlignItems.ToString());
                }
            }
        }
        finally
        {
            _isDoingLayout = false;
        }
    }*/

    private FlexChildEnumerable Children(bool reversed = false)
    {
        return new FlexChildEnumerable(this, reversed);
    }

    private void SetupTransform()
    {
        if (!IsAbsolute)
        {
            var rt = (RectTransform)transform;
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

    void IFlexNode.Measure() =>
        MeasureImpl();

    void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
    {
        _isDoingLayout = true;

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

    void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
    {
        _isDoingLayout = true;

        try
        {
            if (IsHorizontal) LayoutCrossAxis(maxWidth, maxHeight);
            else LayoutMainAxis(maxWidth, maxHeight);
        }
        finally
        {
            _isDoingLayout = false;
        }
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

    protected override void OnEnable() => SetLayoutDirty();

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
