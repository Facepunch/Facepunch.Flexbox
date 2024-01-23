using System;
using System.Collections.Generic;
using Facepunch.Flexbox.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

namespace Facepunch.Flexbox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FlexElement : FlexElementBase
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
        
        private ChildSizingParameters[] _childSizes = Array.Empty<ChildSizingParameters>();
        
        private struct ChildSizingParameters
        {
            public float Size;
            public float MinSize;
            public float MaxSize;
            public bool IsFlexible;
            public float Scale;
        }

        private bool IsHorizontal => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse;
        protected override bool IsReversed => FlexDirection == FlexDirection.RowReverse || FlexDirection == FlexDirection.ColumnReverse;

        protected override void MeasureHorizontalImpl()
        {
            if (IsHorizontal) MeasureMainAxis();
            else MeasureCrossAxis();
        }

        protected override void LayoutHorizontalImpl(float maxWidth, float maxHeight)
        {
            if (IsHorizontal) LayoutMainAxis(maxWidth, maxHeight);
            else LayoutCrossAxis(maxWidth, maxHeight);
        }

        protected override void MeasureVerticalImpl()
        {
            if (IsHorizontal) MeasureCrossAxis();
            else MeasureMainAxis();
        }

        protected override void LayoutVerticalImpl(float maxWidth, float maxHeight)
        {
            if (IsHorizontal) LayoutCrossAxis(maxWidth, maxHeight);
            else LayoutMainAxis(maxWidth, maxHeight);
        }

        private void MeasureMainAxis()
        {
            Profiler.BeginSample(nameof(MeasureMainAxis), this);

            var horizontal = IsHorizontal;
            ref var prefSize = ref Pick(horizontal, ref PrefWidth, ref PrefHeight);
            var padding = horizontal
                ? Padding.left + Padding.right
                : Padding.top + Padding.bottom;

            var mainAxisPreferredSize = 0f;
            var first = true;
            foreach (var child in Children)
            {
                if (child.IsDirty)
                {
                    if (horizontal) child.MeasureHorizontal();
                    else child.MeasureVertical();
                }

                child.GetScale(out var childScaleX, out var childScaleY);
                child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);
                
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
            var basisClamp = Basis.HasValue && Basis.Unit == FlexUnit.Pixels ? Basis.Value : 0;
            var minClamp = minSize.HasValue && minSize.Unit == FlexUnit.Pixels ? minSize.Value : 0;
            var maxClamp = maxSize.HasValue && maxSize.Unit == FlexUnit.Pixels ? maxSize.Value : float.PositiveInfinity;

            prefSize = Mathf.Clamp(mainAxisPreferredSize + padding, Mathf.Max(minClamp, basisClamp), maxClamp);

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
            var gapCount = Mathf.Max(Children.Count - 1, 0);
            var innerSizeMinusGap = innerSize - Gap * gapCount;

            SizingChildren.Clear();
            if (_childSizes.Length < Children.Count) Array.Resize(ref _childSizes, Children.Count);

            var lineGrowSum = 0;
            var lineShrinkSum = 0;
            var prefMainContentSize = 0f;
            var first = true;
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                ref var childParams = ref _childSizes[i];

                var childMinMain = CalculateLengthValue(horizontal ? child.MinWidth : child.MinHeight, innerSizeMinusGap, 0);
                var childMaxMain = CalculateLengthValue(horizontal ? child.MaxWidth : child.MaxHeight, innerSizeMinusGap, float.PositiveInfinity);
                var childFlexible = childMinMain < childMaxMain;

                child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);
                var childPrefMain = horizontal ? childPreferredWidth : childPreferredHeight;

                child.GetScale(out var childScaleX, out var childScaleY);
                var childScaleMain = horizontal ? childScaleX : childScaleY;

                lineGrowSum += child.Grow;
                lineShrinkSum += child.Shrink;

                var initialSize = CalculateLengthValue(child.Basis, innerSizeMinusGap, childPrefMain);
                var startingMainSize = Mathf.Clamp(initialSize, childMinMain, childMaxMain);

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

            //Debug.Log($"({name}) main setup: w={maxWidth} h={maxHeight} inner={innerSize} pref={(horizontal ? PrefWidth : PrefHeight)} grow={growthAllowance} shrink={shrinkAllowance}", this);

            while (SizingChildren.Exists(n => n != null))
            {
                var growSum = lineGrowSum;
                var shrinkSum = lineShrinkSum;

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
                            value += scale > 0 ? growAmount / scale : 0;
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
                            value -= scale > 0 ? shrinkAmount / scale : 0;
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

            var actualMainSize = Gap * gapCount;
            for (var i = 0; i < Children.Count; i++)
            {
                actualMainSize += _childSizes[i].Size * _childSizes[i].Scale;
            }
            actualMainSize = Mathf.Min(actualMainSize, innerSize);

            var extraGap = 0f;
            var extraOffset = 0f;
            if (JustifyContent == FlexJustify.SpaceBetween && gapCount > 0)
            {
                extraGap = (innerSize - actualMainSize) / gapCount; // no spacing at flex start/end
                actualMainSize = innerSize;
            }
            else if (JustifyContent == FlexJustify.SpaceAround)
            {
                extraGap = (innerSize - actualMainSize) / (gapCount + 1);
                extraOffset = extraGap / 2; // half size spacing at flex start/end
                actualMainSize = innerSize;
            }
            else if (JustifyContent == FlexJustify.SpaceEvenly)
            {
                extraGap = (innerSize - actualMainSize) / (gapCount + 2);
                extraOffset = extraGap; // full size spacing at flex start/end
                actualMainSize = innerSize;
            }
            
            var mainAxisSpacing = Gap + extraGap;
            var mainAxisOffset = GetMainAxisStart(horizontal, reversed);
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                ref var childParams = ref _childSizes[i];

                if (horizontal) child.LayoutHorizontal(childParams.Size, float.PositiveInfinity);
                else child.LayoutVertical(float.PositiveInfinity, childParams.Size);

                //Debug.Log($"({name}) main: min={childParams.MinSize} max={childParams.MaxSize} size={childParams.Size}", child.Transform);

                var childRt = child.Transform;

                var childSizeDelta = childRt.sizeDelta;
                childRt.sizeDelta = horizontal
                    ? new Vector2(childParams.Size, childSizeDelta.y)
                    : new Vector2(childSizeDelta.x, childParams.Size);

                var childAnchoredPos = childRt.anchoredPosition;
                childRt.anchoredPosition = horizontal
                    ? new Vector2(mainAxisOffset, childAnchoredPos.y)
                    : new Vector2(childAnchoredPos.x, mainAxisOffset);
                
                var scaledMainSize = childParams.Size * childParams.Scale;
                mainAxisOffset += horizontal
                    ? scaledMainSize + mainAxisSpacing
                    : -scaledMainSize - mainAxisSpacing;
            }

            Profiler.EndSample();

            float GetMainAxisStart(bool isHorizontal, bool isReversed)
            {
                switch (JustifyContent)
                {
                    case FlexJustify.Start:
                    case FlexJustify.SpaceBetween:
                    case FlexJustify.SpaceAround:
                    case FlexJustify.SpaceEvenly:
                        return isHorizontal
                            ? (isReversed ? innerSize - actualMainSize + Padding.left + extraOffset : Padding.left + extraOffset)
                            : -(isReversed ? innerSize - actualMainSize + Padding.top + extraOffset : Padding.top + extraOffset);
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
            ref var prefSize = ref Pick(horizontal, ref PrefHeight, ref PrefWidth);
            var padding = horizontal
                ? Padding.top + Padding.bottom
                : Padding.left + Padding.right;

            var crossAxisPreferredSize = 0f;
            foreach (var child in Children)
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

            if (IsAbsolute && !AutoSizeY && horizontal)
            {
                var rect = ((RectTransform)transform).rect;
                prefSize = rect.height;
            }
            else if (IsAbsolute && !AutoSizeX && !horizontal)
            {
                var rect = ((RectTransform)transform).rect;
                prefSize = rect.width;
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

            foreach (var child in Children)
            {
                child.GetScale(out var childScaleX, out var childScaleY);
                child.GetPreferredSize(out var childPreferredWidth, out var childPreferredHeight);

                var childScaleCross = horizontal ? childScaleY : childScaleX;
                var scaledInnerSize = childScaleCross > 0 ? innerSize / childScaleCross : 0;

                var childAlign = child.AlignSelf.GetValueOrDefault(AlignItems);
                var childMinCross = CalculateLengthValue(horizontal ? child.MinHeight : child.MinWidth, scaledInnerSize, 0);
                var childMaxCross = CalculateLengthValue(horizontal ? child.MaxHeight : child.MaxWidth, scaledInnerSize, float.PositiveInfinity);
                var childPrefCross = horizontal ? childPreferredHeight : childPreferredWidth;
                var crossSize = childAlign == FlexAlign.Stretch ? scaledInnerSize : childPrefCross;
                var clampedCrossSize = Mathf.Clamp(crossSize, childMinCross, childMaxCross);

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
    }
}
