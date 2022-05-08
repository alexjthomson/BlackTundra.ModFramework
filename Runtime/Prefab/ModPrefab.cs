using BlackTundra.Foundation.Utility;
using BlackTundra.ModFramework.Utility;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.ModFramework.Prefab {

    public sealed class ModPrefab : IDisposable {

        #region variable

        /// <summary>
        /// Reference to the cached <see cref="GameObject"/> to use as the <see cref="ModPrefab"/> <see cref="GameObject"/>.
        /// </summary>
        private readonly GameObject prefab;

        #endregion

        #region constructor

        internal ModPrefab(in JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            prefab = ProcessObject(json, PrefabManager.CacheTransform);
        }

        #endregion

        #region logic

        #region CreateInstance

        public GameObject CreateInstance() {
            GameObject gameObject = Object.Instantiate(prefab, null);
            gameObject.name = prefab.name;
            return gameObject;
        }

        public GameObject CreateInstance(in Transform parent) {
            GameObject gameObject = Object.Instantiate(prefab, parent);
            gameObject.name = prefab.name;
            return gameObject;
        }

        public GameObject CreateInstance(in Vector3 position, in Quaternion rotation) {
            GameObject gameObject = Object.Instantiate(prefab, position, rotation, null);
            gameObject.name = prefab.name;
            return gameObject;
        }

        public GameObject CreateInstance(in Vector3 position, in Quaternion rotation, in Transform parent) {
            GameObject gameObject = Object.Instantiate(prefab, position, rotation, parent);
            gameObject.name = prefab.name;
            return gameObject;
        }

        #endregion

        #region ProcessObject

        /// <summary>
        /// Processes a <paramref name="json"/> object into a <see cref="GameObject"/>.
        /// </summary>
        private GameObject ProcessObject(in JObject json, in Transform parent) {
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
                        PrefabImporter.ConsoleFormatter.Warning(
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
                                PrefabImporter.ConsoleFormatter.Warning(
                                    $"Failed to find property `{propertyName}` on component `{targetType}` on object `{name}`."
                                );
                                continue; // ignore this property
                            }
                            if (!propertyInfo.CanWrite) { // cannot write to property
                                PrefabImporter.ConsoleFormatter.Warning(
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
                                    PrefabImporter.ConsoleFormatter.Error(
                                        $"Failed to write to property `{propertyName}` on component `{targetType}` on object `{name}`.",
                                        exception
                                    );
                                    continue; // ignore this property
                                }
                            } else {
                                PrefabImporter.ConsoleFormatter.Warning(
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
                    ProcessObject((JObject)childJson, childParent);
                }
            }
            return gameObject;
        }

        #endregion

        #region Dispose

        public void Dispose() {
            Object.Destroy(prefab);
        }

        #endregion

        #endregion

    }

}