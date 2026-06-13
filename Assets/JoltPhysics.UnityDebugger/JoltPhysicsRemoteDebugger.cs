using System;
using System.Collections.Generic;
using JoltPhysics;
using MemoryPack;
using Network;
using Network.Server;
using UnityEngine;

namespace JoltPhysics.UnityDebugger
{
    public class JoltPhysicsRemoteDebugger : MonoBehaviour
    {
        [Header("Server Settings")] public int port = 24419;
        public bool compress = true;

        private NetworkServer _server;
        private TelepathyServerSocket _socket;

        // 连接管理
        private readonly Dictionary<int, WorldData> _worldDataByConn = new();
        private readonly List<int> _connIds = new();
        private int _viewIndex;
        private int _lastViewConnId = -1;

        // 场景 GameObject 管理
        private Transform _sceneRoot;
        private readonly List<DebugBodyProxy> _bodyProxies = new();

        // 颜色表
        private static readonly Color[] LayerColors =
        {
            Color.red, Color.blue, Color.green, Color.yellow, new(0.5f, 0f, 0.5f), // purple
            new(1f, 0.5f, 0f), // orange
            Color.magenta, Color.cyan, new(0.5f, 1f, 0f), // lime
            new(1f, 0.84f, 0f), // gold
        };

        // GL 绘制材质
        private Material _lineMaterial;

        private Material LineMaterial
        {
            get
            {
                if (_lineMaterial == null)
                {
                    var shader = Shader.Find("Hidden/Internal-Colored");
                    _lineMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _lineMaterial.SetInt("_ZWrite", 0);
                    _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }

                return _lineMaterial;
            }
        }

        public WorldData CurrentWorldData
        {
            get
            {
                if (_connIds.Count == 0 || _viewIndex >= _connIds.Count) return null;
                _worldDataByConn.TryGetValue(_connIds[_viewIndex], out var data);
                return data;
            }
        }

        public int CurrentConnectionId => _connIds.Count > 0 && _viewIndex < _connIds.Count ? _connIds[_viewIndex] : -1;

        private void Awake()
        {
            
            
            _socket = new TelepathyServerSocket((ushort)port);
            _server = new NetworkServer(_socket, compress: compress);
            Debug.Log(NetworkId.CalculateId<WorldDataMessage>());
            _server.AddMsgHandler<WorldDataMessage>((in int connId, in WorldDataMessage msg) =>
            {
                if (msg.data == null) return;
                try
                {
                    var data = MemoryPackSerializer.Deserialize<WorldData>(msg.data);
                    if (data != null)
                        _worldDataByConn[connId] = data;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RemoteDebugger] Deserialize error: {e.Message}");
                }
            });

            _socket.OnConnected += id =>
            {
                _connIds.Add(id);
                Debug.Log($"[RemoteDebugger] Client {id} connected (total: {_connIds.Count})");
            };

            _socket.OnDisconnected += id =>
            {
                _connIds.Remove(id);
                _worldDataByConn.Remove(id);
                if (_viewIndex >= _connIds.Count)
                    _viewIndex = Mathf.Max(0, _connIds.Count - 1);
                Debug.Log($"[RemoteDebugger] Client {id} disconnected (total: {_connIds.Count})");
            };

            _server.Run(autoTick: false);
            Debug.Log($"[RemoteDebugger] Listening on port {port}...");
        }

        private void Update()
        {
            _server?.OnUpdate(0);
            SyncSceneObjects();
        }

        private void OnDestroy()
        {
            _server?.Stop();
            _server?.Dispose();
            ClearSceneObjects();
            if (_lineMaterial != null)
                DestroyImmediate(_lineMaterial);
        }

        #region 场景 GameObject 同步

        private void EnsureSceneRoot()
        {
            if (_sceneRoot != null) return;
            var go = new GameObject("[JoltDebugger] Bodies");
            go.hideFlags = HideFlags.DontSave;
            _sceneRoot = go.transform;
        }

