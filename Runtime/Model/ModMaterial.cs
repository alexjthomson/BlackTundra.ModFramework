using BlackTundra.Foundation.IO;

using System;

namespace BlackTundra.ModFramework.Model {

    public abstract class ModMaterial : ModAsset {

        #region variable

        #endregion

        #region property

        #endregion

        #region constructor

        protected internal ModMaterial(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
        }

        #endregion

        #region logic

        #region IsReferenced

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ModMaterial"/> is referenced by a <see cref="ModModel"/> in any <see cref="ModInstance"/>
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
        /// Returns <c>true</c> if the <see cref="ModMaterial"/> is referenced a <see cref="ModModel"/> in the specified
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

        #endregion

    }

}