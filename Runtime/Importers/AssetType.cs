namespace BlackTundra.ModFramework {

    public enum AssetType : int {

        #region special

        /// <summary>
        /// No asset type (no extension).
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown asset type.
        /// </summary>
        Unknown = -1,
        
        /// <summary>
        /// Invalid asset type.
        /// </summary>
        Invalid = -2,

        /// <summary>
        /// .config file.
        /// </summary>
        Config = -3,

        #endregion

        #region logic

        /// <summary>
        /// CSharp script.
        /// </summary>
        ScriptCSharp = 1,

        /// <summary>
        /// Shader script.
        /// </summary>
        ScriptShader = 2,

        #endregion

        #region model

        /// <summary>
        /// .obj file.
        /// </summary>
        ModelObj = 100,

        /// <summary>
        /// .fbx file.
        /// </summary>
        ModelFbx = 101,

        #endregion

        #region json

        /// <summary>
        /// .obj.json file.
        /// </summary>
        JsonObj = 200,

        #endregion

    }

}