using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOGR2024Template.Materials
{
    /// <summary>
    /// A material handles the logic of what happens on a collision.
    /// </summary>
    public class Material
    {
        public Material(Color4 color) 
        {
            Color = color;
        }
        public Color4 Color { get; set; } //The color of a Material.
        public Color4 OnCollision(Vector3 impactPoint, float angle)//The function called upon collision
        {
            return Color;
        } 

    }
}
