using BlackTundra.Foundation.IO;

using System;
using System.Collections.Generic;

namespace BlackTundra.ModFramework.Model {

    public abstract class ModMaterialCollection : ModAsset {

        #region variable

        protected internal readonly Dictionary<string, ModMaterial> materialDictionary;

        #endregion

        #region property

        public int Count => materialDictionary.Count;

        public override bool IsValid => materialDictionary.Count > 0;

        #endregion

        #region constructor

        protected internal ModMaterialCollection(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
            materialDictionary = new Dictionary<string, ModMaterial>();
        }

        #endregion

        #region logic

        #region IsReferenced

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ModMaterialCollection"/> is referenced by a <see cref="ModModel"/> in any <see cref="ModInstance"/>
        /// that is currently loaded.
        /// </returns>
        public bool IsReferenced() {
            if (IsReferencedByMod(modInstance)) return true;
            foreach (ModInstance mod in ModInstance.ModDictionary.Values) {
                if (mod != modInstance && IsReferencedByMod(mod)) {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IsReferencedByMod

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ModMaterialCollection"/> is referenced a <see cref="ModModel"/> in the specified
        /// <paramref name="modInstance"/>.
        /// </returns>
        public bool IsReferencedByMod(in ModInstance modInstance) {
            if (modInstance == null) throw new ArgumentNullException(nameof(modInstance));
            foreach (ModAsset asset in modInstance._assets.Values) {
                if (asset is ModModel model) {
                    if (model.IsReferencingMaterial(this)) return true;
                }
            }
            return false;
        }

        #endregion

        #region Dispose

        public override void Dispose() {
            DisposeOfMaterials();
        }

        #endregion

        #region DisposeOfMaterials

        protected void DisposeOfMaterials() {
            foreach (ModMaterial material in materialDictionary.Values) {
                material.Dispose();
            }
            materialDictionary.Clear();
        }

        #endregion

        #endregion

    }

}