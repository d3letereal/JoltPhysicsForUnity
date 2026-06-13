using JoltPhysics;
using UnityEngine;

namespace JoltPhysics.UnityDebugger
{
    /// <summary>
    /// 挂在调试用 GameObject 上，方便在 Inspector 中查看 Body 属性。
    /// 选中后 Scene View 会自动聚焦到该物体位置。
    /// </summary>
    public class DebugBodyProxy : MonoBehaviour
    {
        [Header("Body Info")]
        public string bodyName;
        public MotionType motionType;
        public uint objectLayer;
        public float friction;
        public float restitution;
        public float linearDamping;
        public float angularDamping;
        public float gravityFactor;

        [Header("Shape Info")]
        public JoltShapeType shapeType;
        public Vector3 shapeCenter;
        public Vector3 halfExtent;
        public float radius;
        public float halfHeight;
        public Vector3 radii;
        public int vertexCount;
        public int triangleCount;

        /// <summary>
        /// 从 BodyData 同步所有可显示属性
        /// </summary>
        public void SyncFrom(BodyData body)
        {
            bodyName = body.name ?? "";
            motionType = body.motionType;
            objectLayer = body.objectLayer;
            friction = body.friction;
            restitution = body.restitution;
            linearDamping = body.linearDamping;
            angularDamping = body.angularDamping;
            gravityFactor = body.gravityFactor;

            if (body.shape != null)
            {
                shapeType = body.shape.type;
                shapeCenter = new Vector3(body.shape.center.x, body.shape.center.y, body.shape.center.z);
                halfExtent = new Vector3(body.shape.halfExtent.x, body.shape.halfExtent.y, body.shape.halfExtent.z);
                radius = body.shape.radius;
                halfHeight = body.shape.halfHeight;
                radii = new Vector3(body.shape.radii.x, body.shape.radii.y, body.shape.radii.z);
                vertexCount = body.shape.vertices?.Length ?? 0;
                triangleCount = body.shape.triangles != null ? body.shape.triangles.Length / 3 : 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 选中时绘制高亮圆环，便于定位
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
