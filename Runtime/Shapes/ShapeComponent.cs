﻿using System;

namespace UnityEngine.ProBuilder
{
    [RequireComponent(typeof(ProBuilderMesh))]
    public class ShapeComponent : MonoBehaviour
    {
        [SerializeReference]
        Shape m_shape = new Cube();

        public Shape m_Shape => m_shape;

        ProBuilderMesh m_Mesh;

        [SerializeField]
        Vector3 m_Size;

        [HideInInspector]
        [SerializeField]
        Quaternion m_RotationQuaternion = Quaternion.identity;
        Vector3[] m_OrigVertex;

        public Vector3 size
        {
            get { return m_Size; }
            set { m_Size = value; }
        }

        public ProBuilderMesh mesh
        {
            get { return m_Mesh == null ? m_Mesh = GetComponent<ProBuilderMesh>() : m_Mesh; }
        }

        // Bounds where center is in world space, size is mesh.bounds.size
        internal Bounds meshFilterBounds
        {
            get
            {
                var mb = mesh.mesh.bounds;
                return new Bounds(transform.TransformPoint(mb.center), mb.size);
            }
        }

        public void Rebuild(Bounds bounds, Quaternion rotation)
        {
            size = Math.Abs(bounds.size);
            transform.position = bounds.center;
            transform.rotation = rotation;
            Rebuild();
        }

        public void Rebuild()
        {
            m_shape.RebuildMesh(mesh, size);
            m_OrigVertex = mesh.mesh.vertices;
            RotateTo(m_RotationQuaternion);
            FitToSize();
        }

        public void SetShape(Shape shape)
        {
            m_shape = shape;
            Rebuild();
        }

        void FitToSize()
        {
            if (mesh.vertexCount < 1)
                return;

            var scale = size.DivideBy(mesh.mesh.bounds.size);
            if (scale == Vector3.one)
                return;

            var positions = mesh.positionsInternal;

            if (System.Math.Abs(mesh.mesh.bounds.size.x) < 0.001f)
                scale.x = 0;
            if (System.Math.Abs(mesh.mesh.bounds.size.y) < 0.001f)
                scale.y = 0;
            if (System.Math.Abs(mesh.mesh.bounds.size.z) < 0.001f)
                scale.z = 0;

            for (int i = 0, c = mesh.vertexCount; i < c; i++)
            {
                positions[i] -= mesh.mesh.bounds.center;
                positions[i].Scale(scale);
            }

            mesh.ToMesh();
            mesh.Rebuild();
        }

        /// <summary>
        /// Set the rotation of the Shape to a given set of eular angles, then rotates it
        /// </summary>
        /// <param name="eulerAngles">The angles to rotate by</param>
        public void SetRotation(Vector3 eulerAngles)
        {
            m_RotationQuaternion = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            RotateTo(m_RotationQuaternion);
        }

        /// <summary>
        /// Rotates the Shape by a given set of eular angles
        /// </summary>
        /// <param name="eulerAngles">The angles to rotate by</param>
        public void RotateBy(Vector3 eulerAngles)
        {
            Quaternion rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            m_RotationQuaternion = rotation * m_RotationQuaternion;
            RotateTo(m_RotationQuaternion);
            FitToSize();
        }

        void RotateTo(Quaternion angles)
        {
            if (angles == Quaternion.identity)
            {
                return;
            }

           var newVerts = new Vector3[m_OrigVertex.Length];

            int i = 0;
            while (i < m_OrigVertex.Length)
            {
                newVerts[i] = angles * m_OrigVertex[i];
                i++;
            }
            mesh.mesh.vertices = newVerts;
            mesh.ReplaceVertices(newVerts);
        }
    }
}
