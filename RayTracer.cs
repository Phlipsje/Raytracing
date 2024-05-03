using INFOGR2024Template.Scenes;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing;

namespace OpenTK
{
    class RayTracer
    {
        IScene scene;
        // constructor
        public RayTracer(Surface screen)
        {
            
        }
        // initialize
        public void Init()
        {
            ScreenHelper.Resize(1280, 720);
        }
        // tick: renders one frame
        public void Tick()
        {
            ScreenHelper.Clear();
            scene.Tick();
        }

        
    }
}