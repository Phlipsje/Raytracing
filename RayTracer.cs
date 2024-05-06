using System.Runtime.CompilerServices;
using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Scenes;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;
using SixLabors.ImageSharp;

namespace OpenTK
{
    class RayTracer
    {
        private CameraMode cameraMode = CameraMode.Debug2D;
        private Camera camera => scene.camera;
        private IScene scene;
        // constructor
        public RayTracer()
        {
            scene = new TestScene1();
        }
        // initialize
        public void Init()
        {
            ScreenHelper.Resize(1280, 1280);
            
        }
        // tick: renders one frame
        public void Tick()
        {
            ScreenHelper.Clear();
            scene.Tick();
            switch (cameraMode)
            {
                case CameraMode.Debug2D:
                    RenderDebug2D();
                    break;
                case CameraMode.Debug3D:
                    RenderDebug3D();
                    break;
                case CameraMode.Raytracing:
                    RenderRaytracer();
                    break;
            }
        }

        #region Debug2D
        private void RenderDebug2D()
        {
            Vector2 topLeftPlane = new Vector2(-5, -5);
            Vector2 bottomRightPlane = new Vector2(5, 5);
            ViewDirection viewDirection = ViewDirection.Topdown;

            switch (viewDirection)
            {
                case ViewDirection.Topdown:
                    RenderDebugTopDown(topLeftPlane, bottomRightPlane);
                    break;
            }
        }

        private void RenderDebugTopDown(Vector2 topLeftPlane, Vector2 bottomRightPlane)
        {
            //Draw all primitives, drawn first to be drawn under the camera things
            foreach (IPrimitive primitive in scene.primitives)
            {
                //Don't draw planes
                if (primitive.GetType() == typeof(Plane))
                {
                    continue;
                }

                //Check if primitive would be offscreen
                if (primitive.BoundingBox[0].X > bottomRightPlane.X) continue;
                if (primitive.BoundingBox[0].Z > bottomRightPlane.Y) continue;
                if (primitive.BoundingBox[1].X < topLeftPlane.X) continue;
                if (primitive.BoundingBox[1].Z < topLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                if (primitive.GetType() == typeof(Triangle))
                {
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Z),
                    ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Z), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Z),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Z), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Z),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Z), primitive.Material.Color);
                }
                else if (primitive.GetType() == typeof(Sphere))
                {
                    int linesPerCircle = 100;
                    float radians = 0;
                    float radianIncrement = 2*MathF.PI / (float)linesPerCircle;
                    Vector2 center = new Vector2(primitive.Center.X, primitive.Center.Z);
                    float radius = ((Sphere)primitive).Radius;
                    for (int i = 0; i < linesPerCircle; i++)
                    {
                        Console.WriteLine(ScaleToPixel(center.X + radius * MathF.Sin(radians), center.Y + radius * MathF.Cos(radians)));
                        ScreenHelper.DrawLine(ScaleToPixel(center.X + radius * MathF.Sin(radians), center.Y + radius * MathF.Cos(radians)),
                            ScaleToPixel(center.X + radius * MathF.Sin(radians+radianIncrement), 
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.Color);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Get camera info
            Vector2 cameraPos = new (camera.Position.X, camera.Position.Z);
            Vector2i pixelPositionCamera = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos));

            //Draw camera plane
            Vector2 leftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Z);
            Vector2 rightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Z);
            Vector2i pixelPosLeftEdge = ScreenHelper.Vector2ToPixel(leftEdgeCameraPlane);
            Vector2i pixelPosRightEdge = ScreenHelper.Vector2ToPixel(rightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPosLeftEdge, pixelPosRightEdge, Color4.White);
            
            //Draw camera angles
            Vector2 viewDirection = new(camera.ViewDirection.X, camera.ViewDirection.Z);
            viewDirection.Normalize();
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);
            
            Vector2 rightDirection = new(camera.RightDirection.X, camera.RightDirection.Z);
            rightDirection.Normalize();
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);

            //Draw view info text (Drawn last to be drawn on top of everything else)
            ScreenHelper.screen.Print("2D debug mode", 3, 3, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("X>>", 23, ScreenHelper.screen.height - 20, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 80, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 65, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("Z", 3, ScreenHelper.screen.height - 50, ColorHelper.ColorToInt(Color4.White));
            
            Vector2 ScaleToBaseFloatsByGivenPlane(float x, float y)
            {
                float width = MathF.Abs(bottomRightPlane.X - topLeftPlane.X);
                float height = MathF.Abs(bottomRightPlane.Y - topLeftPlane.Y);
                x /= width;
                y /= height;
                return new Vector2(x, y);
            }
            
            Vector2 ScaleToBaseVectorByGivenPlane(Vector2 vector)
            {
                float width = MathF.Abs(bottomRightPlane.X - topLeftPlane.X);
                float height = MathF.Abs(bottomRightPlane.Y - topLeftPlane.Y);
                vector.X /= width;
                vector.Y /= height;
                return vector;
            }

            Vector2i ScaleToPixel(float x, float y)
            {
                float width = MathF.Abs(bottomRightPlane.X - topLeftPlane.X);
                float height = MathF.Abs(bottomRightPlane.Y - topLeftPlane.Y);
                x += width / 2;
                y += height / 2;
                x /= width;
                y /= height;
                return new Vector2i((int)(x * ScreenHelper.GetPixelWidth()), (int)(y * ScreenHelper.GetPixelHeight()));
            }
        }
        
        //Only used for 2D debug
        enum ViewDirection
        {
            Topdown,
            SideViewXAxis,
            SideViewZAxis,
        }
        #endregion

        #region Debug3D
        private void RenderDebug3D()
        {
            
        }
        #endregion

        #region Raytracer
        private void RenderRaytracer()
        {
            
        }
        #endregion
    }

    enum CameraMode
    {
        Debug2D,
        Debug3D,
        Raytracing
    }
}