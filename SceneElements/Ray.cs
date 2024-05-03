using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

/// <summary>
/// A ray (so line with starting point)
/// </summary>
public class Ray
{
    public Vector3 Origin { get; set; }
    public Vector3 Direction { get; set; } //Should be a normal vector
    public Color4 Color { get; set; }
    public float T { get; set; } //Not used yet, but represents the distance to the intersection with a primitive
}