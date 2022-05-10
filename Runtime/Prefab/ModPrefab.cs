using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.ModFramework.Utility;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.ModFramework.Prefab {

    public sealed class ModPrefab : ModAsset {

        #region constant

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(ModPrefab));

        #endregion

        #region constructor

        internal ModPrefab(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
            
        }

        #endregion

        #region logic

        #region CreateInstance

        public GameObject CreateInstance() {
            if (_asset != null && _asset is GameObject prefab) {
                GameObject gameObject = Object.Instantiate(prefab, null);
                gameObject.name = prefab.name;
                return gameObject;
            } else {
                throw new InvalidOperationException($"{nameof(_asset)} type is not type `{nameof(GameObject)}`.");
            }
        }

        public GameObject CreateInstance(in Transform parent) {
            if (_asset != null && _asset is GameObject prefab) {
                GameObject gameObject = Object.Instantiate(prefab, parent);
                gameObject.name = prefab.name;
                return gameObject;
            } else {
                throw new InvalidOperationException($"{nameof(_asset)} type is not type `{nameof(GameObject)}`.");
            }
        }

        public GameObject CreateInstance(in Vector3 position, in Quaternion rotation) {
            if (_asset != null && _asset is GameObject prefab) {
                GameObject gameObject = Object.Instantiate(prefab, position, rotation, null);
                gameObject.name = prefab.name;
                return gameObject;
            } else {
                throw new InvalidOperationException($"{nameof(_asset)} type is not type `{nameof(GameObject)}`.");
            }
        }

        public GameObject CreateInstance(in Vector3 position, in Quaternion rotation, in Transform parent) {
            if (_asset != null && _asset is GameObject prefab) {
                GameObject gameObject = Object.Instantiate(prefab, position, rotation, parent);
                gameObject.name = prefab.name;
                return gameObject;
            } else {
                throw new InvalidOperationException($"{nameof(_asset)} type is not type `{nameof(GameObject)}`.");
            }
        }

        #endregion

        #region Import

        protected internal sealed override void Import() {
            // dispose of existing asset:
            DisposeOfAsset();
            // read json file:
            if (!FileSystem.Read(fsr, out string jsonString, FileFormat.Standard)) throw new IOException($"Failed to read prefab JSON file at `{fsr}`.");
            // parse json file contents:
            JObject json = JObject.Parse(jsonString);
            // import prefab asset:
            _asset = ParseJsonPrefab(json, PrefabManager.CacheTransform);
        }

        #endregion

        #region ParseJsonPrefab

        /// <summary>
        /// Parses <paramref name="json"/> to a <see cref="GameObject"/> prefab.
        /// </summary>
        private static GameObject ParseJsonPrefab(in JObject json, in Transform parent) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            // get object name:
            string name = (string)json["name"];
            // get object layer:
            int layer;
            if (json.TryGetValue("layer", StringComparison.OrdinalIgnoreCase, out JToken layerJson)) {
                layer = layerJson.Type switch {
                    JTokenType.Integer => (int)layerJson,
                    JTokenType.String => LayerMask.NameToLayer((string)layerJson),
                    _ => throw new FormatException($"Invalid layer type `{layerJson.Type}`.")
                };
            } else {
                layer = 0;
            }
            // get object tag:
            string tag;
            if (json.TryGetValue("tag", StringComparison.OrdinalIgnoreCase, out JToken tagJson)) {
                tag = (string)tagJson;
                if (tag.Length == 0 || tag.Equals("untagged", StringComparison.OrdinalIgnoreCase)) {
                    tag = "Untagged";
                }
            } else {
                tag = "Untagged";
            }
            // get object static:
            bool isStatic = json.TryGetValue("static", StringComparison.OrdinalIgnoreCase, out JToken staticJson) && (bool)staticJson;
            // find components:
            int componentCount;
            JObject[] jsonComponents;
            Type[] componentTypes;
            if (json.TryGetValue("components", StringComparison.OrdinalIgnoreCase, out JToken componentsJson)) {
                JArray componentArray = (JArray)componentsJson;
                componentCount = componentArray.Count;
                jsonComponents = new JObject[componentCount];
                componentTypes = new Type[componentCount];
                int index = 0;
                foreach (JToken component in componentArray) {
                    jsonComponents[index] = (JObject)component;
                    componentTypes[index] = ObjectUtility.FindType((string)component["type"]);
                    index++;
                }
            } else {
                componentCount = 0;
                jsonComponents = new JObject[0];
                componentTypes = new Type[0];
            }
            // create object:
            GameObject gameObject = new GameObject(
                name, componentTypes.Remove(typeof(Transform), true)
            ) {
                layer = layer,
                tag = tag,
                isStatic = isStatic
            };
            gameObject.transform.parent = parent;
            // initialise components:
            if (componentCount > 0) {
                int componentIndex = 0; // track position through components array
                Component[] components = gameObject.GetComponents<Component>(); // get all components on the object
                // define temporary variables:
                Type targetType, currentType;
                JObject jsonComponent, jsonProperties;
                // iterate components:
                for (int i = 0; i < componentCount; i++) {
                    // get a reference to the current json component:
                    jsonComponent = jsonComponents[i];
                    // find target component type:
                    targetType = componentTypes[i];
                    // find next component matching the target type:
                    do {
                        currentType = components[componentIndex].GetType();
                    } while (currentType != targetType && ++componentIndex < components.Length);
                    if (currentType != targetType) { // target component type not found, this must mean the target component was not added to the object; stop here
                        ConsoleFormatter.Warning(
                            $"Failed to find component of type `{targetType}` on object `{name}`."
                        );
                        break;
                    }
                    // get reference to component:
                    Component component = components[componentIndex];
                    // check for enabled property in json:
                    if (jsonComponent.TryGetValue("enabled", StringComparison.OrdinalIgnoreCase, out JToken enabledJson) && component is Behaviour behaviour) {
                        behaviour.enabled = (bool)enabledJson;
                    }
                    // process properties:
                    if (jsonComponent.TryGetValue("properties", StringComparison.OrdinalIgnoreCase, out JToken propertiesJson)) { // has properties
                        jsonProperties = (JObject)propertiesJson;
                        foreach (JProperty property in jsonProperties.Children<JProperty>()) { // iterate each property defined in the JSON
                            string propertyName = property.Name; // get the name of the property
                            PropertyInfo propertyInfo = targetType.GetProperty(propertyName); // search for the property
                            if (propertyInfo == null) { // no property found
                                ConsoleFormatter.Warning(
                                    $"Failed to find property `{propertyName}` on component `{targetType}` on object `{name}`."
                                );
                                continue; // ignore this property
                            }
                            if (!propertyInfo.CanWrite) { // cannot write to property
                                ConsoleFormatter.Warning(
                                    $"Cannot write to property `{propertyName}` on component `{targetType}` on object `{name}`."
                                );
                                continue; // ignore this property
                            }
                            // get property type:
                            Type propertyType = propertyInfo.PropertyType;
                            // parse json property value and set component property value:
                            if (JsonTokenUtility.TryParseToType(property.Value, propertyType, out object value)) {
                                try {
                                    propertyInfo.SetValue(component, value);
                                } catch (Exception exception) {
                                    ConsoleFormatter.Error(
                                        $"Failed to write to property `{propertyName}` on component `{targetType}` on object `{name}`.",
                                        exception
                                    );
                                    continue; // ignore this property
                                }
                            } else {
                                ConsoleFormatter.Warning(
                                    $"Unsupported property type `{propertyType}` for property `{propertyName}` on component `{targetType}` on object `{name}`."
                                );
                                continue; // ignore this property
                            }
                        }
                    }
                }
            }
            // process object children:
            if (json.TryGetValue("children", StringComparison.OrdinalIgnoreCase, out JToken childrenJson)) {
                // get the parent that the child objects should be parented to:
                Transform childParent = gameObject.transform;
                // create each child object:
                foreach (JToken childJson in childrenJson) {
                    ParseJsonPrefab((JObject)childJson, childParent);
                }
            }
            return gameObject;
        }

        #endregion

        #region Dispose

        public sealed override void Dispose() {
            DisposeOfAsset();
        }

        #endregion

        #endregion

    }

}