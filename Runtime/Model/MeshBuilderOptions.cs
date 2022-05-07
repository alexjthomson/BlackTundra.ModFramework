using System;

namespace BlackTundra.ModFramework.Model {

    /// <summary>
    /// Detaials how to build a mesh.
    /// </summary>
    [Flags]
    [Serializable]
    public enum MeshBuilderOptions : int {
        PerFaceUV = 1 << 0,
        OptimizeMesh = 1 << 1,
        CalculateNormals = 1 << 2,
        CalculateTangents = 1 << 3
    }

}