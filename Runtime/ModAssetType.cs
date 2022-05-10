namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes the type of an asset.
    /// </summary>
    public enum ModAssetType : int {
        None            = 0,

        // special:
        Config          = -1, // .config
        AssetBundle     = 1, // .bundle

        // logic:
        Assembly        = -1000, // .dll
        ScriptShader    = -1001,

        // image:
        MediaPng        = 1000,
        MediaBmp        = 1001,
        MediaTif        = 1002,
        MediaTga        = 1003,
        MediaPsd        = 1004,
        MediaJpg        = 1005,

        // audio:
        MediaWav        = 2000,
        MediaFlac       = 2001,
        MediaMP3        = 2002,

        // material:
        MaterialMtl     = 3000,

        // model:
        ModelObj        = 4000,
        ModelFbx        = 4001,

        // json:
        JsonObj         = 10000,
    }

}