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
        public Material(Color4 color, bool isMetal, int specularWidth = 2) 
        {
            Color = color;
            IsMetal = isMetal;
            SpecularWidth = specularWidth;

            if (isMetal )
            {
                SpecularColor = color;
            }
            else { SpecularColor = Color4.White; }
        }
        public Color4 Color { get; set; } //The color of a Material.
        public bool IsMetal {  get; set; }// Bool for metal or plastic like reflaction.
        public Color4 SpecularColor { get; set; } //The Specular color.
        public int SpecularWidth { get; set; }
    }
}
