// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [AddComponentMenu("Jolt Physics/Mesh Shape")]
    public sealed class JoltMeshShape : JoltShape
    {
        [SerializeField] Mesh _mesh;

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }

        public override Shape CreateShape()
        {
            if (_mesh == null)
            {
                Debug.LogError("Mesh is not assigned.", this);
                return null;
            }

            return FromUnityMesh(_mesh);
        }

        /// <summary>
        /// Converts a Unity Mesh to a Jolt MeshShape.
        /// </summary>
        public static MeshShape FromUnityMesh(Mesh unityMesh)
        {
            var unityVertices = unityMesh.vertices;
            var unityTriangles = unityMesh.triangles;

            var vertices = new Float3[unityVertices.Length];
            for (int i = 0; i < unityVertices.Length; i++)
            {
                vertices[i] = unityVertices[i].ToJolt();
            }

            using var settings = new MeshShapeSettings(vertices, unityTriangles);
            return settings.CreateShape();
        }

        /// <summary>
        /// Creates a Unity Mesh from raw vertex and triangle data.
        /// Useful for debug visualization of Jolt mesh data.
        /// </summary>
        public static Mesh ToUnityMesh(Float3[] vertices, int[] triangles, string name = "JoltMesh")
        {
            var mesh = new Mesh { name = name };

            var unityVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                unityVertices[i] = vertices[i].ToUnity();
            }

            mesh.vertices = unityVertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            if (_mesh == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(_mesh);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
