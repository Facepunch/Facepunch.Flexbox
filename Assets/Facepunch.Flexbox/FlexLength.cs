using System;
using UnityEngine;

[Serializable]
public struct FlexLength
{
    public bool HasValue;
    public float Value;
    public FlexUnit Unit;
}

public enum FlexUnit
{
    [InspectorName("px")]
    Pixels,

    [InspectorName("%")]
    Percent,
}
