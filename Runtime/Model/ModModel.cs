using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;

namespace BlackTundra.ModFramework.Model {

    public abstract class ModModel : ModAsset {

        #region variable

        /// <summary>
        /// <see cref="MeshBuilderOptions"/> used when building the <see cref="UnityEngine.Mesh"/> <see cref="_asset"/>.
        /// </summary>
        public readonly MeshBuilderOptions meshBuilderOptions;

        /// <summary>
        /// <see cref="ModMaterial"/> assets used by the <see cref="ModModel"/>.
        /// </summary>
        private ModMaterial[] materials;

        #endregion

        #region property

        /// <summary>
        /// Total number of <see cref="ModMaterial"/> assets referenced/used by the <see cref="ModModel"/>.
        /// </summary>
        /// <seealso cref="GetMaterialAt(in int)"/>
        public int MaterialCount => materials.Length;

        #endregion

        #region constructor

        protected internal ModModel(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path,
            in MeshBuilderOptions meshBuilderOptions
            ) : base(modInstance, guid, type, fsr, path) {
            this.meshBuilderOptions = meshBuilderOptions;
            materials = new ModMaterial[0];
        }

        #endregion

        #region logic

        #region GetMaterialAt

        /// <returns>
        /// Return the <see cref="ModMaterial"/> at the specified <paramref name="index"/>.
        /// </returns>
        /// <seealso cref="MaterialCount"/>
        public ModMaterial GetMaterialAt(in int index) {
            if (index < 0 || index >= materials.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return materials[index];
        }

        #endregion

        #region ReferenceMaterial

        /// <summary>
        /// Imports a material from the specified <paramref name="materialFsr"/>.
        /// </summary>
        protected internal ModMaterial ReferenceMaterial(in FileSystemReference materialFsr) {
            if (materialFsr == null) throw new ArgumentNullException(nameof(materialFsr));
            ModAsset materialAsset = ModInstance.GetAsset(materialFsr);
            if (materialAsset is ModMaterial material) {
                materials = materials.AddLast(material);
                return material;
            } else {
                materialAsset.Dispose();
                throw new FormatException($"Failed to import material at `{materialFsr}` because asset type `{materialAsset.type}` is not a material.");
            }
        }

        #endregion

        #region IsReferencingMaterial

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ModModel"/> is referencing the specified <paramref name="material"/>.
        /// </returns>
        public bool IsReferencingMaterial(in ModMaterial material) {
            if (material == null) throw new ArgumentNullException(nameof(material));
            return materials.Contains(material);
        }

        #endregion

        #region DisposeOfAsset

        protected override void DisposeOfAsset() {
            base.DisposeOfAsset();
            DisposeOfMaterialAssets();
        }

        #endregion

        #region DisposeOfMaterialAssets

        /// <summary>
        /// Disposes of any <see cref="ModMaterial"/> assets that the <see cref="ModModel"/> uses that are
        /// not referenced by any other <see cref="ModModel"/> assets.
        /// </summary>
        private void DisposeOfMaterialAssets() {
            // get a reference to the old materials:
            ModMaterial[] oldMaterials = materials;
            // clear the materials that the current model uses:
            materials = new ModMaterial[0];
            // iterate each of the old materials:
            ModMaterial material;
            for (int i = oldMaterials.Length - 1; i >= 0; i--) {
                material = oldMaterials[i];
                // check if the current material is referenced:
                if (!material.IsReferenced()) {
                    material.Dispose();
                }
            }
        }

        #endregion

        #endregion

    }

}