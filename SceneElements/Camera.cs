using OpenTK.Mathematics;

namespace OpenTK.SceneElements;

/// <summary>
/// The camera
/// </summary>
public class Camera
{
    public Vector3 Position;
    public float DistanceToCenter;
    public float Width;
    public float Height;
    public Vector3 ViewDirection { get; private set; } //Unit vector, through center of camera plain
    public Vector3 RightDirection { get; private set; } //Unit vector
    public Vector3 UpDirection => Vector3.Cross(ViewDirection, RightDirection); //Unit vector
    
    public Vector3 ImagePlaneCenter => Position + DistanceToCenter * ViewDirection;
    public float FieldOfView => MathF.Acos(Height / 2 / DistanceToCenter) * 2;
    public float AspectRatio => Width / Height;

    public Vector3 TopLeftCameraPlane => Position + (ViewDirection * DistanceToCenter) + (UpDirection * Height / 2) +
                                         (-RightDirection * Width / 2);
    public Vector3 BottomLeftCameraPlane => Position + (ViewDirection * DistanceToCenter) + (-UpDirection * Height / 2) +
                                         (-RightDirection * Width / 2);
    public Vector3 TopRightCameraPlane => Position + (ViewDirection * DistanceToCenter) + (UpDirection * Height / 2) +
                                          (RightDirection * Width / 2);
    public Vector3 BottomRightCameraPlane => Position + (ViewDirection * DistanceToCenter) + (-UpDirection * Height / 2) +
                                         (RightDirection * Width / 2);

    //This will automatically focus on an object on (0,0,0)
    public Camera()
    {
        Position = new Vector3(-2, 2, -2);
        DistanceToCenter = 1f;
        Width = 1.6f;
        Height = 0.9f;
        ViewDirection = new Vector3(1f, 0f, 1f);
        RightDirection = new Vector3(1f, 0, -1f);
        ViewDirection.Normalize();
        RightDirection.Normalize();
    }
    
    //Custom values
    public Camera(Vector3 position, Vector3 viewDirection, float distanceToCenter, float width, float height)
    {
        Position = position;
        ViewDirection = viewDirection;
        ViewDirection.Normalize();
        DistanceToCenter = distanceToCenter;
        Width = width;
        Height = height;
    }

    public void SetViewDirection(Vector3 vector3)
    {
        vector3.Normalize();
        ViewDirection = vector3;
    }
    
    public void SetViewDirection(Vector3 viewDirection, Vector3 rightDirection)
    {
        viewDirection.Normalize();
        rightDirection.Normalize();
        ViewDirection = viewDirection;
        RightDirection = rightDirection;
    }
}