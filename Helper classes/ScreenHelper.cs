using OpenTK.Mathematics;
using OpenTK.SceneElements;

namespace OpenTK.Helper_classes;

/// <summary>
/// This class helps with the connecting of the screen to the rest of the code
/// </summary>
public static class ScreenHelper
{
    public static Surface screen;

    /// <summary>
    /// Called once when the screen is made, don't touch after that
    /// </summary>
    /// <param name="screen"></param>
    public static void Initialize(Surface screen)
    {
        ScreenHelper.screen = screen;
    }

    /// <summary>
    /// Clears the screen to a set color
    /// </summary>
    /// <param name="color"></param>
    public static void Clear(Color4 color)
    {
        screen.Clear(ColorHelper.ColorToInt(color));
    }
    
    /// <summary>
    /// Clears the screen to black
    /// </summary>
    public static void Clear()
    {
        screen.Clear(0);
    }

    public static int GetPixelWidth()
    {
        return screen.width;
    }
    
    public static int GetPixelHeight()
    {
        return screen.height;
    }

    #region Get pixel variants
    /// <summary>
    /// Note that this is non immutable (meaning changing this won't change the original screen)
    /// </summary>
    /// <param name="x">The x pixel component</param>
    /// <param name="y">The y pixel component</param>
    /// <returns>Returns the color of that pixel</returns>
    public static int GetPixel(int x, int y)
    {
        return screen.pixels[x + (screen.height - y) * screen.width];
    }
    
    /// <summary>
    /// Note that this is non immutable (meaning changing this won't change the original screen)
    /// </summary>
    /// <param name="vector">A vector2 with X and Y values as pixels count in x and y</param>
    /// <returns>Returns the color of that pixel</returns>
    public static int GetPixel(Vector2i vector)
    {
        return screen.pixels[vector.X + (screen.height - vector.Y) * screen.width];
    }

    /// <summary>
    /// Note that this is non immutable (meaning changing this won't change the original screen)
    /// </summary>
    /// <param name="vector">The vector2 in the mathematical plane</param>
    /// <param name="camera">The camera</param>
    /// <returns>Returns the color of that pixel</returns>
    public static int GetPixelFromCameraPlane(Vector2 vector, Camera camera)
    {
        Vector2i vectorInt = CameraPlaneToPixel(vector, camera);
        return screen.pixels[vectorInt.X + (screen.height - vectorInt.Y) * screen.width];
    }
    #endregion

    #region Set pixel variants
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x pixel component</param>
    /// <param name="y">The y pixel component</param>
    /// <param name="value">The color to set the pixel to</param>
    public static void SetPixel(int x, int y, int value)
    {
        screen.pixels[x + (screen.height - y) * screen.width] = value;
    }
    
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x pixel component</param>
    /// <param name="y">The y pixel component</param>
    /// <param name="color">The color to set the pixel to</param>
    public static void SetPixel(int x, int y, Color4 color)
    {
        x = MathHelper.Clamp(x, 0, screen.width - 1);
        y = MathHelper.Clamp(y, 0, screen.height - 1);
        screen.pixels[x + (screen.height - 1 - y) * screen.width] = ColorHelper.ColorToInt(color);
    }

    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x in mathematical space</param>
    /// <param name="y">The y in mathematical space</param>
    /// <param name="value">The color to set the pixel to</param>
    /// <param name="camera">The camera</param>
    public static void SetPixelByCameraPlane(float x, float y, int value, Camera camera)
    {
        Vector2i pixelPosition = CameraPlaneToPixel(x, y, camera);
        SetPixel(pixelPosition.X, pixelPosition.Y, value);
    }

    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="vector">The vector in mathematical space</param>
    /// <param name="value">The color to set the pixel to</param>
    /// <param name="camera">The camera</param>
    public static void SetPixelByCameraPlane(Vector2 vector, int value, Camera camera)
    {
        Vector2i pixelPosition = CameraPlaneToPixel(vector, camera);
        SetPixel(pixelPosition.X, pixelPosition.Y, value);
    }

    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x in mathematical space</param>
    /// <param name="y">The y in mathematical space</param>
    /// <param name="color">The color to set the pixel to</param>
    /// <param name="camera">The camera</param>
    public static void SetPixelByCameraPlane(float x, float y, Color4 color, Camera camera)
    {
        Vector2i pixelPosition = CameraPlaneToPixel(x, y, camera);
        SetPixel(pixelPosition.X, pixelPosition.Y, color);
    }

    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="vector">The vector in mathematical space</param>
    /// <param name="color">The color to set the pixel to</param>
    /// <param name="camera">The camera</param>
    public static void SetPixelByCameraPlane(Vector2 vector, Color4 color, Camera camera)
    {
        Vector2i pixelPosition = CameraPlaneToPixel(vector.X, vector.Y, camera);
        SetPixel(pixelPosition.X, pixelPosition.Y, color);
    }
    #endregion

