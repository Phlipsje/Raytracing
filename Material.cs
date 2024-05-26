using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace INFOGR2024Template
{
    /// <summary>
    /// A material handles the logic of what happens on a collision.
    /// </summary>
    public class Material
    {
        float specularWidth;
        public Color4 DiffuseColor { get; set; } //The color of a Material.
        public Color4 SpecularColor { get; set; } //The specular color of the material
        public Color4 EmissionColor { get; set; } //The emission color, the values determine the intensity as well
        public bool IsPureSpecular { get; set; } //Whether the object acts like a mirror
        public float SpecularWidth  //the specularity or glossiness of a material
        {
            get => specularWidth;
            set { specularWidth = MathF.Max(value, 1); }
        }
        public Material(Color4 diffuseColor, Color4 specularColor, bool isPureSpecular, float specularWidth)
        {
            DiffuseColor = diffuseColor;
            SpecularColor = specularColor;
            IsPureSpecular = isPureSpecular;
            SpecularWidth = specularWidth;
            EmissionColor = Color4.Black;
        }
        public Material(Color4 diffuseColor) 
        {
            DiffuseColor = diffuseColor;
            SpecularColor = Color4.Black;
            SpecularWidth = 1;
            IsPureSpecular = false;
            EmissionColor = Color4.Black;
        }

        public Material(Color4 emissionColor, float intensity)
        {
            EmissionColor = new Color4(emissionColor.R * intensity, emissionColor.G * intensity, emissionColor.B * intensity, 1.0f);
            SpecularColor = Color4.Black;
            SpecularWidth = 1;
            IsPureSpecular = false;
            DiffuseColor = emissionColor;
        }
        public Material(Color4 diffuseColor, bool isMetal, float specularity, bool isPureSpecular, float specularWidth)
        {
            DiffuseColor = diffuseColor;
            if (isMetal)
                SpecularColor = new Color4(diffuseColor.R * specularity, diffuseColor.G * specularity, diffuseColor.B * specularity, 1.0f);
            else
                SpecularColor = new Color4(specularity, specularity, specularity, 1.0f);
            IsPureSpecular = isPureSpecular;
            SpecularWidth = specularWidth;
        }
    }
}
