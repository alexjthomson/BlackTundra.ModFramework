using System;

namespace BlackTundra.ModFramework {

    public static class ModAssetTypeUtility {

        /// <summary>
        /// Converts a file <paramref name="extension"/> to a <see cref="ModAssetType"/>.
        /// </summary>
        public static ModAssetType ExtensionToAssetType(in string extension) {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            return extension.ToLower() switch {
                "config" => ModAssetType.Config,
                "bundle" => ModAssetType.AssetBundle,
                "dll" => ModAssetType.Assembly,
                "shader" => ModAssetType.ScriptShader,
                "obj" => ModAssetType.ModelObj,
                "fbx" => ModAssetType.ModelFbx,
                "json" => ModAssetType.JsonObj,
                _ => ModAssetType.None
            };
        }

    }

}