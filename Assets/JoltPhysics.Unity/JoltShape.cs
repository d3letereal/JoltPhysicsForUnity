// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    [DisallowMultipleComponent]
    public abstract class JoltShape : MonoBehaviour
    {
        /// <summary>
        /// Creates the Jolt Shape. The caller is responsible for disposing it.
        /// </summary>
        public abstract Shape CreateShape();

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
        }
#endif
    }
}
