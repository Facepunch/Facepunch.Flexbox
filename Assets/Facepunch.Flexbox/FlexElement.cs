using System;
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FlexElement : UIBehaviour
{
    public FlexDirection FlexDirection = FlexDirection.Row;
    public FlexJustify JustifyContent = FlexJustify.Start;
    public FlexAlign AlignItems = FlexAlign.Stretch;
    [Min(0)]
    public int Grow = 1;
    public RectOffset Padding;
    [Min(0)]
    public float Gap = 0;
    public bool IsAbsolute;
    public FlexLength MinWidth, MaxWidth;
    public FlexLength MinHeight, MaxHeight;
    public bool OverflowX, OverflowY;

    private bool _isDirty;
    private bool _isDoingLayout;
    private float _minWidth, _maxWidth;
    private float _minHeight, _maxHeight;
    private float _contentWidth, _contentHeight;
    private float _contentMaxWidth, _contentMaxHeight;
    private float _preferredWidth, _preferredHeight;
    private int _growSum;

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

    public void MarkDirty()
    {
        if (_isDoingLayout || !IsActive())
        {
            return;
        }

#if UNITY_EDITOR
        SetupTransform();
#endif

        var element = this;
        while (true)
        {
#if !UNITY_EDITOR
            if (element._isDirty)
            {
                return; 
            }
#endif

            element._isDirty = true;

            var parent = element.transform.parent;
            if (parent == null || !parent.TryGetComponent<FlexElement>(out var parentElem) || !parentElem.IsActive())
            {
                break;
            }

            element = parentElem;
        }

        FlexLayoutManager.EnqueueLayout(element);
    }

    internal void CalculateSizes()
    {
        var parentObj = transform.parent != null
            ? transform.parent.gameObject
            : gameObject;
        if (!parentObj.TryGetComponent<RectTransform>(out var parentRectTransform))
        {
            Debug.LogWarning("FlexElement has no parent or self RectTransform - cannot do layout!");
            _minWidth = _maxWidth = 0;
            _minHeight = _maxHeight = 0;
            _contentWidth = 0;
            _contentHeight = 0;
            _preferredWidth = 0;
            _preferredHeight = 0;
            return;
        }

        var parentRect = parentRectTransform.rect;
        CalculateSizesImpl(parentRect.width, parentRect.height);
    }

    private void CalculateSizes(FlexElement parent)
    {
        CalculateSizesImpl(parent._maxWidth, parent._maxHeight);
    }

    private void CalculateSizesImpl(float maxWidth, float maxHeight)
    {
        var oldMinWidth = _minWidth;
        var oldMaxWidth = _maxWidth;
        var oldMinHeight = _minHeight;
        var oldMaxHeight = _maxHeight;
        var oldContentWidth = _contentWidth;
        var oldContentHeight = _contentHeight;
        var oldContentMaxWidth = _contentMaxWidth;
        var oldContentMaxHeight = _contentMaxHeight;
        var oldPreferredWidth = _preferredWidth;
        var oldPreferredHeight = _preferredHeight;
        var oldGrowSum = _growSum;

        if (IsAbsolute)
        {
            var rect = ((RectTransform)transform).rect;
            _minWidth = _maxWidth = rect.width;
            _minHeight = _maxHeight = rect.height;
        }
        else
        {
            CalculateMinMaxSizes(maxWidth, maxHeight);
        }

        var horizontal = IsHorizontal;
        var mainAxisSize = 0f;
        var crossAxisSize = 0f;
        var mainAxisMaxSize = 0f;
        var crossAxisMaxSize = 0f;
        var growSum = 0;
        var first = true;
        foreach (var child in Children())
        {
            child.CalculateSizes(this);

            var gap = first ? 0f : Gap;

            if (horizontal)
            {
                mainAxisSize += child._preferredWidth + gap;
                crossAxisSize = Mathf.Max(crossAxisSize, child._preferredHeight);

                mainAxisMaxSize += child.MaxWidth.GetValueOrDefault(float.PositiveInfinity) + gap;
                crossAxisMaxSize = Mathf.Max(crossAxisMaxSize, child.MaxHeight.GetValueOrDefault(float.PositiveInfinity));
            }
            else
            {
                mainAxisSize += child._preferredHeight + gap;
                crossAxisSize = Mathf.Max(crossAxisSize, child._preferredWidth);

                mainAxisMaxSize += child.MaxHeight.GetValueOrDefault(float.PositiveInfinity) + gap;
                crossAxisMaxSize = Mathf.Max(crossAxisSize, child.MaxWidth.GetValueOrDefault(float.PositiveInfinity));
            }

            growSum += child.Grow;

            if (first)
            {
                first = false;
            }
        }

        _contentWidth = Padding.left + (horizontal ? mainAxisSize : crossAxisSize) + Padding.right;
        _contentHeight = Padding.top + (horizontal ? crossAxisSize : mainAxisSize) + Padding.bottom;

        _contentMaxWidth = Padding.left + (horizontal ? mainAxisMaxSize : crossAxisMaxSize) + Padding.right;
        _contentMaxHeight = Padding.top + (horizontal ? crossAxisMaxSize : mainAxisMaxSize) + Padding.bottom;

        if (IsAbsolute)
        {
            _preferredWidth = _minWidth;
            _preferredHeight = _minHeight;
        }
        else
        {
            _preferredWidth = Mathf.Clamp(_contentWidth, _minWidth, _maxWidth);
            _preferredHeight = Mathf.Clamp(_contentHeight, _minHeight, _maxHeight);
        }

        _growSum = growSum;

        //Debug.Log($"{gameObject.name}: Calculated sizes minWidth={_minWidth} maxWidth={_maxWidth} minHeight={_minHeight} maxHeight={_maxHeight} prefWidth={_preferredWidth} prefHeight={_preferredHeight}");
        //Debug.Log($"{gameObject.name}: Calculated sizes contentWidth={_contentWidth} contentHeight={_contentHeight} contentMaxWidth={_contentMaxWidth} contentMaxHeight={_contentMaxHeight}");

        if (!_isDirty && (_minWidth != oldMinWidth || _maxWidth != oldMaxWidth ||
            _minHeight != oldMinHeight || _maxHeight != oldMaxHeight ||
            _contentWidth != oldContentWidth || _contentHeight != oldContentHeight ||
            _contentMaxWidth != oldContentMaxWidth || _contentMaxHeight != oldContentMaxHeight ||
            _preferredWidth != oldPreferredWidth || _preferredHeight != oldPreferredHeight ||
            _growSum != oldGrowSum))
        {
            //Debug.Log($"{gameObject.name}: Size parameters changed, marking dirty");
            _isDirty = true;
        }
    }

    private void CalculateMinMaxSizes(float maxWidth, float maxHeight)
    {
        _minWidth = MinWidth.HasValue
            ? MinWidth.Value
            : 0;

        _maxWidth = MaxWidth.HasValue
            ? MaxWidth.Value
            : maxWidth;

        _minHeight = MinHeight.HasValue
            ? MinHeight.Value
            : 0;

        _maxHeight = MaxHeight.HasValue
            ? MaxHeight.Value
            : maxHeight;

        if (OverflowX) _maxWidth = float.PositiveInfinity;
        if (OverflowY) _maxHeight = float.PositiveInfinity;
    }

    internal void PerformLayout()
    {
        PerformLayoutImpl(_preferredWidth, _preferredHeight);
    }

    private void PerformLayoutImpl(float width, float height)
    {
        //Debug.Log($"{gameObject.name}: Doing layout");

        _isDoingLayout = true;

        try
        {
            if (!IsAbsolute)
            {
                var rt = (RectTransform)transform;
                rt.sizeDelta = new Vector2(width, height);
            }

            var horizontal = IsHorizontal;
            var reversed = IsReversed;
            var stretchCross = AlignItems == FlexAlign.Stretch;

            var growthAllowance = horizontal
                ? Mathf.Max(0, width - _contentWidth)
                : Mathf.Max(0, height - _contentHeight);

            var actualContentWidth = _contentWidth;
            var actualContentHeight = _contentHeight;
            if (_growSum > 0 && growthAllowance > 0)
            {
                if (horizontal) actualContentWidth = Mathf.Min(width, _contentMaxWidth);
                else actualContentHeight = Mathf.Min(height, _contentMaxHeight);
            }

            var innerWidth = width - Padding.left - Padding.right;
            var innerHeight = height - Padding.top - Padding.bottom;

            var mainAxisOffset = GetMainAxisStart(horizontal, reversed);
            foreach (var child in Children(reversed))
            {
                var childWidth = stretchCross && !horizontal
                    ? innerWidth
                    : child._preferredWidth;
                var childHeight = stretchCross && horizontal
                    ? innerHeight
                    : child._preferredHeight;

                if (growthAllowance > 0 && child.Grow > 0)
                {
                    var growAmount = ((float)child.Grow / _growSum) * growthAllowance;
                    if (horizontal) childWidth += growAmount; // todo: do we need to clamp and reduce growth allowance accordingly?
                    else childHeight += growAmount;
                }

                childWidth = Mathf.Clamp(childWidth, child._minWidth, child._maxWidth);
                childHeight = Mathf.Clamp(childHeight, child._minHeight, child._maxHeight);

                //if (child._isDirty) // TODO: can we do this to avoid doing extra work?
                {
                    child.PerformLayoutImpl(childWidth, childHeight);
                }

                var crossAxis = GetCrossAxis(horizontal, childWidth, childHeight);
                var childRt = (RectTransform)child.transform;
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
                            ? (isReversed ? width - actualContentWidth + Padding.left : Padding.left)
                            : -(isReversed ? height - actualContentHeight + Padding.top : Padding.top);
                    case FlexJustify.End:
                        return isHorizontal
                            ? (isReversed ? Padding.left : width - actualContentWidth + Padding.left)
                            : -(isReversed ? Padding.top : height - actualContentHeight + Padding.top);
                    case FlexJustify.Center:
                        return isHorizontal
                            ? ((width - actualContentWidth) / 2) + Padding.left
                            : -((height - actualContentHeight) / 2) - Padding.top;
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
                            ? -height + Padding.bottom + childHeight
                            : width - Padding.right - childWidth;
                    case FlexAlign.Center:
                        return isHorizontal
                            ? -((height / 2) - (childHeight / 2))
                            : (width / 2) - (childWidth / 2);
                    default:
                        throw new NotSupportedException(AlignItems.ToString());
                }
            }
        }
        finally
        {
            _isDoingLayout = false;
        }
    }

    private FlexElementEnumerable Children(bool reversed = false)
    {
        return new FlexElementEnumerable(this, reversed);
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

    protected override void OnEnable() => MarkDirty();

    protected override void OnDisable() => MarkDirty();

    protected override void OnRectTransformDimensionsChange() => MarkDirty();

    protected override void OnTransformParentChanged() => MarkDirty();

    protected virtual void OnTransformChildrenChanged() => MarkDirty();

#if UNITY_EDITOR
    protected override void OnValidate() => MarkDirty();
#endif
}
