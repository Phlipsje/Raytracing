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
        //Clamping the values, because otherwise they can exceed 256 in integer form and then green become red and other stupid stuff
        color.R = MathHelper.Clamp(color.R, 0, 1);
        color.G = MathHelper.Clamp(color.G, 0, 1);
        color.B = MathHelper.Clamp(color.B, 0, 1);
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
}