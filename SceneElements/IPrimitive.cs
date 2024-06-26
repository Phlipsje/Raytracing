using INFOGR2024Template;
using INFOGR2024Template.Helper_classes;
using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

/// <summary>
/// An object in 3d space
/// </summary>
public interface IPrimitive
{
    public Vector3 Center { get; set; } //The center of a primitive
    
    public Material Material { get; set; } //The material of a primitive
    public Tuple<float, Material> RayIntersect(Ray ray); //If a ray hits this primitive (return scalar of ray direction (negative means not hit))
    public BoundingBox BoundingBox { get; }
}

