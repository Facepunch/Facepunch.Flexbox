using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class FlexText : TMPro.TextMeshProUGUI, IFlexNode
{
    [Min(0)]
    public int Grow = 1;
    public FlexLength MinWidth, MaxWidth;
    public FlexLength MinHeight, MaxHeight;

    private float _minWidth, _minHeight;
    private float _maxWidth, _maxHeight;
    private float _preferredWidth, _preferredHeight;

    public override void SetLayoutDirty()
    {
        base.SetLayoutDirty();

        var parent = transform.parent;
        if (parent != null && parent.TryGetComponent<IFlexNode>(out var parentNode) && parentNode.IsActive)
        {
            parentNode.SetLayoutDirty();
        }
    }

    #region IFlexNode
    RectTransform IFlexNode.Transform => (RectTransform)transform;
    bool IFlexNode.IsActive => isActiveAndEnabled;
    bool IFlexNode.IsAbsolute => false;
    int IFlexNode.Grow => Grow;
    FlexLength IFlexNode.MaxWidth => MaxWidth;
    FlexLength IFlexNode.MaxHeight => MinWidth;
    
    void IFlexNode.CalculateSizes(IFlexNode parent)
    {
        parent.GetCalculatedMaxSize(out var parentMaxWidth, out var parentMaxHeight);

        _minWidth = MinWidth.GetValueOrDefault(0);
        _maxWidth = MaxWidth.GetValueOrDefault(parentMaxWidth);

        _minHeight = MinHeight.GetValueOrDefault(0);
        _maxHeight = MaxHeight.GetValueOrDefault(parentMaxHeight);

        var preferredSize = GetPreferredValues(_maxWidth, _maxHeight);
        _preferredWidth = preferredSize.x;
        _preferredHeight = preferredSize.y;

        Debug.Log(preferredSize);
    }

    void IFlexNode.GetCalculatedMinSize(out float minWidth, out float minHeight)
    {
        minWidth = _minWidth;
        minHeight = _minHeight;
    }

    void IFlexNode.GetCalculatedMaxSize(out float maxWidth, out float maxHeight)
    {
        maxWidth = _maxWidth;
        maxHeight = _maxHeight;
    }

    void IFlexNode.GetPreferredSize(out float preferredWidth, out float preferredHeight)
    {
        preferredWidth = _preferredWidth;
        preferredHeight = _preferredHeight;
    }

    void IFlexNode.PerformLayout(float width, float height)
    {
        var rt = (RectTransform)transform;

#if UNITY_EDITOR
        //_drivenTracker.Clear();
        //_drivenTracker.Add(this, rt, DrivenTransformProperties.All);
#endif

        rt.sizeDelta = new Vector2(width, height);
    }
    #endregion
}
