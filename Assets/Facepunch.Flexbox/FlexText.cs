using UnityEngine;
using UnityEngine.Profiling;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class FlexText : TMPro.TextMeshProUGUI, IFlexNode
{
    [Min(0), Tooltip("How much this flex element should grow relative to its siblings.")]
    public int Grow = 1;

    [Min(0), Tooltip("How much this flex element should shrink relative to its siblings.")]
    public int Shrink = 1;

    [Tooltip("The minimum allowed dimensions of this flex element.")]
    public FlexLength MinWidth, MaxWidth;

    [Tooltip("The maximum allowed dimensions of this flex element.")]
    public FlexLength MinHeight, MaxHeight;

    private bool _isDirty;
    private bool _isDoingLayout;
    private float _minWidth, _minHeight;
    private float _maxWidth, _maxHeight;
    private float _preferredWidth, _preferredHeight;

#if UNITY_EDITOR
    private DrivenRectTransformTracker _drivenTracker = new DrivenRectTransformTracker();
#endif

    protected override void Awake()
    {
        base.Awake();

        SetupTransform();
    }

    protected override void OnDisable()
    {
#if UNITY_EDITOR
        _drivenTracker.Clear();
#endif

        base.OnDisable();
    }

    public override void SetLayoutDirty()
    {
#if !UNITY_EDITOR
        if (_isDirty)
        {
            return;
        }
#endif

#if UNITY_EDITOR
        SetupTransform();
#endif

        _isDirty = true;

        base.SetLayoutDirty();

        var parent = transform.parent;
        if (parent != null && parent.TryGetComponent<IFlexNode>(out var parentNode) && parentNode.IsActive)
        {
            parentNode.SetLayoutDirty();
        }
    }

    private void SetupTransform()
    {
        var rt = (RectTransform)transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.pivot = new Vector2(0, 1); // top left
        rt.anchorMin = new Vector2(0, 1); // top left
        rt.anchorMax = new Vector2(0, 1); // top left
    }

    #region IFlexNode
    RectTransform IFlexNode.Transform => (RectTransform)transform;
    bool IFlexNode.IsActive => isActiveAndEnabled;
    bool IFlexNode.IsAbsolute => false;
    bool IFlexNode.IsDirty => _isDirty;
    int IFlexNode.Grow => Grow;
    int IFlexNode.Shrink => Shrink;

    void IFlexNode.SetLayoutDirty(bool force)
    {
        if (!force && (_isDoingLayout || !IsActive()))
        {
            return;
        }

        SetLayoutDirty();
    }
    
    void IFlexNode.MeasureHorizontal()
    {
        Profiler.BeginSample(nameof(IFlexNode.MeasureHorizontal), this);

        _minWidth = MinWidth.GetValueOrDefault(0);
        _maxWidth = MaxWidth.GetValueOrDefault(float.PositiveInfinity);

        _minHeight = MinHeight.GetValueOrDefault(0);
        _maxHeight = MaxHeight.GetValueOrDefault(float.PositiveInfinity);

        var preferredSize = GetPreferredValues(_maxWidth, _maxHeight);
        _preferredWidth = Mathf.Clamp(preferredSize.x, _minWidth, _maxWidth);
        _preferredHeight = Mathf.Clamp(preferredSize.y, _minHeight, _maxHeight);

        Profiler.EndSample();
    }

    void IFlexNode.LayoutHorizontal(float maxWidth, float maxHeight)
    {
    }

    void IFlexNode.MeasureVertical()
    {
        Profiler.BeginSample(nameof(IFlexNode.MeasureVertical), this);

        var rt = (RectTransform)transform;
        var size = rt.sizeDelta;
        //Debug.Log($"text vertical w={size.x}");

        var preferredSize = GetPreferredValues(Mathf.Min(size.x, _maxWidth), float.PositiveInfinity);
        _preferredWidth = Mathf.Clamp(preferredSize.x, _minWidth, _maxWidth);
        _preferredHeight = Mathf.Clamp(preferredSize.y, _minHeight, _maxHeight);

        Profiler.EndSample();
    }

    void IFlexNode.LayoutVertical(float maxWidth, float maxHeight)
    {
        _isDirty = false;
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
    #endregion
}
