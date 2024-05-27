using OpenTK.Mathematics;

namespace INFOGR2024Template.SceneElements
{
    public class PointLight
    {
        public Vector3 Position { get; set; }
        //the intensity and color of the light source
        public Color4 Intensity { get; set; }
        public PointLight(Vector3 position, Color4 intensity) 
        { 
            Position = position;
            Intensity = intensity;
        }
    }
}
