using OpenTK.SceneElements;
using INFOGR2024Template.Helper_classes;
using OpenTK.Mathematics;

namespace INFOGR2024Template.Scenes
{
    /// <summary>
    /// The interface that represents a scene. Every scene has a list of primitives and a camera.
    /// </summary>
    public interface IScene
    {
        public List<IPrimitive> Primitives { get; }
        public List<Vector3> PointLights { get; set; }
        public Camera Camera { get; set; }
        public RTree AccelerationStructure { get; protected set; }
        public float[] AccelerationStructureData { get; protected set; }

        public void Tick();

        public void ActivateAccelerationStructure()
        {
            AccelerationStructure = new RTree(this);
            
            for (int i = 0; i < Primitives.Count; i++)
            {
                AccelerationStructure.AddPrimitive(i);
            }

            AccelerationStructureData = AccelerationStructure.TurnIntoFloatArray();
        }
    }
}
