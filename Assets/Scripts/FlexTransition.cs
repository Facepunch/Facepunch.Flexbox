using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class FlexTransition : MonoBehaviour
{
    public enum TransitionProperty
    {
        PaddingLeft,
        PaddingRight,
        PaddingTop,
        PaddingBottom,
        Gap,
        MinWidth,
        MinHeight,
        MaxWidth,
        MaxHeight,
        
        ScaleX = 100,
        ScaleY,
        ImageColor,
        TextColor,
        CanvasAlpha,
    }

    [Serializable]
    public struct Definition
    {
        public TransitionProperty Property;
        public Object Object;

        public float FromFloat;
        public float ToFloat;

        public Color FromColor;
        public Color ToColor;

        [Min(0)]
        public float Duration;
        public LeanTweenType Ease;
    }
    
    public Definition[] Transitions;

    private readonly List<int> _pendingIds = new List<int>();
    private bool _currentState;

    public void SwitchState(bool enabled)
    {
        _currentState = enabled;

        if (Transitions == null || Transitions.Length == 0)
        {
            return;
        }

        foreach (var id in _pendingIds)
        {
            LeanTween.cancel(id);
        }
        _pendingIds.Clear();

        foreach (var transition in Transitions)
        {
            LTDescr tween = null;

            switch (transition.Property)
            {
                case TransitionProperty.ScaleX:
                    {
                        var element = transition.Object as FlexElement;
                        if (element == null)
                        {
                            break;
                        }
                        
                        var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                        tween = LeanTween.scaleX(element.gameObject, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float value, object obj) =>
                            {
                                var elem = (FlexElement)obj;
                                if (elem != null)
                                {
                                    elem.SetLayoutDirty();
                                }
                            }, element);
                    }
                    break;
                    
                case TransitionProperty.ScaleY:
                    {
                        var element = transition.Object as FlexElement;
                        if (element == null)
                        {
                            break;
                        }
                            
                        var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                        tween = LeanTween.scaleY(element.gameObject, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float value, object obj) =>
                            {
                                var elem = (FlexElement)obj;
                                if (elem != null)
                                {
                                    elem.SetLayoutDirty();
                                }
                            }, element);
                    }
                    break;

                case TransitionProperty.ImageColor:
                    {
                        var image = transition.Object as Image;
                        if (image == null)
                        {
                            break;
                        }

                        var startValue = image.color;
                        var targetValue = _currentState ? transition.ToColor : transition.FromColor;
                        tween = LeanTween.value(image.gameObject, 0, 1, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float value) =>
                            {
                                if (image != null)
                                {
                                    image.color = Color.Lerp(startValue, targetValue, value);
                                }
                            });
                    }
                    break;

                case TransitionProperty.TextColor:
                    {
                        var text = transition.Object as TMP_Text;
                        if (text == null)
                        {
                            break;
                        }

                        var startValue = text.color;
                        var targetValue = _currentState ? transition.ToColor : transition.FromColor;
                        tween = LeanTween.value(text.gameObject, 0, 1, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float value) =>
                            {
                                if (text != null)
                                {
                                    text.color = Color.Lerp(startValue, targetValue, value);
                                }
                            });
                    }
                    break;

                case TransitionProperty.CanvasAlpha:
                    {
                        var canvas = transition.Object as CanvasGroup;
                        if (canvas == null)
                        {
                            break;
                        }
                        
                        var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                        tween = LeanTween.alphaCanvas(canvas, targetValue, transition.Duration).setEase(transition.Ease);
                    }
                    break;

                default:
                    {
                        var element = transition.Object as FlexElement;
                        if (element == null)
                        {
                            break;
                        }
                        
                        var property = transition.Property;
                        var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                        tween = LeanTween.value(element.gameObject, Property(element, property), targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float newValue, object _) =>
                            {
                                // todo: remove GC using with pooling?
                                if (element != null)
                                {
                                    Property(element, property) = newValue;
                                    element.SetLayoutDirty();
                                }
                            }, this);
                    }
                    break;
            }

            if (tween != null)
            {
                _pendingIds.Add(tween.uniqueId);
            }
        }
    }

    public void ToggleState() => SwitchState(!_currentState);

    private static ref float Property(FlexElement element, TransitionProperty property)
    {
        switch (property)
        {
            case TransitionProperty.PaddingLeft:
                return ref element.Padding.left;
            case TransitionProperty.PaddingRight:
                return ref element.Padding.right;
            case TransitionProperty.PaddingTop:
                return ref element.Padding.top;
            case TransitionProperty.PaddingBottom:
                return ref element.Padding.bottom;
            case TransitionProperty.Gap:
                return ref element.Gap;
            case TransitionProperty.MinWidth:
                return ref element.MinWidth.Value;
            case TransitionProperty.MinHeight:
                return ref element.MinHeight.Value;
            case TransitionProperty.MaxWidth:
                return ref element.MaxWidth.Value;
            case TransitionProperty.MaxHeight:
                return ref element.MaxHeight.Value;
            default:
                throw new NotSupportedException($"{nameof(TransitionProperty)} {property}");
        }
    }
}
