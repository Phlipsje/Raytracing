using OpenTK.Graphics.ES11;
using OpenTK.Mathematics;

namespace OpenTK.Helper_classes;

/// <summary>
/// This class helps with the connecting of the screen to the rest of the code
/// </summary>
public static class ScreenHelper
{
    public static Surface screen;
    
    //The plane is the mathematical plane which represents the screen, default is (-5, -5) to (5, 5), with (0, 0) as center
    private static Vector2 planeSize;
    public static float PlaneWidth => planeSize.X;
    public static float PlaneHeight => planeSize.Y;
    public static Vector2 PlaneTopLeft => new Vector2(planeSize.X / -2, planeSize.Y / -2);
    public static Vector2 PlaneBottomRight => new Vector2(planeSize.X / 2, planeSize.Y / 2);

    /// <summary>
    /// Called once when the screen is made, don't touch after that
    /// </summary>
    /// <param name="screen"></param>
    public static void Initialize(Surface screen, Vector2 planeSize)
    {
        ScreenHelper.screen = screen;
        ScreenHelper.planeSize = planeSize;
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
        return screen.pixels[x + y * screen.width];
    }
    
    /// <summary>
    /// Note that this is non immutable (meaning changing this won't change the original screen)
    /// </summary>
    /// <param name="vector">A vector2 with X and Y values as pixels count in x and y</param>
    /// <returns>Returns the color of that pixel</returns>
    public static int GetPixel(Vector2i vector)
    {
        return screen.pixels[vector.X + vector.Y * screen.width];
    }
    
    /// <summary>
    /// Note that this is non immutable (meaning changing this won't change the original screen)
    /// </summary>
    /// <param name="vector">The vector2 in the mathematical plane</param>
    /// <returns>Returns the color of that pixel</returns>
    public static int GetPixelFromCameraPlane(Vector2 vector)
    {
        Vector2i vectorInt = CameraPlaneToScreenPixel(vector);
        return screen.pixels[vectorInt.X + vectorInt.Y * screen.width];
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
        screen.pixels[x + y * screen.width] = value;
    }
    
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x pixel component</param>
    /// <param name="y">The y pixel component</param>
    /// <param name="color">The color to set the pixel to</param>
    public static void SetPixel(int x, int y, Color4 color)
    {
        screen.pixels[x + y * screen.width] = ColorHelper.ColorToInt(color);
    }

    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x in mathematical space</param>
    /// <param name="y">The y in mathematical space</param>
    /// <param name="value">The color to set the pixel to</param>
    public static void SetPixelByPlane(float x, float y, int value)
    {
        Vector2i pixelPosition = CameraPlaneToScreenPixel(x, y);
        SetPixel(pixelPosition.X, pixelPosition.Y, value);
    }
    
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="vector">The vector in mathematical space</param>
    /// <param name="value">The color to set the pixel to</param>
    public static void SetPixelByPlane(Vector2 vector, int value)
    {
        Vector2i pixelPosition = CameraPlaneToScreenPixel(vector.X, vector.Y);
        SetPixel(pixelPosition.X, pixelPosition.Y, value);
    }
    
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="x">The x in mathematical space</param>
    /// <param name="y">The y in mathematical space</param>
    /// <param name="color">The color to set the pixel to</param>
    public static void SetPixelByPlane(float x, float y, Color4 color)
    {
        Vector2i pixelPosition = CameraPlaneToScreenPixel(x, y);
        SetPixel(pixelPosition.X, pixelPosition.Y, color);
    }
    
    /// <summary>
    /// Sets a pixel on screen to a given color value
    /// </summary>
    /// <param name="vector">The vector in mathematical space</param>
    /// <param name="color">The color to set the pixel to</param>
    public static void SetPixelByPlane(Vector2 vector, Color4 color)
    {
        Vector2i pixelPosition = CameraPlaneToScreenPixel(vector.X, vector.Y);
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
    /// <returns></returns>
    public static Vector2i CameraPlaneToScreenPixel(Vector2 vector)
    {
        return CameraPlaneToScreenPixel(vector.X, vector.Y);
    }
    
    /// <summary>
    /// Converts a position on the 2D screen plane, to a pixel position
    /// </summary>
    /// <param name="x">The x co-ordinate in mathematical space</param>
    /// <param name="y">The y co-ordinate in mathematical space</param>
    /// <returns></returns>
    public static Vector2i CameraPlaneToScreenPixel(float x, float y)
    {
        //Get Get the top left to be (0,0)
        x += PlaneWidth / 2;
        y += PlaneHeight / 2;
        
        //Clamp values, because otherwise it can go negative or exceed the width of the screen
        x = MathHelper.Clamp(x, 0, 1);
        y = MathHelper.Clamp(y, 0, 1);

        //Covert to pixel position (-1 because array of pixels starts at 0 and ends at n-1)
        int intX = (int)MathF.Round(x * (screen.width - 1));
        int intY = (int)MathF.Round(y * (screen.height - 1));

        return new Vector2i(intX, intY);
    }
}