﻿using System;

namespace Facepunch.Flexbox
{
    [Serializable]
    public struct FlexPadding
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public FlexPadding(float value)
        {
            left = right = top = bottom = value;
        }

        public FlexPadding(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }
    }
}
