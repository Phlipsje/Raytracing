using INFOGR2024Template;
using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

public class Sphere : IPrimitive
{
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
    public Material Material { get; set; }
    
    public Sphere(Vector3 center, float radius, Material material)
    {
        Center = center;
        Radius = radius;
        Material = material;
    }

    //only works if ray.Direction is normalised (should be the case)
    public Tuple<float, Material> RayIntersect(Ray ray)
    {
        //note that the a in the discriminant is always 1 as the ray is normalised
        //Algorithm for ABC formule
        Vector3 V = ray.Origin - Center;
        float b = 2 * (ray.Direction.X * V.X + ray.Direction.Y * V.Y + ray.Direction.Z * V.Z);
        float c = MathF.Pow(ray.Origin.X - Center.X, 2) + MathF.Pow(ray.Origin.Y - Center.Y, 2) + MathF.Pow(ray.Origin.Z - Center.Z, 2) - MathF.Pow(Radius, 2);
        float discriminant = MathF.Pow(b, 2) - 4 * c;
        if (discriminant < 0)
            return new Tuple<float, Material>( float.MinValue, Material);
        float rootOfDiscriminant = MathF.Sqrt(discriminant);
        float t1 = (-b + rootOfDiscriminant) / 2;
        float t2 = (-b - rootOfDiscriminant) / 2;
        if(t1 <= 0f)
            return new Tuple<float, Material>(float.MinValue, Material);
        //small margin to avoid intersecting with itself in shadow calculation
        if (t2 <= 0.001f)
            return new Tuple<float, Material>(t1, Material);
        return new Tuple<float, Material>(t2, Material);
    }

    public Vector3[] BoundingBox
    {
        get
        {
            Vector3[] vectors = new Vector3[2];
            vectors[0] = new Vector3(Center.X - Radius, Center.Y - Radius, Center.Z - Radius);
            vectors[1] = new Vector3(Center.X + Radius, Center.Y + Radius, Center.Z + Radius);
            return vectors;
        }
    }
}