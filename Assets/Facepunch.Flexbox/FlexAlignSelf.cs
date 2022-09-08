using System;

[Serializable]
public struct FlexAlignSelf
{
    public bool HasValue;
    public FlexAlign Value;

    internal FlexAlign GetValueOrDefault(FlexAlign defaultValue)
    {
        return HasValue ? Value : defaultValue;
    }
}
