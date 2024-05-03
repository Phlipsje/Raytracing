using OpenTK.SceneElements;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                new Sphere(new Vector3(1,1,1), 1, new Material(new Color4(1,1,1,1))),
                new Sphere(new Vector3(2,2,2), 0.2f, new Material(new Color4(1,0,0,1)))
            };
            camera = new Camera();
        }
        public void Tick()
        {
            
        }
    }
}
