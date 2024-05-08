using INFOGR2024Template;
using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

/// <summary>
/// A ray (so line with starting point)
/// </summary>
public class Ray
{
    public Vector3 Origin { get; set; }
    public Vector3 Direction { get; set; } //Should be a normal vector
    public Material Material { get; set; }
    public float T { get; set; } //Not used yet, but represents the distance to the intersection with a primitive
    /// <summary>
    /// Constructs a ray and normalises its direction.
    /// </summary>
    /// <param name="origin"></param>the origin, or 'support vector' of the ray
    /// <param name="direction"></param>the direction, does not have to be normalised
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
        T = float.MinValue;
    }
}