        private void ClearSceneObjects()
        {
            foreach (var proxy in _bodyProxies)
            {
                if (proxy != null)
                    DestroyImmediate(proxy.gameObject);
            }

            _bodyProxies.Clear();

            if (_sceneRoot != null)
            {
                DestroyImmediate(_sceneRoot.gameObject);
                _sceneRoot = null;
            }
        }

        private void SyncSceneObjects()
        {
            var worldData = CurrentWorldData;
            int connId = CurrentConnectionId;

            // 切换了连接 或 连接断开 → 清除旧对象
            if (connId != _lastViewConnId)
            {
                ClearSceneObjects();
                _lastViewConnId = connId;
            }

            if (worldData?.bodies == null || worldData.bodies.Count == 0)
            {
                if (_bodyProxies.Count > 0)
                    ClearSceneObjects();
                return;
            }

            EnsureSceneRoot();

            var bodies = worldData.bodies;

            // 数量变化 → 调整
            if (_bodyProxies.Count != bodies.Count)
            {
                // 清除多余的
                while (_bodyProxies.Count > bodies.Count)
                {
                    int last = _bodyProxies.Count - 1;
                    if (_bodyProxies[last] != null)
                        DestroyImmediate(_bodyProxies[last].gameObject);
                    _bodyProxies.RemoveAt(last);
                }

                // 补充不足的
                while (_bodyProxies.Count < bodies.Count)
                {
                    var go = new GameObject();
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(_sceneRoot, false);
                    var proxy = go.AddComponent<DebugBodyProxy>();
                    _bodyProxies.Add(proxy);
                }
            }

            // 同步 Transform 和属性
            for (int i = 0; i < bodies.Count; i++)
            {
                ref var body = ref bodies.Array[i];
                var proxy = _bodyProxies[i];
                if (body == null || proxy == null) continue;

                // 名字
                string displayName = string.IsNullOrEmpty(body.name) ? $"Body_{i}" : body.name;
                if (proxy.gameObject.name != displayName)
                    proxy.gameObject.name = displayName;

                // Transform
                proxy.transform.position = new Vector3(body.position.x, body.position.y, body.position.z);
                proxy.transform.rotation = new Quaternion(body.rotation.x, body.rotation.y, body.rotation.z,
                    body.rotation.w);

                // Inspector 属性
                proxy.SyncFrom(body);
            }
        }

        #endregion

        #region OnGUI - 连接切换按钮

