using BlackTundra.Foundation.IO;
using BlackTundra.ModFramework.Importers;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, Mod> ModDictionary = new Dictionary<string, Mod>();

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
        public const string LocalManifestPath = "manifest.json";

        /// <summary>
        /// Local path to expect to find mod assets.
        /// </summary>
        public const string LocalAssetPath = "Assets/";

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

        #endregion

        #region property

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
            if (ModDictionary.ContainsKey(_name)) throw new Exception("Duplicate mod with same name already exists.");
            string directoryName = fsr.DirectoryName;
            if (!_name.Equals(directoryName, StringComparison.OrdinalIgnoreCase)) throw new Exception(
                $"Mod name does not match mod directory name (mod_name: `{_name}`, directory_name: `{directoryName}`)."
            );
            // reload mod content:
            Reload(manifest);
            // register mod:
            ModDictionary[_name] = this; // add to mod dictionary
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
            if (ModDictionary.ContainsKey(_name)) {
                ModDictionary.Remove(_name);
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
                List<ModDependency> dependencies = new List<ModDependency>();
                foreach (JProperty dependency in jsonDependencies.Children<JProperty>()) {
                    dependencies.Add(
                        new ModDependency(
                            dependency.Name,
                            Version.Parse((string)dependency.Value)
                        )
                    );
                }
                _dependencies = dependencies.ToArray();
            } else {
                _dependencies = new ModDependency[0];
            }
            // reload content:
            ReloadContent();
        }

        #endregion

        #region ReloadContent

        /// <summary>
        /// Reloads the mod content.
        /// </summary>
        private void ReloadContent() {
            // implement here
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
                    if (!ModDictionary.TryGetValue(dependencyInfo.name, out Mod dependency) || dependency.version < dependencyInfo.version) {
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
            FileSystemReference manifestReference = new FileSystemReference(absolutePath + LocalManifestPath, false, false);
            FileSystem.Read(manifestReference, out string manifestString, FileFormat.Standard);
            return JObject.Parse(manifestString);
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
            return ModDictionary.ContainsKey(name);
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
            return ModDictionary.TryGetValue(name, out Mod mod) ? mod : throw new KeyNotFoundException(name);
        }

        #endregion

        #region TryGetMod

        /// <returns>
        /// Returns <c>true</c> if a <see cref="Mod"/> with the specified <paramref name="name"/> was found; otherwise
        /// <c>false</c> is returned.
        /// </returns>
        public static bool TryGetMod(in string name, out Mod mod) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return ModDictionary.TryGetValue(name, out mod);
        }

        #endregion

        #endregion

    }

}