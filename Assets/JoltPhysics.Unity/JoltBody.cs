// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(JoltShape))]
    public sealed class JoltBody : MonoBehaviour
    {
        [Header("Body Settings")]
        [SerializeField] MotionType _motionType = MotionType.Dynamic;
        [SerializeField] uint _objectLayer = 1;

        [Header("Material")]
        [SerializeField] float _friction = 0.2f;
        [SerializeField] float _restitution = 0.0f;

        [Header("Damping")]
        [SerializeField] float _linearDamping = 0.05f;
        [SerializeField] float _angularDamping = 0.05f;

        [Header("Gravity")]
        [SerializeField] float _gravityFactor = 1.0f;

        BodyID _bodyId = BodyID.Invalid;
        JoltShape _shape;
        Shape _nativeShape;
        bool _bodyCreated;

        public BodyID BodyId => _bodyId;
        public MotionType MotionType => _motionType;

        void Start()
        {
            CreateBody();
        }

        void CreateBody()
        {
            var world = JoltPhysicsWorld.Instance;
            if (world == null)
            {
                Debug.LogError("JoltPhysicsWorld not found. Please add a JoltPhysicsWorld component to the scene.", this);
                return;
            }

            _shape = GetComponent<JoltShape>();
            if (_shape == null)
            {
                Debug.LogError("JoltShape component not found on this GameObject.", this);
                return;
            }

            _nativeShape = _shape.CreateShape();
            if (_nativeShape == null || _nativeShape.IsDisposed)
            {
                Debug.LogError("Failed to create Jolt shape.", this);
                return;
            }

            var t = transform;
            var position = t.position.ToJolt();
            var rotation = t.rotation.ToJolt();

            using var settings = new BodyCreationSettings(
                _nativeShape, position, rotation, _motionType, _objectLayer);

            settings.Friction = _friction;
            settings.Restitution = _restitution;
            settings.LinearDamping = _linearDamping;
            settings.AngularDamping = _angularDamping;
            settings.GravityFactor = _gravityFactor;

            var bodyInterface = world.BodyInterface;
            _bodyId = bodyInterface.CreateAndAddBody(settings, Activation.Activate);
            _bodyCreated = true;

            world.RegisterBody(this);
        }

        public void SyncTransformFromPhysics()
        {
            if (!_bodyCreated || !_bodyId.IsValid) return;
            if (_motionType == MotionType.Static) return;

            var world = JoltPhysicsWorld.Instance;
            if (world == null) return;

            var bodyInterface = world.BodyInterface;

            if (_motionType == MotionType.Kinematic)
            {
                // Push Unity transform to Jolt
                var t = transform;
                bodyInterface.SetPositionAndRotation(
                    _bodyId,
                    t.position.ToJolt(),
                    t.rotation.ToJolt(),
                    Activation.DontActivate);
            }
            else
            {
                // Pull Jolt transform to Unity
                bodyInterface.GetPositionAndRotation(_bodyId, out var pos, out var rot);
                var t = transform;
                t.position = pos.ToUnity();
                t.rotation = rot.ToUnity();
            }
        }

        public void DestroyBody()
        {
            if (!_bodyCreated) return;

            var world = JoltPhysicsWorld.Instance;
            if (world != null && _bodyId.IsValid)
            {
                world.BodyInterface.RemoveAndDestroyBody(_bodyId);
                world.UnregisterBody(this);
            }

            _nativeShape?.Dispose();
            _nativeShape = null;
            _bodyId = BodyID.Invalid;
            _bodyCreated = false;
        }

        void OnDestroy()
        {
            DestroyBody();
        }
    }
}
