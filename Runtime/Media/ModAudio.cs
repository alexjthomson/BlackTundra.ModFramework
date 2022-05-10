using BlackTundra.Foundation.IO;

using System;

namespace BlackTundra.ModFramework.Media {

    public abstract class ModAudio : ModAsset {

        #region variable

        #endregion

        #region property

        #endregion

        #region constructor

        protected internal ModAudio(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {

        }

        #endregion

        #region logic

        #endregion

    }

}