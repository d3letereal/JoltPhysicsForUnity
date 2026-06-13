// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics.Unity;
using UnityEngine;

namespace JoltPhysics.Sample
{
    public class JoltTest : MonoBehaviour
    {
        private const uint LayerNonMoving = 0;
        private const uint LayerMoving = 1;
        private const byte BPLayerNonMoving = 0;
        private const byte BPLayerMoving = 1;

        private PhysicsSystem _physicsSystem;
        private JobSystemThreadPool _jobSystem;
        private TempAllocator _tempAllocator;
        private BroadPhaseLayerInterfaceTable _bpLayerInterface;
        private ObjectLayerPairFilterTable _objectFilter;
        private ObjectVsBroadPhaseLayerFilterTable _bpFilter;

        private BodyID _floorId;
        private BodyID _ballId;

        private GameObject _floorGO, _ballGO;

        private void Awake()
        {
            JoltAPI.Init();

            // Set up broad phase layers
            _bpLayerInterface = new BroadPhaseLayerInterfaceTable(2, 2);
            _bpLayerInterface.MapObjectToBroadPhaseLayer(LayerNonMoving, BPLayerNonMoving);
            _bpLayerInterface.MapObjectToBroadPhaseLayer(LayerMoving, BPLayerMoving);

            // Set up object layer collision filtering
            _objectFilter = new ObjectLayerPairFilterTable(2);
            _objectFilter.EnableCollision(LayerNonMoving, LayerMoving);
            _objectFilter.EnableCollision(LayerMoving, LayerMoving);

            // Set up broad phase vs object layer filtering
            _bpFilter = new ObjectVsBroadPhaseLayerFilterTable(_bpLayerInterface, 2, _objectFilter, 2);

            // Create physics system
            _physicsSystem = new PhysicsSystem(
                PhysicsSystemSettings.Default,
                _bpLayerInterface, _objectFilter, _bpFilter);

            // Create job system
            _jobSystem = new JobSystemThreadPool();

            // Create shapes
            using var floorShape = new BoxShape(new Float3(100f, 1f, 100f));
            using var ballShape = new SphereShape(0.5f);

            // Create bodies
            var bodyInterface = _physicsSystem.GetBodyInterface();

            var floorSettings = new BodyCreationSettings(floorShape,
                new Float3(0f, -1f, 0f), Quat.Identity,
                MotionType.Static, LayerNonMoving);
            floorSettings.Friction = 0.5f;
            floorSettings.Restitution = 1.0f;
            _floorId = bodyInterface.CreateAndAddBody(floorSettings, Activation.DontActivate);

            var ballSettings = new BodyCreationSettings(ballShape,
                new Float3(0f, 10f, 0f), Quat.Identity,
                MotionType.Dynamic, LayerMoving);
            ballSettings.Friction = 0.5f;
            ballSettings.Restitution = 1.0f;
            _ballId = bodyInterface.CreateAndAddBody(ballSettings, Activation.Activate);

            Debug.Log($"JoltPhysics initialized. Floor={_floorId}, Ball={_ballId}");

            // Create floor GameObject
            _floorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floorGO.transform.position = new Vector3(0f, -1f, 0f);
            _floorGO.transform.localScale = new Vector3(100f, 1f, 100f);
            _floorGO.name = "JoltFloor";
            _floorGO.GetComponent<Renderer>().material.color = Color.gray;

            // Create ball GameObject
            _ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ballGO.transform.position = new Vector3(0f, 10f, 0f);
            _ballGO.transform.localScale = Vector3.one * 1f;
            _ballGO.name = "JoltBall";
            _ballGO.GetComponent<Renderer>().material.color = Color.red;
        }

        private void FixedUpdate()
        {
            if (_physicsSystem == null) return;

            _physicsSystem.Update(Time.fixedDeltaTime, 1, _tempAllocator, _jobSystem);

            var bodyInterface = _physicsSystem.GetBodyInterface();
            var pos = bodyInterface.GetPosition(_ballId);
            var rot = bodyInterface.GetRotation(_ballId);
            _ballGO.transform.position = pos.ToUnity();
            _ballGO.transform.rotation = rot.ToUnity();
            Debug.Log($"Ball position: {pos.ToUnity()}");
        }

        private void OnDestroy()
        {
            var bodyInterface = _physicsSystem.GetBodyInterface();
            bodyInterface.RemoveAndDestroyBody(_ballId);
            bodyInterface.RemoveAndDestroyBody(_floorId);

            _physicsSystem?.Dispose();
            _jobSystem?.Dispose();
            _bpFilter?.Dispose();
            _objectFilter?.Dispose();
            _bpLayerInterface?.Dispose();

            if (_ballGO != null) Destroy(_ballGO);
            if (_floorGO != null) Destroy(_floorGO);

            JoltAPI.Shutdown();
        }
    }
}