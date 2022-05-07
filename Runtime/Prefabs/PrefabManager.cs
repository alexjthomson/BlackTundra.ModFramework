using UnityEngine;

namespace BlackTundra.ModFramework.Prefabs {

    public static class PrefabManager {

        #region constant

        #endregion

        #region variable

        private static Transform _cacheTransform = null;

        #endregion

        #region property

        /// <summary>
        /// <see cref="Transform"/> to parent prefabs with.
        /// </summary>
        public static Transform CacheTransform {
            get {
                if (_cacheTransform == null) {
                    GameObject cacheGameObject = new GameObject("__PrefabCache") {
                        layer = 2, // ignore raycast
                        isStatic = true,
                        //hideFlags = HideFlags.HideAndDontSave
                    };
                    cacheGameObject.SetActive(false);
                    Object.DontDestroyOnLoad(cacheGameObject);
                    _cacheTransform = cacheGameObject.transform;
                }
                return _cacheTransform;
            }
        }

        #endregion

        #region logic

        #endregion

    }

}