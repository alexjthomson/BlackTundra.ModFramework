using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public sealed class ObjModel : ModModel {

        #region constructor

        internal ObjModel(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path,
            MeshBuilderOptions meshBuilderOptions
            ) : base(modInstance, guid, type, fsr, path, meshBuilderOptions) {
        }

        #endregion

        #region logic

        #region Import

        protected internal sealed override void Import() {
            // dipose of exist asset:
            DisposeOfAsset();
            // read obj data:
            if (!FileSystem.Read(fsr, out string obj, FileFormat.Standard)) {
                throw new IOException($"Failed to read OBJ file at `{fsr}`.");
            }
            // parse obj data:
            _mesh = ParseObjData(guid.ToHex(), obj, meshBuilderOptions);
        }

        #endregion

        #region ParseObjData

        private Mesh ParseObjData(in string name, in string obj, in MeshBuilderOptions options = 0) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            // find lines:
            string[] lines = obj.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );
            // find current directory:
            FileSystemReference parentFsr = fsr.GetParent();
            string parentFsrPath = fsr.AbsolutePath;
            // create model data:
            List<Vector3> verticies = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<VertexData> triangles = new List<VertexData>();
            // define helper variables:
            string[] lineData;
            int lineDataCount;
            int dataCount;
            // iterate obj lines:
            for (int i = 0; i < lines.Length; i++) {
                lineData = lines[i].Split();
                lineDataCount = lineData.Length;
                dataCount = lineDataCount - 1; // find the number of entries in the data
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
                    case "mtllib": { // material
                        /*
                         * Materials describe the visual aspects of the polygons. Materials are stored in .mtl files; more than one
                         * .mtl file may be referenced from within the OBJ file. The .mtl file may contains one or more named material
                         * definition.
                         */
                        StringBuilder materialFile = new StringBuilder().Append(lineData[1]);
                        for (int j = 2; j < lineDataCount; j++) {
                            materialFile.Append(' ').Append(lineData[j]);
                        }
                        ReferenceMaterial(
                            new FileSystemReference(
                                string.Concat(parentFsrPath, materialFile.ToString()),
                                false, // is not local
                                false  // is file
                            )
                        );
                        break;
                    }
                    case "#": break; // comment
                    default: break; // unknown command
                }
            }
            // build mesh:
            return MeshBuilder.Build(name, verticies, uvs, normals, triangles, options);
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