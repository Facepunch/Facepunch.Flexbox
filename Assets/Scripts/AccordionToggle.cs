using System;
using UnityEngine;

public class AccordionToggle : MonoBehaviour
{
    public enum AccordionAxis { Vertical, Horizontal }

    public AccordionAxis Axis;
    public FlexElement Element;
    public CanvasGroup CanvasGroup;

    [Header("Animation")]
    [Min(0)]
    public float Duration = 0.25f;
    public LeanTweenType Ease = LeanTweenType.linear;

    private static readonly  Action<float, object> DirtyLayoutForObject = (value, obj) =>
    {
        var self = (AccordionToggle)obj;
        var element = self.Element;
        if (element != null)
        {
            element.SetLayoutDirty();
        }
    };

    public void Toggle()
    {
        SetExpanded(!IsExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        if (Element == null)
        {
            return;
        }

        var isExpanded = IsExpanded;
        if (expanded == isExpanded)
        {
            return;
        }
        
        var targetGo = Element.gameObject;
        var targetValue = isExpanded ? 0 : 1;

        LeanTween.cancel(targetGo);
        if (Axis == AccordionAxis.Vertical)
        {
            LeanTween.scaleY(targetGo, targetValue, Duration)
                .setEase(Ease)
                .setOnUpdate(DirtyLayoutForObject, this);
        }
        else
        {
            LeanTween.scaleX(targetGo, targetValue, Duration)
                .setEase(Ease)
                .setOnUpdate(DirtyLayoutForObject, this);
        }

        if (CanvasGroup != null)
        {
            var canvasGo = CanvasGroup.gameObject;
            if (!ReferenceEquals(canvasGo, targetGo))
            {
                LeanTween.cancel(canvasGo);
            }

            LeanTween.alphaCanvas(CanvasGroup, targetValue, Duration).setEase(Ease);
        }
    }

    public bool IsExpanded
    {
        get
        {
            if (Element == null)
            {
                return false;
            }
            
            var rectTransform = (RectTransform)Element.transform;
            var rtScale = rectTransform.localScale;
            return Axis == AccordionAxis.Vertical
                ? rtScale.y >= 1
                : rtScale.x >= 1;
        }
    }
}
