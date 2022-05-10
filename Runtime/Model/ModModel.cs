using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.ModFramework.Model {

    public abstract class ModModel : ModAsset {

        #region variable

        /// <summary>
        /// <see cref="MeshBuilderOptions"/> used when building the <see cref="UnityEngine.Mesh"/> <see cref="_asset"/>.
        /// </summary>
        public readonly MeshBuilderOptions meshBuilderOptions;

        /// <summary>
        /// <see cref="Mesh"/> imported.
        /// </summary>
        protected Mesh _mesh;

        /// <summary>
        /// <see cref="ModMaterialCollection"/> assets used by the <see cref="ModModel"/>.
        /// </summary>
        private ModMaterialCollection[] materialCollections;

        #endregion

        #region property

        public Mesh mesh => _mesh;

        public override bool IsValid => _mesh != null;

        /// <summary>
        /// Total number of <see cref="ModMaterialCollection"/> assets referenced/used by the <see cref="ModModel"/>.
        /// </summary>
        /// <seealso cref="GetMaterialAt(in int)"/>
        public int MaterialCount => materialCollections.Length;

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
            materialCollections = new ModMaterialCollection[0];
        }

        #endregion

        #region logic

        #region GetMaterialAt

        /// <returns>
        /// Return the <see cref="ModMaterialCollection"/> at the specified <paramref name="index"/>.
        /// </returns>
        /// <seealso cref="MaterialCount"/>
        public ModMaterialCollection GetMaterialAt(in int index) {
            if (index < 0 || index >= materialCollections.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return materialCollections[index];
        }

        #endregion

        #region ReferenceMaterial

        /// <summary>
        /// Imports a material from the specified <paramref name="materialFsr"/>.
        /// </summary>
        protected internal ModMaterialCollection ReferenceMaterial(in FileSystemReference materialFsr) {
            if (materialFsr == null) throw new ArgumentNullException(nameof(materialFsr));
            if (ModInstance.TryGetAsset(materialFsr, out ModMaterialCollection materialCollection)) {
                materialCollections = materialCollections.AddLast(materialCollection);
                return materialCollection;
            } else {
                return null;
            }
        }

        #endregion

        #region IsReferencingMaterial

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ModModel"/> is referencing the specified <paramref name="material"/>.
        /// </returns>
        public bool IsReferencingMaterial(in ModMaterialCollection material) {
            if (material == null) throw new ArgumentNullException(nameof(material));
            return materialCollections.Contains(material);
        }

        #endregion

        #region DisposeOfAsset

        protected virtual void DisposeOfAsset() {
            if (_mesh != null) {
                Object.Destroy(_mesh);
                _mesh = null;
            }
            DisposeOfMaterialAssets();
        }

        #endregion

        #region DisposeOfMaterialAssets

        /// <summary>
        /// Disposes of any <see cref="ModMaterialCollection"/> assets that the <see cref="ModModel"/> uses that are
        /// not referenced by any other <see cref="ModModel"/> assets.
        /// </summary>
        private void DisposeOfMaterialAssets() {
            // get a reference to the old materials:
            ModMaterialCollection[] oldMaterials = materialCollections;
            // clear the materials that the current model uses:
            materialCollections = new ModMaterialCollection[0];
            // iterate each of the old materials:
            ModMaterialCollection material;
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