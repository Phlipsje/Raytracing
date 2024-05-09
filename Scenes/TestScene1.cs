using OpenTK.SceneElements;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Helper_classes;

namespace INFOGR2024Template.Scenes
{
    public class TestScene1 : IScene
    {
        public List<IPrimitive> Primitives { get; set; }
        public Camera Camera {  get; set; }
        public List<Vector3> PointLights { get; set; }

        public TestScene1() 
        {
            Vector3 offset = new Vector3(10, 0, 0);
            Camera = new Camera(new Vector3(1, 5f, -5), new Vector3(0f, -1f, 1f), new Vector3(1, 0f, 0), 1f, 1.6f, 0.9f);
            //Camera = new Camera(new Vector3(0, 1f, -5), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0), 1f, 1.6f, 0.9f);
            Primitives = new List<IPrimitive>
            {
                new Sphere(new Vector3(1,1,1), 1, new Material(new Color4(1f,1f,1f,1))),
                new Sphere(new Vector3(2,2,2), 0.2f, new Material(Color4.Red)),
                new Triangle(new Vector3(0, 0, 0), new Vector3(2, 2, 0), new Vector3(-1, 1, 0), new Material(Color4.Green)),
                new Triangle(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Material(Color4.Green)),
                new Sphere(new Vector3(-1, 0.5f, 0), 0.5f, new Material(Color4.Red)),
                new Sphere(new Vector3(0f, 1f, 2f), 1f, new Material(Color4.Pink)),
                new Sphere(new Vector3(-1, 0.3f, -1f), 0.3f, new Material(Color4.Gold)),
                new Sphere(new Vector3(-2, 0.8f, -2), 0.8f, new Material(Color4.Purple)),
                new Plane(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Material(Color4.White)),
                new Sphere(new Vector3(-1, 0.5f, 0) + offset, 0.5f, new Material(Color4.Red)),
                new Sphere(new Vector3(0f, 1f, 2f) + offset, 1f, new Material(Color4.Pink)),
                new Sphere(new Vector3(-1, 0.3f, -1f) + offset, 0.3f, new Material(Color4.Gold)),
                new Sphere(new Vector3(-2, 0.8f, -2) + offset, 0.8f, new Material(Color4.Purple)),
                new Plane(new Vector3(0, 0, 0) + offset, new Vector3(0, 1, 0), new Material(Color4.White))
            };
            Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("test"), 0.5f, new Vector3(3, 0, 1), new Material(Color4.Yellow))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.03f, new Vector3(1, 0, 0), new Material(Color4.Blue))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("cube"), 0.02f, new Vector3(3, 0, 1), new Material(Color4.Yellow))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.03f, new Vector3(1, 0, 0), new Material(Color4.Blue))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.05f, new Vector3(3, 0, -2), new Material(Color4.Turquoise))).ToList();
            float lampExtraDistance = 10f;
            PointLights = new List<Vector3>
            {
                
                //new Vector3(-10f, 5f, 0f), 
                new Vector3(0f, 5f, -10f), 
                new Vector3(-10f, 10f, 5f), 
                new Vector3(0f, 1f, 0f)
                new Vector3(-10f, 5f, 0f) * lampExtraDistance,
                new Vector3(0f, 5f, -10f) * lampExtraDistance,
                new Vector3(-10f, 10f, 5f) * lampExtraDistance,
                new Vector3(0f, 1f, -0.5f),
                new Vector3(0f, 10f, 0f) * lampExtraDistance,
                new Vector3(3f, 10f, 3f) * lampExtraDistance,
                new Vector3(-3f, 10f, -3f) * lampExtraDistance,
                new Vector3(10f, 2f, -2f) * lampExtraDistance,
                new Vector3(0f, 3f, 10f) * lampExtraDistance
            };
        }
        public void Tick()
        {
            
        }
    }
}
