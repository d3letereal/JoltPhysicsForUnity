// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using UnityEditor;
using UnityEngine;

namespace JoltPhysics.Unity.Editor
{
    [CustomEditor(typeof(JoltBody))]
    public sealed class JoltBodyEditor : UnityEditor.Editor
    {
        SerializedProperty _motionType;
        SerializedProperty _objectLayer;
        SerializedProperty _friction;
        SerializedProperty _restitution;
        SerializedProperty _linearDamping;
        SerializedProperty _angularDamping;
        SerializedProperty _gravityFactor;

        void OnEnable()
        {
            _motionType = serializedObject.FindProperty("_motionType");
            _objectLayer = serializedObject.FindProperty("_objectLayer");
            _friction = serializedObject.FindProperty("_friction");
            _restitution = serializedObject.FindProperty("_restitution");
            _linearDamping = serializedObject.FindProperty("_linearDamping");
            _angularDamping = serializedObject.FindProperty("_angularDamping");
            _gravityFactor = serializedObject.FindProperty("_gravityFactor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Body Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_motionType);

            // Layer dropdown using names from JoltPhysicsSettings
            var settings = JoltPhysicsSettings.Instance;
            if (settings != null && settings.LayerCount > 0)
            {
                var layerNames = settings.GetLayerNames();
                int layerIndex = (int)_objectLayer.longValue;
                if (layerIndex < 0 || layerIndex >= layerNames.Length)
                    layerIndex = 0;
                int newLayer = EditorGUILayout.Popup("Object Layer", layerIndex, layerNames);
                _objectLayer.longValue = (uint)newLayer;
            }
            else
            {
                EditorGUILayout.PropertyField(_objectLayer);
                EditorGUILayout.HelpBox(
                    "JoltPhysicsSettings not found. Configure layers in Project Settings > Jolt Physics.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_friction, 0f, 1f);
            EditorGUILayout.Slider(_restitution, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Damping", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_linearDamping);
            EditorGUILayout.PropertyField(_angularDamping);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_gravityFactor);

            // Runtime info
            if (Application.isPlaying)
            {
                var body = (JoltBody)target;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Body ID", body.BodyId.ToString());
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
