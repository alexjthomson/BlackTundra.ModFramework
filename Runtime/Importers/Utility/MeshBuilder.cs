using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BlackTundra.AssetImporter.Importers.Utility {

    internal static class MeshBuilder {

        #region constructor

        /// <summary>
        /// <see cref="MeshUpdateFlags"/> used during mesh creation.
        /// </summary>
        private const MeshUpdateFlags MeshCreationMeshUpdateFlags =
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontResetBoneBounds |
            MeshUpdateFlags.DontNotifyMeshUsers;

        #endregion

        #region logic

        internal static Mesh Build(
            in List<Vector3> verticies,
            in List<Vector2> uvs,
            in List<Vector3> normals,
            in List<VertexData> triangles
        ) {
            if (verticies == null) throw new ArgumentNullException(nameof(verticies));
            if (uvs == null) throw new ArgumentNullException(nameof(uvs));
            if (normals == null) throw new ArgumentNullException(nameof(normals));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));

            Mesh mesh = new Mesh();
            // set verticies:
            mesh.SetVertices(
                verticies,
                0, verticies.Count,
                MeshCreationMeshUpdateFlags
            );
            // set uvs:
            //mesh.SetUVs();
            // set triangles:

            mesh.SetTriangles();
        }

        #endregion

    }

}