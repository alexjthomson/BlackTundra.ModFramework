using System;

namespace BlackTundra.ModFramework.Utility {

    public static class ImportUtility {

        /// <summary>
        /// Converts a file <paramref name="extension"/> to a <see cref="ModAssetType"/>.
        /// </summary>
        public static ModAssetType ExtensionToAssetType(in string extension) {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            return extension.ToLower() switch {
                // special:
                "config" => ModAssetType.Config,
                "bundle" => ModAssetType.AssetBundle,
                // logic:
                "dll" => ModAssetType.Assembly,
                "shader" => ModAssetType.ScriptShader,
                // image:
                "png" => ModAssetType.MediaPng,
                "bmp" => ModAssetType.MediaBmp,
                "tif" or "tiff" => ModAssetType.MediaTif,
                "tga" => ModAssetType.MediaTga,
                "psd" => ModAssetType.MediaPsd,
                "jpg" or "jpeg" => ModAssetType.MediaJpg,
                // audio:
                "wav" => ModAssetType.MediaWav,
                "flac" => ModAssetType.MediaFlac,
                "mp3" => ModAssetType.MediaMP3,
                // material:
                "mtl" => ModAssetType.MaterialMtl,
                // model:
                "obj" => ModAssetType.ModelObj,
                "fbx" => ModAssetType.ModelFbx,
                // json:
                "json" => ModAssetType.JsonObj,
                _ => ModAssetType.None
            };
        }

    }

}