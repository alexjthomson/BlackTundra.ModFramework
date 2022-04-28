using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BlackTundra.AssetImporter.Importers.Utility {

    /// <summary>
    /// Utility used to build a mesh.
    /// </summary>
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
            // create triangle buffers:
            int triangleCount = triangles.Count / 3;
            int triangleVertexCount = triangleCount * 3;
            int[] triangleVertexBuffer = new int[triangleVertexCount];
            Vector2[] triangleUVBuffer = new Vector2[triangleVertexCount];
            Vector3[] triangleNormalBuffer = new Vector3[triangleVertexCount];
            // process triangles:
            VertexData vertexData;
            for (int i = 0; i < triangleVertexCount; i++) {
                vertexData = triangles[i];
                triangleVertexBuffer[i] = vertexData.vertexIndex;
                triangleUVBuffer[i] = vertexData.uvIndex > -1 ? uvs[vertexData.uvIndex] : Vector2.zero;
                triangleNormalBuffer[i] = vertexData.normalIndex > -1 ? normals[vertexData.normalIndex].normalized : Vector3.up;
            }
            // create mesh:
            Mesh mesh = new Mesh();
            // set verticies:
            mesh.SetVertices(
                verticies,
                0, verticies.Count,
                MeshCreationMeshUpdateFlags
            );
            // set triangles:
            mesh.SetTriangles(
                triangleVertexBuffer,
                0,
                triangleVertexCount,
                0,
                true,
                0
            );
            /*
            // set uvs:
            mesh.SetUVs(
                0,
                triangleUVBuffer,
                0,
                triangleVertexCount,
                MeshCreationMeshUpdateFlags
            );
            // set normals:
            mesh.SetNormals(
                triangleNormalBuffer,
                0,
                triangleVertexCount,
                MeshCreationMeshUpdateFlags
            );
            */
            // finalize mesh:
            return mesh;
        }

        #endregion

    }

}