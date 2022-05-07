using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;
using BlackTundra.ModFramework.Prefabs;

using Newtonsoft.Json.Linq;

using System;
using System.IO;

namespace BlackTundra.ModFramework.Importers {

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

        internal static ModPrefab Import(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} is not referencing a file.");
            if (!FileSystem.Read(fsr, out string jsonString, FileFormat.Standard)) throw new IOException($"Failed to read prefab JSON file at `{fsr}`.");
            // read json:
            JObject json = JObject.Parse(jsonString);
            // create prefab:
            try {
                return new ModPrefab(json);
            } catch (Exception exception) {
                ConsoleFormatter.Error(
                    $"Failed to import prefab at `{fsr}`.",
                    exception
                );
                return null;
            }
        }

        #endregion

        #endregion

    }

}