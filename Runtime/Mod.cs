using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.ModFramework.Importers;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Version = BlackTundra.Foundation.Version;

namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes a mod loaded in at runtime.
    /// </summary>
    public sealed class Mod {

        #region constant

        /// <summary>
        /// Mod <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        private static readonly Dictionary<int, Mod> ModDictionary = new Dictionary<int, Mod>();

        /// <summary>
        /// Regex pattern used for matching a mod name.
        /// </summary>
        public const string ModNameRegexPattern = @"[a-z0-9_\-\.]+";

        /// <summary>
        /// Compiled regex pattern matcher used for testing if a mod name is valid.
        /// </summary>
        public static readonly Regex ModNameRegex = new Regex(
            ModNameRegexPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        /// <summary>
        /// Maximum number of characters allowed in a mod name.
        /// </summary>
        public const int MaxModNameLength = 64;

        /// <summary>
        /// Local path to expect to find the mod manifest file.
        /// </summary>
        public const string ManifestFileName = "manifest.json";

        public const ulong ModGUIDMask = 0b11111111_11111111_11111111_11111111_00000000_00000000_00000000_00000000;
        public const ulong ModAssetGUIDMask = 0b00000000_00000000_00000000_00000000_11111111_11111111_11111111_11111111;

        #endregion

        #region variable

        /// <summary>
        /// <see cref="FileSystemReference"/> to the mod directory.
        /// </summary>
        private readonly FileSystemReference fsr;

        /// <summary>
        /// Name of the <see cref="Mod"/>.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Part of the GUID that makes up an assets unique GUID.
        /// </summary>
        public readonly ulong _guidIdentifier;

        /// <summary>
        /// Version of the <see cref="Mod"/>.
        /// </summary>
        private Version _version;

        /// <summary>
        /// Display name of the <see cref="Mod"/>.
        /// </summary>
        private string _displayName;

        /// <summary>
        /// Description of the <see cref="Mod"/>.
        /// </summary>
        private string _description;

        /// <summary>
        /// Authors of the <see cref="Mod"/>.
        /// </summary>
        private ModAuthor[] _authors;

        /// <summary>
        /// Dependencies of the <see cref="Mod"/>.
        /// </summary>
        private ModDependency[] _dependencies;

        /// <summary>
        /// Assets associated with the <see cref="Mod"/>.
        /// </summary>
        private Dictionary<ulong, ModAsset> _assets;

        #endregion

        #region property

        /// <summary>
        /// ID of the <see cref="Mod"/>. This will be different every time the application is launched.
        /// </summary>
        public int id => (int)(_guidIdentifier >> 32);

        /// <inheritdoc cref="_name"/>
        public string name => _name;

        /// <inheritdoc cref="_displayName"/>
        public string displayName => _displayName;

        /// <inheritdoc cref="_description"/>
        public string description => _description;

        /// <inheritdoc cref="_version"/>
        public Version version => _version;

        /// <summary>
        /// Total number of dependencies that the <see cref="Mod"/> has.
        /// </summary>
        public int DependencyCount => _dependencies.Length;

        /// <summary>
        /// Total number of authors that the <see cref="Mod"/> has.
        /// </summary>
        public int AuthorCount => _authors.Length;

        /// <summary>
        /// Total number of assets that are part of the <see cref="Mod"/>.
        /// </summary>
        public int AssetCount => _assets.Count;

        #endregion

        #region constructor

        internal Mod(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsDirectory) throw new ArgumentException($"{nameof(fsr)} is not a directory.");
            this.fsr = fsr;
            // load manifest:
            JObject manifest = ReadManifest(fsr);
            if (manifest == null) throw new FormatException("No manifest file found.");
            // assign mod name:
            _name = (string)manifest["name"]; // read mod name
            if (!ValidateName(_name)) throw new FormatException("Invalid mod name.");
            int modId = _name.GetHashCode();
            if (ModDictionary.ContainsKey(modId)) throw new Exception("Duplicate mod with same name already exists.");
            string directoryName = fsr.DirectoryName;
            if (!_name.Equals(directoryName, StringComparison.OrdinalIgnoreCase)) throw new Exception(
                $"Mod name does not match mod directory name (mod_name: `{_name}`, directory_name: `{directoryName}`)."
            );
            // calculate guid mask:
            _guidIdentifier = ((ulong)modId & ModAssetGUIDMask) << 32;
            // create assets dictionary:
            _assets = new Dictionary<ulong, ModAsset>();
            // reload mod content:
            Reload(manifest);
            // register mod:
            ModDictionary[modId] = this; // add to mod dictionary
        }

        #endregion

        #region logic

        #region Register

        /// <summary>
        /// Register the mod.
        /// </summary>
        public void Register() {

        }

        #endregion

        #region Unregister

        /// <summary>
        /// Unregisters the mod (unloads from memory).
        /// </summary>
        /// <param name="validate">When <c>true</c>, other mods will be validated after this <see cref="Mod"/> is unregistered.</param>
        public void Unregister(in bool validate) {
            if (ModDictionary.Remove(id)) {
                ModImporter.ConsoleFormatter.Info($"Unloaded mod `{_name}`.");
                if (validate) ValidateMods();
            }
        }

        #endregion

        #region Reload

        /// <summary>
        /// Reloads the mod.
        /// </summary>
        public void Reload() {
            FileSystemReference fsr = ModImporter.GetModFSR(name);
            JObject manifest = ReadManifest(fsr);
            Reload(manifest);
        }

        /// <summary>
        /// Reloads the mod from a JSON <paramref name="manifest"/>.
        /// </summary>
        private void Reload(JObject manifest) {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            // read basic mod information:
            _version = Version.Parse((string)manifest["version"]);
            _displayName = (string)manifest["displayName"];
            _description = (string)manifest["description"];
            // read authors:
            JArray jsonAuthors = (JArray)manifest["authors"];
            if (jsonAuthors != null) {
                int authorCount = jsonAuthors.Count;
                _authors = new ModAuthor[authorCount];
                JToken author;
                for (int i = 0; i < authorCount; i++) {
                    author = jsonAuthors[i];
                    _authors[i] = new ModAuthor(
                        (string)author["name"],
                        (string)author["email"],
                        (string)author["url"]
                    );
                }
            } else {
                _authors = new ModAuthor[0];
            }
            // read dependencies:
            JObject jsonDependencies = (JObject)manifest["dependencies"];
            if (jsonDependencies != null) {
                List<int> dependencyTracker = new List<int>();
                List<ModDependency> dependencies = new List<ModDependency>();
                foreach (JProperty dependency in jsonDependencies.Children<JProperty>()) {
                    string dependencyName = dependency.Name.ToLower();
                    int dependencyHashCode = dependencyName.GetHashCode();
                    if (dependencyTracker.Contains(dependencyHashCode)) throw new FormatException(
                        $"Multiple dependency entries for dependency `{dependencyName}`"
                    );
                    dependencies.Add(
                        new ModDependency(
                            dependency.Name,
                            Version.Parse((string)dependency.Value)
                        )
                    );
                    dependencyTracker.Add(dependencyHashCode);
                }
                _dependencies = dependencies.ToArray();
            } else {
                _dependencies = new ModDependency[0];
            }
            // reload content:
            ReloadAssets();
        }

        #endregion

        #region ReloadAssets

        /// <summary>
        /// Reloads the mod content.
        /// </summary>
        private void ReloadAssets() {
            // remove existing content:
            RemoveExistingAssets();
            // find absolute path length:
            string absolutePath = fsr.AbsolutePath;
            int absolutePathLength = absolutePath.Length;
            // discover assets:
            OrderedList<ModAssetType, ModAsset> assets = new OrderedList<ModAssetType, ModAsset>();
            DiscoverAssets(fsr, absolutePathLength, assets);
            ModImporter.ConsoleFormatter.Info($"Mod `{_name}` discovered {assets.Count} assets.");
            // import assets:
            ImportAssets(assets);
        }

        #endregion

        #region RemoveExistingAssets

        /// <summary>
        /// Removes any existing assets from the mod.
        /// </summary>
        private void RemoveExistingAssets() {
            // dispose of each asset:
            foreach (ModAsset asset in _assets.Values) {
                try {
                    asset.Dispose();
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Mod `{_name}` failed to dispose of asset `{asset.name}`.",
                        exception
                    );
                }
            }
            // clear assets dictionary:
            _assets.Clear();
        }

        #endregion

        #region DiscoverAssets

        /// <summary>
        /// Discovers assets inside the specified <paramref name="fsr"/> and adds them to the <paramref name="assets"/> list.
        /// </summary>
        private void DiscoverAssets(in FileSystemReference fsr, in int fsrNameStartIndex, in OrderedList<ModAssetType, ModAsset> assets) {
            // discover files:
            FileSystemReference file;
            FileSystemReference[] files = fsr.GetFiles();
            for (int i = files.Length - 1; i >= 0; i--) {
                file = files[i];
                string fileName = file.FileName;
                if (fileName[0] == '.' || fileName.Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue; // skip hidden files and the manifest file
                try {
                    ModAsset asset = new ModAsset(this, file, fsrNameStartIndex);
                    if (asset.type != ModAssetType.None) {
                        assets.Add(asset.type, asset);
                    }
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Mod `{_name}` failed to discover asset `{file.AbsolutePath[fsrNameStartIndex..]}`",
                        exception
                    );
                }
            }
            // discover directories:
            FileSystemReference directory;
            FileSystemReference[] directories = fsr.GetDirectories();
            for (int i = directories.Length - 1; i >= 0; i--) {
                directory = directories[i];
                if (directory.DirectoryName[0] == '.') continue; // skip hidden directories
                DiscoverAssets(directories[i], fsrNameStartIndex, assets);
            }
        }

        #endregion

        #region ImportAssets

        private void ImportAssets(in OrderedList<ModAssetType, ModAsset> assets) {
            int assetCount = assets.Count;
            ModAsset asset;
            int failCount = 0;
            for (int i = 0; i < assetCount; i++) {
                asset = assets[i];
                try {
                    asset.Import();
                    _assets[asset.guid] = asset;
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error($"Mod `{_name}` failed to import asset `{asset.name}`.", exception);
                    failCount++;
                }
            }
            ModImporter.ConsoleFormatter.Info($"Mod `{_name}` imported {assetCount - failCount}/{assetCount} assets.");
        }

        #endregion

        #region Validate

        private bool Validate() {
            // validate name:
            if (!ValidateName(_name)) {
                ModImporter.ConsoleFormatter.Error($"Mod `{_name}` has an invalid name.");
                return false;
            }
            // validate dependencies:
            int dependencyCount = _dependencies.Length;
            if (dependencyCount > 0) {
                List<string> missingDependencies = new List<string>();
                ModDependency dependencyInfo;
                for (int i = dependencyCount - 1; i >= 0; i--) {
                    dependencyInfo = _dependencies[i];
                    if (!ModDictionary.TryGetValue(dependencyInfo.name.GetHashCode(), out Mod dependency) || dependency.version < dependencyInfo.version) {
                        missingDependencies.Add(dependencyInfo.ToString());
                    }
                }
                int missingDependencyCount = missingDependencies.Count;
                if (missingDependencyCount > 0) {
                    StringBuilder errorMessage = new StringBuilder("Mod `")
                        .Append(_name)
                        .Append("` is missing ")
                        .Append(missingDependencyCount)
                        .Append(missingDependencyCount == 1 ? " dependency: " : " dependencies: ")
                        .Append(missingDependencies[0]);
                    for (int i = 1; i < missingDependencyCount; i++) {
                        errorMessage.Append(", ").Append(missingDependencies[i]);
                    }
                    ModImporter.ConsoleFormatter.Error(errorMessage.ToString());
                    return false;
                }
            }
            // all tests passed:
            return true;
        }

        #endregion

        #region ToString

        public sealed override string ToString() => $"{_name} {version} [{id.ToHex()}]: {_assets.Count}a {_dependencies.Length}d";

        #endregion

        #region ReadManifest

        /// <summary>
        /// Finds, reads, and parses the specified mod manifest file in the <paramref name="modDirectory"/>.
        /// </summary>
        private static JObject ReadManifest(in FileSystemReference modDirectory) {
            if (modDirectory == null) throw new ArgumentNullException(nameof(modDirectory));
            if (!modDirectory.IsDirectory) throw new ArgumentException($"{nameof(modDirectory)} is not a directory.");
            // find absolute path to mod directory:
            string absolutePath = modDirectory.AbsolutePath;
            // load mod manifest:
            FileSystemReference manifestReference = new FileSystemReference(absolutePath + ManifestFileName, false, false);
            if (FileSystem.Read(manifestReference, out string manifestString, FileFormat.Standard)) {
                return JObject.Parse(manifestString);
            } else {
                throw new IOException($"Failed to read mod manifest file at `{manifestReference.AbsolutePath}`.");
            }
        }

        #endregion

        #region ValidateName

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="name"/> is a valid <see cref="Mod"/> name.
        /// </returns>
        public static bool ValidateName(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return name.Length > 0 && name.Length <= MaxModNameLength && ModNameRegex.IsMatch(name);
        }

        #endregion

        #region ValidateMods

        /// <summary>
        /// Validates imported mods and removes any mods with missing dependencies.
        /// </summary>
        public static void ValidateMods() {
            bool validate;
            int modCount = ModDictionary.Count;
            int failCount = 0;
            do {
                validate = false;
                foreach (Mod mod in ModDictionary.Values) { // iterate each loaded mod
                    if (!mod.Validate()) {
                        mod.Unregister(false);
                        validate = true;
                        failCount++;
                        break;
                    }
                }
            } while (validate);
            int validatedCount = modCount - failCount;
            ModImporter.ConsoleFormatter.Info($"{validatedCount}/{modCount} mods validated; {failCount} {(failCount == 1 ? "mod" : "mods")} failed.");
        }

        #endregion

        #region IsModLoaded

        /// <returns>
        /// Returns <c>true</c> if a mod with the specified <paramref name="name"/> has been loaded.
        /// </returns>
        public static bool IsModLoaded(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return ModDictionary.ContainsKey(name.GetHashCode());
        }

        #endregion

        #region GetMod

        /// <returns>
        /// Returns the loaded <see cref="Mod"/> with specified <paramref name="name"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if there is not a <see cref="Mod"/> with the specified <paramref name="name"/> loaded into memory.
        /// </exception>
        public static Mod GetMod(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return GetMod(name.GetHashCode());
        }

        /// <returns>
        /// Returns the loaded <see cref="Mod"/> with specified <paramref name="id"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if there is not a <see cref="Mod"/> with the specified <paramref name="id"/> loaded into memory.
        /// </exception>
        public static Mod GetMod(in int id) => ModDictionary.TryGetValue(id, out Mod mod) ? mod : throw new KeyNotFoundException(id.ToHex());

        #endregion

        #region TryGetMod

        /// <returns>
        /// Returns <c>true</c> if a <see cref="Mod"/> with the specified <paramref name="name"/> was found; otherwise
        /// <c>false</c> is returned.
        /// </returns>
        public static bool TryGetMod(in string name, out Mod mod) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return TryGetMod(name.GetHashCode(), out mod);
        }

        public static bool TryGetMod(in int id, out Mod mod) => ModDictionary.TryGetValue(id, out mod);

        #endregion

        #region GetAssetGUID

        public ulong GetAssetGUID(in string assetName) => _guidIdentifier | ((ulong)assetName.GetHashCode() & ModAssetGUIDMask);

        public static ulong GetAssetGUID(in int modId, in string assetName) => (((ulong)modId & ModAssetGUIDMask) << 32) | ((ulong)assetName.GetHashCode() & ModAssetGUIDMask);

        #endregion

        #region GetAsset

        public ModAsset GetAsset(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return GetAsset(_guidIdentifier & ((ulong)name.GetHashCode() & ModAssetGUIDMask));
        }

        public ModAsset GetAsset(in ulong guid) {
            if (_assets.TryGetValue(guid, out ModAsset value)) {
                return value;
            } else {
                throw new KeyNotFoundException(guid.ToHex());
            }
        }

        #endregion

        #region TryGetAsset

        public bool TryGetAsset(in string name, out ModAsset asset) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return TryGetAsset(_guidIdentifier & ((ulong)name.GetHashCode() & ModAssetGUIDMask), out asset);
        }

        private bool InternalTryGetAsset(in ulong guid, out ModAsset asset) => _assets.TryGetValue(guid, out asset);

        public static bool TryGetAsset(in string modName, in string assetName, out ModAsset asset) {
            int modId = modName.GetHashCode();
            if (ModDictionary.TryGetValue(modId, out Mod mod)) {
                return mod.TryGetAsset(assetName, out asset);
            } else {
                asset = null;
                return false;
            }
        }

        public static bool TryGetAsset(in ulong guid, out ModAsset asset) {
            int modId = (int)((guid & ModGUIDMask) >> 32);
            if (ModDictionary.TryGetValue(modId, out Mod mod)) {
                return mod._assets.TryGetValue(guid, out asset);
            } else {
                asset = null;
                return false;
            }
        }

        #endregion

        #endregion

    }

}