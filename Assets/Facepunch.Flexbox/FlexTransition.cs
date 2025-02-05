using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Facepunch.Flexbox
{
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
            RotationZ,

            TransformTranslateX = 200,
            TransformTranslateY,
            TransformScaleX,
            TransformScaleY,
            TransformRotate,
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
        private bool _hasSwitchedState;

        public void Awake()
        {
            if (!_hasSwitchedState)
            {
                SwitchState(false, false);
            }
        }

        public void SwitchState(bool enabled, bool animate)
        {
            _currentState = enabled;
            _hasSwitchedState = true;

            if (Transitions == null || Transitions.Length == 0)
            {
                return;
            }

            foreach (var id in _pendingIds)
            {
                LeanTween.cancel(id);
            }

            _pendingIds.Clear();

            for (var i = 0; i < Transitions.Length; i++)
            {
                var tween = RunTransitionImpl(in Transitions[i], animate);
                if (tween != null)
                {
                    _pendingIds.Add(tween.uniqueId);
                }
            }
        }

        public void SwitchState(bool enabled) => SwitchState(enabled, true);

        public void ToggleState() => SwitchState(!_currentState);

        private LTDescr RunTransitionImpl(in Definition transition, bool animate)
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
                    if (animate)
                    {
                        tween = LeanTween.scaleX(element.gameObject, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdate((float value, object obj) =>
                            {
                                if (obj is FlexElement elem)
                                {
                                    elem.SetLayoutDirty();
                                }
                            }, element);
                    }
                    else
                    {
                        var scale = element.transform.localScale;
                        scale.x = targetValue;
                        element.transform.localScale = scale;
                        element.SetLayoutDirty();
                    }

                    break;
                }

                case TransitionProperty.ScaleY:
                {
                    var element = transition.Object as FlexElement;
                    if (element == null)
                    {
                        break;
                    }

                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
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
                    else
                    {
                        var scale = element.transform.localScale;
                        scale.y = targetValue;
                        element.transform.localScale = scale;
                        element.SetLayoutDirty();
                    }

                    break;
                }

                case TransitionProperty.ImageColor:
                {
                    var image = transition.Object as Image;
                    if (image == null)
                    {
                        break;
                    }

                    var startValue = image.color;
                    var targetValue = _currentState ? transition.ToColor : transition.FromColor;
                    if (animate)
                    {
                        tween = LeanTween.value(image.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(image)
                            .setOnUpdateColor((Color value, object obj) =>
                            {
                                if (obj is Image img)
                                {
                                    img.color = value;
                                }
                            });
                    }
                    else
                    {
                        image.color = targetValue;
                    }
                
                    break;
                }

                case TransitionProperty.TextColor:
                {
                    var text = transition.Object as TMP_Text;
                    if (text == null)
                    {
                        break;
                    }

                    var startValue = text.color;
                    var targetValue = _currentState ? transition.ToColor : transition.FromColor;
                    if (animate)
                    {
                        tween = LeanTween.value(text.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(text)
                            .setOnUpdateColor((Color value, object state) =>
                            {
                                if (state is TMP_Text txt)
                                {
                                    txt.color = value;
                                }
                            });
                    }
                    else
                    {
                        text.color = targetValue;
                    }
                
                    break;
                }

                case TransitionProperty.CanvasAlpha:
                {
                    var canvas = transition.Object as CanvasGroup;
                    if (canvas == null)
                    {
                        break;
                    }

                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.alphaCanvas(canvas, targetValue, transition.Duration).setEase(transition.Ease);
                    }
                    else
                    {
                        canvas.alpha = targetValue;
                    }

                    break;
                }

                case TransitionProperty.RotationZ:
                {
                    var transform = transition.Object as Transform;
                    if (transform == null)
                    {
                        break;
                    }

                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.rotateZ(transform.gameObject, targetValue, transition.Duration)
                            .setEase(transition.Ease);
                    }
                    else
                    {
                        var angles = transform.localEulerAngles;
                        angles.z = targetValue;
                        transform.localEulerAngles = angles;
                    }

                    break;
                }

                case TransitionProperty.TransformTranslateX:
                {
                    var graphicTransform = transition.Object as FlexGraphicTransform;
                    if (graphicTransform == null)
                    {
                        break;
                    }

                    var startValue = graphicTransform.TranslateX;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.value(graphicTransform.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(graphicTransform)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is FlexGraphicTransform gt)
                                {
                                    gt.TranslateX = value;
                                    gt.SetVerticesDirty();
                                }
                            });
                    }
                    else
                    {
                        graphicTransform.TranslateX = targetValue;
                        graphicTransform.SetVerticesDirty();
                    }

                    break;
                }

                case TransitionProperty.TransformTranslateY:
                {
                    var graphicTransform = transition.Object as FlexGraphicTransform;
                    if (graphicTransform == null)
                    {
                        break;
                    }

                    var startValue = graphicTransform.TranslateY;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.value(graphicTransform.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(graphicTransform)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is FlexGraphicTransform gt)
                                {
                                    gt.TranslateY = value;
                                    gt.SetVerticesDirty();
                                }
                            });
                    }
                    else
                    {
                        graphicTransform.TranslateY = targetValue;
                        graphicTransform.SetVerticesDirty();
                    }

                    break;
                }

                case TransitionProperty.TransformScaleX:
                {
                    var graphicTransform = transition.Object as FlexGraphicTransform;
                    if (graphicTransform == null)
                    {
                        break;
                    }

                    var startValue = graphicTransform.ScaleX;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.value(graphicTransform.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(graphicTransform)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is FlexGraphicTransform gt)
                                {
                                    gt.ScaleX = value;
                                    gt.SetVerticesDirty();
                                }
                            });
                    }
                    else
                    {
                        graphicTransform.ScaleX = targetValue;
                        graphicTransform.SetVerticesDirty();
                    }

                    break;
                }

                case TransitionProperty.TransformScaleY:
                {
                    var graphicTransform = transition.Object as FlexGraphicTransform;
                    if (graphicTransform == null)
                    {
                        break;
                    }

                    var startValue = graphicTransform.ScaleY;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.value(graphicTransform.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(graphicTransform)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is FlexGraphicTransform gt)
                                {
                                    gt.ScaleY = value;
                                    gt.SetVerticesDirty();
                                }
                            });
                    }
                    else
                    {
                        graphicTransform.ScaleY = targetValue;
                        graphicTransform.SetVerticesDirty();
                    }

                    break;
                }

                case TransitionProperty.TransformRotate:
                {
                    var graphicTransform = transition.Object as FlexGraphicTransform;
                    if (graphicTransform == null)
                    {
                        break;
                    }

                    var startValue = graphicTransform.Rotate;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.value(graphicTransform.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(graphicTransform)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is FlexGraphicTransform gt)
                                {
                                    gt.Rotate = value;
                                    gt.SetVerticesDirty();
                                }
                            });
                    }
                    else
                    {
                        graphicTransform.Rotate = targetValue;
                        graphicTransform.SetVerticesDirty();
                    }

                    break;
                }

                default:
                {
                    var element = transition.Object as FlexElement;
                    if (element == null)
                    {
                        break;
                    }

                    var property = transition.Property;
                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
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
                    else
                    {
                        Property(element, property) = targetValue;
                        element.SetLayoutDirty();
                    }

                    break;
                }
            }

            return tween;
        }

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

