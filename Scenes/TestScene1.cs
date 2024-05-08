using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Helper_classes;
using OpenTK.Helper_classes;

namespace INFOGR2024Template.Scenes
{
    public class TestScene1 : IScene
    {
        public List<IPrimitive> Primitives { get; set; }
        public Camera Camera {  get; set; }
        public List<PointLight> PointLights { get; set; }

        public TestScene1() 
        {
            Vector3 offset = new Vector3(10, 0, 0);
            Camera = new Camera(new Vector3(1, 5f, -5), new Vector3(0f, -1f, 1f), new Vector3(1, 0f, 0), 1f, 1.6f, 0.9f);
            //Camera = new Camera(new Vector3(0, 1f, -5), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0), 1f, 1.6f, 0.9f);
            Primitives = new List<IPrimitive>
            {                
                new Sphere(new Vector3(-1, 0.5f, 0), 0.5f, new Material(Color4.Red, Color4.Gray, 1f)),
                new Sphere(new Vector3(0f, 1f, 2f), 1f, new Material(Color4.Pink, Color4.Gray, 3f)),
                new Sphere(new Vector3(-1, 0.3f, -1f), 0.3f, new Material(Color4.Gold, Color4.Gold, 1f)),
                new Sphere(new Vector3(-2, 0.8f, -2), 0.8f, new Material(Color4.Purple, Color4.Purple, 3f)),
                new Plane(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Material(Color4.White)),
                new Sphere(new Vector3(-1, 0.5f, 0) + offset, 0.5f, new Material(Color4.Red, Color4.Gray, 10f)),
                new Sphere(new Vector3(0f, 1f, 2f) + offset, 1f, new Material(Color4.Pink, Color4.White, 1f)),
                new Sphere(new Vector3(-1, 0.3f, -1f) + offset, 0.3f, new Material(Color4.Yellow, Color4.White, 100f)),
                new Sphere(new Vector3(-2, 0.8f, -2) + offset, 0.8f, new Material(Color4.Purple)),
            };
            Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("cube"), 0.02f, new Vector3(3, 0, 1), new Material(Color4.Orange, Color4.Orange, 1f))).ToList();
            Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.03f, new Vector3(1, 0, 0), new Material(Color4.Blue))).ToList();
            Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.05f, new Vector3(3, 0, -2), new Material(Color4.Turquoise, Color4.White, 1f))).ToList();
            float lampExtraDistance = 10f;
            PointLights = new List<PointLight>
            {
                new PointLight(new Vector3(-10f, 5f, 0f) * lampExtraDistance, new Color4(0.2f, 0.2f, 0.2f, 1.0f)),
                new PointLight(new Vector3(0f, 5f, -10f) * lampExtraDistance, new Color4(0.2f, 0.2f, 0.2f, 1.0f)),
                new PointLight(new Vector3(-10f, 10f, 5f) * lampExtraDistance, new Color4(0.5f, 0.5f, 0.5f, 1.0f))
            };
        }
        public void Tick()
        {
            
        }
    }
}
