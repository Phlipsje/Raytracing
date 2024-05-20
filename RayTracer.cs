using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Scenes;
using INFOGR2024Template;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using INFOGR2024Template.SceneElements;

namespace OpenTK
{
    class RayTracer
    {
        int vertexArrayObject;
        int programID, vertexShaderID, fragmentShaderID;
        int attribute_vPosition;
        int uniform_planes, uniform_spheres, uniform_triangles, uniform_camera, uniform_lights, uniform_lengths;
        float[] planesData, spheresData, trianglesData, cameraData, lightsData;
        public CameraMode CameraMode = CameraMode.OpenGL;
        private Camera camera => scene.Camera;
        private IScene scene;
        private ViewAxis viewAxis = ViewAxis.Topdown;
        // constructor
        public RayTracer()
        {
            scene = new TestScene1();
        }
        // initialize
        public void Init()
        {
            ScreenHelper.Resize(1280, 720);

            //these lines together with the similar one in RenderGL somehow fixed the unintended data sharing between this program and the screen program. I don't exactly know why so have to look into it
            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);

            //load shaders
            programID = GL.CreateProgram();
            LoadShader("../../../shaders/raytracer_vs.glsl", ShaderType.VertexShader, programID, out vertexShaderID);
            LoadShader("../../../shaders/raytracer_fs.glsl", ShaderType.FragmentShader, programID, out fragmentShaderID);
            GL.LinkProgram(programID);
            /*Debug.WriteLine(GL.GetProgramInfoLog(programID));
            Debug.WriteLine(GL.GetShaderInfoLog(fragmentShaderID));
            Debug.WriteLine(GL.GetShaderInfoLog(vertexShaderID));
            Debug.WriteLine(GL.GetError());*/

            // the program contains the compiled shaders, we can delete the source
            GL.DetachShader(programID, vertexShaderID);
            GL.DetachShader(programID, fragmentShaderID);
            GL.DeleteShader(vertexShaderID);
            GL.DeleteShader(fragmentShaderID);

            //create vertex information for screen filling quad
            float[] vertexData = new float[]
            {
                -1f, 1f, 0f,
                1f, 1f, 0f,
                -1f, -1f, 0f,
                1f, 1f, 0f,
                -1f, -1f, 0f,
                1f, -1f, 0f
            };

            //gain access to input variables
            attribute_vPosition = GL.GetAttribLocation(programID, "vPosition");
            uniform_planes = GL.GetUniformLocation(programID, "planes");
            uniform_spheres = GL.GetUniformLocation(programID, "spheres");
            uniform_triangles = GL.GetUniformLocation(programID, "triangles");
            uniform_camera = GL.GetUniformLocation(programID, "camera");
            uniform_lengths = GL.GetUniformLocation(programID, "lengths");
            uniform_lights = GL.GetUniformLocation(programID, "lights");

            Debug.WriteLine(GL.GetString(StringName.Vendor));
            Debug.WriteLine("planesLoc: " + uniform_planes + ". spheresLoc: " + uniform_spheres + ". trianglesLoc: " + uniform_triangles + ". ligthsLoc: " + uniform_lights);
            Debug.WriteLine("max fragment uniform data size: " + GL.GetInteger(GetPName.MaxFragmentUniformComponents));
            SendPrimitivesToShader();

