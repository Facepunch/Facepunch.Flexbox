using System;

[Serializable]
public struct FlexLength
{
    public bool HasValue;
    public float Value;

    internal float GetValueOrDefault(float defaultValue)
    {
        return HasValue ? Value : defaultValue;
    }
}
