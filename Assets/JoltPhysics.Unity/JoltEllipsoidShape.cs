// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [AddComponentMenu("Jolt Physics/Ellipsoid Shape")]
    public sealed class JoltEllipsoidShape : JoltShape
    {
        [SerializeField] Vector3 _radii = new(0.5f, 0.5f, 0.5f);

        public Vector3 Radii
        {
            get => _radii;
            set => _radii = value;
        }

        public override Shape CreateShape()
        {
            // Jolt has no native ellipsoid — use a unit sphere scaled by radii.
            var sphere = new SphereShape(1.0f);
            var scaled = new ScaledShape(sphere, _radii.ToJolt());
            // ScaledShape adds a reference to the inner shape,
            // so we can release our handle to the sphere.
            sphere.Dispose();
            return scaled;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, _radii);
            Gizmos.DrawWireSphere(Vector3.zero, 1.0f);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
