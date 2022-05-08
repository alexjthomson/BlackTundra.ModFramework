using BlackTundra.Foundation.IO;

using System;
using System.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Media {

    public static class ImageImporter {

        public static Texture2D Import(in string name, in FileSystemReference fsr) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            // read data:
            if (!FileSystem.Read(fsr, out byte[] bytes, FileFormat.Standard)) {
                throw new IOException($"Failed to read wav file at `{fsr}`.");
            }
            // import data:
            return Import(name, bytes);
        }

        public static Texture2D Import(
            in string name,
            in byte[] bytes,
            in TextureFormat format = TextureFormat.RGBA32,
            in bool mipmaps = false,
            in TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            in FilterMode filterMode = FilterMode.Point,
            in int anisoLevel = 0,
            in bool linear = false
        ) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            Texture2D texture = new Texture2D(2, 2, format, mipmaps, linear, true) {
                name = name,
                wrapMode = wrapMode,
                filterMode = filterMode,
                anisoLevel = anisoLevel
            };
            texture.LoadImage(bytes, false);
            return texture;
        }

    }

}