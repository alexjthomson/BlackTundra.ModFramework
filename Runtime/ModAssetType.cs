namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes the type of an asset.
    /// </summary>
    public enum ModAssetType : int {

        #region special

        /// <summary>
        /// <c>.config</c> file.
        /// </summary>
        Config = -1,

        /// <summary>
        /// No asset type (no extension).
        /// </summary>
        None = 0,

        /// <summary>
        /// <c>.bundle</c> file.
        /// </summary>
        AssetBundle = 1,

        #endregion

        #region logic

        /// <summary>
        /// <c>.dll</c> file.
        /// </summary>
        Assembly = -1000,

        /// <summary>
        /// <c>.shader</c> script.
        /// </summary>
        ScriptShader = -1001,

        #endregion

        #region model

        /// <summary>
        /// <c>.obj</c> file.
        /// </summary>
        ModelObj = 1000,

        /// <summary>
        /// <c>.fbx</c> file.
        /// </summary>
        ModelFbx = 1001,

        #endregion

        #region json

        /// <summary>
        /// <c>.json</c> file.
        /// </summary>
        JsonObj = 10000,

        #endregion

    }

}