using INFOGR2024Template.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace INFOGR2024Template.SceneElements
{
    public class Plane : IPrimitive
    {
        public Vector3 Center { get; set; }
        public Vector3 Normal { get; set; }
        public Material Material { get; set; }

        public Vector3 UVector { get; set; }
        public Vector3 VVector { get; set; }

        public Plane(Vector3 center, Vector3 normal, Material material, Vector3 uVector, Vector3 vVector)
        {
            Center = center;
            Normal = normal.Normalized();
            Material = material;
            UVector = uVector;
            VVector = vVector;
        }
        public Tuple<float, Material> RayIntersect(Ray ray)
        {
            float denominator = Vector3.Dot(ray.Direction, Normal);
            if(denominator == 0)
                return new Tuple<float, Material>(float.MinValue, Material);
            return new Tuple<float, Material>(Vector3.Dot(Center - ray.Origin, Normal) / denominator , Material);
        }
        
        //NOTE: BoundingBox is not used for planes, because they are infinitely long
        public BoundingBox BoundingBox
        {
            get
            {
                Vector3[] vectors = new Vector3[2];
                vectors[0] = new Vector3(0, 0, 0);
                vectors[1] = new Vector3(0, 0, 0);
                return new BoundingBox(vectors[0], vectors[1]);
            }
        }
    }
}