            //bind buffer for positions
            GL.UseProgram(programID);
            int vbo_pos = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_pos);
            GL.BufferData<float>(BufferTarget.ArrayBuffer,
             vertexData.Length * 4,
            vertexData, BufferUsageHint.StaticDraw
             );
            GL.VertexAttribPointer(attribute_vPosition, 3,
             VertexAttribPointerType.Float,
            false, 0, 0
             );
        }
        // tick: renders one frame
        public void Tick()
        {
            ScreenHelper.Clear();
            scene.Tick();
            HandleInput();

            if (InputHelper.keyBoard.IsKeyPressed(Keys.F1))
            {
                CameraMode = CameraMode.OpenGL;
            }
            if (InputHelper.keyBoard.IsKeyPressed(Keys.F2))
            {
                CameraMode = CameraMode.Debug2D;
            }
            
            switch (CameraMode)
            {
                case CameraMode.Debug2D:
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D1))
                        viewAxis = ViewAxis.Topdown;
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D2))
                        viewAxis = ViewAxis.SideViewXAxis;
                    if (InputHelper.keyBoard.IsKeyPressed(Keys.D3))
                        viewAxis = ViewAxis.SideViewZAxis;
                    RenderDebug2D();
                    break;
                case CameraMode.Debug3D:
                    RenderDebug3D();
                    break;
                case CameraMode.Raytracing:
                    RenderRaytracer();
                    break;
                case CameraMode.OpenGL:
                    //in this case actual rendering is handled by RenderGL()
                    PrepareRenderOpenGL();
                    break;
            }
        }
        //called by OpenTK class. The order is: Tick() first, and then Render()
        public void RenderGL()
        {
            if(CameraMode == CameraMode.OpenGL)
            {
                //make sure we are using the right program, and thus the right shaders
                GL.UseProgram(programID);

                //execute shaders
                //this line together with the similar lines in Init fixed the unintended data sharing between this program and the screen program. I don't exactly know why so have to look into it
                GL.BindVertexArray(vertexArrayObject);
                GL.EnableVertexAttribArray(attribute_vPosition);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                //Debug.WriteLine("frame finished");
            }
            else
            {
                //make sure the screen shader works
            }
        }

        #region Debug2D
        private void RenderDebug2D()
        {
            float viewingRadius = 15f;
            int linesPerCircle = 100;
            int exampleRayCount = 15; //Minimum 2

            switch (viewAxis)
            {
                case ViewAxis.Topdown:
                    RenderDebugTopDown(viewingRadius, linesPerCircle, exampleRayCount);
                    break;
                case ViewAxis.SideViewXAxis:
                    RenderDebugSideXAxis(viewingRadius, linesPerCircle, exampleRayCount);
                    break;
                case ViewAxis.SideViewZAxis:
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
                if (primitive.BoundingBox.MinimumValues.X > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Z > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Z < bottomLeftPlane.Y) continue;
                
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
            Vector2 leftSideCameraPlane = new Vector2(camera.BottomLeftCameraPlane.X, camera.BottomLeftCameraPlane.Z);
            Vector2 rightSideCameraPlane = new Vector2(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Z);
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
            Vector2 leftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomLeftCameraPlane.X, camera.BottomLeftCameraPlane.Z);
            Vector2 rightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Z);
            Vector2i pixelPosLeftEdge = ScreenHelper.Vector2ToPixel(leftEdgeCameraPlane);
            Vector2i pixelPosRightEdge = ScreenHelper.Vector2ToPixel(rightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPosLeftEdge, pixelPosRightEdge, Color4.White);
            
            //Draw camera angles
            Vector2 viewDirection = new Vector2(camera.ViewDirection.X, camera.ViewDirection.Z).Normalized();
            if (float.IsNaN(viewDirection.X)) viewDirection.X = 0;
            if (float.IsNaN(viewDirection.Y)) viewDirection.Y = 0;
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);
            
            Vector2 rightDirection = new Vector2(camera.RightDirection.X, camera.RightDirection.Z).Normalized();
            if (float.IsNaN(rightDirection.X)) rightDirection.X = 0;
            if (float.IsNaN(rightDirection.Y)) rightDirection.Y = 0;
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);
            
            //Draw the bounding boxes of acceleration structure
            int count = 0;
            while (true)
            {
                //If at the end, stop
                if (count + 7 > scene.AccelerationStructureData.Length - 1)
                    break;
                
                //Draw the bounding box
                DrawRectangle(scene.AccelerationStructureData[count], scene.AccelerationStructureData[count+2], 
                    scene.AccelerationStructureData[count+3], scene.AccelerationStructureData[count+5],Color4.White);
                
                //Update the count to start at the next bounding box
                count += (int)scene.AccelerationStructureData[count + 7] + 7;
            }
            

            //Draw view info text (Drawn last to be drawn on top of everything else)
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-85, 3, white);
            ScreenHelper.screen.Print("X>>", 118, ScreenHelper.screen.height - 20, white);
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

            //World position to line on screen
            void DrawRectangle(float x0, float y0, float x1, float y1, Color4 color)
            {
                ScreenHelper.DrawLine(ScaleToPixel(x0, y0), ScaleToPixel(x1, y0), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y0), ScaleToPixel(x1, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y1), ScaleToPixel(x0, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x0, y1), ScaleToPixel(x0, y0), color);
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
                if (primitive.BoundingBox.MinimumValues.Z > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.Z < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Y < bottomLeftPlane.Y) continue;
                
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
            Vector2 viewDirection = new Vector2(camera.ViewDirection.Z, camera.ViewDirection.Y).Normalized();
            if (float.IsNaN(viewDirection.X)) viewDirection.X = 0;
            if (float.IsNaN(viewDirection.Y)) viewDirection.Y = 0;
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);

            Vector2 rightDirection = new Vector2(camera.RightDirection.Z, camera.RightDirection.Y).Normalized();
            if (float.IsNaN(rightDirection.X)) rightDirection.X = 0;
            if (float.IsNaN(rightDirection.Y)) rightDirection.Y = 0;
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);

            //Draw view info text (Drawn last to be drawn on top of everything else)
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-85, 3, white);
            ScreenHelper.screen.Print("Z>>", 118, ScreenHelper.screen.height - 20, white);
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
                if (primitive.BoundingBox.MinimumValues.X > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Y < bottomLeftPlane.Y) continue;
                
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
            Vector2 leftSideCameraPlane = new Vector2(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Y);
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
            Vector2 leftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Y);
            Vector2 rightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Y);
            Vector2i pixelPosLeftEdge = ScreenHelper.Vector2ToPixel(leftEdgeCameraPlane);
            Vector2i pixelPosRightEdge = ScreenHelper.Vector2ToPixel(rightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPosLeftEdge, pixelPosRightEdge, Color4.White);
            
            //Draw camera angles
            Vector2 viewDirection = new Vector2(camera.ViewDirection.X, camera.ViewDirection.Y).Normalized();
            if (float.IsNaN(viewDirection.X)) viewDirection.X = 0;
            if (float.IsNaN(viewDirection.Y)) viewDirection.Y = 0;
            Vector2i pixelPosViewDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + viewDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosViewDirectionCamera, Color4.Red);

            Vector2 rightDirection = new Vector2(camera.RightDirection.X, camera.RightDirection.Y).Normalized();
            if (float.IsNaN(rightDirection.X)) rightDirection.X = 0;
            if (float.IsNaN(rightDirection.Y)) rightDirection.Y = 0;
            Vector2i pixelPosRightDirectionCamera =
                ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(cameraPos + rightDirection));
            ScreenHelper.DrawLine(pixelPositionCamera, pixelPosRightDirectionCamera, Color4.Blue);
            
            //Draw camera (this is done after the lines, because then it is drawn over it, which looks nicer)
            ScreenHelper.DrawCircle(pixelPositionCamera.X, pixelPositionCamera.Y, 10, Color4.Yellow);

            //Draw view info text (Drawn last to be drawn on top of everything else)
            int white = ColorHelper.ColorToInt(Color4.White);
            ScreenHelper.screen.Print("2D debug mode", 3, 3, white);
            ScreenHelper.screen.Print($"({bottomLeftPlane.X},{bottomLeftPlane.Y})", 3, ScreenHelper.screen.height- 20, white);
            ScreenHelper.screen.Print($"({topRightPlane.X},{topRightPlane.Y})", ScreenHelper.screen.width-85, 3, white);
            ScreenHelper.screen.Print("X>>", 118, ScreenHelper.screen.height - 20, white);
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
        enum ViewAxis
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

        #region OpenGL stuff
        private void PrepareRenderOpenGL()
        {
            GL.UseProgram(programID);

            //fill float array for camera data and send it to shader.
            cameraData = new float[]
            {
                camera.Position.X, camera.Position.Y, camera.Position.Z,
                camera.BottomLeftCameraPlane.X, camera.BottomLeftCameraPlane.Y, camera.BottomLeftCameraPlane.Z,
                camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Y, camera.BottomRightCameraPlane.Z,
                camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Y, camera.TopLeftCameraPlane.Z,
                ScreenHelper.screen.width, ScreenHelper.screen.height
            };
            GL.Uniform1(uniform_camera, cameraData.Length, cameraData);
        }
        private void SendPrimitivesToShader()
        {
            //shader limit values:
            int maxSpheres = 90;
            int maxPlanes = 5;
            int maxTriangles = 150;
            int maxLights = 10;

            //fill float arrays for primitives data
            List<IPrimitive> primitives = scene.Primitives;
            int spheresAmount = 0;
            int planesAmount = 0;
            int trianglesAmount = 0;
            for (int i = 0; i < primitives.Count; i++)
            {
                if (primitives[i] is Sphere)
                    spheresAmount++;
                else if (primitives[i] is Plane)
                    planesAmount++;
                else
                    trianglesAmount++;
            }
            spheresAmount = Math.Min(spheresAmount, maxSpheres);
            planesAmount = Math.Min(planesAmount, maxPlanes);
            trianglesAmount = Math.Min(trianglesAmount, maxTriangles);
            spheresData = new float[spheresAmount * 11];
            planesData = new float[planesAmount * 13];
            trianglesData = new float[trianglesAmount * 19];
            int sphereCounter = 0;
            int planesCounter = 0;
            int trianglesCounter = 0;
            for (int i = 0; i < primitives.Count; i++)
            {
                IPrimitive primitive = primitives[i];
                if (primitive is Sphere)
                {
                    if (sphereCounter > maxSpheres - 1)
                        continue;
                    Sphere sphere = (Sphere)primitive;
                    int offset = 11 * sphereCounter;
                    spheresData[0 + offset] = sphere.Center.X;
                    spheresData[1 + offset] = sphere.Center.Y;
                    spheresData[2 + offset] = sphere.Center.Z;
                    spheresData[3 + offset] = sphere.Radius;
                    //diffuse color
                    spheresData[4 + offset] = sphere.Material.Color.R;
                    spheresData[5 + offset] = sphere.Material.Color.G;
                    spheresData[6 + offset] = sphere.Material.Color.B;
                    //space for specular color
                    spheresData[7 + offset] = 0f;
                    spheresData[8 + offset] = 0f;
                    spheresData[9 + offset] = 0f;
                    //space for specular exponent n
                    spheresData[10 + offset] = 0f;
                    sphereCounter++;
                }
                else if (primitive is Plane)
                {
                    if (planesCounter > maxPlanes - 1)
                        continue;
                    Plane plane = (Plane)primitive;
                    int offset = 13 * planesCounter;
                    planesData[0 + offset] = plane.Center.X;
                    planesData[1 + offset] = plane.Center.Y;
                    planesData[2 + offset] = plane.Center.Z;
                    planesData[3 + offset] = plane.Normal.X;
                    planesData[4 + offset] = plane.Normal.Y;
                    planesData[5 + offset] = plane.Normal.Z;
                    //diffuse color
                    planesData[6 + offset] = plane.Material.Color.R;
                    planesData[7 + offset] = plane.Material.Color.G;
                    planesData[8 + offset] = plane.Material.Color.B;
                    //space for specular color
                    planesData[9 + offset] = 0f;
                    planesData[10 + offset] = 0f;
                    planesData[11 + offset] = 0f;
                    //space for specularity exponent n
                    planesData[12 + offset] = 0f;
                    planesCounter++;
                }
                else
                {
                    if (trianglesCounter > maxTriangles - 1)
                        continue;
                    Triangle triangle = (Triangle)primitive;
                    int offset = 19 * trianglesCounter;
                    trianglesData[0 + offset] = triangle.PointA.X;
                    trianglesData[1 + offset] = triangle.PointA.Y;
                    trianglesData[2 + offset] = triangle.PointA.Z;
                    trianglesData[3 + offset] = triangle.PointB.X;
                    trianglesData[4 + offset] = triangle.PointB.Y;
                    trianglesData[5 + offset] = triangle.PointB.Z;
                    trianglesData[6 + offset] = triangle.PointC.X;
                    trianglesData[7 + offset] = triangle.PointC.Y;
                    trianglesData[8 + offset] = triangle.PointC.Z;
                    trianglesData[9 + offset] = triangle.Normal.X;
                    trianglesData[10 + offset] = triangle.Normal.Y;
                    trianglesData[11 + offset] = triangle.Normal.Z;
                    //diffuse color
                    trianglesData[12 + offset] = triangle.Material.Color.R;
                    trianglesData[13 + offset] = triangle.Material.Color.G;
                    trianglesData[14 + offset] = triangle.Material.Color.B;
                    //space for specular color
                    trianglesData[15 + offset] = 0f;
                    trianglesData[16 + offset] = 0f;
                    trianglesData[17 + offset] = 0f;
                    //space for specularity exponent n
                    trianglesData[18 + offset] = 0f;
                    trianglesCounter++;
                }
            }
            //only space for ten ligths
            int lightsAmount = Math.Min(maxLights, scene.PointLights.Count);
            lightsData = new float[lightsAmount * 7];
            for (int i = 0; i < lightsAmount; i++)
            {
                int offset = i * 7;
                lightsData[0 + offset] = scene.PointLights[i].X;
                lightsData[1 + offset] = scene.PointLights[i].Y;
                lightsData[2 + offset] = scene.PointLights[i].Z;
                //space for light color
                lightsData[3 + offset] = 0f;
                lightsData[4 + offset] = 0f;
                lightsData[5 + offset] = 0f;
                //space for light intensity
                lightsData[6 + offset] = 0f;
            }
            float[] lengths = new float[]
            {
                planesData.Length, spheresData.Length, trianglesData.Length, lightsData.Length
            };
            //send the primitives data to the shader
            GL.UseProgram(programID);
            GL.Uniform1(uniform_planes, planesData.Length, planesData);
            GL.Uniform1(uniform_triangles, trianglesData.Length, trianglesData);
            GL.Uniform1(uniform_spheres, spheresData.Length, spheresData);
            GL.Uniform1(uniform_lights, lightsData.Length, lightsData);
            GL.Uniform1(uniform_lengths, lengths.Length, lengths);
        }
        private void LoadShader(String name, ShaderType type, int program, out int ID)
        {
            ID = GL.CreateShader(type);
            using (StreamReader sr = new StreamReader(name))
                GL.ShaderSource(ID, sr.ReadToEnd());
            GL.CompileShader(ID);
            GL.AttachShader(program, ID);
            Console.WriteLine(GL.GetShaderInfoLog(ID));
        }
        #endregion
        void HandleInput()
        {
            //moving camera
            float delta = 1 / 60f;
            float speed = 1f;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.LeftShift))
                speed = 5f;
            Vector3 moveDirection = Vector3.Zero;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.A))
                moveDirection -= camera.RightDirection;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.D))
                moveDirection += camera.RightDirection;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.W))
                moveDirection  += new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.S))
                moveDirection -= new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z);
            if(moveDirection != Vector3.Zero)
                camera.Position += moveDirection.Normalized() * speed * delta;

            //switching rendering
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.D4))
                CameraMode = CameraMode.Debug3D;
            else if(InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.D5))
                CameraMode = CameraMode.OpenGL;
        }
    }

    enum CameraMode
    {
        Debug2D,
        Debug3D,
        Raytracing,
        OpenGL
    }
}