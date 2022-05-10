using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;
using System.IO;

using UnityEngine;

namespace BlackTundra.ModFramework.Model {

    public sealed class MtlMaterialCollection : ModMaterialCollection {

        #region variable

        #endregion

        #region property

        #endregion

        #region constructor

        internal MtlMaterialCollection(
            in ModInstance modInstance,
            in ulong guid,
            in ModAssetType type,
            in FileSystemReference fsr,
            in string path
            ) : base(modInstance, guid, type, fsr, path) {
        }

        #endregion

        #region logic

        #region Import

        protected internal override void Import() {
            // remove exting material asset:
            DisposeOfMaterials();
            // load material data:
            if (!FileSystem.Read(fsr, out string mtl, FileFormat.Standard)) {
                throw new IOException($"Failed to read MTL file at `{fsr}`.");
            }
            // parse material data:
            ParseMaterialData(guid.ToHex(), mtl);
        }

        #endregion

        #region ParseMaterialData

        private static void ParseMaterialData(in string name, in string mtl) {
            // read each line:
            string[] lines = mtl.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );
            // find number of lines:
            int lineCount = lines.Length;
            // validate there are at least 2 lines:
            if (lineCount < 2) return;
            // create temporary variables:
            string[] lineData;
            int lineDataCount;
            // iterate each line:
            for (int i = 0; i < lineCount; i++) {
                // get line data:
                lineData = lines[i].Split();
                lineDataCount = lineData.Length;
                // validate number of entries:
                if (lineDataCount < 2) continue; // ignore line
                // process line command:
                switch (lineData[0].ToLower()) {
                    case "newmtl": { // define a new material
                        break;
                    }
                    case "ka": { // ambient colour (r=0.2,g=0.2,b=0.2)
                        break;
                    }
                    case "kd": { // diffuse colour (r=0.8,g=0.8,b=0.8)
                        break;
                    }
                    case "ks": { // specular colour (r=1.0,g=1.0,b=1.0)
                        break;
                    }
                    case "a": { // alpha (a=1.0) (opposite of `tr`), a=1.0 is not transparent at all, a=0.0 is completely transparent
                        break;
                    }
                    case "tr": { // transparency (t=0.0) (opposite or `a`), t=0.0 is not transparent at all, t=1.0 is completely transparent
                        break;
                    }
                    case "ns": { // shininess of material (s=0.0)
                        break;
                    }
                    case "illum": { // illumination model (i=1)
                        /*
                         * i=1      flat material with no specular highlights (ks not used)
                         * i=2      material that uses specular light (ks is used)
                         */
                        break;
                    }
                    case "map_ka": { // names a file containing a texture map
                        break;
                    }
                    case "#": break; // comment
                    default: break; // unknown command
                }
            }
        }

        #endregion

        #endregion

    }

}