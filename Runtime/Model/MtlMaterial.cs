using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;
using System.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public sealed class MtlMaterial : ModMaterial {

        #region variable

        #endregion

        #region property

        #endregion

        #region constructor

        internal MtlMaterial(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
        }

        #endregion

        #region logic

        #region Import

        protected internal override void Import() {
            // remove exting material asset:
            DisposeOfAsset();
            // load material data:
            if (!FileSystem.Read(fsr, out string mtl, FileFormat.Standard)) {
                throw new IOException($"Failed to read MTL file at `{fsr}`.");
            }
            // parse material data:
            _asset = ParseMaterialData(guid.ToHex(), mtl);
        }

        #endregion

        #region ParseMaterialData

        private static Material ParseMaterialData(in string name, in string mtl) {
            throw new NotImplementedException();
        }

        #endregion

        #region Dispose

        public override void Dispose() {
            DisposeOfAsset();
        }

        #endregion

        #endregion

    }

}