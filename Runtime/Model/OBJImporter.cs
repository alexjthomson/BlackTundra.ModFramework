//#define ASSET_IMPORTER_OBJ_FAIL_ON_UNKNOWN_CMD

using BlackTundra.Foundation.IO;

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public static class ObjImporter {

        public static Mesh Import(in string name, in FileSystemReference fsr, in MeshBuilderOptions options = 0) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsFile) throw new ArgumentException($"{nameof(fsr)} must reference a file.");
            if (FileSystem.Read(fsr, out string obj, FileFormat.Standard)) {
                return Import(name, obj, options);
            } else {
                throw new IOException($"Failed to read OBJ file at `{fsr.AbsolutePath}`.");
            }
        }

        public static Mesh Import(in string name, in string obj, in MeshBuilderOptions options = 0) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            // find lines:
            string[] lines = obj.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );
            // create model data:
            List<Vector3> verticies = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<VertexData> triangles = new List<VertexData>();
            // define helper variables:
            string[] lineData;
            int dataCount;
            // iterate obj lines:
            for (int i = 0; i < lines.Length; i++) {
                lineData = lines[i].Split();
                dataCount = lineData.Length - 1; // find the number of entries in the data
                if (dataCount == -1) continue;
                if (dataCount == 0) throw new FormatException($"Invalid line with no data at line `{i + 1}`.");
                switch (lineData[0].ToLower()) { // switch on commands
                    case "v": { // vertex (x, y, z [,w = 1.0]) (w is used for vertex colours)
                        if (dataCount < 3 || dataCount > 4) throw new FormatException($"Invalid vertex at line `{i + 1}`.");
                        verticies.Add(
                            new Vector3(
                                float.Parse(lineData[1]),
                                float.Parse(lineData[2]),
                                float.Parse(lineData[3])
                            )
                        );
                        break;
                    }
                    case "f": { // polygonal face (triangle)
                        /*
                         * format:
                         * x, y, z
                         * 
                         * each of x, y, and z can be made up of 3 indicies:
                         * vertex_index/uv_index/normal_index
                         * 
                         * the following are valid faces:
                         * f 1 3 2
                         * f 1/3 3/2 2/3
                         * f 1//3 3//2 2//3
                         * f 1/2/3 2/3/4 3/4/5
                         * f 1// 3// 2//
                         * 
                         * the following are invalid faces:
                         * f // // //
                         * f / / /
                         * f /1 /2 /3
                         * f
                         * f 1/2 3 2
                         * f 1/1 3 2/2
                         */
                        if (dataCount != 3) throw new FormatException($"Invalid face at line `{i + 1}`.");
                        for (int t = 1; t < 4; t++) { // iterate each triangle
                            string[] elements = lineData[t].Split('/', StringSplitOptions.None);
                            int uvIndex = 0, normalIndex = 0;
                            switch (elements.Length) {
                                case 3: { // uv index
                                    int.TryParse(elements[2], out normalIndex);
                                    goto case 2;
                                }
                                case 2: { // uv channel
                                    int.TryParse(elements[1], out uvIndex);
                                    goto case 1;
                                }
                                case 1: { // vertex index
                                    triangles.Add(new VertexData(int.Parse(elements[0]) - 1, uvIndex - 1, normalIndex - 1)); // add the triangle
                                    break;
                                }
                                default: throw new FormatException($"Unable to parse triangle {t} in face at line `{i + 1}`.");
                            }
                        }
                        break;
                    }
                    case "vt": { // uv coords (u ,v [,w = 0.0])
                        if (dataCount < 2 || dataCount > 3) throw new FormatException($"Invalid uv coordinate at line `{i + 1}`.");
                        uvs.Add(
                            new Vector2(
                                float.Parse(lineData[1]), // u
                                float.Parse(lineData[2])  // v
                            )
                        );
                        break;
                    }
                    case "vn": { // vertex normal (x, y, z) (may not be unit vectors)
                        if (dataCount != 3) throw new FormatException($"Invalid vertex normal at line `{i + 1}`.");
                        normals.Add(
                            new Vector3(
                                float.Parse(lineData[1]),
                                float.Parse(lineData[2]),
                                float.Parse(lineData[3])
                            )
                        );
                        break;
                    }
                    case "vp": { // parameter space verticies (u [,v] [,w])
                        // not implemented
                        break;
                    }
                    case "l": { // line element
                        // not implemented
                        break;
                    }
                    case "#": break; // comment
                    default: {
#if ASSET_IMPORTER_OBJ_FAIL_ON_UNKNOWN_CMD
                        throw new FormatException($"Invalid line type: `{lineData[0]}`.");
#else
                        break;
#endif
                    }
                }
            }
            // build mesh:
            return MeshBuilder.Build(name, verticies, uvs, normals, triangles, options);
        }

    }

}