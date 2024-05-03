using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOGR2024Template.Materials
{
    public class Material
    {
        public Color4 Color { get; set; } //The color of a Material.
        public Color4 OnCollision(Vector3 impactPoint, float angle)//The function called upon collision
        {
            return Color;
        } 

    }
}
