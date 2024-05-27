using INFOGR2024Template.SceneElements;
using INFOGR2024Template.Scenes;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;

namespace OpenTK
{
    class RayTracer
    {
        public CameraMode CameraMode = CameraMode.Debug2D;
        int vertexArrayObject;
        int programID, vertexShaderID, fragmentShaderID;
        int attribute_vPosition;
        int uniform_camera, uniform_ligths, uniform_sphereLights, uniform_triangleLights, uniform_lengths, uniform_time, uniform_iterations;
        int ssbo_spheres, ssbo_planes, ssbo_triangles, ssbo_accelerationStructure;
        float[] cameraData, lightsData;
        SphereStruct[] spheresData;
        PlaneStruct[] planesData;
        TriangleStruct[] trianglesData;
        public bool MouseEnabled = false;
        private Camera camera => scene.Camera;
        private IScene scene;
        private ViewAxis viewAxis = ViewAxis.Topdown;

        private Stopwatch stopwatch = new Stopwatch();
        private float time;
        private int pathFindingIterations = 0;
        Stopwatch timer = new Stopwatch();
        // constructor
        public RayTracer()
        {
            timer.Start();
            scene = new TestScene1();
            timer.Stop();
            Console.WriteLine("Time to build scene + acceleration structure: " + timer.ElapsedMilliseconds);
            timer.Reset();
        }
        // initialize
        public void Init()
        {
            stopwatch.Start();
            ScreenHelper.Resize(1280, 720);

            //these lines together with the similar one in RenderGL somehow fixed the unintended data sharing between this program and the screen program. I don't exactly know why so have to look into it
            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);

            //load shaders
            programID = GL.CreateProgram();
            LoadShader("../../../shaders/raytracer_vs.glsl", ShaderType.VertexShader, programID, out vertexShaderID);
            LoadShader("../../../shaders/raytracer_fs.glsl", ShaderType.FragmentShader, programID, out fragmentShaderID);
            GL.LinkProgram(programID);
            Debug.WriteLine(GL.GetProgramInfoLog(programID));
            Debug.WriteLine(GL.GetShaderInfoLog(fragmentShaderID));
            Debug.WriteLine(GL.GetShaderInfoLog(vertexShaderID));
            Debug.WriteLine(GL.GetError());

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
            uniform_camera = GL.GetUniformLocation(programID, "camera");
            uniform_lengths = GL.GetUniformLocation(programID, "lengths");
            uniform_ligths = GL.GetUniformLocation(programID, "lights");
            uniform_time = GL.GetUniformLocation(programID, "time");
            uniform_iterations = GL.GetUniformLocation(programID, "iterations");
            uniform_sphereLights = GL.GetUniformLocation(programID, "sphereLightPointers");
            uniform_triangleLights = GL.GetUniformLocation(programID, "triangleLightPointers");

            Debug.WriteLine(GL.GetString(StringName.Vendor));
            Debug.WriteLine("lightsLoc: " + uniform_ligths);
            Debug.WriteLine("max fragment uniform data size: " + GL.GetInteger(GetPName.MaxFragmentUniformComponents));       

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
            Console.WriteLine("Checkpoint 1");
            
            SendPrimitivesToShader();
            
