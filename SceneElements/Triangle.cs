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
        public Material Material { get; set; }

        /// <summary>
        /// Create triangle based off of the 3 vertex points
        /// </summary>
        /// <param name="PointA"></param>
        /// <param name="PointB"></param>
        /// <param name="PointC"></param>
        /// <param name="color"></param>
        /// <param name="material"></param>
        public Triangle(Vector3 PointA, Vector3 PointB, Vector3 PointC, Material material)
        {
            Center = (PointA + PointB + PointC) / 3;
            VectorA = PointA - Center;
            VectorB = PointB - Center;
            VectorC = PointC - Center;
            Vector3 cross = Vector3.Cross(PointB - PointA, PointC - PointA);
            Normal = cross / cross.Length;
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
        public Tuple<float, Material> RayIntersect(Ray ray)
        {
            float denominator = Vector3.Dot(ray.Direction, Normal);
            //if denominator is 0 then the vector is parallel and the ray will not hit.
            if (denominator == 0)
                return new Tuple<float, Material>(float.MinValue, Material);  
            float t = Vector3.Dot(Center - ray.Origin, Normal) / denominator;
            //the intersection can be ignored if it is behind the camera
            if (t <= 0)
                return new Tuple<float, Material>(float.MinValue, Material);
            Vector3 intersection = ray.Origin + t * ray.Direction;
            //the intersection can be ignored if the point lies outside the triangle
            if (Vector3.Dot(Vector3.Cross(PointB - PointA, intersection - PointA), Normal) < 0 
                || Vector3.Dot(Vector3.Cross(PointC - PointB, intersection - PointB), Normal) < 0
                || Vector3.Dot(Vector3.Cross(PointA - PointC, intersection - PointC), Normal) < 0)
                return new Tuple<float, Material>(float.MinValue, Material);
            return new Tuple<float, Material>(t, Material);
        }
    }
}
