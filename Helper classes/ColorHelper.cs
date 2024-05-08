using System;
using OpenTK.Mathematics;

namespace OpenTK.Helper_classes;

/// <summary>
/// Converting colors to ints
/// </summary>
public static class ColorHelper
{
    /// <summary>
    /// Returns the int value of a color with which the screen can understand the color
    /// </summary>
    /// <param name="color"></param>
    /// <returns>The integer value of the color</returns>
    public static int ColorToInt(Color4 color)
    {
        return ((int)MathF.Round(color.R*255) << 16) + ((int)MathF.Round(color.G*255) << 8) + (int)MathF.Round(color.B*255);
    }

    /// <summary>
    /// Returns the int value of a color with which the screen can understand the color
    /// </summary>
    /// <param name="r">The red component</param>
    /// <param name="g">The green component</param>
    /// <param name="b">The blue component</param>
    /// <returns>The integer value of the color</returns>
    public static int ColorToInt(float r, float g, float b)
    {
        //Clamping the values, because otherwise they can exceed 256 in integer form and then green become red and other stupid stuff
        r = MathHelper.Clamp(r, 0, 1);
        g = MathHelper.Clamp(g, 0, 1);
        b = MathHelper.Clamp(b, 0, 1);

        return ((int)MathF.Round(r * 255) << 16) + ((int)MathF.Round(g * 255) << 8) + (int)MathF.Round(b * 255);
    }
    /// <summary>
    /// Function to calculate the entrywise product between a color and float. Always returns a 1 for the alpha value
    /// </summary>
    /// <param name="color"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Color4 Multiplication(Color4 color, float factor)
    {
        return new Color4(color.R * factor, color.G * factor, color.B * factor, 1);
    }
    /// <summary>
    /// Adds the components of two vectors togheter.
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <returns></returns>
    public static Color4 Adition(Color4 color1, Color4 color2) 
    {
        return new Color4(color1.R + color2.R, color1.G + color2.G, color1.B + color2.B, 1);
    }
    /// <summary>
    /// multiplies the components of two vectors togheter.
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <returns></returns>
    public static Color4 EntrywiseProduct(Color4 color1, Color4 color2)
    {
        return new Color4(color1.R * color2.R, color1.G * color2.G, color1.B * color2.B, 1);
    }
}