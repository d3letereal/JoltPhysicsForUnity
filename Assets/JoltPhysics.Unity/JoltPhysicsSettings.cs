// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    /// <summary>
    /// Unity ScriptableObject wrapper around <see cref="PhysicsSettingsData"/>.
    /// Stored at Resources/JoltPhysicsSettings. Editable in Project Settings > Jolt Physics.
    /// </summary>
    public sealed class JoltPhysicsSettings : ScriptableObject
    {
        const string ResourcePath = "JoltPhysicsSettings";

        static JoltPhysicsSettings _instance;

        [SerializeField] PhysicsSettingsData _data = new();

        public static JoltPhysicsSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<JoltPhysicsSettings>(ResourcePath);
#if UNITY_EDITOR
                    if (_instance == null)
                        _instance = CreateDefaultAsset();
#endif
                }
                return _instance;
            }
        }

        /// <summary>
        /// The underlying pure-C# settings data. Can be used directly without Unity.
        /// </summary>
        public PhysicsSettingsData Data => _data;

        // --- Convenience delegates ---

        public Vector3 Gravity => new(_data.gravity.x, _data.gravity.y, _data.gravity.z);
        public int CollisionSteps => _data.collisionSteps;
        public uint MaxBodies => _data.maxBodies;
        public uint MaxContactConstraints => _data.maxContactConstraints;
        public PhysicsLayerDefinition[] Layers => _data.layers;
        public int LayerCount => _data.LayerCount;

        public int GetLayerIndex(string layerName) => _data.GetLayerIndex(layerName);
        public string[] GetLayerNames() => _data.GetLayerNames();
        public bool GetCollisionEnabled(int layerA, int layerB) => _data.GetCollisionEnabled(layerA, layerB);

        public void SetCollisionEnabled(int layerA, int layerB, bool enabled)
            => _data.SetCollisionEnabled(layerA, layerB, enabled);

        public void EnsureCollisionMaskSize() => _data.EnsureCollisionMaskSize();
        public int GetBroadPhaseLayerCount() => _data.GetBroadPhaseLayerCount();

#if UNITY_EDITOR
        static JoltPhysicsSettings CreateDefaultAsset()
        {
            var settings = CreateInstance<JoltPhysicsSettings>();

            const string folderPath = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            string assetPath = $"{folderPath}/{ResourcePath}.asset";
            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Created JoltPhysicsSettings at {assetPath}");
            return settings;
        }
#endif
    }
}
