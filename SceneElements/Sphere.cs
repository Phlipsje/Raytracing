using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

public class Sphere : IPrimitive
{
    public Vector3 Center { get; set; }
    public Color4 Color { get; set; }
    public Material Material { get; set; }
    public float RayIntersect(Ray ray)
    {
        throw new NotImplementedException();
    }
}