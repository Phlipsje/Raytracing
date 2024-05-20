using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using INFOGR2024Template.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace INFOGR2024Template.SceneElements
{
    internal class Plane : IPrimitive
    {
        public Vector3 Center { get; set; }
        public Vector3 Normal { get; set; }
        public Material Material { get; set; }

        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            Center = center;
            Normal = normal.Normalized();
            Material = material;
        }
        public Tuple<float, Material> RayIntersect(Ray ray)
        {
            float denominator = Vector3.Dot(ray.Direction, Normal);
            if(denominator == 0)
                return new Tuple<float, Material>(float.MinValue, Material);
            return new Tuple<float, Material>(Vector3.Dot(Center - ray.Origin, Normal) / denominator , Material);
        }
        
        //NOTE: BoundingBox is not used for planes, because they are infinitely long
        public Vector3[] BoundingBox
        {
            get
            {
                Vector3[] vectors = new Vector3[2];
                vectors[0] = new Vector3(0, 0, 0);
                vectors[1] = new Vector3(0, 0, 0);
                return vectors;
            }
        }
    }
}
