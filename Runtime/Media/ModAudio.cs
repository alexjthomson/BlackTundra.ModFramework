using BlackTundra.Foundation.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Media {

    public abstract class ModAudio : ModAsset {

        #region variable

        protected AudioClip _clip;

        #endregion

        #region property

        public AudioClip clip => _clip;

        public override bool IsValid => _clip != null;

        #endregion

        #region constructor

        protected internal ModAudio(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
            _clip = null;
        }

        #endregion

        #region logic

        #region Dispose

        public override void Dispose() {
            DisposeOfAudio();
        }

        #endregion

        #region DisposeOfAudio

        protected virtual void DisposeOfAudio() {
            if (_clip != null) {
                Object.Destroy(_clip);
                _clip = null;
            }
        }

        #endregion

        #endregion

    }

}