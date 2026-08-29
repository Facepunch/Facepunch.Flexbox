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
            ScaleXY,

            TransformTranslateX = 200,
            TransformTranslateY,
            TransformScaleX,
            TransformScaleY,
            TransformRotate,
            TranslateX,
            TranslateY,
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
            public AnimationCurve Curve;
        }

        public Definition[] Transitions;
        [SerializeField] private bool _playOnAwake = false;

        private readonly List<int> _pendingIds = new List<int>();
        private bool _currentState;
        private bool _hasSwitchedState;
        private Action _restoreStateCached;

        public void Awake()
        {
            if (!_hasSwitchedState)
            {
                SwitchState(false, false);
            }
        }

        public void OnEnable()
        {
            if (_playOnAwake)
            {
                SwitchState(true, true);
            }
        }

        public void OnDisable()
        {
            if (_playOnAwake)
            {
                SwitchState(false, false);
            }
        }

        [FlexEvent]
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

        [FlexEvent]
        public void SwitchState(bool enabled) => SwitchState(enabled, true);

        [FlexEvent]
        public void ToggleState() => SwitchState(!_currentState);
        
        [FlexEvent]
        public void PlayOneOff()
        {
            _currentState = true;
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
                var tween = RunTransitionImpl(in Transitions[i], true);
                if (tween != null)
                {
                    _pendingIds.Add(tween.uniqueId);
                }
            }
        }

        [FlexEvent]
        public void PlayPop()
        {
            if (Transitions == null || Transitions.Length == 0)
                return;

            _hasSwitchedState = true;
            _currentState = true;

            foreach (var id in _pendingIds)
            {
                LeanTween.cancel(id);
            }

            _pendingIds.Clear();
            float longest = 0f;
            LTDescr longestTween = null;

            for (int i = 0; i < Transitions.Length; i++)
            {
                var t = RunTransitionImpl(in Transitions[i], true);
                if (t != null)
                {
                    _pendingIds.Add(t.uniqueId);
                    if (Transitions[i].Duration > longest)
                    {
                        longest = Transitions[i].Duration;
                        longestTween = t;
                    }
                }
            }
            
            // Schedule the reverse transition after the longest animation
            if (longestTween != null)
            {
                _restoreStateCached ??= RestoreState;
                longestTween.setOnComplete(_restoreStateCached);
            }
        }

        private void RestoreState()
        {
            SwitchState(false, true);
        }

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

                case TransitionProperty.ScaleXY:
                {
                    var element = transition.Object as FlexElement;
                    if (element == null)
                    {
                        break;
                    }

                    var targetValue = _currentState ? transition.ToFloat : transition.FromFloat;
                    if (animate)
                    {
                        tween = LeanTween.scale(element.gameObject, new Vector3(targetValue, targetValue, element.transform.localScale.z), transition.Duration)
                            .setOnUpdate((Vector3 value, object obj) =>
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
                        tween = LeanTween.rotateZ(transform.gameObject, targetValue, transition.Duration);
                    }
                    else
                    {
                        var angles = transform.eulerAngles;
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
                
                case TransitionProperty.TranslateY:
                {
                    var tr = transition.Object as Transform;
                    if (tr == null)
                    {
                        break;
                    }

                    float startValue = tr.localPosition.y;
                    float targetValue = _currentState ? transition.ToFloat : transition.FromFloat;

                    if (animate)
                    {
                        tween = LeanTween.value(tr.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(tr)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is Transform t)
                                {
                                    var pos = t.localPosition;
                                    pos.y = value;
                                    t.localPosition = pos;
                                }
                            });
                    }
                    else
                    {
                        var pos = tr.localPosition;
                        pos.y = targetValue;
                        tr.localPosition = pos;
                    }

                    break;
                }
                
                case TransitionProperty.TranslateX:
                {
                    var tr = transition.Object as Transform;
                    if (tr == null)
                    {
                        break;
                    }

                    float startValue = tr.localPosition.x;
                    float targetValue = _currentState ? transition.ToFloat : transition.FromFloat;

                    if (animate)
                    {
                        tween = LeanTween.value(tr.gameObject, startValue, targetValue, transition.Duration)
                            .setEase(transition.Ease)
                            .setOnUpdateParam(tr)
                            .setOnUpdateObject((float value, object state) =>
                            {
                                if (state is Transform t)
                                {
                                    var pos = t.localPosition;
                                    pos.x = value;
                                    t.localPosition = pos;
                                }
                            });
                    }
                    else
                    {
                        var pos = tr.localPosition;
                        pos.x = targetValue;
                        tr.localPosition = pos;
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

            if (tween != null)
            {
                if (transition.Ease == LeanTweenType.animationCurve)
                {
                    tween.setEase(transition.Curve);
                }
                else
                {
                    tween.setEase(transition.Ease);
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

        public float GetTransitionTime()
        {
            float longestTransition = 0;
            foreach (var transition in Transitions)
            {
                if (transition.Duration > longestTransition)
                {
                    longestTransition = transition.Duration;
                }
            }

            return longestTransition;
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
                    return transform != null ? transform.eulerAngles.z : 0;
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
