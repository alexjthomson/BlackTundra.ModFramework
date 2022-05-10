using BlackTundra.Foundation.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public abstract class ModMaterial : ModAsset {

        #region constant

        protected internal static readonly Shader StandardLitShader = Shader.Find(
#if USE_UNIVERSAL_RENDER_PIPELINE
            "Universal Render Pipeline/Lit"
#else
            "Standard"
#endif
        );

        #endregion

        #region variable

        private Material _material;

        #endregion

        #region property

        public Material material => _material;

        public override bool IsValid => _material != null;

        #endregion

        #region constructor

        protected internal ModMaterial(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
        ) : base(modInstance, guid, type, fsr, path) {
            _material = null;
        }

        #endregion

        #region logic

        #region Dispose

        public override void Dispose() {
            DisposeOfMaterial();
        }

        #endregion

        #region DisposeOfMaterial

        protected virtual void DisposeOfMaterial() {
            if (_material != null) {
                Object.Destroy(_material);
                _material = null;
            }
        }

        #endregion

        #endregion

    }

}