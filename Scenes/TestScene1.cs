using OpenTK.SceneElements;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Helper_classes;

namespace INFOGR2024Template.Scenes
{
    public class TestScene1 : IScene
    {
        public List<Plane> PlanePrimitives { get; set; }
        public List<Sphere> SpherePrimitives { get; set; }
        public List<Triangle> TrianglePrimitives { get; set; }
        public Camera Camera {  get; set; }
        public List<PointLight> PointLights { get; set; }

        public TestScene1()
        {
            Vector3 offset = new Vector3(10, 0, 0);
            Camera = new Camera(new Vector3(1, 5f, -5), new Vector3(0f, -1f, 1f), new Vector3(1, 0f, 0), 1f, 1.6f, 0.9f);
            //Camera = new Camera(new Vector3(0, 1f, -5), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0), 1f, 1.6f, 0.9f);
            PlanePrimitives = new List<Plane>
            {
                new Plane(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Material(Color4.DimGray, Color4.LightGray, true, 1f)),
            };
            SpherePrimitives = new List<Sphere>
            {
                new Sphere(new Vector3(-1, 0.5f, 0), 0.5f, new Material(Color4.Red, Color4.Black, false, 50f)),
                new Sphere(new Vector3(0f, 1f, 2f), 1f, new Material(Color4.Black, Color4.Gray, true, 100f)),
                new Sphere(new Vector3(-1, 0.3f, -1f), 0.3f, new Material(Color4.Gold, Color4.LightGray, false, 50f)),
                new Sphere(new Vector3(-2, 0.8f, -2), 0.8f, new Material(Color4.Purple, Color4.Purple, false, 15f)),
                new Sphere(new Vector3(-1, 0.5f, 0) + offset, 0.5f, new Material(Color4.Red, Color4.Gray, false, 50f)),
                new Sphere(new Vector3(0f, 1f, 2f) + offset, 1f, new Material(Color4.DeepPink, Color4.LightGray, false, 50f)),
                new Sphere(new Vector3(-1, 0.3f, -1f) + offset, 0.3f, new Material(Color4.Yellow, Color4.LightGray, false, 100f)),
                new Sphere(new Vector3(-2, 0.8f, -2) + offset, 0.8f, new Material(Color4.Black, Color4.LightGray, true, 5f)),
            };
            
            TrianglePrimitives = new List<Triangle>();
            TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("cube"), 0.02f, new Vector3(3, 0, 1), new Material(Color4.Gold, Color4.Gold, true, 1f))).ToList();
            TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.03f, new Vector3(1, 0, 0), new Material(Color4.Black, new Color4(50, 50, 255, 255), true, 1f))).ToList();
            TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.05f, new Vector3(3, 0, -2), new Material(Color4.Turquoise, Color4.White, false, 1f))).ToList();
            TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("teapot"), 0.03f, new Vector3(0, 2, 5), new Material(Color4.Beige, Color4.Gray, false, 0.01f))).ToList();
            PointLights = new List<PointLight>
            {
                new PointLight(new Vector3(-5f, 10f, 0f), new Color4(70, 70, 70, 1.0f)),
                //new PointLight(new Vector3(0f, 3f, 0f), new Color4(0, 4, 4, 1.0f)),
                //new PointLight(new Vector3(0.5f, 2.5f, -3f), new Color4(3f, 0, 6, 1.0f)),
                //new PointLight(new Vector3(8f, 5f, 2f), new Color4(10f, 5f, 0f, 1.0f)),
                //new PointLight(new Vector3(5f, 10f, 5f), new Color4(30, 30, 30, 1.0f)),
                //new PointLight(new Vector3(30f, 20f, 0f), new Color4(300, 300, 300, 1f))
            };
            
            //This makes sure we used location based searching of intersections
            //No more primitives should be added after this point (otherwise they won't be included)
            //Also automatically updates the float array of data used by openGL
            ((IScene)this).ActivateAccelerationStructure();
        }

        private RTree AccelerationStructure { get; set; }
        public float[] AccelerationStructureData { get; private set; }

        float[] IScene.AccelerationStructureData
        {
            get => AccelerationStructureData;
            set => AccelerationStructureData = value;
        }

        RTree IScene.AccelerationStructure
        {
            get => AccelerationStructure;
            set => AccelerationStructure = value;
        }

        public void Tick()
        {
            
        }
    }
}
