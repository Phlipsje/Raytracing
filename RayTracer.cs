using INFOGR2024Template.Scenes;
using INFOGR2024Template;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.SceneElements;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using System;
using INFOGR2024Template.SceneElements;

namespace OpenTK
{
    class RayTracer
    {
        int programID, vertexShaderID, fragmentShaderID;
        int attribute_vPosition;
        int uniform_planes, uniform_spheres, uniform_triangles, uniform_camera, uniform_lengths;
        float[] planesData, spheresData, trianglesData, cameraData;
        public CameraMode CameraMode = CameraMode.OpenGL;
        private Camera camera => scene.Camera;
        private IScene scene;
        // constructor
        public RayTracer()
        {
            scene = new TestScene1();
        }
        // initialize
        public void Init()
        {
            ScreenHelper.Resize(1280, 720);

            //load shaders
            programID = GL.CreateProgram();
            LoadShader("../../../shaders/raytracer_vs.glsl", ShaderType.VertexShader, programID, out vertexShaderID);
            LoadShader("../../../shaders/raytracer_fs.glsl", ShaderType.FragmentShader, programID, out fragmentShaderID);
            GL.LinkProgram(programID);
            Debug.WriteLine(GL.GetProgramInfoLog(programID));
            Debug.WriteLine(GL.GetShaderInfoLog(fragmentShaderID));
            Debug.WriteLine(GL.GetShaderInfoLog(vertexShaderID));

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
            uniform_planes = GL.GetUniformLocation(programID, "planes");
            uniform_camera = GL.GetUniformLocation(programID, "camera");
            uniform_lengths = GL.GetUniformLocation(programID, "lengths");

            SendPrimitivesToShader();

            // bind buffer for positions
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

            switch (CameraMode)
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
                //GL.UseProgram(programID);
                GL.EnableVertexAttribArray(attribute_vPosition);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                //Debug.WriteLine(GL.GetError());
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
            //Draw view info text
            ScreenHelper.screen.Print("2D debug mode", 3, 3, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("X>>", 23, ScreenHelper.screen.height - 20, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 80, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("^", 3, ScreenHelper.screen.height - 65, ColorHelper.ColorToInt(Color4.White));
            ScreenHelper.screen.Print("Z", 3, ScreenHelper.screen.height - 50, ColorHelper.ColorToInt(Color4.White));
            
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
                    //space for specularity exponent n
                    spheresData[10 + offset] = 0f;
                    sphereCounter++;
                }
                else if (primitive is Plane)
                {
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
                    Triangle triangle = (Triangle)primitive;
                    int offset = 19 * planesCounter;
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
            float[] lengths = new float[]
            {
                planesData.Length, spheresData.Length, trianglesData.Length 
            };
            //send the primitives data to the shader
            GL.UseProgram(programID);
            GL.Uniform1(uniform_planes, planesData.Length, planesData);
            GL.Uniform1(uniform_triangles, trianglesData.Length, trianglesData);
            GL.Uniform1(uniform_spheres, spheresData.Length, spheresData);
            GL.Uniform1(uniform_lengths, 3, lengths);
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
    }

    enum CameraMode
    {
        Debug2D,
        Debug3D,
        Raytracing,
        OpenGL
    }
}