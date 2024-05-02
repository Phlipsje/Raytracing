using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

/// <summary>
/// The camera
/// </summary>
public class Camera
{
    public Vector3 Position;
    public float DistanceToCenter;
    public int Width;
    public int Height;
    public Vector3 ViewDirection; //Unit vector, through center of camera plain
    //Rotate around X axis
    public Vector3 UpDirection => new(0, -ViewDirection.X, ViewDirection.Y); //Unit vector
    //Rotate around Y axis
    public Vector3 RightDirection => new(ViewDirection.Z, ViewDirection.Y, -ViewDirection.X); //Unit vector
    public Vector3 ImagePlaneCenter => Position + DistanceToCenter * ViewDirection;
    public float FieldOfView => MathF.Acos(Height / 2 / DistanceToCenter) * 2;
    public float AspectRatio => Width / (float)Height;

    //This will automatically focus on an object on (0,0,0)
    public Camera()
    {
        Position = new Vector3(-2, 2, -2);
        DistanceToCenter = 1f;
        Width = 1280;
        Height = 720;
        ViewDirection = new Vector3(0.5f, -0.5f, 0.5f);
    }
    
    //Custom values
    public Camera(Vector3 position, Vector3 viewDirection, float distanceToCenter, int width, int height)
    {
        Position = position;
        ViewDirection = viewDirection;
        DistanceToCenter = distanceToCenter;
        Width = width;
        Height = height;
    }
}