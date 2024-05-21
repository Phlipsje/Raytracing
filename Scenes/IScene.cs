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

        /*
        //Will put the primitives in order of shape, because that is how we handel it in the fragment shader
        //List order: Planes, Spheres, Triangles
        public void SortPrimitives()
        {
            //Making it as an array, so that it doesn't keep having to resize
            //+3 because slots 0, 1, and 2, indicate where the 3 different shapes start
            IPrimitive[] tempPrimitives = new IPrimitive[Primitives.Count + 3];

            int count = 0;
            int planeCount = 0;
            int sphereCount = 0;
            int triangleCount = 0;
            for (int i = 0; i < 3; i++)
            {
                foreach (IPrimitive primitive in Primitives)
                {
                    switch (i)
                    {
                        case 0: //Planes
                            if (primitive.GetType() == typeof(Plane))
                            {
                                tempPrimitives[2 + count] = primitive;
                                count++;
                                planeCount++;
                            }
                            break;
                        case 1: //Spheres
                            if (primitive.GetType() == typeof(Plane))
                            {
                                tempPrimitives[2 + count] = primitive;
                                count++;
                                sphereCount++;
                            }
                            break;
                        case 2: //Triangles
                            if (primitive.GetType() == typeof(Plane))
                            {
                                tempPrimitives[2 + count] = primitive;
                                count++;
                                triangleCount++;
                            }
                            break;
                    }
                }
            }
            
            tempPrimitives[0] =
        }
        */

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
