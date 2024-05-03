using OpenTK.SceneElements;
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
            primitives = new List<IPrimitive> { };
            camera = new Camera();
        }
        public void Tick()
        {
            
        }
    }
}