#if UNITY_EDITOR
        public static float GetCurrentValueFloat(Object obj, TransitionProperty property)
        {
            switch (property)
            {
                case TransitionProperty.ScaleX:
                {
                    var element = obj as FlexElement;
                    return element != null ? element.transform.localScale.x : 0;
                }

                case TransitionProperty.ScaleY:
                {
                    var element = obj as FlexElement;
                    return element != null ? element.transform.localScale.y : 0;
                }

                case TransitionProperty.CanvasAlpha:
                {
                    var canvas = obj as CanvasGroup;
                    return canvas != null ? canvas.alpha : 0;
                }

                case TransitionProperty.RotationZ:
                {
                    var transform = obj as Transform;
                    return transform != null ? transform.localEulerAngles.z : 0;
                }

                case TransitionProperty.ImageColor:
                case TransitionProperty.TextColor:
                    return 0f;

                default:
                {
                    var element = obj as FlexElement;
                    return element != null ? Property(element, property) : 0;
                }
            }
        }

        public static Color GetCurrentValueColor(Object obj, TransitionProperty property)
        {
            switch (property)
            {
                case TransitionProperty.ImageColor:
                {
                    var image = obj as Image;
                    return image != null ? image.color : Color.black;
                }

                case TransitionProperty.TextColor:
                {
                    var text = obj as TMP_Text;
                    return text != null ? text.color : Color.black;
                }

                default:
                    return Color.black;
            }
        }
#endif
    }
}
