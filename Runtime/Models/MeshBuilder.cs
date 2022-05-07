using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BlackTundra.ModFramework.Model {

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
            in string name,
            in List<Vector3> verticies,
            in List<Vector2> uvs,
            in List<Vector3> normals,
            in List<VertexData> triangles,
            MeshBuilderOptions options = 0
        ) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (verticies == null) throw new ArgumentNullException(nameof(verticies));
            if (uvs == null) throw new ArgumentNullException(nameof(uvs));
            if (normals == null) throw new ArgumentNullException(nameof(normals));
            if (triangles == null) throw new ArgumentNullException(nameof(triangles));
            // create triangle buffers:
            int triangleCount = triangles.Count / 3;
            int triangleVertexCount = triangleCount * 3;
            // create buffers:
            Vector3[] vertexBuffer;
            int[] triangleVertexBuffer;
            Vector2[] triangleUVBuffer;
            Vector3[] triangleNormalBuffer;
            // populate buffers:
            if ((options & MeshBuilderOptions.PerFaceUV) != 0) { // per face uvs
                // initialise buffers:
                vertexBuffer = new Vector3[triangleVertexCount];
                triangleVertexBuffer = new int[triangleVertexCount];
                triangleUVBuffer = new Vector2[triangleVertexCount];
                triangleNormalBuffer = new Vector3[triangleVertexCount];
                // process triangles:
                VertexData vertexData;
                for (int i = 0; i < triangleVertexCount; i++) {
                    vertexData = triangles[i];
                    vertexBuffer[i] = verticies[vertexData.vertexIndex];
                    triangleVertexBuffer[i] = i;
                    triangleUVBuffer[i] = vertexData.uvIndex > -1 ? uvs[vertexData.uvIndex] : Vector2.zero;
                    triangleNormalBuffer[i] = vertexData.normalIndex > -1 ? normals[vertexData.normalIndex].normalized : Vector3.up;
                }
            } else { // uvs shared between faces
                // initialise buffers:
                vertexBuffer = verticies.ToArray();
                int vertexCount = vertexBuffer.Length;
                triangleVertexBuffer = new int[triangleVertexCount];
                triangleUVBuffer = new Vector2[vertexCount];
                triangleNormalBuffer = new Vector3[vertexCount];
                // process triangles:
                VertexData vertexData;
                for (int i = 0; i < triangleVertexCount; i++) {
                    vertexData = triangles[i];
                    triangleVertexBuffer[i] = vertexData.vertexIndex;
                    triangleUVBuffer[vertexData.vertexIndex] = vertexData.uvIndex > -1 ? uvs[vertexData.uvIndex] : Vector2.zero;
                    triangleNormalBuffer[vertexData.vertexIndex] = vertexData.normalIndex > -1 ? normals[vertexData.normalIndex].normalized : Vector3.up;
                }
            }
            #region create mesh
            // create mesh:
            Mesh mesh = new Mesh() { name = name };
            // set verticies:
            mesh.SetVertices(
                vertexBuffer,
                0, vertexBuffer.Length,
                MeshCreationMeshUpdateFlags
            );
            // set triangles:
            mesh.SetTriangles(
                triangleVertexBuffer,
                0, triangleVertexBuffer.Length,
                0, false, 0
            );
            // set uvs:
            mesh.SetUVs(
                0,
                triangleUVBuffer,
                0, triangleUVBuffer.Length,
                MeshCreationMeshUpdateFlags
            );
            // set normals:
            mesh.SetNormals(
                triangleNormalBuffer,
                0, triangleNormalBuffer.Length,
                MeshCreationMeshUpdateFlags
            );
            #endregion
            // finalize mesh:
            if ((options & MeshBuilderOptions.CalculateNormals) != 0) mesh.RecalculateNormals();
            if ((options & MeshBuilderOptions.CalculateTangents) != 0) mesh.RecalculateTangents();
            if ((options & MeshBuilderOptions.OptimizeMesh) != 0) mesh.OptimizeReorderVertexBuffer();
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion

    }

}