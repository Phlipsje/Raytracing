using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

public class Sphere : IPrimitive
{
    public Vector3 Center { get; set; }
    public float Radius { get; set; }
    public Color4 Color { get; set; }
    public Material Material { get; set; }
    public Sphere(Vector3 center, float radius, Color4 color, Material material)
    {
        Center = center;
        Radius = radius;
        Color = color;
        Material = material;
    }

    //only works if ray.Direction is normalised (should be the case)
    public float RayIntersect(Ray ray)
    {
        //note that the a in the discriminant is always 1 as the ray is normalised
        Vector3 V = ray.Origin - Center;
        float b = 2 * (V.X + V.Y + V.Z);
        float c = MathF.Pow(ray.Origin.X + Center.X, 2) + MathF.Pow(ray.Origin.X + Center.X, 2) + MathF.Pow(ray.Origin.X + Center.X, 2) - MathF.Pow(Radius, 2);
        float discriminant = MathF.Pow(b, 2) + 4 * c;
        if (discriminant < 0)
            return float.MinValue;
        float rootOfDiscriminant = MathF.Sqrt(discriminant);
        float t1 = (-b + rootOfDiscriminant) / 2;
        float t2 = (-b - rootOfDiscriminant) / 2;
        if(t1 <= 0)
            return float.MinValue;
        if (t2 <= 0)
            return t1;
        return t2;


    }
}