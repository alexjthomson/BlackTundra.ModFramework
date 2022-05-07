using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.ModFramework.Media;
using BlackTundra.ModFramework.Model;
using BlackTundra.ModFramework.Prefab;
using BlackTundra.ModFramework.Utility;

using System;

namespace BlackTundra.ModFramework {

    public sealed class ModAsset : IDisposable {

        #region variable

        /// <summary>
        /// <see cref="ModInstance"/> that the <see cref="ModAsset"/> belongs to.
        /// </summary>
        public readonly ModInstance mod;

        /// <summary>
        /// <see cref="FileSystemReference"/> to the <see cref="ModAsset"/>.
        /// </summary>
        public readonly FileSystemReference fsr;

        /// <summary>
        /// Path of the <see cref="ModAsset"/> within the <see cref="mod"/>.
        /// </summary>
        public readonly string path;

        /// <summary>
        /// Unique GUID of the <see cref="ModAsset"/>.
        /// </summary>
        public readonly ulong guid;

        /// <summary>
        /// <see cref="ModAssetType"/> of the <see cref="_asset"/>.
        /// </summary>
        public readonly ModAssetType type;

        /// <summary>
        /// Reference to the actual asset.
        /// </summary>
        private object _asset;

        #endregion

        #region property

        public object value => _asset;

        #endregion

        #region constructor

        private ModAsset() => throw new NotSupportedException();

        /// <param name="mod"><see cref="ModInstance"/> that the <see cref="ModAsset"/> belongs to.</param>
        /// <param name="fsr"><see cref="FileSystemReference"/> to the <see cref="ModAsset"/> file location.</param>
        /// <param name="fsrNameStartIndex">Index to start the substring from in the <see cref="FileSystemReference"/> to find the <see cref="path"/>.</param>
        internal ModAsset(in ModInstance mod, in FileSystemReference fsr, in int fsrNameStartIndex) {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            this.mod = mod;
            this.fsr = fsr;
            string absolutePath = fsr.AbsolutePath;
            path = absolutePath[fsrNameStartIndex..];
            guid = mod._guidIdentifier | ((ulong)path.ToLower().GetHashCode() & ModInstance.ModAssetGUIDMask);
            type = ImportUtility.ExtensionToAssetType(fsr.FileExtension);
            _asset = null;
        }

        #endregion

        #region logic

        #region Import

        /// <summary>
        /// Imports the asset.
        /// </summary>
        internal void Import() {
            switch (type) {
                // audio:
                case ModAssetType.MediaWav: {
                    _asset = WavImporter.Import(guid.ToHex(), fsr);
                    break;
                }
                // model:
                case ModAssetType.ModelObj: {
                    _asset = ObjImporter.Import(guid.ToHex(), fsr, MeshBuilderOptions.OptimizeMesh);
                    break;
                }
                // json:
                case ModAssetType.JsonObj: {
                    _asset = PrefabImporter.Import(fsr);
                    break;
                }
                default: {
                    _asset = null;
                    break;
                }
            }
        }

        #endregion

        #region Dispose

        public void Dispose() {
            // TODO: implement here
            throw new NotImplementedException();
        }

        #endregion

        #region ToString

        public sealed override string ToString() => $"{mod.name}::{path} [{guid.ToHex()}] ({type})";

        #endregion

        #endregion

    }

}