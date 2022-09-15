﻿using UnityEngine;

namespace Facepunch.Flexbox
{
    public interface IFlexNode
    {
        RectTransform Transform { get; }
        bool IsActive { get; }
        bool IsAbsolute { get; }
        bool IsDirty { get; }

        FlexLength MinWidth { get; }
        FlexLength MaxWidth { get; }
        FlexLength MinHeight { get; }
        FlexLength MaxHeight { get; }
        int Grow { get; }
        int Shrink { get; }
        FlexAlignSelf AlignSelf { get; }

        void SetLayoutDirty(bool force = false);

        void MeasureHorizontal();
        void LayoutHorizontal(float maxWidth, float maxHeight);
        void MeasureVertical();
        void LayoutVertical(float maxWidth, float maxHeight);

        void GetScale(out float scaleX, out float scaleY);
        void GetPreferredSize(out float preferredWidth, out float preferredHeight);
    }
}
