using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Version = BlackTundra.Foundation.Version;
using Colour = BlackTundra.Foundation.ConsoleColour;

namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes a mod loaded in at runtime.
    /// </summary>
    public sealed class ModInstance {

        #region constant

        /// <summary>
        /// Mod <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        internal static readonly Dictionary<int, ModInstance> ModDictionary = new Dictionary<int, ModInstance>();

        /// <summary>
        /// A <see cref="PackedBuffer{T}"/> that contains a reference to every <see cref="ModInstance"/>. The order that each <see cref="ModInstance"/> appears in
        /// this list is dependent on which <see cref="ModInstance"/> names are included in the <see cref="_processAfter"/> array for each <see cref="ModInstance"/>.
        /// </summary>
        private static readonly PackedBuffer<ModInstance> ModProcessingBuffer = new PackedBuffer<ModInstance>(0);

        /// <summary>
        /// Regex pattern used for matching a mod name.
        /// </summary>
        public const string ModNameRegexPattern = @"[a-z0-9_\-\.]+";

        /// <summary>
        /// Compiled regex pattern matcher used for testing if a mod name is valid.
        /// </summary>
        public static readonly Regex ModNameRegex = new Regex(
            ModNameRegexPattern,
            RegexOptions.Compiled | RegexOptions.Singleline
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
        /// Index of the an absolute path of a child <see cref="ModAsset"/> to start reading the <see cref="ModAsset.path"/>.
        /// </summary>
        internal int childModAssetFsrPathStartIndex;

        /// <summary>
        /// Name of the <see cref="ModInstance"/>.
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// Part of the GUID that makes up an assets unique GUID.
        /// </summary>
        public readonly ulong _guidIdentifier;

        /// <summary>
        /// Version of the <see cref="ModInstance"/>.
        /// </summary>
        private Version _version;

        /// <summary>
        /// Display name of the <see cref="ModInstance"/>.
        /// </summary>
        private string _displayName;

        /// <summary>
        /// Description of the <see cref="ModInstance"/>.
        /// </summary>
        private string _description;

        /// <summary>
        /// Authors of the <see cref="ModInstance"/>.
        /// </summary>
        private ModAuthor[] _authors;

        /// <summary>
        /// Dependencies of the <see cref="ModInstance"/>.
        /// </summary>
        private ModDependency[] _dependencies;

        /// <summary>
        /// Array that contains a list of mods to process this mod after.
        /// </summary>
        internal string[] _processAfter;

        /// <summary>
        /// Assets associated with the <see cref="ModInstance"/>.
        /// </summary>
        internal readonly Dictionary<ulong, ModAsset> _assets;

        #endregion

        #region property

        /// <summary>
        /// ID of the <see cref="ModInstance"/>. This will be different every time the application is launched.
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
        /// Total number of dependencies that the <see cref="ModInstance"/> has.
        /// </summary>
        public int DependencyCount => _dependencies.Length;

        /// <summary>
        /// Total number of authors that the <see cref="ModInstance"/> has.
        /// </summary>
        public int AuthorCount => _authors.Length;

        /// <summary>
        /// Total number of assets that are part of the <see cref="ModInstance"/>.
        /// </summary>
        public int AssetCount => _assets.Count;

        #endregion

        #region constructor

        internal ModInstance(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsDirectory) throw new ArgumentException($"{nameof(fsr)} is not a directory.");
            this.fsr = fsr;
            string fsrAbsolutePath = fsr.AbsolutePath;
            childModAssetFsrPathStartIndex = fsrAbsolutePath.Length;
            // load manifest:
            JObject manifest = ReadManifest(fsr);
            if (manifest == null) throw new FormatException("No manifest file found.");
            // assign mod name:
            string modName = (string)manifest["name"]; // read mod name
            if (modName == null) throw new FormatException("Mod does manifest not have a name.");
            _name = modName.ToLower();
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
            // reload manifest:
            ReloadManifest(manifest);
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
        /// <param name="updateOtherMods">When <c>true</c>, other mods will be validated after this <see cref="ModInstance"/> is unregistered.</param>
        private void Unregister(in bool updateOtherMods) {
            if (ModDictionary.Remove(id)) {
                ModImporter.ConsoleFormatter.Info($"Unloaded mod `{_name}`.");
                if (updateOtherMods) {
                    RecalculateModProcessingOrder();
                    ValidateDependencies();
                }
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
            ReloadManifest(manifest);
            RecalculateModProcessingOrder();
            ValidateDependencies();
            ReloadAssets();
        }

        #endregion

        #region ReloadManifest

        /// <summary>
        /// Reloads the mod from a JSON <paramref name="manifest"/>.
        /// </summary>
        private void ReloadManifest(JObject manifest) {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            JToken tempToken;
            // read basic mod information:
            _version = Version.Parse((string)manifest["version"]);
            _displayName = manifest.TryGetValue("displayName", StringComparison.OrdinalIgnoreCase, out tempToken) ? (string)tempToken : _name;
            _description = manifest.TryGetValue("description", StringComparison.OrdinalIgnoreCase, out tempToken) ? (string)tempToken : string.Empty;
            // read authors:
            if (manifest.TryGetValue("authors", StringComparison.CurrentCultureIgnoreCase, out tempToken)) {
                JArray authorArray = (JArray)tempToken;
                int authorCount = authorArray.Count;
                _authors = new ModAuthor[authorCount];
                JObject author;
                for (int i = 0; i < authorCount; i++) {
                    author = (JObject)authorArray[i];
                    _authors[i] = new ModAuthor(
                        author.TryGetValue("name", StringComparison.OrdinalIgnoreCase, out tempToken) ? (string)tempToken : "Unknown",
                        author.TryGetValue("email", StringComparison.OrdinalIgnoreCase, out tempToken) ? (string)tempToken : "None",
                        author.TryGetValue("url", StringComparison.OrdinalIgnoreCase, out tempToken) ? (string)tempToken : "None"
                    );
                }
            } else {
                _authors = new ModAuthor[0];
            }
            // read dependencies:
            if (manifest.TryGetValue("dependencies", StringComparison.OrdinalIgnoreCase, out tempToken)) {
                List<int> dependencyTracker = new List<int>();
                List<ModDependency> dependencies = new List<ModDependency>();
                foreach (JProperty dependency in tempToken.Children<JProperty>()) {
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
            // process process order instructions:
            if (manifest.TryGetValue("processAfter", StringComparison.OrdinalIgnoreCase, out tempToken)) {
                JArray processAfterArray = (JArray)tempToken;
                int processAfterCount = processAfterArray.Count;
                _processAfter = new string[processAfterCount];
                for (int i = 0; i < processAfterCount; i++) {
                    _processAfter[i] = ((string)processAfterArray[i]).ToLower();
                }
            } else {
                _processAfter = new string[0];
            }
        }

        #endregion

        #region ReloadAssets

        /// <summary>
        /// Reloads the mod content.
        /// </summary>
        public void ReloadAssets() {
            // remove existing content:
            UnloadAssets();
            // find absolute path length:
            string fsrAbsolutePath = fsr.AbsolutePath;
            childModAssetFsrPathStartIndex = fsrAbsolutePath.Length;
            // discover assets:
            OrderedList<ModAssetType, ModAsset> assets = new OrderedList<ModAssetType, ModAsset>();
            DiscoverAssets(fsr, assets);
            ModImporter.ConsoleFormatter.Info($"Mod `{_name}` discovered {assets.Count} assets.");
            // import assets:
            ImportAssets(assets);
        }

        #endregion

        #region ReloadAllAssets

        /// <summary>
        /// Reloads the assets for every mod.
        /// </summary>
        internal static void ReloadAllAssets() {
            int modCount = ModProcessingBuffer.Count;
            for (int i = 0; i < modCount; i++) {
                ModInstance mod = ModProcessingBuffer[i];
                try {
                    mod.ReloadAssets();
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Failed to import assets for mod `{mod.name}`.",
                        exception
                    );
                }
            }
        }

        #endregion

        #region Unload

        public void Unload() {
            UnloadAssets();
            Unregister(true);
        }

        #endregion

        #region UnloadAssets

        /// <summary>
        /// Removes any existing assets from the mod.
        /// </summary>
        public void UnloadAssets() {
            // dispose of each asset:
            foreach (ModAsset asset in _assets.Values) {
                try {
                    asset.Dispose();
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Mod `{_name}` failed to dispose of asset `{asset.path}`.",
                        exception
                    );
                }
            }
            // clear assets dictionary:
            _assets.Clear();
        }

        #endregion

        #region UnloadAll

        public static void UnloadAll() {
            UnloadAllAssets();
            ModProcessingBuffer.Clear(0);
            ModDictionary.Clear();
        }

        #endregion

        #region UnloadAllAssets

        public static void UnloadAllAssets() {
            ModInstance mod;
            for (int i = ModProcessingBuffer.Count - 1; i >= 0; i--) {
                mod = ModProcessingBuffer[i];
                mod.UnloadAssets();
            }
        }

        #endregion

        #region DiscoverAssets

        /// <summary>
        /// Discovers assets inside the specified <paramref name="fsr"/> and adds them to the <paramref name="assets"/> list.
        /// </summary>
        private void DiscoverAssets(in FileSystemReference fsr, in OrderedList<ModAssetType, ModAsset> assets) {
            // discover files:
            FileSystemReference file;
            FileSystemReference[] files = fsr.GetFiles();
            for (int i = files.Length - 1; i >= 0; i--) {
                file = files[i];
                string fileName = file.FileName;
                if (fileName[0] == '.' || fileName.Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue; // skip hidden files and the manifest file
                try {
                    ModAsset asset = ModAssetFactory.Create(this, file);
                    if (asset.type != ModAssetType.None) {
                        assets.Add(asset.type, asset);
                    }
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Mod `{_name}` failed to discover asset `{file.AbsolutePath[childModAssetFsrPathStartIndex..]}`",
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
                DiscoverAssets(directories[i], assets);
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
                    // import the asset:
                    asset.Import();
                    // validate imported asset:
                    if (asset.value == null) { // asset has no value
                        ModImporter.ConsoleFormatter.Warning(
                            $"Nothing imported for asset `{asset.path}`; the asset file extension is likely not supported, consider removing the file from the mod or using a `.` character prefix to indicate the file should be ignored."
                        );
                        failCount++;
                    } else if (_assets.ContainsKey(asset.guid)) { // asset with the same GUID already exists
                        ModImporter.ConsoleFormatter.Error(
                            $"Failed to import asset `{asset.path}` because an asset with the same GUID ({asset.guid}) already exists; ensure the asset path is unique."
                        );
                        failCount++;
                    } else { // the guid is unique
                        // register the asset:
                        _assets[asset.guid] = asset;
                    }
                } catch (Exception exception) {
                    ModImporter.ConsoleFormatter.Error(
                        $"Mod `{_name}` failed to import asset `{asset.path}`.",
                        exception
                    );
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
                    if (!ModDictionary.TryGetValue(dependencyInfo.name.GetHashCode(), out ModInstance dependency) || dependency.version < dependencyInfo.version) {
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
        /// Returns <c>true</c> if the <paramref name="name"/> is a valid <see cref="ModInstance"/> name.
        /// </returns>
        public static bool ValidateName(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return name.Length > 0 && name.Length <= MaxModNameLength && ModNameRegex.IsMatch(name);
        }

        #endregion

        #region ValidateDependencies

        /// <summary>
        /// Validates imported mods and removes any mods with missing dependencies.
        /// </summary>
        public static void ValidateDependencies() {
            bool validate;
            int modCount = ModDictionary.Count;
            int failCount = 0;
            do {
                validate = false;
                foreach (ModInstance mod in ModDictionary.Values) { // iterate each loaded mod
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

        #region RecalculateModProcessingOrder

        /// <summary>
        /// Recalculates the order that mods should ideally be processed in.
        /// </summary>
        internal static void RecalculateModProcessingOrder() {
            ModInstance[] mods = ModDictionary.Values.ToArray(); // convert mod dictionary to array
            mods.Sort((lhs, rhs) => rhs._processAfter.Contains(lhs.name) ? -1 : 1); // order array based off of process after array
            ModProcessingBuffer.Clear(mods); // reset buffer
        }

        #endregion

        #region IsModLoaded

        /// <returns>
        /// Returns <c>true</c> if a mod with the specified <paramref name="modName"/> has been loaded.
        /// </returns>
        public static bool IsModLoaded(in string modName) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            return ModDictionary.ContainsKey(modName.ToLower().GetHashCode());
        }

        #endregion

        #region GetModID

        public static int GetModID(in string modName) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            return modName.ToLower().GetHashCode();
        }

        public static int GetModID(in ulong assetGuid) => (int)((assetGuid & ModGUIDMask) >> 32);

        #endregion

        #region GetMod

        /// <returns>
        /// Returns the loaded <see cref="ModInstance"/> with specified <paramref name="modName"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if there is not a <see cref="ModInstance"/> with the specified <paramref name="modName"/> loaded into memory.
        /// </exception>
        public static ModInstance GetMod(in string modName) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            return GetMod(modName.ToLower().GetHashCode());
        }

        /// <returns>
        /// Returns the loaded <see cref="ModInstance"/> with specified <paramref name="modId"/>.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if there is not a <see cref="ModInstance"/> with the specified <paramref name="modId"/> loaded into memory.
        /// </exception>
        public static ModInstance GetMod(in int modId) => ModDictionary.TryGetValue(modId, out ModInstance mod) ? mod : throw new KeyNotFoundException(modId.ToHex());

        #endregion

        #region TryGetMod

        /// <returns>
        /// Returns <c>true</c> if a <see cref="ModInstance"/> with the specified <paramref name="modName"/> was found; otherwise
        /// <c>false</c> is returned.
        /// </returns>
        public static bool TryGetMod(in string modName, out ModInstance mod) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            return TryGetMod(modName.ToLower().GetHashCode(), out mod);
        }

        public static bool TryGetMod(in int modId, out ModInstance mod) => ModDictionary.TryGetValue(modId, out mod);

        #endregion

        #region GetAssetGUID

        public ulong GetAssetGUID(in string assetPath) {
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            return _guidIdentifier | ((ulong)assetPath.ToLower().GetHashCode() & ModAssetGUIDMask);
        }

        public static ulong GetAssetGUID(in int modId, in string assetPath) {
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            return (((ulong)modId & ModAssetGUIDMask) << 32) | ((ulong)assetPath.GetHashCode() & ModAssetGUIDMask);
        }

        public static ulong GetAssetGUID(in string modName, in string assetPath) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            return (((ulong)modName.ToLower().GetHashCode() & ModAssetGUIDMask) << 32) | ((ulong)assetPath.GetHashCode() & ModAssetGUIDMask);
        }

        #endregion

        #region GetAsset

        public ModAsset GetAsset(in string assetPath) {
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            return GetAsset(GetAssetGUID(assetPath));
        }

        public ModAsset GetAsset(in ulong assetGuid) => _assets.TryGetValue(assetGuid, out ModAsset value)
            ? value
            : throw new KeyNotFoundException(assetGuid.ToHex());

        public static ModAsset GetAsset(in string modName, in string assetPath) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            int modId = GetModID(modName);
            return ModDictionary.TryGetValue(GetModID(modName), out ModInstance mod) ? mod.GetAsset(assetPath) : throw new KeyNotFoundException(modName);
        }

        public static ModAsset GetAsset(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            string absolutePath = fsr.AbsolutePath;
            string localPath = absolutePath[ModImporter.ModDirectoryFSRLength..];
            int modNameSeparator = localPath.IndexOf('/');
            string modName = localPath[..modNameSeparator];
            string assetPath = localPath[(modNameSeparator + 1)..];
            return GetAsset(modName, assetPath);
        }

        #endregion

        #region TryGetAsset

        public bool TryGetAsset(in string assetPath, out ModAsset asset) {
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            return _assets.TryGetValue(GetAssetGUID(assetPath), out asset);
        }

        public static bool TryGetAsset(in string modName, in string assetPath, out ModAsset asset) {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            if (assetPath == null) throw new ArgumentNullException(nameof(assetPath));
            int modId = GetModID(modName);
            if (ModDictionary.TryGetValue(modId, out ModInstance mod)) {
                return mod.TryGetAsset(assetPath, out asset);
            } else {
                asset = null;
                return false;
            }
        }

        public static bool TryGetAsset(in ulong assetGuid, out ModAsset asset) {
            int modId = GetModID(assetGuid);
            if (ModDictionary.TryGetValue(modId, out ModInstance mod)) {
                return mod._assets.TryGetValue(assetGuid, out asset);
            } else {
                asset = null;
                return false;
            }
        }

        public static bool TryGetAsset(in FileSystemReference fsr, out ModAsset asset) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            string absolutePath = fsr.AbsolutePath;
            string localPath = absolutePath[ModImporter.ModDirectoryFSRLength..];
            int modNameSeparator = localPath.IndexOf('/');
            string modName = localPath[..modNameSeparator];
            string assetPath = localPath[(modNameSeparator + 1)..];
            return TryGetAsset(modName, assetPath, out asset);
        }

        #endregion

        #region GetAssetFromPath

        public static ModAsset GetAssetFromPath(in string fullAssetPath) {
            if (fullAssetPath == null) throw new ArgumentNullException(nameof(fullAssetPath));
            string[] paths = fullAssetPath.Split('/', 2);
            return GetAsset(paths[0], paths[1]);
        }

        #endregion

        #region TryGetAssetFromPath

        public static bool TryGetAssetFromPath(in string fullAssetPath, out ModAsset asset) {
            if (fullAssetPath == null) throw new ArgumentNullException(nameof(fullAssetPath));
            string[] paths = fullAssetPath.Split('/', 2);
            return TryGetAsset(paths[0], paths[1], out asset);
        }

        #endregion

        #region ModCommand

        [Command(
            name: "mod",
            description: "Helps manage the importation of mods and management of imported mods.",
            usage:
            "mod list" +
                "\n\tDisplays a table of the currently imported mods." +
            "\nmod info {mod}" +
                "\n\tDisplays a detailed view of an imported mod." +
            "\nmod reload {mod}" +
                "\n\tReloads / Reimports a mod. The specified mod does not need to be imported to reload it." +
            "\nmod reload-all" +
                "\n\tReloads every mod (including unloaded mods)." +
            "\nmod unload {mod}" +
                "\n\tUnloads a currently loaded mod." +
            "\nmod unload-all" +
                "\n\tUnloads all currently loaded mods.",
            hidden: false
        )]
        private static bool ModCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                ConsoleWindow.Print("Must specify an argument; type `help mod` for help.");
                return false;
            } else {
                switch (info.args[0].ToLower()) {
                    case "list": {
                        if (argumentCount > 1) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                        int modCount = ModProcessingBuffer.Count;
                        if (modCount == 0) {
                            ConsoleWindow.Print("No mods are currently loaded.");
                            return true;
                        }
                        string[,] table = new string[7, modCount + 1];
                        table[0, 0] = $"<color=#{Colour.DarkGray.hex}>##</color>";
                        table[1, 0] = "ID";
                        table[2, 0] = "Name";
                        table[3, 0] = "Version";
                        table[4, 0] = "Display Name";
                        table[5, 0] = "Dependency Count";
                        table[6, 0] = "Asset Count";
                        ModInstance mod;
                        for (int i = 0; i < modCount;) {
                            mod = ModProcessingBuffer[i++];
                            table[0, i] = $"<color=#{Colour.DarkGray.hex}>{i:00}</color>";
                            table[1, i] = $"<color=#{Colour.Red.hex}>{mod.id.ToHex()}</color>";
                            table[2, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(mod._name)}</color>";
                            table[3, i] = ConsoleUtility.Escape(mod.version.ToString());
                            table[4, i] = ConsoleUtility.Escape(mod._displayName);
                            table[5, i] = ConsoleUtility.Escape(mod._dependencies.Length.ToString());
                            table[6, i] = ConsoleUtility.Escape(mod._assets.Count.ToString());
                        }
                        ConsoleWindow.PrintTable(table, true);
                        return true;
                    }
                    case "info": {
                        if (argumentCount == 1) {
                            ConsoleWindow.Print("A mod name must be specifed.");
                            return false;
                        } else if (argumentCount > 2) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 2));
                            return false;
                        }
                        string modName = info.args[1];
                        if (TryGetMod(modName, out ModInstance mod)) {
                            string[,] table; int tableIndex;
                            // mod details:
                            table = new string[,] {
                                { $"<color=#{Colour.Gray.hex}>ID</color>", mod.id.ToHex() },
                                { $"<color=#{Colour.Gray.hex}>Name</color>", ConsoleUtility.Escape(mod._name) },
                                { $"<color=#{Colour.Gray.hex}>Version</color>", ConsoleUtility.Escape(mod._version.ToString()) },
                                { $"<color=#{Colour.Gray.hex}>Display Name</color>", ConsoleUtility.Escape(mod._displayName) },
                                { $"<color=#{Colour.Gray.hex}>Description</color>", ConsoleUtility.Escape(mod._description) },
                            };
                            ConsoleWindow.Print("<b>Mod Details</b>");
                            ConsoleWindow.PrintTable(table, false, true);
                            // author details:
                            int authorCount = mod._authors.Length;
                            if (authorCount > 0) {
                                table = new string[3, authorCount + 1];
                                table[0, 0] = "Author Name";
                                table[1, 0] = "Email";
                                table[2, 0] = "URL";
                                ModAuthor author;
                                for (int i = 0; i < authorCount;) {
                                    author = mod._authors[i++];
                                    table[0, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(author.name ?? "Unknown")}</color>";
                                    table[1, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(author.email ?? "None")}</color>";
                                    table[2, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(author.url ?? "None")}</color>";
                                }
                                ConsoleWindow.Print(string.Empty);
                                //ConsoleWindow.Print(authorCount == 1 ? "<b>Author</b>" : "<b>Authors</b>");
                                ConsoleWindow.PrintTable(table, true);
                            }
                            // mod dependencies:
                            int dependencyCount = mod._dependencies.Length;
                            table = new string[2, dependencyCount];
                            ModDependency dependency;
                            for (int i = 0; i < dependencyCount; i++) {
                                dependency = mod._dependencies[i];
                                table[0, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(dependency.name)}</color>";
                                table[1, i] = ConsoleUtility.Escape(dependency.version.ToString());
                            }
                            ConsoleWindow.Print(string.Empty);
                            ConsoleWindow.Print("<b>Mod Dependencies</b>");
                            ConsoleWindow.PrintTable(table);
                            ConsoleWindow.Print($"<i>Total Dependencies: {dependencyCount}</i>");
                            // mod assets:
                            int assetCount = mod._assets.Count;
                            table = new string[4, assetCount + 1];
                            table[0, 0] = "GUID";
                            table[1, 0] = "Path";
                            table[2, 0] = "Import Type";
                            table[3, 0] = "Asset Type";
                            tableIndex = 1;
                            foreach (ModAsset asset in mod._assets.Values) {
                                table[0, tableIndex] = $"<color=#{Colour.Red.hex}>{asset.guid.ToHex()}</color>";
                                table[1, tableIndex] = ConsoleUtility.Escape(asset.path);
                                table[2, tableIndex] = ConsoleUtility.Escape(asset.type.ToString());
                                table[3, tableIndex] = ConsoleUtility.Escape(asset.value?.GetType().Name ?? $"<color=#{Colour.Purple.hex}>null</color>");
                                tableIndex++;
                            }
                            ConsoleWindow.Print(string.Empty);
                            ConsoleWindow.Print("<b>Mod Assets</b>");
                            ConsoleWindow.PrintTable(table, true);
                            ConsoleWindow.Print($"<i>Total Assets: {assetCount}</i>");
                            // mod processing order:
                            int processAfterCount = mod._processAfter.Length;
                            if (processAfterCount > 0) {
                                ConsoleWindow.Print(string.Empty);
                                ConsoleWindow.Print("<b>Process After</b>");
                                for (int i = 0; i < processAfterCount; i++) {
                                    ConsoleWindow.Print($"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(mod._processAfter[i])}</color>");
                                }
                            }
                            return true;
                        } else {
                            ConsoleWindow.Print($"Mod `{ConsoleUtility.Escape(modName)}` not found.");
                            return false;
                        }
                    }
                    case "reload": {
                        if (argumentCount > 2) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 2));
                            return false;
                        }
                        string modName = info.args[1];
                        if (TryGetMod(modName, out ModInstance mod)) {
                            mod.Reload();
                            ConsoleWindow.Print("Reload complete.");
                            return true;
                        } else {
                            ConsoleWindow.Print($"Mod `{ConsoleUtility.Escape(modName)}` not found.");
                            return false;
                        }
                    }
                    case "reload-all": {
                        if (argumentCount > 1) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                        ModImporter.ReimportAll();
                        ConsoleWindow.Print("Reload complete.");
                        return true;
                    }
                    case "unload": {
                        if (argumentCount > 2) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 2));
                            return false;
                        }
                        string modName = info.args[1];
                        if (TryGetMod(modName, out ModInstance mod)) {
                            mod.Unload();
                            ConsoleWindow.Print("Unload complete.");
                            return true;
                        } else {
                            ConsoleWindow.Print($"Mod `{ConsoleUtility.Escape(modName)}` not found.");
                            return false;
                        }
                    }
                    case "unload-all": {
                        if (argumentCount > 1) {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                        ModInstance.UnloadAll();
                        ConsoleWindow.Print("Unload complete.");
                        return true;
                    }
                    default: {
                        ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args[0], 0));
                        return false;
                    }
                }
            }
        }

        #endregion

        #endregion

    }

}