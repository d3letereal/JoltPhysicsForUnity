// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JoltPhysics.Unity.Editor
{
    [CustomEditor(typeof(JoltBoxShape))]
    [CanEditMultipleObjects]
    public sealed class JoltBoxShapeEditor : UnityEditor.Editor
    {
        SerializedProperty _center;
        SerializedProperty _size;
        SerializedProperty _convexRadius;

        readonly BoxBoundsHandle _boundsHandle = new();
        bool _editing;

        void OnEnable()
        {
            _center = serializedObject.FindProperty("_center");
            _size = serializedObject.FindProperty("_size");
            _convexRadius = serializedObject.FindProperty("_convexRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _editing = GUILayout.Toggle(_editing, EditorGUIUtility.TrTextContent("Edit Collider"),
                "EditModeSingleButton", GUILayout.MinWidth(32));

            EditorGUILayout.PropertyField(_center);
            EditorGUILayout.PropertyField(_size);
            EditorGUILayout.PropertyField(_convexRadius);
            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (!_editing) return;

            var shape = (JoltBoxShape)target;
            var t = shape.transform;

            using (new Handles.DrawingScope(Color.green,
                       Matrix4x4.TRS(t.position, t.rotation, t.lossyScale)))
            {
                _boundsHandle.center = shape.Center;
                _boundsHandle.size = shape.Size;

                EditorGUI.BeginChangeCheck();
                _boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(shape, "Modify Jolt Box Shape");
                    shape.Center = _boundsHandle.center;
                    shape.Size = _boundsHandle.size;
                }
            }
        }
    }

    [CustomEditor(typeof(JoltSphereShape))]
    [CanEditMultipleObjects]
    public sealed class JoltSphereShapeEditor : UnityEditor.Editor
    {
        SerializedProperty _radius;
        readonly SphereBoundsHandle _boundsHandle = new();
        bool _editing;

        void OnEnable()
        {
            _radius = serializedObject.FindProperty("_radius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _editing = GUILayout.Toggle(_editing, EditorGUIUtility.TrTextContent("Edit Collider"),
                "EditModeSingleButton", GUILayout.MinWidth(32));

            EditorGUILayout.PropertyField(_radius);
            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (!_editing) return;

            var shape = (JoltSphereShape)target;
            var t = shape.transform;

            using (new Handles.DrawingScope(Color.green,
                       Matrix4x4.TRS(t.position, t.rotation, Vector3.one)))
            {
                _boundsHandle.center = Vector3.zero;
                _boundsHandle.radius = shape.Radius;

                EditorGUI.BeginChangeCheck();
                _boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(shape, "Modify Jolt Sphere Shape");
                    shape.Radius = Mathf.Max(0.001f, _boundsHandle.radius);
                }
            }
        }
    }

    [CustomEditor(typeof(JoltEllipsoidShape))]
    [CanEditMultipleObjects]
    public sealed class JoltEllipsoidShapeEditor : UnityEditor.Editor
    {
        SerializedProperty _radii;
        readonly BoxBoundsHandle _boundsHandle = new();
        bool _editing;

        void OnEnable()
        {
            _radii = serializedObject.FindProperty("_radii");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _editing = GUILayout.Toggle(_editing, EditorGUIUtility.TrTextContent("Edit Collider"),
                "EditModeSingleButton", GUILayout.MinWidth(32));

            EditorGUILayout.PropertyField(_radii);
            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (!_editing) return;

            var shape = (JoltEllipsoidShape)target;
            var t = shape.transform;

            using (new Handles.DrawingScope(Color.green,
                       Matrix4x4.TRS(t.position, t.rotation, Vector3.one)))
            {
                // Size = diameter, so radii * 2
                _boundsHandle.center = Vector3.zero;
                _boundsHandle.size = shape.Radii * 2f;

                EditorGUI.BeginChangeCheck();
                _boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(shape, "Modify Jolt Ellipsoid Shape");
                    var newSize = _boundsHandle.size;
                    shape.Radii = new Vector3(
                        Mathf.Max(0.001f, newSize.x * 0.5f),
                        Mathf.Max(0.001f, newSize.y * 0.5f),
                        Mathf.Max(0.001f, newSize.z * 0.5f));
                }
            }
        }
    }

    [CustomEditor(typeof(JoltMeshShape))]
    [CanEditMultipleObjects]
    public sealed class JoltMeshShapeEditor : UnityEditor.Editor
    {
        SerializedProperty _mesh;

        void OnEnable()
        {
            _mesh = serializedObject.FindProperty("_mesh");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_mesh);

            var shape = (JoltMeshShape)target;
            if (shape.Mesh != null)
            {
                EditorGUILayout.HelpBox(
                    $"Vertices: {shape.Mesh.vertexCount}\nTriangles: {shape.Mesh.triangles.Length / 3}",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
