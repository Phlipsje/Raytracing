using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using INFOGR2024Template.SceneElements;

namespace INFOGR2024Template.Scenes
{
    public class TestScene1 : IScene
    {
        public List<IPrimitive> primitives { get; set; }
        public Camera camera {  get; set; }

        public TestScene1() 
        {
            primitives = new List<IPrimitive> 
            {
                new Sphere(new Vector3(1,1,1), 1, new Material(new Color4(1f,1f,1f,1))),
                new Sphere(new Vector3(2,2,2), 0.2f, new Material(Color4.Red)),
                new Triangle(new Vector3(0, 0, 0), new Vector3(2, 2, 2), new Vector3(3, 1, 2), new Material(Color4.Green)),
                new Triangle(new Vector3(-2, 0, 1), new Vector3(2, 2, 2), new Vector3(0, 1, 3), new Material(Color4.Green))
            };
            camera = new Camera();
            camera.SetViewDirection(new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f));
        }
        public void Tick()
        {
            
        }
    }
}