    /// <summary>
    /// Resizes the screen, this will clear everything from the screen for the frame that it is run
    /// </summary>
    /// <param name="width">The amount of pixels in x axis</param>
    /// <param name="height">The amount of pixels in y axis</param>
    public static void Resize(int width, int height)
    {
        //Throws away the old screen and replaces it with a new one
        Surface.openTKApplication.ClientSize = new Vector2i(width, height);
        screen = new Surface(width, height);
    }

    /// <summary>
    /// Converts a position on the 2D screen plane, to a pixel position
    /// </summary>
    /// <param name="vector">The vector2 to convert to integers</param>
    /// <param name="camera">The camera</param>
    /// <returns></returns>
    public static Vector2i CameraPlaneToPixel(Vector2 vector, Camera camera)
    {
        return CameraPlaneToPixel(vector.X, vector.Y, camera);
    }

    /// <summary>
    /// Converts a position on the 2D screen plane, to a pixel position
    /// </summary>
    /// <param name="x">The x co-ordinate in mathematical space</param>
    /// <param name="y">The y co-ordinate in mathematical space</param>
    /// <param name="camera">The camera</param>
    /// <returns></returns>
    public static Vector2i CameraPlaneToPixel(float x, float y, Camera camera)
    {
        //Get Get the top left to be (0,0)
        x += camera.Width / 2;
        y += camera.Height / 2;

        x /= camera.Width;
        y /= camera.Height;
        
        //Clamp values, because otherwise it can go negative or exceed the width of the screen
        x = MathHelper.Clamp(x, 0, 1);
        y = MathHelper.Clamp(y, 0, 1);

        //Covert to pixel position (-1 because array of pixels starts at 0 and ends at n-1)
        int intX = (int)MathF.Round(x * (screen.width - 1));
        int intY = (int)MathF.Round(y * (screen.height - 1));

        return new Vector2i(intX, intY);
    }

    public static Vector2 PixelToCameraPlane(int x, int y, Camera camera)
    {
        float newX = (float)x / screen.width;
        float newY = (float)y / screen.height;
        newX = (newX - 0.5f) * camera.Width;
        newY = (newY - 0.5f) * camera.Height;
        return new Vector2(newX, newY);
    }

    /// <summary>
    /// Converts a vector2 between -1 and 1 to the pixel position
    /// </summary>
    /// <param name="vector2">The vector2 to convert, min value of x and y is -1 and max value is 1</param>
    /// <returns>Returns a pixel value within the screen (if offscreen, then the vector2 is not within the required range)</returns>
    public static Vector2i Vector2ToPixel(Vector2 vector2)
    {
        return new Vector2i((int)((vector2.X + 1f)/2f * screen.width),(int)((vector2.Y + 1f)/2f * screen.height));
    }

    /// <summary>
    /// Draws a circle on screen
    /// </summary>
    /// <param name="x">The x pixel of the center</param>
    /// <param name="y">The y pixel of the center</param>
    /// <param name="radius">The amount of pixels of the radius</param>
    /// <param name="color">The color of the circle</param>
    public static void DrawCircle(int x, int y, int radius, Color4 color)
    {
        for (int i = -radius; i < radius; i++)
        {
            for (int j = -radius; j < radius; j++)
            {
                if(i*i + j*j <= radius * radius)
                    SetPixel(x + i, y + j, color);
            }
        }
    }

    /// <summary>
    /// Draws a line on screen
    /// </summary>
    /// <param name="point1">A point on the camera view plane</param>
    /// <param name="point2">The other point on the camera view plane</param>
    /// <param name="color">The color of the line</param>
    /// <param name="camera">The camera</param>
    public static void DrawLine(Vector2 point1, Vector2 point2, Color4 color, Camera camera)
    {
        Vector2i pixelPoint1 = CameraPlaneToPixel(point1, camera);
        Vector2i pixelPoint2 = CameraPlaneToPixel(point2, camera);
        
        screen.Line(pixelPoint1.X, pixelPoint1.Y, pixelPoint2.X, pixelPoint2.Y, ColorHelper.ColorToInt(color));
    }
    
    /// <summary>
    /// Draws a line on screen
    /// </summary>
    /// <param name="point1">The pixel position of point 1</param>
    /// <param name="point2">The pixel position of poin 2</param>
    /// <param name="color">The color of the line</param>
    /// <param name="camera">The camera</param>
    public static void DrawLine(Vector2i point1, Vector2i point2, Color4 color)
    {
        screen.Line(point1.X, screen.height - point1.Y, point2.X, screen.height - point2.Y, ColorHelper.ColorToInt(color));
    }
}