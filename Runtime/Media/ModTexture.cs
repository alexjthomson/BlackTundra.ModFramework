using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Media {

    public sealed class ModTexture : ModAsset {

        #region variable

        public readonly TextureFormat textureFormat;
        public readonly bool mipmaps;
        public readonly TextureWrapMode wrapMode;
        public readonly FilterMode filterMode;
        public readonly int anisoLevel;
        public readonly bool linear;

        private Texture2D _texture;

        #endregion

        #region property

        public Texture2D texture => _texture;

        public sealed override bool IsValid => _texture != null;

        #endregion

        #region constructor

        internal ModTexture(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path,
            in TextureFormat textureFormat = TextureFormat.RGBA32,
            in bool mipmaps = false,
            in TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            in FilterMode filterMode = FilterMode.Point,
            in int anisoLevel = 0,
            in bool linear = false
            ) : base(modInstance, guid, type, fsr, path) {
            this.textureFormat = textureFormat;
            this.mipmaps = mipmaps;
            this.wrapMode = wrapMode;
            this.filterMode = filterMode;
            this.anisoLevel = anisoLevel;
            this.linear = linear;
        }

        #endregion

        #region logic

        #region Import

        protected internal sealed override void Import() {
            // dispose of existing asset:
            DisposeOfTexture();
            // read image data:
            if (!FileSystem.Read(fsr, out byte[] bytes, FileFormat.Standard)) {
                throw new IOException($"Failed to read wav file at `{fsr}`.");
            }
            // parse image data:
            Texture2D texture = new Texture2D(2, 2, textureFormat, mipmaps, linear, true) {
                name = guid.ToHex(),
                wrapMode = wrapMode,
                filterMode = filterMode,
                anisoLevel = anisoLevel
            };
            texture.LoadImage(bytes, false);
            _texture = texture;
        }

        #endregion

        #region Dispose

        public sealed override void Dispose() {
            DisposeOfTexture();
        }

        #endregion

        #region DisposeOfTexture

        private void DisposeOfTexture() {
            if (_texture != null) {
                Object.Destroy(_texture);
                _texture = null;
            }
        }

        #endregion

        #endregion

    }

}