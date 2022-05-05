using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.ModFramework.Importers;

using System;

namespace BlackTundra.ModFramework {

    public sealed class ModAsset : IDisposable {

        #region variable

        /// <summary>
        /// <see cref="Mod"/> that the <see cref="ModAsset"/> belongs to.
        /// </summary>
        public readonly Mod mod;

        /// <summary>
        /// <see cref="FileSystemReference"/> to the <see cref="ModAsset"/>.
        /// </summary>
        public readonly FileSystemReference fsr;

        /// <summary>
        /// Name of the <see cref="ModAsset"/>.
        /// </summary>
        public readonly string name;

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

        /// <param name="mod"><see cref="Mod"/> that the <see cref="ModAsset"/> belongs to.</param>
        /// <param name="fsr"><see cref="FileSystemReference"/> to the <see cref="ModAsset"/> file location.</param>
        /// <param name="fsrNameStartIndex">Index to start the substring from in the <see cref="FileSystemReference"/> to find the <see cref="name"/>.</param>
        internal ModAsset(in Mod mod, in FileSystemReference fsr, in int fsrNameStartIndex) {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            this.mod = mod;
            this.fsr = fsr;
            string absolutePath = fsr.AbsolutePath;
            name = absolutePath[fsrNameStartIndex..];
            guid = mod._guidIdentifier | ((ulong)name.GetHashCode() & Mod.ModAssetGUIDMask);
            type = ModAssetTypeUtility.ExtensionToAssetType(fsr.FileExtension);
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
                case ModAssetType.ModelObj: {
                    _asset = OBJImporter.Import(guid.ToHex(), fsr, Importers.Utility.MeshBuilderOptions.OptimizeMesh);
                    break;
                }
                case ModAssetType.JsonObj: {
                    //asset = JsonObjImporter.Import(fsr);
                    break;
                }
                default: throw new NotSupportedException();
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

        public sealed override string ToString() => $"{mod.name}::{name} [{guid.ToHex()}] ({type})";

        #endregion

        #endregion

    }

}