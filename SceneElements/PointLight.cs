using OpenTK.Mathematics;

namespace INFOGR2024Template.SceneElements
{
    public class PointLight
    {
        public Vector3 Position;
        public Color4 Intesity;

        public PointLight(Vector3 Position, Color4 Intesity)
        {
            this.Position = Position;
            this.Intesity = Intesity;

        }
        public PointLight(float x, float y, float z, float r, float g, float b, float a)
        {
            this.Position = new Vector3(x, y, z);
            this.Intesity = new Color4(r, g, b, a);
        }
    }
}
