using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class TestScene2 : IScene
    {
        public List<IPrimitive> Primitives { get; set; }
        public Camera Camera { get; set; }
        public List<PointLight> PointLights { get; set; }

        public TestScene2()
        {
            Vector3 offset = new Vector3(10, 0, 0);
            Camera = new Camera(new Vector3(1, 5f, -5), new Vector3(0f, -1f, 1f), new Vector3(1, 0f, 0), 1f, 1.6f, 0.9f);
            //Camera = new Camera(new Vector3(0, 1f, -5), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0), 1f, 1.6f, 0.9f);
            Primitives = new List<IPrimitive>
            {
                new Sphere(new Vector3(0, 0.5f, 0), 0.5f, new Material(Color4.Red)),
                new Sphere(new Vector3(-1.5f, 0.5f, 0), 0.5f, new Material(Color4.Red, new Color4(0.3f, 0.3f, 0.3f, 1f), 3f)),
                new Sphere(new Vector3(1.5f, 0.5f, 0), 0.5f, new Material(Color4.Red, new Color4(1f, 0f, 0f, 1f), 5f)),
                new Plane(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Material(Color4.White), new Vector3(1, 0, 0), new Vector3(0, 0, 1))
            };
            float lampExtraDistance = 10f;
            PointLights = new List<PointLight>
            {
                new PointLight(new Vector3(0, 2f, 0f), new Color4(5, 5, 5, 1.0f)),
                //new PointLight(new Vector3(0f, 10f, 0f) * lampExtraDistance, new Color4(20000f, 20000f, 20000f, 1.0f)),
                //new PointLight(new Vector3(-10f, 10f, 5f) * lampExtraDistance, new Color4(20000f, 20000f, 20000f, 1.0f))
            };
        }
        public void Tick()
        {

        }
    }
}