        private void OnGUI()
        {
            float x = 10, y = 10;
            const float btnW = 180, btnH = 30, gap = 5;

            // 标题
            GUI.Label(new Rect(x, y, 300, 20), $"<b><color=white>Jolt Remote Debugger (port {port})</color></b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            y += 25;

            if (_connIds.Count == 0)
            {
                GUI.Label(new Rect(x, y, 300, 20), "<color=#888888>Waiting for connections...</color>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                return;
            }

            // 连接按钮列表
            for (int i = 0; i < _connIds.Count; i++)
            {
                int connId = _connIds[i];
                bool isCurrent = i == _viewIndex;
                int bodyCount = 0;
                if (_worldDataByConn.TryGetValue(connId, out var wd) && wd.bodies != null)
                    bodyCount = wd.bodies.Array.Length;

                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = isCurrent ? Color.green : Color.gray;

                string label = isCurrent
                    ? $">> Conn {connId} ({bodyCount} bodies)"
                    : $"Conn {connId} ({bodyCount} bodies)";

                if (GUI.Button(new Rect(x, y, btnW, btnH), label))
                {
                    _viewIndex = i;
                }

                GUI.backgroundColor = prevColor;
                y += btnH + gap;
            }

            // 当前查看信息
            y += 5;
            var currentData = CurrentWorldData;
            if (currentData != null)
            {
                GUI.Label(new Rect(x, y, 300, 20),
                    $"<color=yellow>Viewing Conn {CurrentConnectionId} | Bodies: {currentData.bodies.Array?.Length ?? 0}</color>",
                    new GUIStyle(GUI.skin.label) { richText = true });
            }
        }

        #endregion

        #region Gizmos - Scene View 绘制

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            var worldData = CurrentWorldData;
            if (worldData?.bodies == null) return;

            foreach (var body in worldData.bodies)
            {
                if (body == null) continue;
                DrawBodyGizmos(body);
            }
        }

        private void DrawBodyGizmos(BodyData body)
        {
            var basePos = new Vector3(body.position.x, body.position.y, body.position.z);
            var rot = new Quaternion(body.rotation.x, body.rotation.y, body.rotation.z, body.rotation.w);
            var localCenter = body.shape?.center ?? default;
            var pos = basePos + new Vector3(localCenter.x, localCenter.y, localCenter.z);
            var color = LayerColors[body.objectLayer % (uint)LayerColors.Length];
            var wireColor = DarkenColor(color, 0.3f);

            // Body 局部坐标轴
            Gizmos.color = Color.red;
            Gizmos.DrawLine(basePos, basePos + rot * Vector3.right);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(basePos, basePos + rot * Vector3.up);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(basePos, basePos + rot * Vector3.forward);

            if (body.shape == null)
            {
                Gizmos.color = color;
                Gizmos.DrawWireSphere(pos, 0.3f);
                return;
            }

            Gizmos.color = wireColor;
            var prevMatrix = Gizmos.matrix;

            switch (body.shape.type)
            {
                case JoltShapeType.Box:
                {
                    var he = body.shape.halfExtent;
                    var size = new Vector3(he.x * 2, he.y * 2, he.z * 2);
                    Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, size);
                    Gizmos.matrix = prevMatrix;
                    break;
                }

                case JoltShapeType.Sphere:
                    Gizmos.DrawWireSphere(pos, body.shape.radius);
                    break;

                case JoltShapeType.Ellipsoid:
                {
                    var radii = body.shape.radii;
                    if (radii.x > 0 || radii.y > 0 || radii.z > 0)
                    {
                        Gizmos.matrix = Matrix4x4.TRS(pos, rot, new Vector3(radii.x, radii.y, radii.z));
                        Gizmos.DrawWireSphere(Vector3.zero, 1f);
                        Gizmos.matrix = prevMatrix;
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(pos, 0.5f);
                    }

                    break;
                }

                case JoltShapeType.Capsule:
                    DrawWireCapsuleGizmos(pos, rot, body.shape.halfHeight, body.shape.radius, wireColor);
                    break;

                case JoltShapeType.ConvexHull:
                {
                    if (body.shape.vertices != null && body.shape.vertices.Length > 0)
                    {
                        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        foreach (var v in body.shape.vertices)
                        {
                            var w = basePos + rot * new Vector3(v.x, v.y, v.z);
                            Gizmos.color = color;
                            Gizmos.DrawSphere(w, 0.05f);
                            min = Vector3.Min(min, w);
                            max = Vector3.Max(max, w);
                        }

                        Gizmos.color = wireColor;
                        var aabbCenter = (min + max) * 0.5f;
                        var aabbSize = max - min;
                        Gizmos.DrawWireCube(aabbCenter, aabbSize);
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(pos, 0.4f);
                    }

                    break;
                }

                case JoltShapeType.Mesh:
                {
                    if (body.shape.vertices != null && body.shape.triangles != null &&
                        body.shape.triangles.Length >= 3)
                    {
                        var verts = body.shape.vertices;
                        var tris = body.shape.triangles;
                        for (int ti = 0; ti + 2 < tris.Length; ti += 3)
                        {
                            int i0 = tris[ti], i1 = tris[ti + 1], i2 = tris[ti + 2];
                            if (i0 < 0 || i1 < 0 || i2 < 0) continue;
                            if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;
                            var v0 = basePos + rot * new Vector3(verts[i0].x, verts[i0].y, verts[i0].z);
                            var v1 = basePos + rot * new Vector3(verts[i1].x, verts[i1].y, verts[i1].z);
                            var v2 = basePos + rot * new Vector3(verts[i2].x, verts[i2].y, verts[i2].z);
                            Gizmos.DrawLine(v0, v1);
                            Gizmos.DrawLine(v1, v2);
                            Gizmos.DrawLine(v2, v0);
                        }
                    }
                    else if (body.shape.vertices != null)
                    {
                        foreach (var v in body.shape.vertices)
                        {
                            var w = pos + new Vector3(v.x, v.y, v.z);
                            Gizmos.DrawSphere(w, 0.03f);
                        }
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(pos, 0.3f);
                    }

                    break;
                }

                default:
                    Gizmos.DrawWireSphere(pos, 0.5f);
                    break;
            }
        }

