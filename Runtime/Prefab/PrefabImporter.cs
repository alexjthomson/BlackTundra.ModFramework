using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;

using Newtonsoft.Json.Linq;

using System;
using System.IO;

namespace BlackTundra.ModFramework.Prefab {

    public static class PrefabImporter {

        #region constant

        internal static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(PrefabImporter));

        #endregion

        #region variable

        #endregion

        #region property

        #endregion

        #region logic

        #region Import

        public static ModPrefab Import(in string name, in FileSystemReference fsr) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} is not referencing a file.");
            if (!FileSystem.Read(fsr, out string jsonString, FileFormat.Standard)) throw new IOException($"Failed to read prefab JSON file at `{fsr}`.");
            // read json:
            JObject json = JObject.Parse(jsonString);
            // create prefab:
            return Import(name, json);
        }

        public static ModPrefab Import(in string name, JObject json) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (json == null) throw new ArgumentNullException(nameof(json));
            try {
                return new ModPrefab(json);
            } catch (Exception exception) {
                ConsoleFormatter.Error(
                    $"Failed to import prefab `{name}`.",
                    exception
                );
                return null;
            }
        }

        #endregion

        #endregion

    }

}