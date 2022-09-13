﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HoverListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent OnEnter;
    public UnityEvent OnExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke();
    }
}
