using System;
using UnityEngine;

namespace Facepunch.Flexbox
{
    [Serializable]
    public struct FlexLength
    {
        public bool HasValue;
        public float Value;
        public FlexUnit Unit;

        public float GetValueOrDefault(float defaultValue)
        {
            return HasValue && Unit == FlexUnit.Pixels ? Value : defaultValue;
        }
    }

    public enum FlexUnit
    {
        [InspectorName("px")]
        Pixels,

        [InspectorName("%")]
        Percent,
    }
}
