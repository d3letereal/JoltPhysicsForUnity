// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [AddComponentMenu("Jolt Physics/Sphere Shape")]
    public sealed class JoltSphereShape : JoltShape
    {
        [SerializeField] float _radius = 0.5f;

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public override Shape CreateShape()
        {
            return new SphereShape(_radius);
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireSphere(Vector3.zero, _radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
