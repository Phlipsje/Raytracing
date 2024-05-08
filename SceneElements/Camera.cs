using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public Vector3 BottomLeftCameraPlane => Position + (ViewDirection * DistanceToCenter) + (-UpDirection * Height / 2) -
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
    
    public void SetViewDirection(Vector3 viewDirection, Vector3 rightDirection)
    {
        viewDirection.Normalize();
        rightDirection.Normalize();
        ViewDirection = viewDirection;
        RightDirection = rightDirection;
    }
}