// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using System.Collections.Generic;
using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    public sealed class JoltPhysicsWorld : MonoBehaviour
    {
        public static JoltPhysicsWorld Instance { get; private set; }

        PhysicsSystem _physicsSystem;
        JobSystemThreadPool _jobSystem;
        private TempAllocator _tempAllocator;

        BroadPhaseLayerInterfaceTable _broadPhaseLayerInterface;
        ObjectLayerPairFilterTable _objectLayerPairFilter;
        ObjectVsBroadPhaseLayerFilterTable _objectVsBroadPhaseLayerFilter;

        readonly List<JoltBody> _bodies = new();
        bool _initialized;

        public BodyInterface BodyInterface { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple JoltPhysicsWorld instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        void Initialize()
        {
            if (_initialized) return;

            var config = JoltPhysicsSettings.Instance;
            if (config == null)
            {
                Debug.LogError("JoltPhysicsSettings not found. Please create one via Project Settings > Jolt Physics.");
                return;
            }

            if (!JoltAPI.Init())
            {
                Debug.LogError("Failed to initialize Jolt Physics.");
                return;
            }

            _jobSystem = new JobSystemThreadPool();
            _tempAllocator = new TempAllocator();

            int numObjectLayers = config.LayerCount;
            int numBroadPhaseLayers = config.GetBroadPhaseLayerCount();

            // Map object layers -> broad phase layers
            _broadPhaseLayerInterface = new BroadPhaseLayerInterfaceTable(
                (uint)numObjectLayers, (uint)numBroadPhaseLayers);

            var layers = config.Layers;
            for (int i = 0; i < numObjectLayers; i++)
            {
                _broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(
                    (uint)i, layers[i].broadPhaseLayer);
            }

            // Set up object layer pair collision filter from settings
            _objectLayerPairFilter = new ObjectLayerPairFilterTable((uint)numObjectLayers);
            for (int i = 0; i < numObjectLayers; i++)
            {
                for (int j = i; j < numObjectLayers; j++)
                {
                    if (config.GetCollisionEnabled(i, j))
                    {
                        _objectLayerPairFilter.EnableCollision((uint)i, (uint)j);
                    }
                    else
                    {
                        _objectLayerPairFilter.DisableCollision((uint)i, (uint)j);
                    }
                }
            }

            _objectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(
                _broadPhaseLayerInterface, (uint)numBroadPhaseLayers,
                _objectLayerPairFilter, (uint)numObjectLayers);

            var settings = new PhysicsSystemSettings
            {
                MaxBodies = config.MaxBodies,
                NumBodyMutexes = 0,
                MaxBodyPairs = config.MaxBodies * 4,
                MaxContactConstraints = config.MaxContactConstraints,
            };

            _physicsSystem = new PhysicsSystem(
                settings,
                _broadPhaseLayerInterface,
                _objectLayerPairFilter,
                _objectVsBroadPhaseLayerFilter);

            _physicsSystem.Gravity = config.Gravity.ToJolt();
            BodyInterface = _physicsSystem.GetBodyInterface();

            _initialized = true;
        }

        void FixedUpdate()
        {
            if (!_initialized) return;

            var config = JoltPhysicsSettings.Instance;
            int collisionSteps = config != null ? config.CollisionSteps : 1;

            _physicsSystem.Update(Time.fixedDeltaTime, collisionSteps, _tempAllocator, _jobSystem);

            // Sync transforms from Jolt to Unity
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                var body = _bodies[i];
                if (body == null)
                {
                    _bodies.RemoveAt(i);
                    continue;
                }

                body.SyncTransformFromPhysics();
            }
        }

        public void RegisterBody(JoltBody body)
        {
            if (!_bodies.Contains(body))
                _bodies.Add(body);
        }

        public void UnregisterBody(JoltBody body)
        {
            _bodies.Remove(body);
        }

        void OnDestroy()
        {
            if (Instance != this) return;

            // Remove all bodies first
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                if (_bodies[i] != null)
                    _bodies[i].DestroyBody();
            }

            _bodies.Clear();

            _physicsSystem?.Dispose();
            _jobSystem?.Dispose();
            _objectVsBroadPhaseLayerFilter?.Dispose();
            _objectLayerPairFilter?.Dispose();
            _broadPhaseLayerInterface?.Dispose();

            _physicsSystem = null;
            _jobSystem = null;

            if (_initialized)
            {
                JoltAPI.Shutdown();
                _initialized = false;
            }

            Instance = null;
        }
    }
}