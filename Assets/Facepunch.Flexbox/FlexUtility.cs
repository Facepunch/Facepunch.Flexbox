﻿using UnityEngine;

namespace Facepunch.Flexbox.Utility
{
    public static class FlexUtility
    {
        public static bool IsPrefabRoot(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return false;
            }

            var thisGo = gameObject;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(thisGo);
            return stage != null && ReferenceEquals(stage.prefabContentsRoot, thisGo);
#else
            return false;
#endif
        }
    }
}
