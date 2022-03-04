using UnityEngine;

public interface IFlexNode
{
    RectTransform Transform { get; }
    bool IsActive { get; }
    bool IsAbsolute { get; }

    int Grow { get; }
    FlexLength MaxWidth { get; }
    FlexLength MaxHeight { get; }

    void SetLayoutDirty();

    void CalculateSizes(IFlexNode parent);

    void GetCalculatedMinSize(out float minWidth, out float minHeight);
    void GetCalculatedMaxSize(out float maxWidth, out float maxHeight);
    void GetPreferredSize(out float preferredWidth, out float preferredHeight);

    void PerformLayout(float width, float height);
}
