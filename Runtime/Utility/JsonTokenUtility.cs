using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.ModFramework.Utility {

    /// <summary>
    /// Utility that extends the <see cref="JToken"/> class.
    /// </summary>
    public static class JsonTokenUtility {

        #region constant

        /// <summary>
        /// Dictionary that maps types to anonymous functions that parse a <see cref="JToken"/> into the specified type.
        /// </summary>
        private static readonly Dictionary<Type, Func<JToken, Type, object>> ValueParserDictionary = new Dictionary<Type, Func<JToken, Type, object>>() {

            #region string
            { typeof(string), (json, _) => (string)json },
            #endregion

            #region int
            { typeof(int), (json, _) => (int)json },
            #endregion

            #region uint
            { typeof(uint), (json, _) => (uint)json },
            #endregion

            #region long
            { typeof(long), (json, _) => (long)json },
            #endregion

            #region ulong
            { typeof(ulong), (json, _) => (ulong)json },
            #endregion

            #region short
            { typeof(short), (json, _) => (short)json },
            #endregion

            #region ushort
            { typeof(ushort), (json, _) => (ushort)json },
            #endregion

            #region decimal
            { typeof(decimal), (json, _) => (decimal)json },
            #endregion

            #region float
            { typeof(float), (json, _) => (float)json },
            #endregion

            #region double
            { typeof(double), (json, _) => (double)json },
            #endregion

            #region byte
            { typeof(byte), (json, _) => (byte)json },
            #endregion

            #region sbyte
            { typeof(sbyte), (json, _) => (sbyte)json },
            #endregion

            #region char
            { typeof(char), (json, _) => (char)json },
            #endregion

            #region bool
            { typeof(bool), (json, _) => (bool)json },
            #endregion

            #region Vector2
            { typeof(Vector2), (json, _) => {
                JArray jsonArray = (JArray)json;
                if (jsonArray.Count != 2) throw new FormatException($"Invalid {nameof(Vector2)}.");
                return new Vector2(
                    (float)jsonArray[0],
                    (float)jsonArray[1]
                );
            }},
            #endregion

            #region Vector3
            { typeof(Vector3), (json, _) => {
                JArray jsonArray = (JArray)json;
                if (jsonArray.Count != 3) throw new FormatException($"Invalid {nameof(Vector3)}.");
                return new Vector3(
                    (float)jsonArray[0],
                    (float)jsonArray[1],
                    (float)jsonArray[2]
                );
            }},
            #endregion

            #region Vector4
            { typeof(Vector4), (json, _) => {
                JArray jsonArray = (JArray)json;
                if (jsonArray.Count != 4) throw new FormatException($"Invalid {nameof(Vector4)}.");
                return new Vector4(
                    (float)jsonArray[0],
                    (float)jsonArray[1],
                    (float)jsonArray[2],
                    (float)jsonArray[3]
                );
            }},
            #endregion

            #region Quaternion
            { typeof(Quaternion), (json, _) => {
                JArray jsonArray = (JArray)json;
                return jsonArray.Count switch {
                    3 => Quaternion.Euler(
                        (float)jsonArray[0],
                        (float)jsonArray[1],
                        (float)jsonArray[2]
                    ),
                    4 => new Quaternion(
                        (float)jsonArray[0],
                        (float)jsonArray[1],
                        (float)jsonArray[2],
                        (float)jsonArray[3]
                    ),
                    _ => throw new FormatException($"Invalid {nameof(Quaternion)}.")
                };
            }},
            #endregion

            #region Enum
            { typeof(Enum), (json, targetType) => Enum.Parse(targetType, (string)json)},
            #endregion

            #region Mesh
            { typeof(Mesh), (json, _) => ModInstance.GetAssetFromPath((string)json).value},
            #endregion

            #region Material
            { typeof(Material), (json, _) => {
                throw new NotImplementedException();
            }},
            #endregion

            #region PhysicMaterial
            { typeof(PhysicMaterial), (json, _) => {
                throw new NotImplementedException();
            }},
            #endregion

            #region Color
            { typeof(Color), (json, _) => {
                JArray jsonArray = (JArray)json;
                return jsonArray.Count switch {
                    3 => new Color(
                        (float)jsonArray[0],
                        (float)jsonArray[1],
                        (float)jsonArray[2]
                    ),
                    4 => new Color(
                        (float)jsonArray[0],
                        (float)jsonArray[1],
                        (float)jsonArray[2],
                        (float)jsonArray[3]
                    ),
                    _ => throw new FormatException($"Invalid {nameof(Color)}.")
                };
            }},
            #endregion

            #region Color32
            { typeof(Color32), (json, _) => {
                JArray jsonArray = (JArray)json;
                if (jsonArray.Count != 4) throw new FormatException($"Invalid {nameof(Color)}.");
                return new Color32(
                    (byte)jsonArray[0],
                    (byte)jsonArray[1],
                    (byte)jsonArray[2],
                    (byte)jsonArray[3]
                );
            }},
            #endregion

        };

        #endregion

        #region logic

        #region ParseToType

        public static object ParseToType(this JToken json, in Type targetType) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (ValueParserDictionary.TryGetValue(targetType, out Func<JToken, Type, object> mapper)) {
                return mapper.Invoke(json, targetType);
            } else {
                throw new NotSupportedException($"Unsupported target type `{targetType}`.");
            }
        }

        #endregion

        #region TryParseToType

        public static bool TryParseToType(this JToken json, in Type targetType, out object value) {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (ValueParserDictionary.TryGetValue(targetType, out Func<JToken, Type, object> mapper)) {
                try {
                    value = mapper.Invoke(json, targetType);
                    return true;
                } catch (Exception) { }
            }
            value = null;
            return false;
        }

        #endregion

        #endregion

    }

}