        private static void DrawWireCapsuleGizmos(Vector3 center, Quaternion rotation, float halfHeight, float radius,
            Color color)
        {
            Gizmos.color = color;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;
            var forward = rotation * Vector3.forward;
            var topCenter = center + up * halfHeight;
            var bottomCenter = center - up * halfHeight;

            const int segments = 24;

            // 上下圆环
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var d0 = right * Mathf.Cos(a0) * radius + forward * Mathf.Sin(a0) * radius;
                var d1 = right * Mathf.Cos(a1) * radius + forward * Mathf.Sin(a1) * radius;
                Gizmos.DrawLine(topCenter + d0, topCenter + d1);
                Gizmos.DrawLine(bottomCenter + d0, bottomCenter + d1);
            }

            // 4 条竖直线
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var d = right * Mathf.Cos(a) * radius + forward * Mathf.Sin(a) * radius;
                Gizmos.DrawLine(topCenter + d, bottomCenter + d);
            }

            // 半球弧线
            const int arcSeg = 12;
            Vector3[] arcAxes = { right, forward };
            foreach (var axis in arcAxes)
            {
                for (int i = 0; i < arcSeg; i++)
                {
                    float a0 = i * Mathf.PI * 0.5f / arcSeg;
                    float a1 = (i + 1) * Mathf.PI * 0.5f / arcSeg;
                    // 上半球
                    Gizmos.DrawLine(
                        topCenter + axis * Mathf.Cos(a0) * radius + up * Mathf.Sin(a0) * radius,
                        topCenter + axis * Mathf.Cos(a1) * radius + up * Mathf.Sin(a1) * radius);
                    Gizmos.DrawLine(
                        topCenter - axis * Mathf.Cos(a0) * radius + up * Mathf.Sin(a0) * radius,
                        topCenter - axis * Mathf.Cos(a1) * radius + up * Mathf.Sin(a1) * radius);
                    // 下半球
                    Gizmos.DrawLine(
                        bottomCenter + axis * Mathf.Cos(a0) * radius - up * Mathf.Sin(a0) * radius,
                        bottomCenter + axis * Mathf.Cos(a1) * radius - up * Mathf.Sin(a1) * radius);
                    Gizmos.DrawLine(
                        bottomCenter - axis * Mathf.Cos(a0) * radius - up * Mathf.Sin(a0) * radius,
                        bottomCenter - axis * Mathf.Cos(a1) * radius - up * Mathf.Sin(a1) * radius);
                }
            }
        }

        #endregion

        #region GL - Game View 绘制

        private void OnRenderObject()
        {
            var worldData = CurrentWorldData;
            if (worldData?.bodies == null) return;

            LineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            // 绘制原点坐标轴
            GL.Begin(GL.LINES);
            GLLine(Vector3.zero, Vector3.right * 5f, Color.red);
            GLLine(Vector3.zero, Vector3.up * 5f, Color.green);
            GLLine(Vector3.zero, Vector3.forward * 5f, Color.blue);
            GL.End();

            // 绘制所有 Body
            foreach (var body in worldData.bodies)
            {
                if (body == null) continue;
                DrawBodyGL(body);
            }

            GL.PopMatrix();
        }

        private void DrawBodyGL(BodyData body)
        {
            var basePos = new Vector3(body.position.x, body.position.y, body.position.z);
            var rot = new Quaternion(body.rotation.x, body.rotation.y, body.rotation.z, body.rotation.w);
            var localCenter = body.shape?.center ?? default;
            var pos = basePos + new Vector3(localCenter.x, localCenter.y, localCenter.z);
            var color = LayerColors[body.objectLayer % (uint)LayerColors.Length];
            var wireColor = DarkenColor(color, 0.3f);

            // Body 局部坐标轴
            GL.Begin(GL.LINES);
            GLLine(basePos, basePos + rot * Vector3.right, Color.red);
            GLLine(basePos, basePos + rot * Vector3.up, Color.green);
            GLLine(basePos, basePos + rot * Vector3.forward, Color.blue);
            GL.End();

            if (body.shape == null)
            {
                GLWireSphere(pos, 0.3f, wireColor);
                return;
            }

            switch (body.shape.type)
            {
                case JoltShapeType.Box:
                {
                    var he = body.shape.halfExtent;
                    Vector3[] corners = new Vector3[8];
                    corners[0] = new Vector3(-he.x, -he.y, -he.z);
                    corners[1] = new Vector3(he.x, -he.y, -he.z);
                    corners[2] = new Vector3(he.x, he.y, -he.z);
                    corners[3] = new Vector3(-he.x, he.y, -he.z);
                    corners[4] = new Vector3(-he.x, -he.y, he.z);
                    corners[5] = new Vector3(he.x, -he.y, he.z);
                    corners[6] = new Vector3(he.x, he.y, he.z);
                    corners[7] = new Vector3(-he.x, he.y, he.z);
                    for (int i = 0; i < 8; i++)
                        corners[i] = pos + rot * corners[i];

                    GL.Begin(GL.LINES);
                    // 底面
                    GLLine(corners[0], corners[1], wireColor);
                    GLLine(corners[1], corners[2], wireColor);
                    GLLine(corners[2], corners[3], wireColor);
                    GLLine(corners[3], corners[0], wireColor);
                    // 顶面
                    GLLine(corners[4], corners[5], wireColor);
                    GLLine(corners[5], corners[6], wireColor);
                    GLLine(corners[6], corners[7], wireColor);
                    GLLine(corners[7], corners[4], wireColor);
                    // 竖边
                    GLLine(corners[0], corners[4], wireColor);
                    GLLine(corners[1], corners[5], wireColor);
                    GLLine(corners[2], corners[6], wireColor);
                    GLLine(corners[3], corners[7], wireColor);
                    GL.End();
                    break;
                }

                case JoltShapeType.Sphere:
                    GLWireSphere(pos, body.shape.radius, wireColor);
                    break;

                case JoltShapeType.Ellipsoid:
                {
                    var radii = body.shape.radii;
                    GLWireEllipsoid(pos, rot, new Vector3(radii.x, radii.y, radii.z), wireColor);
                    break;
                }

                case JoltShapeType.Capsule:
                    GLWireCapsule(pos, rot, body.shape.halfHeight, body.shape.radius, wireColor);
                    break;

                case JoltShapeType.ConvexHull:
                {
                    if (body.shape.vertices != null && body.shape.vertices.Length > 0)
                    {
                        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        foreach (var v in body.shape.vertices)
                        {
                            var w = basePos + rot * new Vector3(v.x, v.y, v.z);
                            min = Vector3.Min(min, w);
                            max = Vector3.Max(max, w);
                        }

                        GLWireBox((min + max) * 0.5f, Quaternion.identity, (max - min) * 0.5f, wireColor);
                    }

                    break;
                }

                case JoltShapeType.Mesh:
                {
                    if (body.shape.vertices != null && body.shape.triangles != null &&
                        body.shape.triangles.Length >= 3)
                    {
                        var verts = body.shape.vertices;
                        var tris = body.shape.triangles;
                        GL.Begin(GL.LINES);
                        for (int ti = 0; ti + 2 < tris.Length; ti += 3)
                        {
                            int i0 = tris[ti], i1 = tris[ti + 1], i2 = tris[ti + 2];
                            if (i0 < 0 || i1 < 0 || i2 < 0) continue;
                            if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;
                            var v0 = basePos + rot * new Vector3(verts[i0].x, verts[i0].y, verts[i0].z);
                            var v1 = basePos + rot * new Vector3(verts[i1].x, verts[i1].y, verts[i1].z);
                            var v2 = basePos + rot * new Vector3(verts[i2].x, verts[i2].y, verts[i2].z);
                            GLLine(v0, v1, wireColor);
                            GLLine(v1, v2, wireColor);
                            GLLine(v2, v0, wireColor);
                        }

                        GL.End();
                    }

                    break;
                }

                default:
                    GLWireSphere(pos, 0.5f, wireColor);
                    break;
            }
        }

        #endregion

        #region GL 绘制工具方法

        private static void GLLine(Vector3 a, Vector3 b, Color color)
        {
            GL.Color(color);
            GL.Vertex(a);
            GL.Vertex(b);
        }

        private static void GLWireSphere(Vector3 center, float radius, Color color, int segments = 16)
        {
            GL.Begin(GL.LINES);
            // XY 平面圆
            DrawGLCircle(center, Vector3.right, Vector3.up, radius, color, segments);
            // XZ 平面圆
            DrawGLCircle(center, Vector3.right, Vector3.forward, radius, color, segments);
            // YZ 平面圆
            DrawGLCircle(center, Vector3.up, Vector3.forward, radius, color, segments);
            GL.End();
        }

        private static void GLWireEllipsoid(Vector3 center, Quaternion rot, Vector3 radii, Color color,
            int segments = 16)
        {
            var r = rot * Vector3.right;
            var u = rot * Vector3.up;
            var f = rot * Vector3.forward;

            GL.Begin(GL.LINES);
            DrawGLEllipse(center, r, u, radii.x, radii.y, color, segments);
            DrawGLEllipse(center, r, f, radii.x, radii.z, color, segments);
            DrawGLEllipse(center, u, f, radii.y, radii.z, color, segments);
            GL.End();
        }

        private static void GLWireBox(Vector3 center, Quaternion rot, Vector3 halfExtent, Color color)
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(-halfExtent.x, -halfExtent.y, -halfExtent.z);
            corners[1] = new Vector3(halfExtent.x, -halfExtent.y, -halfExtent.z);
            corners[2] = new Vector3(halfExtent.x, halfExtent.y, -halfExtent.z);
            corners[3] = new Vector3(-halfExtent.x, halfExtent.y, -halfExtent.z);
            corners[4] = new Vector3(-halfExtent.x, -halfExtent.y, halfExtent.z);
            corners[5] = new Vector3(halfExtent.x, -halfExtent.y, halfExtent.z);
            corners[6] = new Vector3(halfExtent.x, halfExtent.y, halfExtent.z);
            corners[7] = new Vector3(-halfExtent.x, halfExtent.y, halfExtent.z);
            for (int i = 0; i < 8; i++)
                corners[i] = center + rot * corners[i];

            GL.Begin(GL.LINES);
            GLLine(corners[0], corners[1], color);
            GLLine(corners[1], corners[2], color);
            GLLine(corners[2], corners[3], color);
            GLLine(corners[3], corners[0], color);
            GLLine(corners[4], corners[5], color);
            GLLine(corners[5], corners[6], color);
            GLLine(corners[6], corners[7], color);
            GLLine(corners[7], corners[4], color);
            GLLine(corners[0], corners[4], color);
            GLLine(corners[1], corners[5], color);
            GLLine(corners[2], corners[6], color);
            GLLine(corners[3], corners[7], color);
            GL.End();
        }

        private static void GLWireCapsule(Vector3 center, Quaternion rotation, float halfHeight, float radius,
            Color color)
        {
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;
            var forward = rotation * Vector3.forward;
            var topCenter = center + up * halfHeight;
            var bottomCenter = center - up * halfHeight;

            const int segments = 24;
            const int arcSeg = 12;

            GL.Begin(GL.LINES);

            // 上下圆环
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var d0 = right * Mathf.Cos(a0) * radius + forward * Mathf.Sin(a0) * radius;
                var d1 = right * Mathf.Cos(a1) * radius + forward * Mathf.Sin(a1) * radius;
                GLLine(topCenter + d0, topCenter + d1, color);
                GLLine(bottomCenter + d0, bottomCenter + d1, color);
            }

            // 4条竖线
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var d = right * Mathf.Cos(a) * radius + forward * Mathf.Sin(a) * radius;
                GLLine(topCenter + d, bottomCenter + d, color);
            }

            // 半球弧线
            Vector3[] arcAxes = { right, forward };
            foreach (var axis in arcAxes)
            {
                for (int i = 0; i < arcSeg; i++)
                {
                    float a0 = i * Mathf.PI * 0.5f / arcSeg;
                    float a1 = (i + 1) * Mathf.PI * 0.5f / arcSeg;
                    // 上半球
                    GLLine(
                        topCenter + axis * Mathf.Cos(a0) * radius + up * Mathf.Sin(a0) * radius,
                        topCenter + axis * Mathf.Cos(a1) * radius + up * Mathf.Sin(a1) * radius, color);
                    GLLine(
                        topCenter - axis * Mathf.Cos(a0) * radius + up * Mathf.Sin(a0) * radius,
                        topCenter - axis * Mathf.Cos(a1) * radius + up * Mathf.Sin(a1) * radius, color);
                    // 下半球
                    GLLine(
                        bottomCenter + axis * Mathf.Cos(a0) * radius - up * Mathf.Sin(a0) * radius,
                        bottomCenter + axis * Mathf.Cos(a1) * radius - up * Mathf.Sin(a1) * radius, color);
                    GLLine(
                        bottomCenter - axis * Mathf.Cos(a0) * radius - up * Mathf.Sin(a0) * radius,
                        bottomCenter - axis * Mathf.Cos(a1) * radius - up * Mathf.Sin(a1) * radius, color);
                }
            }

            GL.End();
        }

        private static void DrawGLCircle(Vector3 center, Vector3 axis1, Vector3 axis2, float radius, Color color,
            int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var p0 = center + (axis1 * Mathf.Cos(a0) + axis2 * Mathf.Sin(a0)) * radius;
                var p1 = center + (axis1 * Mathf.Cos(a1) + axis2 * Mathf.Sin(a1)) * radius;
                GLLine(p0, p1, color);
            }
        }

        private static void DrawGLEllipse(Vector3 center, Vector3 axis1, Vector3 axis2, float r1, float r2, Color color,
            int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var p0 = center + axis1 * Mathf.Cos(a0) * r1 + axis2 * Mathf.Sin(a0) * r2;
                var p1 = center + axis1 * Mathf.Cos(a1) * r1 + axis2 * Mathf.Sin(a1) * r2;
                GLLine(p0, p1, color);
            }
        }

        #endregion

        #region 工具方法

        private static Color DarkenColor(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r - amount),
                Mathf.Clamp01(color.g - amount),
                Mathf.Clamp01(color.b - amount),
                color.a);
        }

        #endregion
    }
}