using UnityEngine;

public interface IFlexNode
{
    RectTransform Transform { get; }
    bool IsActive { get; }
    bool IsAbsolute { get; }
    bool IsDirty { get; }

    int Grow { get; }
    int Shrink { get; }

    void SetLayoutDirty(bool force = false);

    void MeasureHorizontal();
    void LayoutHorizontal(float maxWidth, float maxHeight);
    void MeasureVertical();
    void LayoutVertical(float maxWidth, float maxHeight);

    void GetCalculatedMinSize(out float minWidth, out float minHeight);
    void GetCalculatedMaxSize(out float maxWidth, out float maxHeight);
    void GetPreferredSize(out float preferredWidth, out float preferredHeight);
}
