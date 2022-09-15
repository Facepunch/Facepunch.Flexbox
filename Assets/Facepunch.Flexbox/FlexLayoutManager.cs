using System;
using System.Collections.Generic;
using UnityEngine;

namespace Facepunch.Flexbox
{
    [ExecuteAlways, DefaultExecutionOrder(-100)]
    public class FlexLayoutManager : MonoBehaviour
    {
        public static FlexLayoutManager Instance { get; private set; }

        private readonly List<FlexElement> _dirtyElements = new List<FlexElement>();
        private readonly List<FlexElement> _updatingElements = new List<FlexElement>();

        public void OnEnable()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Cannot have multiple FlexLayoutManager!", this);
                return;
            }

            Instance = this;
        }

        public void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LateUpdate()
        {
            if (_dirtyElements.Count == 0)
            {
                return;
            }

            _updatingElements.AddRange(_dirtyElements);
            _dirtyElements.Clear();

            try
            {
                foreach (var element in _updatingElements)
                {
                    if (element != null)
                    {
                        element.PerformLayout();
                    }
                }
            }
            finally
            {
                _updatingElements.Clear();
            }
        }

        public static void EnqueueLayout(FlexElement element)
        {
            if (element == null)
            {
                return;
            }

            if (Instance == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    try
                    {
                        element.PerformLayout();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    return;
                }
#endif

                Debug.LogWarning("There is no FlexLayoutManager!");
                return;
            }

            if (!Instance._dirtyElements.Contains(element))
            {
                Instance._dirtyElements.Add(element);
            }
        }
    }
}
