// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [AddComponentMenu("Jolt Physics/Box Shape")]
    public sealed class JoltBoxShape : JoltShape
    {
        [SerializeField] Vector3 _center = Vector3.zero;
        [SerializeField] Vector3 _size = Vector3.one;
        [SerializeField] float _convexRadius = 0.05f;

        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }

        public Vector3 Size
        {
            get => _size;
            set => _size = Vector3.Max(value, Vector3.one * 0.001f);
        }

        public float ConvexRadius
        {
            get => _convexRadius;
            set => _convexRadius = Mathf.Max(0f, value);
        }

        public override Shape CreateShape()
        {
            var halfExtent = (_size * 0.5f).ToJolt();
            return new BoxShape(halfExtent, _convexRadius);
        }

        /// <summary>
        /// Returns the local-space offset for the shape center.
        /// Used by JoltBody to apply center offset when creating the body.
        /// </summary>
        public Vector3 GetShapeCenter() => _center;

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_center, _size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
