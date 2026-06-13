// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.Unity
{
    public static class MathExtensions
    {
        public static Vector3 ToUnity(this Float3 v) => new(v.x, v.y, v.z);
        public static Float3 ToJolt(this Vector3 v) => new(v.x, v.y, v.z);

        public static Quaternion ToUnity(this Quat q) => new(q.x, q.y, q.z, q.w);
        public static Quat ToJolt(this Quaternion q) => new(q.x, q.y, q.z, q.w);
    }
}