            Console.WriteLine("Reached end of initialization");
        }
        // tick: renders one frame
        public void Tick()
        {
            //Stopwatch stops first and then starts, because otherwise it doesn't take into account the time that the GPU uses to render a frame
            timer.Stop();
            Console.WriteLine(timer.ElapsedMilliseconds);
            timer.Reset();
            
            timer.Start();
            
            ScreenHelper.Clear();
            scene.Tick();
            HandleInput();
            
            time += (float)stopwatch.Elapsed.TotalSeconds;
            pathFindingIterations++;
            
            switch (CameraMode)
            {
                case CameraMode.Debug2D:
                    RenderDebug2D();
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
        
        ///This code is copy-pasted 3 times for different axis, not because it is good, because that was the easiest approach
        private void RenderDebugTopDown(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives (except planes), drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.SpherePrimitives)
            {
                //Check if primitive would be offscreen
                if (primitive.BoundingBox.MinimumValues.X > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Z > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Z < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            foreach (var primitive in scene.TrianglePrimitives)
            {
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
                    ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Z), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Z),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Z), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Z),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Z), primitive.Material.DiffuseColor);
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
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.DiffuseColor);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.X, camera.Position.Z);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2((camera.TopLeftCameraPlane.X + camera.BottomLeftCameraPlane.X)/2, (camera.TopLeftCameraPlane.Z + camera.BottomLeftCameraPlane.Z)/2);
            Vector2 rightSideCameraPlane = new Vector2((camera.TopRightCameraPlane.X + camera.BottomRightCameraPlane.X)/2, (camera.TopRightCameraPlane.Z + camera.BottomRightCameraPlane.Z)/2);
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
            Vector2 topLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Z);
            Vector2 topRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Z);
            Vector2 bottomLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomLeftCameraPlane.X, camera.BottomLeftCameraPlane.Z);
            Vector2 bottomRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Z);
            Vector2i pixelPositionBottomLeftEdge = ScreenHelper.Vector2ToPixel(bottomLeftEdgeCameraPlane);
            Vector2i pixelPosBottomRightEdge = ScreenHelper.Vector2ToPixel(bottomRightEdgeCameraPlane);
            Vector2i pixelPosTopLeftEdge = ScreenHelper.Vector2ToPixel(topLeftEdgeCameraPlane);
            Vector2i pixelPosTopRightEdge = ScreenHelper.Vector2ToPixel(topRightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosBottomRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosTopLeftEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopLeftEdge, pixelPosTopRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopRightEdge, pixelPosBottomRightEdge, Color4.White);
            
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
                //+2, +3, and +5 to get all X and Z values from the float array
                DrawRectangle(scene.AccelerationStructureData[count], scene.AccelerationStructureData[count+2], 
                    scene.AccelerationStructureData[count+3], scene.AccelerationStructureData[count+5],Color4.White);
                
                //Update the count to start at the next bounding box
                //[count + 7] is the location where the length of the rest of the data here is stored
                //+8 because of the positioning in this bounding box of the float array
                count += (int)scene.AccelerationStructureData[count + 7] + 8;
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
        
        ///This code is copy-pasted 3 times for different axis, not because it is good, because that was the easiest approach
        private void RenderDebugSideXAxis(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives (except planes), drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.SpherePrimitives)
            {
                //Check if primitive would be offscreen
                if (primitive.BoundingBox.MinimumValues.Z > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.Z < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Y < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            foreach (var primitive in scene.TrianglePrimitives)
            {
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
                    ScaleToPixel(((Triangle)primitive).PointB.Z, ((Triangle)primitive).PointB.Y), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.Z, ((Triangle)primitive).PointB.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.Z, ((Triangle)primitive).PointC.Y), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.Z, ((Triangle)primitive).PointA.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.Z, ((Triangle)primitive).PointC.Y), primitive.Material.DiffuseColor);
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
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.DiffuseColor);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.Z, camera.Position.Y);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2((camera.TopLeftCameraPlane.Z + camera.BottomLeftCameraPlane.Z)/2, (camera.TopLeftCameraPlane.Y + camera.BottomLeftCameraPlane.Y)/2);
            Vector2 rightSideCameraPlane = new Vector2((camera.TopRightCameraPlane.Z + camera.BottomRightCameraPlane.Z)/2, (camera.TopRightCameraPlane.Y + camera.BottomRightCameraPlane.Y)/2);
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
            Vector2 topLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.Z, camera.TopLeftCameraPlane.Y);
            Vector2 topRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.Z, camera.TopRightCameraPlane.Y);
            Vector2 bottomLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomLeftCameraPlane.Z, camera.BottomLeftCameraPlane.Y);
            Vector2 bottomRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomRightCameraPlane.Z, camera.BottomRightCameraPlane.Y);
            Vector2i pixelPositionBottomLeftEdge = ScreenHelper.Vector2ToPixel(bottomLeftEdgeCameraPlane);
            Vector2i pixelPosBottomRightEdge = ScreenHelper.Vector2ToPixel(bottomRightEdgeCameraPlane);
            Vector2i pixelPosTopLeftEdge = ScreenHelper.Vector2ToPixel(topLeftEdgeCameraPlane);
            Vector2i pixelPosTopRightEdge = ScreenHelper.Vector2ToPixel(topRightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosBottomRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosTopLeftEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopLeftEdge, pixelPosTopRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopRightEdge, pixelPosBottomRightEdge, Color4.White);
            
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

            //Draw the bounding boxes of acceleration structure
            int count = 0;
            while (true)
            {
                //If at the end, stop
                if (count + 7 > scene.AccelerationStructureData.Length - 1)
                    break;
                
                //Draw the bounding box
                //+2, +1, +5, and +4 to get all Z and Y values from the float array
                DrawRectangle(scene.AccelerationStructureData[count+2], scene.AccelerationStructureData[count+1], 
                    scene.AccelerationStructureData[count+5], scene.AccelerationStructureData[count+4],Color4.White);
                
                //Update the count to start at the next bounding box
                //[count + 7] is the location where the length of the rest of the data here is stored
                //+8 because of the positioning in this bounding box of the float array
                count += (int)scene.AccelerationStructureData[count + 7] + 8;
            }
            
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
            
            //World position to line on screen
            void DrawRectangle(float x0, float y0, float x1, float y1, Color4 color)
            {
                ScreenHelper.DrawLine(ScaleToPixel(x0, y0), ScaleToPixel(x1, y0), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y0), ScaleToPixel(x1, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y1), ScaleToPixel(x0, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x0, y1), ScaleToPixel(x0, y0), color);
            }
        }
        
        ///This code is copy-pasted 3 times for different axis, not because it is good, because that was the easiest approach
        private void RenderDebugSideZAxis(float viewingRadius, int linesPerCircle, int exampleRayCount)
        {
            Vector2 bottomLeftPlane = new Vector2(-viewingRadius, -viewingRadius);
            Vector2 topRightPlane = new Vector2(viewingRadius, viewingRadius);
            
            //Draw all primitives (except planes), drawn first to be drawn under the camera things
            List<IPrimitive> primitivesToDraw = new List<IPrimitive>();
            foreach (var primitive in scene.SpherePrimitives)
            {
                //Check if primitive would be offscreen
                if (primitive.BoundingBox.MinimumValues.X > topRightPlane.X) continue;
                if (primitive.BoundingBox.MinimumValues.Y > topRightPlane.Y) continue;
                if (primitive.BoundingBox.MaximumValues.X < bottomLeftPlane.X) continue;
                if (primitive.BoundingBox.MaximumValues.Y < bottomLeftPlane.Y) continue;
                
                //If even slightly onscreen, just draw the entire primitive
                primitivesToDraw.Add(primitive);
            }
            foreach (var primitive in scene.TrianglePrimitives)
            {
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
                    ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Y), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointB.X, ((Triangle)primitive).PointB.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Y), primitive.Material.DiffuseColor);
                    ScreenHelper.DrawLine(ScaleToPixel(((Triangle)primitive).PointA.X, ((Triangle)primitive).PointA.Y),
                        ScaleToPixel(((Triangle)primitive).PointC.X, ((Triangle)primitive).PointC.Y), primitive.Material.DiffuseColor);
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
                            center.Y + radius * MathF.Cos(radians+radianIncrement)), primitive.Material.DiffuseColor);
                        radians += radianIncrement;
                    }
                }
            }
            
            //Draw a few camera rays
            Vector2 rayStartingPoint = new Vector2(camera.Position.X, camera.Position.Y);
            Vector2i pixelPositionRayStart = ScreenHelper.Vector2ToPixel(ScaleToBaseVectorByGivenPlane(rayStartingPoint));
            Vector2 leftSideCameraPlane = new Vector2((camera.TopLeftCameraPlane.X + camera.BottomLeftCameraPlane.X)/2, (camera.TopLeftCameraPlane.Y + camera.BottomLeftCameraPlane.Y)/2);
            Vector2 rightSideCameraPlane = new Vector2((camera.TopRightCameraPlane.X + camera.BottomRightCameraPlane.X)/2, (camera.TopRightCameraPlane.Y + camera.BottomRightCameraPlane.Y)/2);
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
            Vector2 topLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopLeftCameraPlane.X, camera.TopLeftCameraPlane.Y);
            Vector2 topRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.TopRightCameraPlane.X, camera.TopRightCameraPlane.Y);
            Vector2 bottomLeftEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomLeftCameraPlane.X, camera.BottomLeftCameraPlane.Y);
            Vector2 bottomRightEdgeCameraPlane = ScaleToBaseFloatsByGivenPlane(camera.BottomRightCameraPlane.X, camera.BottomRightCameraPlane.Y);
            Vector2i pixelPositionBottomLeftEdge = ScreenHelper.Vector2ToPixel(bottomLeftEdgeCameraPlane);
            Vector2i pixelPosBottomRightEdge = ScreenHelper.Vector2ToPixel(bottomRightEdgeCameraPlane);
            Vector2i pixelPosTopLeftEdge = ScreenHelper.Vector2ToPixel(topLeftEdgeCameraPlane);
            Vector2i pixelPosTopRightEdge = ScreenHelper.Vector2ToPixel(topRightEdgeCameraPlane);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosBottomRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPositionBottomLeftEdge, pixelPosTopLeftEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopLeftEdge, pixelPosTopRightEdge, Color4.White);
            ScreenHelper.DrawLine(pixelPosTopRightEdge, pixelPosBottomRightEdge, Color4.White);
            
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

            //Draw the bounding boxes of acceleration structure
            int count = 0;
            while (true)
            {
                //If at the end, stop
                if (count + 7 > scene.AccelerationStructureData.Length - 1)
                    break;
                
                //Draw the bounding box
                //+1, +3, and +4 to get all X and Y values from the float array
                DrawRectangle(scene.AccelerationStructureData[count], scene.AccelerationStructureData[count+1], 
                    scene.AccelerationStructureData[count+3], scene.AccelerationStructureData[count+4],Color4.White);
                
                //Update the count to start at the next bounding box
                //[count + 7] is the location where the length of the rest of the data here is stored
                //+8 because of the positioning in this bounding box of the float array
                count += (int)scene.AccelerationStructureData[count + 7] + 8;
            }
            
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
            
            //World position to line on screen
            void DrawRectangle(float x0, float y0, float x1, float y1, Color4 color)
            {
                ScreenHelper.DrawLine(ScaleToPixel(x0, y0), ScaleToPixel(x1, y0), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y0), ScaleToPixel(x1, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x1, y1), ScaleToPixel(x0, y1), color);
                ScreenHelper.DrawLine(ScaleToPixel(x0, y1), ScaleToPixel(x0, y0), color);
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
            GL.Uniform1(uniform_iterations, pathFindingIterations);
            GL.Uniform1(uniform_time, time);
        }
        private void SendPrimitivesToShader()
        {
            //shader limit values:
            int maxLights = 50;

            //fill float arrays for primitives data
            planesData = new PlaneStruct[scene.PlanePrimitives.Count];
            spheresData = new SphereStruct[scene.SpherePrimitives.Count];
            trianglesData = new TriangleStruct[scene.TrianglePrimitives.Count];
            int spheresAmount = 0;
            int planesAmount = 0;
            int trianglesAmount = 0;
            int sphereLightsAmount = 0;
            int triangleLightsAmount = 0;
            foreach (Sphere sphere in scene.SpherePrimitives)
            {
                if (sphere.Material.EmissionColor != Color4.Black)
                    sphereLightsAmount++;
            }
            foreach (Triangle triangle in scene.TrianglePrimitives)
            {
                if (triangle.Material.EmissionColor != Color4.Black)
                    triangleLightsAmount++;
            }
            spheresData = new SphereStruct[spheresAmount];
            planesData = new PlaneStruct[planesAmount];
            trianglesData = new TriangleStruct[trianglesAmount];
            int[] sphereLightPointers = new int[sphereLightsAmount];
            int[] triangleLightPointers = new int[triangleLightsAmount];

            int sphereCounter = 0;
            int planesCounter = 0;
            int trianglesCounter = 0;
            int sphereLightsCounter = 0;
            int triangleLightsCounter = 0;
            
            foreach (Plane plane in scene.PlanePrimitives)
            {
                Vector3 diffuseColor = new Vector3(plane.Material.DiffuseColor.R, plane.Material.DiffuseColor.G, plane.Material.DiffuseColor.B);
                Vector3 specularColor = new Vector3(plane.Material.SpecularColor.R, plane.Material.SpecularColor.G, plane.Material.SpecularColor.B);
                Vector3 emissionColor = new Vector3(plane.Material.EmissionColor.R, plane.Material.EmissionColor.G, plane.Material.EmissionColor.B);
                planesData[planesCounter] = new PlaneStruct(plane.Center, plane.Normal, diffuseColor, plane.Material.IsPureSpecular, specularColor, plane.Material.SpecularWidth, emissionColor);
                planesCounter++;
            }
            foreach (Sphere sphere in scene.SpherePrimitives)
            {
                Vector3 diffuseColor = new Vector3(sphere.Material.DiffuseColor.R, sphere.Material.DiffuseColor.G, sphere.Material.DiffuseColor.B);
                Vector3 specularColor = new Vector3(sphere.Material.SpecularColor.R, sphere.Material.SpecularColor.G, sphere.Material.SpecularColor.B);
                Vector3 emissionColor = new Vector3(sphere.Material.EmissionColor.R, sphere.Material.EmissionColor.G, sphere.Material.EmissionColor.B);
                spheresData[sphereCounter] = new SphereStruct(sphere.Center, sphere.Radius, diffuseColor, sphere.Material.IsPureSpecular, specularColor, sphere.Material.SpecularWidth, emissionColor);
                if (emissionColor != Vector3.Zero)
                {
                    sphereLightPointers[sphereLightsCounter++] = sphereCounter;
                }
                sphereCounter++;
            }
            foreach (Triangle triangle in scene.TrianglePrimitives)
            {
                Vector3 diffuseColor = new Vector3(triangle.Material.DiffuseColor.R, triangle.Material.DiffuseColor.G, triangle.Material.DiffuseColor.B);
                Vector3 specularColor = new Vector3(triangle.Material.SpecularColor.R, triangle.Material.SpecularColor.G, triangle.Material.SpecularColor.B);
                Vector3 emissionColor = new Vector3(triangle.Material.EmissionColor.R, triangle.Material.EmissionColor.G, triangle.Material.EmissionColor.B);
                trianglesData[trianglesCounter] = new TriangleStruct(triangle.PointA, triangle.PointB, triangle.PointC, triangle.Normal, diffuseColor, triangle.Material.IsPureSpecular, specularColor, triangle.Material.SpecularWidth, emissionColor);
                if (emissionColor != Vector3.Zero)
                {
                    triangleLightPointers[triangleLightsCounter++] = trianglesCounter;
                }
                trianglesCounter++;
            }
            if(scene.PointLights.Count > maxLights)
                Console.WriteLine("Warning: scene light count exceeds maximum! Some lights won't be calculated!");
            int lightsAmount = Math.Min(maxLights, scene.PointLights.Count);
            lightsData = new float[lightsAmount * 6];
            for (int i = 0; i < lightsAmount; i++)
            {
                int offset = i * 6;
                lightsData[0 + offset] = scene.PointLights[i].Position.X;
                lightsData[1 + offset] = scene.PointLights[i].Position.Y;
                lightsData[2 + offset] = scene.PointLights[i].Position.Z;
                //space for light color
                lightsData[3 + offset] = scene.PointLights[i].Intensity.R;
                lightsData[4 + offset] = scene.PointLights[i].Intensity.G;
                lightsData[5 + offset] = scene.PointLights[i].Intensity.B;
            }
            int[] lengths = 
            {
                sphereLightsAmount, triangleLightsAmount, lightsData.Length
            };
            Vector4[] lastScreen = new Vector4[ScreenHelper.screen.width * ScreenHelper.screen.height];
            for (int i = 0; i < lastScreen.Length; i++)
            {
                lastScreen[i] = new Vector4(0, 0, 0, 1.0f);
            }
            //send the primitives data to the shader
            GL.UseProgram(programID);
            GL.Uniform1(uniform_ligths, lightsData.Length, lightsData);
            GL.Uniform1(uniform_lengths, lengths.Length, lengths);
            GL.Uniform1(uniform_sphereLights, sphereLightPointers.Length, sphereLightPointers);
            GL.Uniform1(uniform_triangleLights, triangleLightPointers.Length, triangleLightPointers);

            //bind buffer for the spheres buffer ssbo0 
            ssbo_spheres = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo_spheres);
            //not sure about the order of last two lines
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, ssbo_spheres);
            //not sure about the buffer usage hint here
            GL.BufferData(BufferTarget.ShaderStorageBuffer, spheresData.Length * Marshal.SizeOf<SphereStruct>(), spheresData, BufferUsageHint.StaticRead);

            //bind buffer for the planes buffer ssbo1
            ssbo_planes = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo_planes);
            //not sure about the order of last two lines
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, ssbo_planes);
            //not sure about the buffer usage hint here
            GL.BufferData(BufferTarget.ShaderStorageBuffer, planesData.Length * Marshal.SizeOf<PlaneStruct>(), planesData, BufferUsageHint.StaticRead);

            //bind buffer for the triangles buffer ssbo2 
            ssbo_triangles = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo_triangles);
            //not sure about the order of last two lines
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, ssbo_triangles);
            //not sure about the buffer usage hint here
            GL.BufferData(BufferTarget.ShaderStorageBuffer, trianglesData.Length * Marshal.SizeOf<TriangleStruct>(), trianglesData, BufferUsageHint.StaticRead);
            
            //bind buffer for the triangles buffer ssbo3
            int ssbo_lastScreen = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo_lastScreen);
            //not sure about the order of last two lines
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, ssbo_lastScreen);
            //not sure about the buffer usage hint here
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 16 * lastScreen.Length, lastScreen, BufferUsageHint.DynamicDraw);
            
            //bind buffer for the acceleration structure buffer ssbo4
            ssbo_accelerationStructure = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo_accelerationStructure);
            //not sure about the order of last two lines
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, ssbo_accelerationStructure);
            //not sure about the buffer usage hint here
            GL.BufferData(BufferTarget.ShaderStorageBuffer, scene.AccelerationStructureData.Length*4, scene.AccelerationStructureData, BufferUsageHint.StaticRead);
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
            Vector3 oldTopLeftPlane = camera.TopLeftCameraPlane;
            Vector3 oldBottomRightPlane = camera.BottomRightCameraPlane;
            float minimumPlaneDistance = 0.05f;
            float delta = 1 / 60f;
            float speed = 3f;
            float keySensitivity = 0.3f;
            float mouseSensitivity = 0.05f;
            float mouseScrollSpeed = 5f;
            float zoomSpeed = 0.5f;
            Vector3 moveDirection = Vector3.Zero;

            //mouse input
            if (InputHelper.keyBoard.IsKeyReleased(Windowing.GraphicsLibraryFramework.Keys.M))
                MouseEnabled = !MouseEnabled;
            if(MouseEnabled)
            {
                //rotating
                camera.RotateHorizontal(delta * mouseSensitivity * InputHelper.mouse.Delta.X * MathF.PI);
                camera.RotateVertical(delta * mouseSensitivity * InputHelper.mouse.Delta.Y * MathF.PI);
                //zooming
                camera.DistanceToCenter = MathF.Max(minimumPlaneDistance, camera.DistanceToCenter + delta * InputHelper.mouse.ScrollDelta.Y * mouseScrollSpeed);
            }

            //zooming
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Z))
                camera.DistanceToCenter = MathF.Max(minimumPlaneDistance, camera.DistanceToCenter + delta * zoomSpeed);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.X))
                camera.DistanceToCenter = MathF.Max(minimumPlaneDistance, camera.DistanceToCenter - delta * zoomSpeed);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.F))
                camera.DistanceToCenter = MathF.Max(minimumPlaneDistance, camera.DistanceToCenter + 100 * delta * zoomSpeed);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.G))
                camera.DistanceToCenter = MathF.Max(minimumPlaneDistance, camera.DistanceToCenter - 100 * delta * zoomSpeed);

            //rotating with arrow keys
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Right))
                camera.RotateHorizontal(delta * keySensitivity * MathF.PI);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Left))
                camera.RotateHorizontal(delta * keySensitivity * -MathF.PI);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Up))
                camera.RotateVertical(delta * keySensitivity * -MathF.PI);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Down))
                camera.RotateVertical(delta * keySensitivity * MathF.PI);

            //moving
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.LeftShift))
                speed *= 3f;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.A))
                moveDirection -= camera.RightDirection;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.D))
                moveDirection += camera.RightDirection;
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.W))
                moveDirection  += new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.S))
                moveDirection -= new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.Space))
                moveDirection += new Vector3(0, 1, 0);
            if (InputHelper.keyBoard.IsKeyDown(Windowing.GraphicsLibraryFramework.Keys.LeftControl))
                moveDirection -= new Vector3(0, 1, 0);
            if (moveDirection != Vector3.Zero)
                camera.Position += moveDirection.Normalized() * speed * delta;
            
            //reset pathfindingIterations if camera view changed
            if (oldTopLeftPlane != camera.TopLeftCameraPlane && oldBottomRightPlane != camera.BottomRightCameraPlane)
            {
                pathFindingIterations = 0;
            }

            //switching rendering
            if(InputHelper.keyBoard.IsKeyPressed(Keys.D1))
                CameraMode = CameraMode.OpenGL;
            else if (InputHelper.keyBoard.IsKeyPressed(Keys.D2))
            {
                CameraMode = CameraMode.Debug2D;
                viewAxis = ViewAxis.Topdown;
            }
            else if (InputHelper.keyBoard.IsKeyPressed(Keys.D3))
            {
                CameraMode = CameraMode.Debug2D;
                viewAxis = ViewAxis.SideViewXAxis;
            }
            else if (InputHelper.keyBoard.IsKeyPressed(Keys.D4))
            {
                CameraMode = CameraMode.Debug2D;
                viewAxis = ViewAxis.SideViewZAxis;
            }
        }
    }

    enum CameraMode
    {
        Debug2D,
        OpenGL
    }
    struct PathFindingData
    {
        float time;
        int iterations;

        public PathFindingData(float time, int iterations)
        {
            this.time = time;
            this.iterations = iterations;
        }
    }
}