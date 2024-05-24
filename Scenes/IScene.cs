using OpenTK.SceneElements;
using INFOGR2024Template.Helper_classes;
using INFOGR2024Template.SceneElements;
using OpenTK.Mathematics;
using INFOGR2024Template.SceneElements;

namespace INFOGR2024Template.Scenes
{
    /// <summary>
    /// The interface that represents a scene. Every scene has a list of primitives and a camera.
    /// </summary>
    public interface IScene
    {
        public List<Plane> PlanePrimitives { get; set; }
        public List<Sphere> SpherePrimitives { get; set; }
        public List<Triangle> TrianglePrimitives { get; set; }
        public List<PointLight> PointLights { get; set; }
        public Camera Camera { get; set; }
        public RTree AccelerationStructure { get; protected set; }
        public float[] AccelerationStructureData { get; protected set; }

        public void Tick();

        public void ActivateAccelerationStructure()
        {
            AccelerationStructure = new RTree(this);
            
            //Planes aren't added to the data structure, because they are infinitely large
            for (int i = 0; i < SpherePrimitives.Count; i++)
            {
                AccelerationStructure.AddPrimitive(i);
            }
            for (int i = 0; i < TrianglePrimitives.Count; i++)
            {
                //Add on the count of sphere primitives to be able to see what is a sphere and what a triangle
                AccelerationStructure.AddPrimitive(i + SpherePrimitives.Count);
            }

            AccelerationStructureData = AccelerationStructure.TurnIntoFloatArray();
        }
    }
}
