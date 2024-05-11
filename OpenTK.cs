using System;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Helper_classes;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

// The template provides you with a window which displays a 'linear frame buffer', i.e.
// a 1D array of pixels that represents the graphical contents of the window.

// Under the hood, this array is encapsulated in a 'Surface' object, and copied once per
// frame to an OpenGL texture, which is then used to texture 2 triangles that exactly
// cover the window. This is all handled automatically by the template code.

// Before drawing the two triangles, the template calls the Tick method in MyApplication,
// in which you are expected to modify the contents of the linear frame buffer.

// After (or instead of) rendering the triangles you can add your own OpenGL code.

// We will use both the pure pixel rendering as well as straight OpenGL code in the
// tutorial. After the tutorial you can throw away this template code, or modify it at
// will, or maybe it simply suits your needs.

namespace OpenTK
{
    public class OpenTKApp : GameWindow
    {
        /**
         * IMPORTANT:
         * 
         * Modern OpenGL (introduced in 2009) does NOT allow Immediate Mode or
         * Fixed-Function Pipeline commands, e.g., GL.MatrixMode, GL.Begin,
         * GL.End, GL.Vertex, GL.TexCoord, or GL.Enable certain capabilities
         * related to the Fixed-Function Pipeline. It also REQUIRES you to use
         * shaders.
         * 
         * If you want to try prehistoric OpenGL code, such as many code
         * samples still found online, enable it below.
         * 
         * MacOS doesn't support prehistoric OpenGL anymore since 2018.
         */
        public const bool allowPrehistoricOpenGL = false;

        int screenID;        // unique integer identifier of the OpenGL texture
        RayTracer? app;      // instance of the application
        bool terminated = false; // application terminates gracefully when this is true
        bool lastMouseEnabled = false;

        // The following variables are only needed in Modern OpenGL
        public int vertexArrayObject;
        public int vertexBufferObject;
        public int programID;
        // All the data for the vertices interleaved in one array:
        // - XYZ in normalized device coordinates
        // - UV
        readonly float[] vertices =
        { //  X      Y     Z     U     V
             -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // bottom-left  2-----3 triangles:
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // bottom-right | \   |     012
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f, // top-left     |   \ |     123
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // top-right    0-----1
        };

