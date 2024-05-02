using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace INFOGR2024Template.SceneElements
{
    internal class Triangle : IPrimitive
    {
        public Vector3 Center { get; set; }
        public Vector3 Normal { get; private set; }
        public Vector3 VectorA { get; private set; }
        public Vector3 VectorB { get; private set; }
        public Vector3 VectorC { get; private set; }
        public Vector3 PointA => Center + VectorA;
        public Vector3 PointB => Center + VectorB;
        public Vector3 PointC => Center + VectorC;
        public Color4 Color { get; set; }
        public Material Material { get; set; }

        public Triangle(Vector3 PointA, Vector3 PointB, Vector3 PointC, Color4 color, Material material)
        {
            Center = (PointA + PointB + PointC) / 3;
            VectorA = PointA - Center;
            VectorB = PointB - Center;
            VectorC = PointC - Center;
            Vector3 cross = Vector3.Cross(PointB - PointA, PointC - PointA);
            Normal = cross / cross.Length;
            Color = color;
            Material = material;
        }
        /*public Triangle(Vector3 center, Vector3 vecA, Vector3 vecB, Vector3 vecC, Color4 color, Material material)
        {
            to be added
        }*/
        /*public Triangle(Vector3 center, Vector3 normal, float halfDimension, Color4 color, Material material)
        {   
            to be added
        }*/
        public float RayIntersect(Ray ray)
        {
            float denominator = Vector3.Dot(ray.Direction, Normal);
            //if denominator is 0 then the vector is parallel and the ray will not hit.
            if (denominator == 0)
                return float.MinValue;
            float t = Vector3.Dot(Center - ray.Origin, Normal) / denominator;
            //the intersection can be ignored if it is behind the camera
            if (t <= 0)
                return float.MinValue;
            Vector3 intersection = ray.Origin + t * ray.Direction;
            //the intersection can be ignored if the point lies outside the triangle
            if (Vector3.Dot(Vector3.Cross(PointB - PointA, intersection - PointA), Normal) < 0 
                || Vector3.Dot(Vector3.Cross(PointB - PointA, intersection - PointA), Normal) < 0
                || Vector3.Dot(Vector3.Cross(PointB - PointA, intersection - PointA), Normal) < 0)
                return float.MinValue;
            return t;
        }
    }
}
