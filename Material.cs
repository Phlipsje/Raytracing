using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public int TextureIndex { get; set; } //The index of the texture.
        public float SpecularWidth  //the specularity or glossiness of a material
        {
            get => specularWidth;
            set { specularWidth = MathF.Max(value, 1); }
        }
        public Material(Color4 diffuseColor, Color4 specularColor, float specularWidth, int textureIndex = 0)
        {
            DiffuseColor = diffuseColor;
            SpecularColor = specularColor;
            SpecularWidth = specularWidth;
            TextureIndex = textureIndex;
        }
        public Material(Color4 diffuseColor) 
        {
            DiffuseColor = diffuseColor;
            SpecularColor = Color4.Black;
            SpecularWidth = 1;
            TextureIndex = 0;
        }
        public Material(Color4 diffuseColor, bool isMetal, float specularity, float specularWidth, int textureIndex = 0)
        {
            DiffuseColor = diffuseColor;
            if (isMetal)
                SpecularColor = new Color4(diffuseColor.R * specularity, diffuseColor.G * specularity, diffuseColor.B * specularity, 1.0f);
            else
                SpecularColor = new Color4(specularity, specularity, specularity, 1.0f);
            SpecularWidth = specularWidth;
            TextureIndex = textureIndex;
        }
    }
}
