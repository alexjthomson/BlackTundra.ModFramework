namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes the type of an asset.
    /// </summary>
    public enum ModAssetType : int {
        None = 0,
        // special:
        Config = -1, // .config
        AssetBundle = 1, // .bundle

        // logic:
        Assembly = -1000, // .dll
        ScriptShader = -1001,

        // audio:
        MediaWav  = 1000,
        MediaFlac = 1001,
        MediaMP3  = 1002,

        // model:
        ModelObj = 2000,
        ModelFbx = 2001,

        // json:
        JsonObj = 10000,
    }

}