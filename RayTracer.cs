using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Scenes;
using INFOGR2024Template;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace OpenTK
{
    class RayTracer
    {
        private CameraMode cameraMode = CameraMode.Debug2D;
        private Camera camera => scene.Camera;
        private IScene scene;
        private ViewDirection viewDirection = ViewDirection.Topdown;
        // constructor
        public RayTracer()
        {
            scene = new TestScene1();
        }
        // initialize
        public void Init()
        {
            ScreenHelper.Resize(768, 768);
            
        }
        // tick: renders one frame
        public void Tick()
        {
            ScreenHelper.Clear();
            scene.Tick();
            switch (cameraMode)
            {
                case CameraMode.Debug2D:
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D1))
                        viewDirection = ViewDirection.Topdown;
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D2))
                        viewDirection = ViewDirection.SideViewXAxis;
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D3))
                        viewDirection = ViewDirection.SideViewZAxis;
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
            float viewingRadius = 5f;
            int linesPerCircle = 100;
            int exampleRayCount = 10; //Minimum 2

            switch (viewDirection)
            {
                case ViewDirection.Topdown:
                    RenderDebugTopDown(viewingRadius, linesPerCircle, exampleRayCount);
                    break;
                case ViewDirection.SideViewXAxis:
                    RenderDebugSideXAxis(viewingRadius, linesPerCircle, exampleRayCount);
                    break;
                case ViewDirection.SideViewZAxis:
                    RenderDebugSideZAxis(viewingRadius, linesPerCircle, exampleRayCount);
                    break;
            }
        }

        //This code is copy-pasted 3 times for different axis, because that was the easiest approach
        private void RenderDebugTopDown(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives, drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.Primitives)
            {
                //Don't draw planes
                if (primitive.GetType() == typeof(Plane))
                {
                    continue;
                }

                //Check if primitive would be offscreen
                if (primitive.BoundingBox[0].X > topRightPlane.X) continue;
                if (primitive.BoundingBox[0].Z > topRightPlane.Y) continue;
                if (primitive.BoundingBox[1].X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox[1].Z < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            
            foreach (IPrimitive primitive in primitivesToDraw)
            {
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
                    float radians = 0;
                    float radianIncrement = 2*MathF.PI / (float)linesPerCircle;
                    Vector2 center = new Vector2(primitive.Center.X, primitive.Center.Z);
                    float radius = ((Sphere)primitive).Radius;
                    for (int i = 0; i < linesPerCircle; i++)
                    {
                        ScreenHelper.DrawLine(ScaleToPixel(center.X + radius * MathF.Sin(radians), center.Y + radius * MathF.Cos(radians)),
                            ScaleToPixel(center.X + radius * MathF.Sin(radians+radianIncrement), 
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.Color);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.X, camera.Position.Z);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Z);
            Vector2 rightSideCameraPlane = new Vector2(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Z);
            Vector2 increment = (rightSideCameraPlane - leftSideCameraPlane) / (exampleRayCount - 1);
            for (int i = 0; i < exampleRayCount; i++)
            {
                Vector2 rayDirection = leftSideCameraPlane + increment * i - rayStartingPoint;
                rayDirection.Normalize();

                //Second point if nothing is hit
                float closestT = 20f;
                
                //Search for primitive hit
                foreach (IPrimitive primitive in primitivesToDraw)
                {
                    Vector2 center = new Vector2(primitive.Center.X, primitive.Center.Z);
                    if (primitive.GetType() == typeof(Triangle))
                    {
                        Vector2 pointA = new Vector2(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Z);
                        Vector2 pointB = new Vector2(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Z);
                        Vector2 pointC = new Vector2(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Z);
                        float t0 = IntersectLineWithRay(pointA, pointB);
                        float t1 = IntersectLineWithRay(pointA, pointC);
                        float t2 = IntersectLineWithRay(pointB, pointC);

                        if (t0 > 0 && t0 < closestT)
                            closestT = t0;
                        else if (t1 > 0 && t1 < closestT)
                            closestT = t1;
                        else if (t2 > 0 && t2 < closestT)
                            closestT = t2;
                        
                        float IntersectLineWithRay(Vector2 pointA, Vector2 pointB)
                        {
                            Vector2 lineSegmentDirection = pointB - pointA;
                            float crossDirections = Cross2D(rayDirection, lineSegmentDirection);
                            
                            //If this happens, then we the lines are parallel and they don't intersect
                            if(MathF.Abs(crossDirections) < 0.00001f)
                                return float.MaxValue;

                            if (Cross2D(pointA - rayStartingPoint, rayDirection) / crossDirections is >= 0 and <= 1)
                            {
                                float t = Cross2D(pointA - rayStartingPoint, lineSegmentDirection) / crossDirections;
                                return t;
                            }

                            //Ray and line segment don't intersect
                            return float.MaxValue;
                        }

                        float Cross2D(Vector2 v0, Vector2 v1)
                        {
                            return v0.X * v1.Y - v0.Y * v1.X;
                        }
                    }
                    else if (primitive.GetType() == typeof(Sphere))
                    {
                        Vector2 positionDifference = rayStartingPoint - center;
                        float b = 2 * (positionDifference.X * rayDirection.X + positionDifference.Y * rayDirection.Y);
                        float c = MathF.Pow(positionDifference.X, 2) + MathF.Pow(positionDifference.Y, 2) - MathF.Pow(((Sphere)primitive).Radius, 2);
                        float discriminant = b * b - 4 * c;
                        
                        if (discriminant < 0)
                        {
                            //Not closest positive t, so change nothing
                        }
                        else //1 or 2 solutions
                        {
                            float t1 = (-b + MathF.Sqrt(discriminant)) / 2;
                            float t2 = (-b - MathF.Sqrt(discriminant)) / 2;
                            if (t1 <= 0)
                            {
                                //No good value
                            }
                            else if(t2 <= 0.0001f)
                            {
                                closestT = MathF.Min(closestT, t1);
                            }
                            else
                            {
                                closestT = MathF.Min(closestT, t2);
                            }
                        }
                    }
                }
                
                //ClosestT gets changed depending on if and where ray intersects
                Vector2 secondPoint = ScaleToBaseVectorByGivenPlane(rayStartingPoint + rayDirection * closestT);
                ScreenHelper.DrawLine(pixelPositionRayStart, ScreenHelper.Vector2ToPixel(secondPoint), Color4.Yellow);
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
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-70, 3, white);
            ScreenHelper.screen.Print("X>>", 83, ScreenHelper.screen.height - 20, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 80, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 65, white);
            ScreenHelper.screen.Print("Z", 3, ScreenHelper.screen.height - 50, white);
            
            Vector2 ScaleToBaseFloatsByGivenPlane(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                x /= width / 2;
                y /= height / 2;
                return new Vector2(x, y);
            }
            
            Vector2 ScaleToBaseVectorByGivenPlane(Vector2 vector)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                vector.X /= width / 2;
                vector.Y /= height / 2;
                return vector;
            }

            Vector2i ScaleToPixel(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                x += width / 2;
                y += height / 2;
                x /= width;
                y /= height;
                return new Vector2i((int)(x * ScreenHelper.GetPixelWidth()), (int)(y * ScreenHelper.GetPixelHeight()));
            }
        }
        
        private void RenderDebugSideXAxis(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives, drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.Primitives)
            {
                //Don't draw planes
                if (primitive.GetType() == typeof(Plane))
                {
                    continue;
                }

                //Check if primitive would be offscreen
                if (primitive.BoundingBox[0].Z > topRightPlane.X) continue;
                if (primitive.BoundingBox[0].Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox[1].Z < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox[1].Y < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            
            foreach (IPrimitive primitive in primitivesToDraw)
            {
                if (primitive.GetType() == typeof(Triangle))
                {
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.Z, ((Triangle)primitive).PointA.Y),
                    ScaleToPixel(((Triangle)primitive).PointB.Z, ((Triangle)primitive).PointB.Y), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.Z, ((Triangle)primitive).PointB.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.Z, ((Triangle)primitive).PointC.Y), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.Z, ((Triangle)primitive).PointA.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.Z, ((Triangle)primitive).PointC.Y), primitive.Material.Color);
                }
                else if (primitive.GetType() == typeof(Sphere))
                {
                    float radians = 0;
                    float radianIncrement = 2*MathF.PI / (float)linesPerCircle;
                    Vector2 center = new Vector2(primitive.Center.Z, primitive.Center.Y);
                    float radius = ((Sphere)primitive).Radius;
                    for (int i = 0; i < linesPerCircle; i++)
                    {
                        ScreenHelper.DrawLine(ScaleToPixel(center.X + radius * MathF.Sin(radians), center.Y + radius * MathF.Cos(radians)),
                            ScaleToPixel(center.X + radius * MathF.Sin(radians+radianIncrement), 
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.Color);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.Z, camera.Position.Y);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2(camera.BottomLeftCameraPlane.Z, camera.BottomLeftCameraPlane.Y);
            Vector2 rightSideCameraPlane = new Vector2(camera.TopLeftCameraPlane.Z, camera.TopLeftCameraPlane.Y);
            Vector2 increment = (rightSideCameraPlane - leftSideCameraPlane) / (exampleRayCount - 1);
            for (int i = 0; i < exampleRayCount; i++)
            {
                Vector2 rayDirection = leftSideCameraPlane + increment * i - rayStartingPoint;
                rayDirection.Normalize();

                //Second point if nothing is hit
                float closestT = 20f;
                
                //Search for primitive hit
                foreach (IPrimitive primitive in primitivesToDraw)
                {
                    Vector2 center = new Vector2(primitive.Center.Z, primitive.Center.Y);
                    if (primitive.GetType() == typeof(Triangle))
                    {
                        Vector2 pointA = new Vector2(((Triangle)primitive).PointA.Z, ((Triangle)primitive).PointA.Y);
                        Vector2 pointB = new Vector2(((Triangle)primitive).PointB.Z, ((Triangle)primitive).PointB.Y);
                        Vector2 pointC = new Vector2(((Triangle)primitive).PointC.Z, ((Triangle)primitive).PointC.Y);
                        float t0 = IntersectLineWithRay(pointA, pointB);
                        float t1 = IntersectLineWithRay(pointA, pointC);
                        float t2 = IntersectLineWithRay(pointB, pointC);

                        if (t0 > 0 && t0 < closestT)
                            closestT = t0;
                        else if (t1 > 0 && t1 < closestT)
                            closestT = t1;
                        else if (t2 > 0 && t2 < closestT)
                            closestT = t2;
                        
                        float IntersectLineWithRay(Vector2 pointA, Vector2 pointB)
                        {
                            Vector2 lineSegmentDirection = pointB - pointA;
                            float crossDirections = Cross2D(rayDirection, lineSegmentDirection);
                            
                            //If this happens, then we the lines are parallel and they don't intersect
                            if(MathF.Abs(crossDirections) < 0.00001f)
                                return float.MaxValue;

                            if (Cross2D(pointA - rayStartingPoint, rayDirection) / crossDirections is >= 0 and <= 1)
                            {
                                float t = Cross2D(pointA - rayStartingPoint, lineSegmentDirection) / crossDirections;
                                return t;
                            }

                            //Ray and line segment don't intersect
                            return float.MaxValue;
                        }

                        float Cross2D(Vector2 v0, Vector2 v1)
                        {
                            return v0.X * v1.Y - v0.Y * v1.X;
                        }
                    }
                    else if (primitive.GetType() == typeof(Sphere))
                    {
                        Vector2 positionDifference = rayStartingPoint - center;
                        float b = 2 * (positionDifference.X * rayDirection.X + positionDifference.Y * rayDirection.Y);
                        float c = MathF.Pow(positionDifference.X, 2) + MathF.Pow(positionDifference.Y, 2) - MathF.Pow(((Sphere)primitive).Radius, 2);
                        float discriminant = b * b - 4 * c;
                        
                        if (discriminant < 0)
                        {
                            //Not closest positive t, so change nothing
                        }
                        else //1 or 2 solutions
                        {
                            float t1 = (-b + MathF.Sqrt(discriminant)) / 2;
                            float t2 = (-b - MathF.Sqrt(discriminant)) / 2;
                            if (t1 <= 0)
                            {
                                //No good value
                            }
                            else if(t2 <= 0.0001f)
                            {
                                closestT = MathF.Min(closestT, t1);
                            }
                            else
                            {
                                closestT = MathF.Min(closestT, t2);
                            }
                        }
                    }
                }
                
                //ClosestT gets changed depending on if and where ray intersects
                Vector2 secondPoint = ScaleToBaseVectorByGivenPlane(rayStartingPoint + rayDirection * closestT);
                ScreenHelper.DrawLine(pixelPositionRayStart, ScreenHelper.Vector2ToPixel(secondPoint), Color4.Yellow);
            }
            
            //Get camera info
            Vector2 cameraPos = new (camera.Position.Z, camera.Position.Y);
            Vector2i pixelPositionCamera = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos));

            //Draw camera plane
            Vector2 leftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomLeftCameraPlane.Z, camera.BottomLeftCameraPlane.Y);
            Vector2 rightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.Z, camera.TopLeftCameraPlane.Y);
            Vector2i pixelPosLeftEdge = ScreenHelper.Vector2ToPixel(leftEdgeCameraPlane);
            Vector2i pixelPosRightEdge = ScreenHelper.Vector2ToPixel(rightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPosLeftEdge, pixelPosRightEdge, Color4.White);
            
            //Draw camera angles
            Vector2 viewDirection = new(camera.ViewDirection.Z, camera.ViewDirection.Y);
            viewDirection.Normalize();
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);
            
            Vector2 rightDirection = new(camera.RightDirection.Z, camera.RightDirection.Y);
            rightDirection.Normalize();
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);

            //Draw view info text (Drawn last to be drawn on top of everything else)
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-70, 3, white);
            ScreenHelper.screen.Print("Z>>", 83, ScreenHelper.screen.height - 20, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 80, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 65, white);
            ScreenHelper.screen.Print("Y", 3, ScreenHelper.screen.height - 50, white);
            
            Vector2 ScaleToBaseFloatsByGivenPlane(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                x /= width / 2;
                y /= height / 2;
                return new Vector2(x, y);
            }
            
            Vector2 ScaleToBaseVectorByGivenPlane(Vector2 vector)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                vector.X /= width / 2;
                vector.Y /= height / 2;
                return vector;
            }

            Vector2i ScaleToPixel(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                x += width / 2;
                y += height / 2;
                x /= width;
                y /= height;
                return new Vector2i((int)(x * ScreenHelper.GetPixelWidth()), (int)(y * ScreenHelper.GetPixelHeight()));
            }
        }
        
        private void RenderDebugSideZAxis(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives, drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.Primitives)
            {
                //Don't draw planes
                if (primitive.GetType() == typeof(Plane))
                {
                    continue;
                }

                //Check if primitive would be offscreen
                if (primitive.BoundingBox[0].X > topRightPlane.X) continue;
                if (primitive.BoundingBox[0].Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox[1].X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox[1].Y < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            
            foreach (IPrimitive primitive in primitivesToDraw)
            {
                if (primitive.GetType() == typeof(Triangle))
                {
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Y),
                    ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Y), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Y), primitive.Material.Color);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Y), primitive.Material.Color);
                }
                else if (primitive.GetType() == typeof(Sphere))
                {
                    float radians = 0;
                    float radianIncrement = 2*MathF.PI / (float)linesPerCircle;
                    Vector2 center = new Vector2(primitive.Center.X, primitive.Center.Y);
                    float radius = ((Sphere)primitive).Radius;
                    for (int i = 0; i < linesPerCircle; i++)
                    {
                        ScreenHelper.DrawLine(ScaleToPixel(center.X + radius * MathF.Sin(radians), center.Y + radius * MathF.Cos(radians)),
                            ScaleToPixel(center.X + radius * MathF.Sin(radians+radianIncrement), 
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.Color);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.X, camera.Position.Y);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Y);
            Vector2 rightSideCameraPlane = new Vector2(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Y);
            Vector2 increment = (rightSideCameraPlane - leftSideCameraPlane) / (exampleRayCount - 1);
            for (int i = 0; i < exampleRayCount; i++)
            {
                Vector2 rayDirection = leftSideCameraPlane + increment * i - rayStartingPoint;
                rayDirection.Normalize();

                //Second point if nothing is hit
                float closestT = 20f;
                
                //Search for primitive hit
                foreach (IPrimitive primitive in primitivesToDraw)
                {
                    Vector2 center = new Vector2(primitive.Center.X, primitive.Center.Y);
                    if (primitive.GetType() == typeof(Triangle))
                    {
                        Vector2 pointA = new Vector2(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Y);
                        Vector2 pointB = new Vector2(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Y);
                        Vector2 pointC = new Vector2(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Y);
                        float t0 = IntersectLineWithRay(pointA, pointB);
                        float t1 = IntersectLineWithRay(pointA, pointC);
                        float t2 = IntersectLineWithRay(pointB, pointC);

                        if (t0 > 0 && t0 < closestT)
                            closestT = t0;
                        else if (t1 > 0 && t1 < closestT)
                            closestT = t1;
                        else if (t2 > 0 && t2 < closestT)
                            closestT = t2;
                        
                        float IntersectLineWithRay(Vector2 pointA, Vector2 pointB)
                        {
                            Vector2 lineSegmentDirection = pointB - pointA;
                            float crossDirections = Cross2D(rayDirection, lineSegmentDirection);
                            
                            //If this happens, then we the lines are parallel and they don't intersect
                            if(MathF.Abs(crossDirections) < 0.00001f)
                                return float.MaxValue;

                            if (Cross2D(pointA - rayStartingPoint, rayDirection) / crossDirections is >= 0 and <= 1)
                            {
                                float t = Cross2D(pointA - rayStartingPoint, lineSegmentDirection) / crossDirections;
                                return t;
                            }

                            //Ray and line segment don't intersect
                            return float.MaxValue;
                        }

                        float Cross2D(Vector2 v0, Vector2 v1)
                        {
                            return v0.X * v1.Y - v0.Y * v1.X;
                        }
                    }
                    else if (primitive.GetType() == typeof(Sphere))
                    {
                        Vector2 positionDifference = rayStartingPoint - center;
                        float b = 2 * (positionDifference.X * rayDirection.X + positionDifference.Y * rayDirection.Y);
                        float c = MathF.Pow(positionDifference.X, 2) + MathF.Pow(positionDifference.Y, 2) - MathF.Pow(((Sphere)primitive).Radius, 2);
                        float discriminant = b * b - 4 * c;
                        
                        if (discriminant < 0)
                        {
                            //Not closest positive t, so change nothing
                        }
                        else //1 or 2 solutions
                        {
                            float t1 = (-b + MathF.Sqrt(discriminant)) / 2;
                            float t2 = (-b - MathF.Sqrt(discriminant)) / 2;
                            if (t1 <= 0)
                            {
                                //No good value
                            }
                            else if(t2 <= 0.0001f)
                            {
                                closestT = MathF.Min(closestT, t1);
                            }
                            else
                            {
                                closestT = MathF.Min(closestT, t2);
                            }
                        }
                    }
                }
                
                //ClosestT gets changed depending on if and where ray intersects
                Vector2 secondPoint = ScaleToBaseVectorByGivenPlane(rayStartingPoint + rayDirection * closestT);
                ScreenHelper.DrawLine(pixelPositionRayStart, ScreenHelper.Vector2ToPixel(secondPoint), Color4.Yellow);
            }
            
            //Get camera info
            Vector2 cameraPos = new (camera.Position.X, camera.Position.Y);
            Vector2i pixelPositionCamera = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos));

            //Draw camera plane
            Vector2 leftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Y);
            Vector2 rightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Y);
            Vector2i pixelPosLeftEdge = ScreenHelper.Vector2ToPixel(leftEdgeCameraPlane);
            Vector2i pixelPosRightEdge = ScreenHelper.Vector2ToPixel(rightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPosLeftEdge, pixelPosRightEdge, Color4.White);
            
            //Draw camera angles
            Vector2 viewDirection = new(camera.ViewDirection.X, camera.ViewDirection.Y);
            viewDirection.Normalize();
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);
            
            Vector2 rightDirection = new(camera.RightDirection.X, camera.RightDirection.Y);
            rightDirection.Normalize();
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);

            //Draw view info text (Drawn last to be drawn on top of everything else)
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-70, 3, white);
            ScreenHelper.screen.Print("X>>", 83, ScreenHelper.screen.height - 20, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 80, white);
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 65, white);
            ScreenHelper.screen.Print("Y", 3, ScreenHelper.screen.height - 50, white);
            
            Vector2 ScaleToBaseFloatsByGivenPlane(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                x /= width / 2;
                y /= height / 2;
                return new Vector2(x, y);
            }
            
            Vector2 ScaleToBaseVectorByGivenPlane(Vector2 vector)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
                vector.X /= width / 2;
                vector.Y /= height / 2;
                return vector;
            }

            Vector2i ScaleToPixel(float x, float y)
            {
                float width = MathF.Abs(topRightPlane.X - bottomLeftPlane.X);
                float height = MathF.Abs(topRightPlane.Y - bottomLeftPlane.Y);
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
            int width = ScreenHelper.screen.width;
            int height = ScreenHelper.screen.height;
            Camera camera = scene.Camera;
            //for every pixel
            for (int x = 0; x < width; x++) 
            {
                for(int y = 0; y < height; y++)
                {
                    //make ray from camera through the plane in the correct spot
                    Vector3 bottomLeft = camera.BottomLeftCameraPlane;
                    Vector3 bottomRight = camera.BottomRightCameraPlane;
                    Vector3 topLeft = camera.TopLeftCameraPlane;
                    Vector3 planePos = bottomLeft + ((float)x/width) * (bottomRight - bottomLeft) + ((float)y/height) * (topLeft - bottomLeft);
                    Vector3 direction = planePos - camera.Position;
                    Ray viewRay = new Ray(camera.Position, direction);

                    //for every object in the scene
                    for(int i = 0; i < scene.Primitives.Count; i++) 
                    {
                        //Check for intersection and if the object is the closest hit object so far, store the distance and color
                        Tuple<float, Material> tuple = scene.Primitives[i].RayIntersect(viewRay);
                        if(tuple.Item1 > 0 && (tuple.Item1 < viewRay.T || viewRay.T == float.MinValue))
                        {
                            viewRay.T = tuple.Item1;
                            viewRay.Color = tuple.Item2.Color;
                        }
                    }
                    //The ray didn't hit so the rest can be skipped
                    if (viewRay.T < 0f)
                        continue;
                    //if there are no point lights in the scene, immediately return the color without lighting
                    if (scene.PointLights.Count == 0)
                    {
                        ScreenHelper.SetPixel(x, y, viewRay.Color);
                        continue;
                    }

                    //the illumination of the current pixel
                    float illumination = 0f;
                    //the intensity of each point light
                    float lightIntensity = 1f / scene.PointLights.Count;
                    Vector3 hitPos = viewRay.Origin + viewRay.Direction * viewRay.T;
                    //for each light
                    for (int l = 0; l < scene.PointLights.Count; l++)
                    {
                        Vector3 lightPos = scene.PointLights[l];
                        float distanceToLight = (lightPos - hitPos).Length;
                        Ray shadowRay = new Ray(hitPos, lightPos - hitPos);  
                        //for each object
                        for (int p = 0; p < scene.Primitives.Count; p++)
                        {
                            //check if it is between the lamp and the light
                            Tuple<float, Material> tuple = scene.Primitives[p].RayIntersect(shadowRay);
                            if (tuple.Item1 > 0.001f && tuple.Item1 < distanceToLight)
                            {
                                shadowRay.T = tuple.Item1;
                                break;
                            }
                        }
                        //if no objects were between the lamp and the light
                        if (shadowRay.T < 0f)
                        {
                            //add the lamps 'intensity' to the light
                            illumination += lightIntensity;
                        }
                    }
                    //if there is illumanition 
                    if (illumination > 0f)
                    {
                        Color4 c = viewRay.Color;
                        //draw the color adjusted by illumination to the screen
                        ScreenHelper.SetPixel(x, y, new Color4(c.R * illumination, c.G * illumination, c.B * illumination, 1f));
                    }
                }
            }
            Debug.WriteLine("frame finished");
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