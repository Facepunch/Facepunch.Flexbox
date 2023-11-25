using System;

namespace Facepunch.Flexbox
{
    [Serializable]
    public struct FlexValue
    {
        public bool HasValue;
        public float Value;

        public float GetOrDefault(float defaultValue)
        {
            return HasValue ? Value : defaultValue;
        }
    }
}
