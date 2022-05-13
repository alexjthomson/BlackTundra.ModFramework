using BlackTundra.Foundation.IO;
using BlackTundra.ModFramework.Media;

using System;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public sealed class MtlMaterial : ModMaterial {

        #region variable

        internal Color _baseColour;
        internal Color _specularColour;
        internal Color _ambientColour;
        internal float _alpha;
        internal float _shininess;
        internal MtlIlluminationModel _illuminationModel;
        internal ModTexture _baseMap;

        #endregion

        #region property

        public Color BaseColour => _baseColour;

        public Color SpecularColour => _specularColour;

        public Color AmbientColour => _ambientColour;

        public float Alpha => _alpha;

        public float Opacity => 1.0f - _alpha;

        public float Shininess => _shininess;

        public MtlIlluminationModel IlluminationModel => _illuminationModel;

        public ModTexture BaseMapAsset => _baseMap;

        #endregion

        #region constructor

        internal MtlMaterial(
            in ModInstance modInstance,
            in ulong guid,
            in string name
        ) : base(modInstance, guid, ModAssetType.MaterialMtl, null, null, name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            _baseColour = new Color(0.2f, 0.2f, 0.2f);
            _specularColour = new Color(1.0f, 1.0f, 1.0f);
            _ambientColour = new Color(0.8f, 0.8f, 0.8f);
            _alpha = 1.0f;
            _shininess = 0.0f;
            _illuminationModel = MtlIlluminationModel.Specular;
            _baseMap = null;
        }

        #endregion

        #region logic

        #region Import

        protected internal override void Import() {
            _material = new Material(StandardLitShader);
#if USE_UNIVERSAL_RENDER_PIPELINE
            _material.SetOverrideTag("RenderType", _alpha >= 1.0f ? "Opaque" : "Transparent");
            _material.SetColor("_BaseColor", new Color(_baseColour.r, _baseColour.g, _baseColour.b, _alpha));
            _material.SetColor("_SpecColor", _specularColour);
            _material.SetFloat("_Smoothness", _shininess);
            if (_baseMap != null && _baseMap.texture != null) {
                _material.SetTexture("_BaseMap", _baseMap.texture);
            }
#else
            #warning Current render pipeline not supported by MtlMaterial import
#endif
        }

        #endregion

        #endregion

    }

}