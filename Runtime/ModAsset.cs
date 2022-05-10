using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;

using Object = UnityEngine.Object;

namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes a generic asset that belongs to a <see cref="modInstance"/>.
    /// </summary>
    public abstract class ModAsset : IDisposable {

        #region variable

        /// <summary>
        /// <see cref="ModInstance"/> that the <see cref="ModAsset"/> belongs to.
        /// </summary>
        public readonly ModInstance modInstance;

        /// <summary>
        /// <see cref="FileSystemReference"/> to the <see cref="ModAsset"/>.
        /// </summary>
        public readonly FileSystemReference fsr;

        /// <summary>
        /// Path of the <see cref="ModAsset"/> within the <see cref="modInstance"/>.
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
        protected object _asset;

        #endregion

        #region property

        public object value => _asset;

        #endregion

        #region constructor

        private ModAsset() => throw new NotSupportedException();

        /// <param name="modInstance"><see cref="ModInstance"/> that the <see cref="ModAsset"/> belongs to.</param>
        /// <param name="fsr"><see cref="FileSystemReference"/> to the <see cref="ModAsset"/> file location.</param>
        /// <param name="fsrNameStartIndex">Index to start the substring from in the <see cref="FileSystemReference"/> to find the <see cref="path"/>.</param>
        protected internal ModAsset(in ModInstance modInstance, in ulong guid, in ModAssetType type, in FileSystemReference fsr, in string path) {
            if (modInstance == null) throw new ArgumentNullException(nameof(modInstance));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            if (path == null) throw new ArgumentNullException(nameof(path));
            this.modInstance = modInstance;
            this.guid = guid;
            this.type = type;
            this.fsr = fsr;
            this.path = path;
            _asset = null;
        }

        #endregion

        #region logic

        #region Import

        /// <summary>
        /// Imports the asset.
        /// </summary>
        protected internal abstract void Import();

        #endregion

        #region Dispose

        public abstract void Dispose();

        #endregion

        #region DisposeOfAsset

        /// <summary>
        /// Disposes of the <see cref="_asset"/> and assigns a <c>null</c> reference.
        /// </summary>
        protected virtual void DisposeOfAsset() {
            if (_asset != null) {
                if (_asset is Object obj) {
                    Object.Destroy(obj);
                }
                _asset = null;
            }
        }

        #endregion

        #region ToString

        public override string ToString() => $"{modInstance.name}::{path} [{guid.ToHex()}] ({type})";

        #endregion

        #endregion

    }

}