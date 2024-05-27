using OpenTK.SceneElements;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Helper_classes;

namespace INFOGR2024Template.Scenes
{
    public class PathtracingScene : IScene
    {
        public Camera Camera {  get; set; }
        public List<Plane> PlanePrimitives { get; set; }
        public List<Sphere> SpherePrimitives { get; set; }
        public List<Triangle> TrianglePrimitives { get; set; }
        public List<PointLight> PointLights { get; set; }
        
        public PathtracingScene()
        {
            float roomSize = 2f;
            Vector3 offset = new Vector3(10, 0, 0);
            Camera = new Camera(new Vector3(-1, 3f, -2), new Vector3(0f, -1f, 1f), new Vector3(1, 0f, 0), 1f, 1.6f, 0.9f);
            //Camera = new Camera(new Vector3(0, 1f, -5), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0), 1f, 1.6f, 0.9f);
            PlanePrimitives = new List<Plane>
            {                
                new Plane(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Material(Color4.White, Color4.Black, false, 1f))
                
            };
            SpherePrimitives = new List<Sphere>
            {
                new Sphere(new Vector3(0.75f, 1.5f, 0.75f) * roomSize, 0.5f, new Material(Color4.White, 0f))
            };
            TrianglePrimitives = new List<Triangle>
            {
                new Triangle(new Vector3(1.5f, 0, 1.5f) * roomSize, new Vector3(1.5f, 0, -1.5f) * roomSize, new Vector3(1.5f, 3f, 1.5f) * roomSize, new Material(Color4.Red)),
                new Triangle(new Vector3(1.5f, 3f, 1.5f) * roomSize, new Vector3(1.5f, 3f, -1.5f) * roomSize, new Vector3(1.5f, 0f, -1.5f) * roomSize, new Material(Color4.Red)),
                new Triangle(new Vector3(-1.5f, 0, 1.5f) * roomSize, new Vector3(-1.5f, 0, -1.5f) * roomSize, new Vector3(-1.5f, 3f, 1.5f) * roomSize, new Material(Color4.Green)),
                new Triangle(new Vector3(-1.5f, 3f, 1.5f) * roomSize, new Vector3(-1.5f, 3f, -1.5f) * roomSize, new Vector3(-1.5f, 0f, -1.5f) * roomSize, new Material(Color4.Green)),
                new Triangle(new Vector3(-1.5f, 0, -1.5f) * roomSize, new Vector3(1.5f, 0, -1.5f) * roomSize, new Vector3(-1.5f, 3f, -1.5f) * roomSize, new Material(Color4.Blue)),
                new Triangle(new Vector3(-1.5f, 3f, -1.5f) * roomSize, new Vector3(1.5f, 3f, -1.5f) * roomSize, new Vector3(1.5f, 0f, -1.5f) * roomSize, new Material(Color4.Blue)),
                new Triangle(new Vector3(1.5f, 0, 1.5f) * roomSize, new Vector3(-1.5f, 0, 1.5f) * roomSize, new Vector3(1.5f, 3f, 1.5f) * roomSize, new Material(Color4.White)),
                new Triangle(new Vector3(1.5f, 3f, 1.5f) * roomSize, new Vector3(-1.5f, 3f, 1.5f) * roomSize, new Vector3(-1.5f, 0f, 1.5f) * roomSize, new Material(Color4.White)),
                new Triangle(new Vector3(1.5f, 3f, 1.5f) * roomSize, new Vector3(1.5f, 3f, -1.5f) * roomSize, new Vector3(-1.5f, 3f, -1.5f) * roomSize, new Material(Color4.White)),
                new Triangle(new Vector3(-1.5f, 3f, 1.5f) * roomSize, new Vector3(1.5f, 3f, 1.5f) * roomSize, new Vector3(-1.5f, 3f, -1.5f) * roomSize, new Material(Color4.White)),
                new Triangle(new Vector3(0.3f, 2.999f, 0.3f) * roomSize, new Vector3(0.3f, 2.999f, -0.3f) * roomSize, new Vector3(-0.3f, 2.999f, -0.3f) * roomSize, new Material(Color4.White, 10f)),
                new Triangle(new Vector3(-0.3f, 2.999f, 0.3f) * roomSize, new Vector3(0.3f, 2.999f, 0.3f) * roomSize, new Vector3(-0.3f, 2.999f, -0.3f) * roomSize, new Material(Color4.White, 10f)),
            };
            //TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("cube"), 0.02f, new Vector3(0.5f, 0, 0), new Material(Color4.Red, Color4.LightGray, false, 100f))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.03f, new Vector3(1, 0, 0), new Material(Color4.Black, new Color4(50, 50, 255, 255), true, 1f))).ToList();
            //Primitives = Primitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("pyramid"), 0.05f, new Vector3(3, 0, -2), new Material(Color4.Turquoise, Color4.White, false, 1f))).ToList();
            TrianglePrimitives = TrianglePrimitives.Concat(OBJImportHelper.ImportModel(OBJImportHelper.FilePath("teapot"), 0.01f, new Vector3(0, 0.5f, 0), new Material(Color4.Beige, Color4.Gray, false, 0.01f))).ToList();
            float lightStrength = 0f;
            PointLights = new List<PointLight>
            {
                new PointLight(new Vector3(0f, 2.25f, 0f) * roomSize, new Color4(lightStrength, lightStrength, lightStrength, 1.0f)),
                /*new PointLight(new Vector3(0f, 3f, 0f), new Color4(0, 4, 4, 1.0f)),
                new PointLight(new Vector3(0.5f, 2.5f, -3f), new Color4(3f, 0, 6, 1.0f)),
                new PointLight(new Vector3(8f, 5f, 2f), new Color4(10f, 5f, 0f, 1.0f)),
                new PointLight(new Vector3(5f, 10f, 5f), new Color4(30, 30, 30, 1.0f)),
                new PointLight(new Vector3(30f, 20f, 0f), new Color4(300, 300, 300, 1f))*/
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
