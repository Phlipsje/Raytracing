using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace INFOGR2024Template.SceneElements
{
    //all padding floats here are for memory alignment, they can be ignored
    //the idea is that vec3's in glsl can't be tightly packed so they are stored in 4-word boundaries. By adding a float here in C# to make up for it the alignment can be restored
    [StructLayout(LayoutKind.Sequential)]
    internal struct SphereStruct
    {
        public Vector3 Center;
        public float Radius;
        public Vector3 DiffuseColor;
        public float Specularity;
        public Vector3 SpecularColor;
        private float padding = 0;
        public SphereStruct(Vector3 center, float radius, Vector3 diffuseColor, Vector3 specularColor, float specularity)
        {
            Center = center;
            Radius = radius;
            DiffuseColor = diffuseColor;
            SpecularColor = specularColor;
            Specularity = specularity;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PlaneStruct
    {
        public Vector3 Position;
        float padding0 = 0;
        public Vector3 Normal;
        float padding1 = 0;
        public Vector3 DiffuseColor;
        float padding2 = 0;
        public Vector3 SpecularColor;
        public float Specularity;
        public PlaneStruct(Vector3 position, Vector3 normal, Vector3 diffuseColor, Vector3 specularColor, float specularity)
        {
            Position = position;
            Normal = normal;
            DiffuseColor = diffuseColor;
            SpecularColor = specularColor;
            Specularity = specularity;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct TriangleStruct
    {
        public Vector3 PointA;
        float padding0 = 0;
        public Vector3 PointB;
        float padding1 = 0;
        public Vector3 PointC;
        float padding2 = 0;
        public Vector3 Normal;
        float padding3 = 0;
        public Vector3 DiffuseColor;
        float padding4 = 0;
        public Vector3 SpecularColor;
        public float Specularity;
        public TriangleStruct(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 normal, Vector3 diffuseColor, Vector3 specularColor, float specularity)
        {
            PointA = pointA;
            PointB = pointB;
            PointC = pointC;
            Normal = normal;
            DiffuseColor = diffuseColor;
            SpecularColor = specularColor;
            Specularity = specularity;
        }
    }
}
