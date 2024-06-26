using System.Diagnostics;
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
    public Vector3 TopRightCameraPlane => Position + (ViewDirection * DistanceToCenter) + (UpDirection * Height / 2) +
                                          (RightDirection * Width / 2);
    public Vector3 BottomRightCameraPlane => Position + (ViewDirection * DistanceToCenter) + (-UpDirection * Height / 2) +
                                         (RightDirection * Width / 2);
    public Vector3 BottomLeftCameraPlane => Position + (ViewDirection * DistanceToCenter) + (-UpDirection * Height / 2) -
                                         (RightDirection * Width / 2);

    //This will automatically focus on an object on (0,0,0)
    public Camera()
    {
        Position = new Vector3(-2, 2, -2);
        DistanceToCenter = 1f;
        Width = 1.6f;
        Height = 0.9f;
        ViewDirection = new Vector3(1f, -1f, 1f);
        ViewDirection.Normalize();
        //Rotate around Y axis
        RightDirection = new Vector3(ViewDirection.Z, ViewDirection.Y, -ViewDirection.X);
    }
    /// <summary>
    /// Creates camera by rotating a camera facing in the z direction, with default 1.6/9 aspect ratio and 1.0 units focal length
    /// </summary>
    /// <param name="position"></param>
    /// <param name="horizontalRotation"></param>angle in radians
    /// <param name="verticalRotation"></param>angle in radians
    public Camera(Vector3 position, float horizontalRotation, float verticalRotation)
    {
        Position = position;
        ViewDirection = new Vector3(0f, 0f, 1f);
        RightDirection = new Vector3(1f, 0f, 0f);
        DistanceToCenter = 1f;
        Width = 1.6f;
        Height = 0.9f;
        RotateHorizontal(horizontalRotation);
        RotateVertical(verticalRotation);
    }

    /// <summary>
    /// Creates camera by rotating a camera facing in the z direction
    /// </summary>
    /// <param name="position"></param>
    /// <param name="horizontalRotation"></param>angle in radians
    /// <param name="verticalRotation"></param>angle in radians
    /// <param name="distanceToCenter"></param>focal length
    /// <param name="width"></param>width of the camera plane
    /// <param name="height"></param>height of the camera plane
    public Camera(Vector3 position, float horizontalRotation, float verticalRotation, float distanceToCenter, float width, float height)
    {
        Position = position;
        ViewDirection = new Vector3(0f, 0f, 1f);
        RightDirection = new Vector3(1f, 0f, 0f);
        DistanceToCenter = 1f;
        Width = 1.6f;
        Height = 0.9f;
        RotateHorizontal(horizontalRotation);
        RotateVertical(verticalRotation);
    }

    /// <summary>
    /// Constructs a camera with custom values, make sure viewDirection and rightDirection are orthogonal and oriented in the right way
    /// </summary>
    /// <param name="position"></param>
    /// <param name="viewDirection"></param>
    /// <param name="rightDirection"></param>
    /// <param name="distanceToCenter"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public Camera(Vector3 position, Vector3 viewDirection, Vector3 rightDirection, float distanceToCenter, float width, float height)
    {
        Position = position;
        ViewDirection = viewDirection.Normalized();
        RightDirection = rightDirection.Normalized();
        DistanceToCenter = distanceToCenter;
        Width = width;
        Height = height;
        float dot = Vector3.Dot(ViewDirection, RightDirection);
        if (dot < -0.01f || dot > 0.01f)
        {
            Debug.WriteLine("dot product of viewdirection and rightdirection is not near zero, dot:" + dot + ". Make sure they are orthogonal to form the right basis");
        }
    }

    public void SetDirection(Vector3 viewDirection, Vector3 rightDirection)
    {
        ViewDirection = viewDirection.Normalized();
        RightDirection = rightDirection.Normalized();
    }
    public void RotateHorizontal(float radianAngle)
    {
        //didn't expect this to work this well lol, literally chose some random function by feeling
        Quaternion quat = Quaternion.FromAxisAngle(Vector3.UnitY, radianAngle);
        ViewDirection = Vector3.Transform(ViewDirection, quat);
        RightDirection = Vector3.Transform(RightDirection, quat);
    }
    public void RotateVertical(float radianAngle)
    {
        Quaternion quat = Quaternion.FromAxisAngle(RightDirection, radianAngle);
        ViewDirection = Vector3.Transform(ViewDirection, quat);
        RightDirection = Vector3.Transform(RightDirection, quat);
    }
}