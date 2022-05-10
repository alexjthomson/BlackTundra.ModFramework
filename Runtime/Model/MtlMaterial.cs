using BlackTundra.Foundation.IO;

using System;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public sealed class MtlMaterial : ModMaterial {

        #region variable

        public readonly string name;

        internal Color baseColour;
        internal Color diffuseColour;
        internal Color specularColour;
        internal float alpha;
        internal float shininess;
        internal MtlIlluminationModel illuminationModel;
        internal FileSystemReference textureMapFsr;

        #endregion

        #region constructor

        internal MtlMaterial(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path,
            in string name
        ) : base(modInstance, guid, type, fsr, path) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            this.name = name;
            baseColour = new Color(0.2f, 0.2f, 0.2f);
            diffuseColour = new Color(0.8f, 0.8f, 0.8f);
            specularColour = new Color(1.0f, 1.0f, 1.0f);
            alpha = 1.0f;
            shininess = 0.0f;
            illuminationModel = MtlIlluminationModel.Specular;
            textureMapFsr = null;
        }

        #endregion

        #region logic

        #region Import

        protected internal override void Import() {
            throw new System.NotImplementedException();
        }

        #endregion

        #endregion

    }

}