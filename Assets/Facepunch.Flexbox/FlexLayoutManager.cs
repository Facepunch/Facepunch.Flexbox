using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Facepunch.Flexbox
{
    [ExecuteAlways, DefaultExecutionOrder(-100)]
    public class FlexLayoutManager : MonoBehaviour
    {
        public static FlexLayoutManager Instance { get; private set; }

        private static readonly List<FlexElement> DirtyElements = new List<FlexElement>();
        private static readonly List<FlexElement> UpdatingElements = new List<FlexElement>();

#if UNITY_EDITOR
        private static bool EditorHookedUpdate = false;
#endif

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
            FlushQueue();
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
                    if (!EditorHookedUpdate)
                    {
                        EditorApplication.update += FlushQueue;
                        EditorHookedUpdate = true;
                    }

                    EditorApplication.QueuePlayerLoopUpdate();
                }
                else
#endif
                {
                    Debug.LogWarning("There is no FlexLayoutManager!");
                    return;
                }
            }

            if (!DirtyElements.Contains(element))
            {
                DirtyElements.Add(element);
            }
        }

        private static void FlushQueue()
        {
            if (DirtyElements.Count == 0)
            {
                return;
            }

            UpdatingElements.AddRange(DirtyElements);
            DirtyElements.Clear();

            try
            {
                foreach (var element in UpdatingElements)
                {
                    if (element != null)
                    {
                        element.PerformLayout();
                    }
                }
            }
            finally
            {
                UpdatingElements.Clear();
            }
        }
    }
}