        public OpenTKApp()
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                ClientSize = new Vector2i(640, 400),
                Profile = allowPrehistoricOpenGL ? ContextProfile.Compatability : ContextProfile.Core,
                Flags = allowPrehistoricOpenGL ? ContextFlags.Default : ContextFlags.ForwardCompatible,
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            // called during application initialization
            Surface screen = new(ClientSize.X, ClientSize.Y);
            Surface.openTKApplication = this;
            ScreenHelper.Initialize(screen);
            app = new RayTracer();
            screenID = ScreenHelper.screen.GenTexture();
            if (allowPrehistoricOpenGL)
            {
                GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            }
            else
            {   // setting up a Modern OpenGL pipeline takes a lot of code
                // Vertex Array Object: will store the meaning of the data in the buffer
                vertexArrayObject = GL.GenVertexArray();
                GL.BindVertexArray(vertexArrayObject);
                // Vertex Buffer Object: a buffer of raw data
                vertexBufferObject = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
                // Vertex Shader
                string shaderSource = File.ReadAllText("../../../shaders/screen_vs.glsl");
                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, shaderSource);
                GL.CompileShader(vertexShader);
                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int status);
                if (status != (int)All.True)
                {
                    string log = GL.GetShaderInfoLog(vertexShader);
                    throw new Exception($"Error occurred whilst compiling vertex shader ({vertexShader}):\n{log}");
                }
                // Fragment Shader
                shaderSource = File.ReadAllText("../../../shaders/screen_fs.glsl");
                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, shaderSource);
                GL.CompileShader(fragmentShader);
                GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
                if (status != (int)All.True)
                {
                    string log = GL.GetShaderInfoLog(fragmentShader);
                    throw new Exception($"Error occurred whilst compiling fragment shader ({fragmentShader}):\n{log}");
                }
                // Program: a set of shaders to be used together in a pipeline
                programID = GL.CreateProgram();
                GL.AttachShader(programID, vertexShader);
                GL.AttachShader(programID, fragmentShader);
                GL.LinkProgram(programID);
                GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out status);
                if (status != (int)All.True)
                {
                    string log = GL.GetProgramInfoLog(programID);
                    throw new Exception($"Error occurred whilst linking program ({programID}):\n{log}");
                }
                // the program contains the compiled shaders, we can delete the source
                GL.DetachShader(programID, vertexShader);
                GL.DetachShader(programID, fragmentShader);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                // send all the following draw calls through this pipeline
                GL.UseProgram(programID);
                // tell the VAO which part of the VBO data should go to each shader input
                int locationPos = GL.GetAttribLocation(programID, "vPosition");
                GL.EnableVertexAttribArray(locationPos);
                GL.VertexAttribPointer(locationPos, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                int locationUV = GL.GetAttribLocation(programID, "vUV");
                GL.EnableVertexAttribArray(locationUV);
                GL.VertexAttribPointer(locationUV, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
                // connect the texture to the shader uniform variable
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, screenID);
                GL.Uniform1(GL.GetUniformLocation(programID, "pixels"), 0);

                Debug.WriteLine(programID + ", " + locationPos + ", " + locationUV);
            }
            app.Init();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            // called upon app close
            GL.DeleteTextures(1, ref screenID);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            // called upon window resize. Note: does not change the size of the pixel buffer.
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            if (allowPrehistoricOpenGL)
            {
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
            }
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // called once per frame; app logic
            InputHelper.keyBoard = KeyboardState;
            InputHelper.mouse = MouseState;
            if (InputHelper.keyBoard[Keys.Escape]) terminated = true;
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            // called once per frame; render
            if (app != null)
            {
                app.Tick();
                if (app.MouseEnabled != lastMouseEnabled)
                {
                    if (app.MouseEnabled)
                        this.CursorState = CursorState.Hidden;
                    else
                        this.CursorState = CursorState.Normal;
                }
                lastMouseEnabled = app.MouseEnabled;
            }
            if (terminated)
            {
                Close();
                return;
            }
            // convert MyApplication.screen to OpenGL texture
            if (app != null)
            {
                //preperation code that used to be in OnLoad, yet it has to occur every frame according to practical 0 exercise
                GL.ClearColor(0, 0, 0, 0);
                if (allowPrehistoricOpenGL)
                    GL.Enable(EnableCap.Texture2D);
                GL.Disable(EnableCap.DepthTest);

                GL.UseProgram(programID);
                GL.BindTexture(TextureTarget.Texture2D, screenID);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                               ScreenHelper.screen.width, ScreenHelper.screen.height, 0,
                               PixelFormat.Bgra,
                               PixelType.UnsignedByte, ScreenHelper.screen.pixels
                             );
                // draw screen filling quad
                if (allowPrehistoricOpenGL)
                {
                    //I believe this code draws the screen plane, the one of type Surface where we update the pixels, so it might not have to occur when in OpenGL rendering mode
                    GL.Begin(PrimitiveType.Quads);
                    GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-1.0f, -1.0f);
                    GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(1.0f, -1.0f);
                    GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(1.0f, 1.0f);
                    GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1.0f, 1.0f);
                    GL.End();

                    // prepare for generic OpenGL rendering (code from practical 0 exercices)
                    GL.Enable(EnableCap.DepthTest);
                    GL.Disable(EnableCap.Texture2D);
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                }
                else if(app.CameraMode != CameraMode.OpenGL)
                {
                    GL.UseProgram(programID);
                    GL.BindVertexArray(vertexArrayObject);
                    GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                }
                //render the scene after preperations are finished, notice this occurs after tick.
                app.RenderGL();
            }
            // tell OpenTK we're done rendering
            SwapBuffers();
        }
        public static void Main()
        {
            // entry point
            using OpenTKApp app = new();
            app.UpdateFrequency = 30.0;
            app.Run();
        }
    }
}