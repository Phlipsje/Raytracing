using OpenTK.SceneElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOGR2024Template.Scenes
{
    /// <summary>
    /// The interface that represents a scene. Every scene has a list of primitives and a camera.
    /// </summary>
    public interface IScene
    {
        public List<IPrimitive> primitives { get; set; }
        public Camera camera { get; set; }

        public void Tick();
    }
}
