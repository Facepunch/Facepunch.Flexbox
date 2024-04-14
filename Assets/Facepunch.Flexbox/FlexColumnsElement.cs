using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Facepunch.Flexbox
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FlexColumnsElement : FlexElementBase
    {
        [Tooltip("Spacing to add from this elements borders to where children are laid out.")]
        public FlexPadding Padding;

        [Min(0), Tooltip("Spacing to add between each child flex item.")]
        public float Gap = 0;

        [Tooltip("Enable this to use a fixed number of columns.")]
        public bool FixedColumnCount = false;

        [Min(1), Tooltip("The number of columns to use when using a fixed number of columns.")]
        public int ColumnCount = 1;

        [Min(1), Tooltip("The minimum width of each column when not using a fixed number of columns.")]
        [FormerlySerializedAs("ColumnWidth")]
        public int ColumnMinWidth = 100;

        private int _calculatedColumnCount;

        private ColumnParameters[] _columnParams = Array.Empty<ColumnParameters>();

        private struct ColumnParameters
        {
            public float Height; // for measuring
            public float Offset; // for layout
        }

        protected override void MeasureHorizontalImpl()
        {
            Profiler.BeginSample(nameof(MeasureHorizontalImpl), this);

            var mainAxisPreferredSize = 0f;
            if (FixedColumnCount && ColumnCount > 0)
            {
                var actualColumnCount = Mathf.Min(ColumnCount, Children.Count);
                Span<float> columnWidths = stackalloc float[actualColumnCount];

                var columnIdx = 0;
                foreach (var child in Children)
                {
                    if (child.IsDirty)
                    {
                        child.MeasureHorizontal();
                    }

                    child.GetScale(out var childScaleX, out _);
                    child.GetPreferredSize(out var childPreferredWidth, out _);

                    columnWidths[columnIdx] = Mathf.Max(columnWidths[columnIdx], childPreferredWidth * childScaleX);

                    columnIdx++;
                    if (columnIdx >= actualColumnCount)
                    {
                        columnIdx = 0;
                    }
                }

                var first = true;
                for (var i = 0; i < actualColumnCount; i++)
                {
                    var gap = first ? 0f : Gap;
                    mainAxisPreferredSize += columnWidths[i] + gap;

                    if (first)
                    {
                        first = false;
                    }
                }
            }
            else
            {
                var first = true;
                foreach (var child in Children)
                {
                    if (child.IsDirty)
                    {
                        child.MeasureHorizontal();
                    }

                    child.GetScale(out var childScaleX, out _);
                    child.GetPreferredSize(out var childPreferredWidth, out _);

                    var gap = first ? 0f : Gap;
                    mainAxisPreferredSize += (childPreferredWidth * childScaleX) + gap;

                    if (first)
                    {
                        first = false;
                    }
                }
            }

            var basisClamp = Basis.HasValue && Basis.Unit == FlexUnit.Pixels ? Basis.Value : 0;
            var minClamp = MinWidth.HasValue && MinWidth.Unit == FlexUnit.Pixels ? MinWidth.Value : 0;
            var maxClamp = MaxWidth.HasValue && MaxWidth.Unit == FlexUnit.Pixels ? MaxWidth.Value : float.PositiveInfinity;

            var padding = Padding.left + Padding.right;
            PrefWidth = Mathf.Clamp(mainAxisPreferredSize + padding, Mathf.Max(minClamp, basisClamp), maxClamp);

            Profiler.EndSample();
        }

        protected override void LayoutHorizontalImpl(float maxWidth, float maxHeight)
        {
            Profiler.BeginSample(nameof(LayoutHorizontalImpl), this);

            var innerWidth = maxWidth - Padding.left - Padding.right;

            _calculatedColumnCount = FixedColumnCount
                ? ColumnCount
                : Mathf.Max(Mathf.FloorToInt((innerWidth + Gap) / (ColumnMinWidth + Gap)), 1);

            var gapCount = Mathf.Max(_calculatedColumnCount - 1, 0);
            var columnWidth = (innerWidth - (Gap * gapCount)) / _calculatedColumnCount;

            var columnIdx = 0;
            foreach (var child in Children)
            {
                var childMinWidth = CalculateLengthValue(child.MinWidth, innerWidth, 0);
                var childMaxWidth = CalculateLengthValue(child.MaxWidth, innerWidth, float.PositiveInfinity);
                var childWidth = Mathf.Clamp(columnWidth, childMinWidth, childMaxWidth);

                child.LayoutHorizontal(childWidth, float.PositiveInfinity);

                var childRt = child.Transform;

                var childSizeDelta = childRt.sizeDelta;
                childRt.sizeDelta = new Vector2(childWidth, childSizeDelta.y);

                var childAnchoredPos = childRt.anchoredPosition;
                childRt.anchoredPosition = new Vector2(Padding.left + (columnWidth + Gap) * columnIdx, childAnchoredPos.y);

                columnIdx++;
                if (columnIdx >= _calculatedColumnCount) columnIdx = 0;
            }

            Profiler.EndSample();
        }

        protected override void MeasureVerticalImpl()
        {
            Profiler.BeginSample(nameof(MeasureVerticalImpl), this);

            EnsureColumnParamsSize();

            for (var i = 0; i < _calculatedColumnCount; i++)
            {
                _columnParams[i].Height = 0;
            }

            var columnIdx = 0;
            var first = true;
            foreach (var child in Children)
            {
                if (child.IsDirty)
                {
                    child.MeasureVertical();
                }

                child.GetScale(out _, out var childScaleY);
                child.GetPreferredSize(out _, out var childPreferredHeight);

                var gap = first ? 0f : Gap;
                _columnParams[columnIdx].Height += (childPreferredHeight * childScaleY) + gap;

                columnIdx++;
                if (columnIdx >= _calculatedColumnCount)
                {
                    columnIdx = 0;
                    first = false;
                }
            }

            var basisClamp = Basis.HasValue && Basis.Unit == FlexUnit.Pixels ? Basis.Value : 0;
            var minClamp = MinHeight.HasValue && MinHeight.Unit == FlexUnit.Pixels ? MinHeight.Value : 0;
            var maxClamp = MaxHeight.HasValue && MaxHeight.Unit == FlexUnit.Pixels ? MaxHeight.Value : float.PositiveInfinity;

            var maxHeight = 0f;
            for (var i = 0; i < _calculatedColumnCount; i++)
            {
                var height = _columnParams[i].Height;
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }

            var padding = Padding.top + Padding.bottom;
            PrefHeight = Mathf.Clamp(maxHeight + padding, Mathf.Max(minClamp, basisClamp), maxClamp);

            Profiler.EndSample();
        }

        protected override void LayoutVerticalImpl(float maxWidth, float maxHeight)
        {
            Profiler.BeginSample(nameof(LayoutVerticalImpl), this);

            var innerHeight = maxHeight - Padding.top - Padding.bottom;

            EnsureColumnParamsSize();

            for (var i = 0; i < _calculatedColumnCount; i++)
            {
                _columnParams[i].Offset = 0;
            }

            var columnIdx = 0;
            foreach (var child in Children)
            {
                ref var columnParams = ref _columnParams[columnIdx];

                var childMinHeight = CalculateLengthValue(child.MinHeight, innerHeight, 0);
                var childMaxHeight = CalculateLengthValue(child.MaxHeight, innerHeight, float.PositiveInfinity);
                child.GetPreferredSize(out _, out var childPrefHeight);
                var childHeight = Mathf.Clamp(childPrefHeight, childMinHeight, childMaxHeight);

                child.LayoutVertical(float.PositiveInfinity, childHeight);

                var childRt = child.Transform;

                var childSizeDelta = childRt.sizeDelta;
                childRt.sizeDelta = new Vector2(childSizeDelta.x, childHeight);

                var childAnchoredPos = childRt.anchoredPosition;
                childRt.anchoredPosition = new Vector2(childAnchoredPos.x, -(Padding.top + columnParams.Offset));

                columnParams.Offset += childHeight + Gap;

                columnIdx++;
                if (columnIdx >= _calculatedColumnCount) columnIdx = 0;
            }

            Profiler.EndSample();
        }

        private void EnsureColumnParamsSize()
        {
            if (_columnParams.Length < _calculatedColumnCount)
            {
                Array.Resize(ref _columnParams, _calculatedColumnCount);
            }
        }
    }
}
