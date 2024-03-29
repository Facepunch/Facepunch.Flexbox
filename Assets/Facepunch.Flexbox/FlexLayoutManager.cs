﻿using System.Collections.Generic;
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

        internal static readonly HashSet<FlexElementBase> ActiveScopedUpdates = new HashSet<FlexElementBase>();

        private static readonly List<FlexElementBase> DirtyElements = new List<FlexElementBase>();
        private static readonly List<FlexElementBase> UpdatingElements = new List<FlexElementBase>();

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

        public static void EnqueueLayout(FlexElementBase element)
        {
            if (element == null)
            {
                return;
            }

#if UNITY_EDITOR
            var scene = element.gameObject.scene;
            if (!scene.IsValid())
            {
                return;
            }
#endif

            if (Instance == null)
            {
#if UNITY_EDITOR
                if (!EditorHookedUpdate)
                {
                    EditorApplication.update += FlushQueue;
                    EditorHookedUpdate = true;
                }

                EditorApplication.QueuePlayerLoopUpdate();
#else
                Debug.LogWarning("There is no FlexLayoutManager!");
                return;
#endif
            }
            else if (!Instance.isActiveAndEnabled)
            {
                Debug.LogWarning("FlexLayoutManager is not active!");
            }      

#if UNITY_EDITOR
            // Unity does something weird when switching to/from play mode... this will be called with a valid element,
            // but it'll turn null when we atually switch modes, yet it's still equal to the (new?) element
            DirtyElements.RemoveAll(e => e == null);
#endif

            if (!DirtyElements.Contains(element) && !ActiveScopedUpdates.Contains(element))
            {
                DirtyElements.Add(element);
            }
        }

        internal static void LayoutImmediate(FlexElementBase element)
        {
            if (element == null || !element.IsAbsolute)
            {
                return;
            }

            DirtyElements.Remove(element);
            element.PerformLayout();
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
