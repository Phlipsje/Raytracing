using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace INFOGR2024Template.SceneElements
{
    internal class Plane : IPrimitive
    {
        public Vector3 Center { get; set; }
        public Vector3 Normal { get; set; }
        public Color4 Color { get; set; }
        public Material Material { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Plane(Vector3 center, Vector3 normal, Color4 color, Material material)
        {
            Center = center;
            Normal = normal;
            Color = color;
            Material = material;
        }
        public float RayIntersect(Ray ray)
        {
            float denominator = Vector3.Dot(ray.Direction, Normal);
            if(denominator == 0)
                return float.MinValue;
            return Vector3.Dot(Center - ray.Origin, Normal) / denominator;
        }
    }
}
