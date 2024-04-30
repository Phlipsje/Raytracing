using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing;

namespace OpenTK
{
    class RayTracer
    {
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
        }

        
    }
}