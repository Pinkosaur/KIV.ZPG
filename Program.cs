using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using ZPG;

/// <summary>
/// Main application window hosting rendering, input handling, and scene updates.
/// </summary>
public class Program : GameWindow 
{
    const int POINT_SIZE = 10;

    BitmapFontRenderer fontRenderer;

    /// <summary>
    /// Mouse sensitivity multiplier used by camera controllers.
    /// </summary>
    float mouseSensitivity = 1f;

    /// <summary>
    /// Scene object collection rendered each frame.
    /// </summary>
    List<Model> Objects = new();

    /// <summary>
    /// Global scene shader used for all model draw calls.
    /// </summary>
    Shader shader;

    /// <summary>
    /// Active viewport helper converting normalized and pixel regions.
    /// </summary>
    Viewport viewport;

    /// <summary>
    /// Active camera instance.
    /// </summary>
    Camera camera;

    /// <summary>
    /// Global lights sent to the shader.
    /// </summary>
    List<Light> Lights = new();

    /// <summary>
    /// When true, lamp behavior is controlled by simulated day/night cycle.
    /// </summary>
    bool lampModeAuto = true;

    /// <summary>
    /// Manual lamp state toggle.
    /// </summary>
    bool lampOn = false;

    /// <summary>
    /// Toggles HUD text rendering.
    /// </summary>
    bool displayHud = true;

    /// <summary>
    /// Exponential moving average of rendered FPS.
    /// </summary>
    double fps = 0;

    /// <summary>
    /// Real-world start time used for simulation timeline.
    /// </summary>
    float StartTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

    /// <summary>
    /// Elapsed simulation time in radians.
    /// </summary>
    float ElapsedTime => ((float)DateTime.Now.TimeOfDay.TotalSeconds - StartTime) * .05f; // 10 minutes = 1 real world second

    /// <summary>
    /// Simulated hour-of-day value in range [0, 24).
    /// </summary>
    float hours => (ElapsedTime + MathF.PI) % (2f * MathF.PI) / (2f * MathF.PI) * 24;

    /// <summary>
    /// Simulated minute component.
    /// </summary>
    int minutes => (int)((hours - (int)hours) * 60);

    /// <summary>
    /// Sunlight intensity multiplier derived from simulated sun angle.
    /// </summary>
    float sunlightMultiplier => MathHelper.Clamp(MathF.Cos(ElapsedTime) + .15f, 0f, 1f);

    /// <summary>
    /// Terrain map path loaded during initialization.
    /// </summary>
    string mapFilePath = "maps/hruba_skala_objekty.png";

    /// <summary>
    /// Initializes a new instance of the Program main window with standard OpenGL Core configurations.
    /// </summary>
    public Program() : base(GameWindowSettings.Default, new NativeWindowSettings() { 
        Profile = ContextProfile.Core,
        Flags = ContextFlags.Default,
        API = ContextAPI.OpenGL,
        APIVersion = new Version(3, 3),
        Vsync = VSyncMode.On
    }) { }

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Optional command-line arguments. First argument overrides terrain map path.</param>
    public static void Main(string[] args) {
        if (args.Length > 0)
        {
            Console.WriteLine($"Using terrain file: {args[0]}");
            new Program() { mapFilePath = args[0] }.Run();
        }
        else
            new Program().Run();
    }

    /// <summary>
    /// Executes initialization logic prior to the primary update and render loops.
    /// Loads core 3D models, configures the environment lights, and maps terrain geometry to the active camera.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine("GL_VERSION:  " + GL.GetString(StringName.Version));
        Console.WriteLine("GL_RENDERER: " + GL.GetString(StringName.Renderer));
        GL.GetInteger(GetPName.ContextFlags, out int ctxFlags);
        GL.GetInteger(GetPName.ContextProfileMask, out int profileMask);
        Console.WriteLine($"FORWARD COMPATIBILTY: {(ctxFlags & 0x1) != 0}");
        Console.WriteLine($"COMPATIBILITY: {(profileMask & 0x2) != 0} ");
        CursorState |= CursorState.Grabbed;

        viewport = new Viewport();
        viewport.ClientSize = ClientSize;
        
        camera = new CameraWalk() { Position = new Vector3(0, 11.8f, 0) };

        var (terrainVertices, terrainMeshParts, obj, width, height) = TerrainLoader.Load(mapFilePath);
        terrainMeshParts[0].Texture = new Texture2D("textures/Ground023_1K-JPG/Ground023_1K-JPG_Color.jpg");
        terrainMeshParts[0].Material = new Material() { diffuse = new Vector3(.5f, .38f, .15f), specular = new Vector3(0.1f), shininess = 5f };
        Model terrain = new Model(terrainVertices, terrainMeshParts);
        
        
        
        Objects.Add(terrain);

        if (camera is CameraWalk walkCam)
        {
            walkCam.ComputeHeightMap(terrainVertices, terrainMeshParts, width, height);
        }


        //tree
        var (treeVert, treeTri) = ObjLoader.Load("objects/LowPoly_Tree_v1.obj");
        Material treeTrunkMat = new Material() { diffuse = new Vector3(.65f, .28f, .07f), specular = new Vector3(0.1f), shininess = 1f};
        Material treeLeavesMat = new Material() { diffuse = new Vector3(.04f, .4f, .08f), specular = new Vector3(0.2f), shininess = 1f };
        Texture2D treeLeavesTexture = new Texture2D("textures/leaves1.jpg");
        Texture2D treeTrunkTexture = new Texture2D("textures/Bark014_1K-JPG/Bark014_1K-JPG_Color.jpg");
        Vector3 treeRot = new Vector3(-MathF.PI / 2, 0, 0);

        //rock
        var (rockVert, rockTri) = ObjLoader.Load("objects/Rock1.obj");
        Material rockMat = new Material() { diffuse = new Vector3(0.2f, 0.2f, 0.2f), specular = new Vector3(0.5f), shininess = 5f };
        Random random = new Random();
        foreach (var o in obj)
        {
            if (o.W == 1) // tree
            {
                Console.WriteLine($"Tree at ({o.X},{o.Y},{o.Z})");
                treeRot.Y = (float)random.NextDouble() * MathF.PI * 2f;
                Model tree = new Model(treeVert, treeTri) { 
                    Scale = Vector3.One * ((float)random.NextDouble() * .07f + .05f),
                    Rotation = treeRot,
                    Position = o.Xyz
                    };
                tree.meshParts[0].Texture = treeTrunkTexture;
                tree.meshParts[0].Material = treeTrunkMat;
                tree.meshParts[1].Texture = treeLeavesTexture;
                tree.meshParts[1].Material = treeLeavesMat;

                Console.WriteLine($"Tree has {tree.meshParts.Length} mesh parts");
                foreach (var part in tree.meshParts)
                {
                    Console.WriteLine($"Mesh part '{part.Name}' has {part.Triangles.Length} triangles");
                }
                Objects.Add(tree);
            }
            else if (o.W == 2) // rock
            {
                Console.WriteLine($"Rock at ({o.X},{o.Y},{o.Z})");
                Model rock = new Model(rockVert, rockTri) { 
                    Scale = Vector3.One * ((float)random.NextDouble() * .1f + 1.5f),
                    //Rotation = new Vector3(-MathF.PI / 2, 0, 0), 
                    Material = rockMat,
                    Position = o.Xyz
                    };
                Objects.Add(rock);
            }
        }
        
        // sun
        Lights.Add(Light.CreateDirectional(direction: new Vector3(0, -1, 0), color: new Vector3(1, 1, .9f), intensity: 1f));

        // lamp (at night)
        Lights.Add(Light.CreatePoint(direction: camera.Position, color: new Vector3(1, .8f, .6f), intensity: 0f));

        shader = new Shader("shaders/basic.vert", "shaders/basic.frag");

        fontRenderer = new BitmapFontRenderer("fonts/arial/ARIAL.TTF", 18, ClientSize.X, ClientSize.Y);
    }

    /// <summary>
    /// Updates application viewport bounds in response to window manipulation.
    /// </summary>
    /// <param name="e">Event arguments containing resize dimensions.</param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        viewport.ClientSize = ClientSize;
        if (fontRenderer != null)
        {
            fontRenderer.UpdateScreenSize(ClientSize.X, ClientSize.Y);
        }
    }

    /// <summary>
    /// Executes the game logic simulation frame-by-frame, resolving inputs and propagating transformation vectors.
    /// </summary>
    /// <param name="args">Timing data associated with the current frame execution.</param>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        
        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();
            
        if (KeyboardState.IsKeyPressed(Keys.F))
        {
            if (WindowState == WindowState.Fullscreen)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Fullscreen;
        }

        if (camera is CameraFly flyCam) 
        {
            float speed = 1f;
            if (KeyboardState.IsKeyDown(Keys.Space)) speed = 10f;

            var camVelocity = Vector3.Zero;
            if (KeyboardState.IsKeyDown(Keys.W)) flyCam.Advance(speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.S)) flyCam.Advance(-speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.D)) flyCam.Strafe(speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.A)) flyCam.Strafe(-speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.LeftShift)) camVelocity.Y = speed;
            if (KeyboardState.IsKeyDown(Keys.LeftControl)) camVelocity.Y = -speed;
            flyCam.Velocity = camVelocity;

            flyCam.AngularVelocity = MouseState.Delta * mouseSensitivity;

            if (KeyboardState.IsKeyDown(Keys.Equal))
                flyCam.Fov = MathF.Max(0.1f, flyCam.Fov - (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.Minus))
                flyCam.Fov = MathF.Min(MathF.PI - 0.1f, flyCam.Fov + (float)args.Time);
        }
        if (camera is CameraWalk walkCam) 
        {
            // Doom walking speed: approx. 290 units per second
            // 1 map unit approx 1 inch, so 290 units/s = 290 inches/s = approx. 7.4 m/s

            float speed = 7.4f;


            if (KeyboardState.IsKeyPressed(Keys.Space)) {
                Vector3 jumpVelocity = walkCam.Velocity;
                jumpVelocity.Y += 8f;
                walkCam.Jump(jumpVelocity);
            }

            if (KeyboardState.IsKeyPressed(Keys.LeftControl))
            {
                walkCam.ToggleCrouch();
            }

            walkCam.Velocity = Vector3.Zero;
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                if (KeyboardState.IsKeyDown(Keys.LeftShift)) walkCam.Run(speed, (float)args.Time);
                else walkCam.Advance(speed, (float)args.Time);
            }
            if (KeyboardState.IsKeyDown(Keys.S)) walkCam.Advance(-speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.D)) walkCam.Strafe(speed, (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.A)) walkCam.Strafe(-speed, (float)args.Time);
            

            walkCam.AngularVelocity = MouseState.Delta * mouseSensitivity;

            if (KeyboardState.IsKeyDown(Keys.Equal))
                walkCam.Fov = MathF.Max(0.1f, walkCam.Fov - (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.Minus))
                walkCam.Fov = MathF.Min(MathF.PI - 0.1f, walkCam.Fov + (float)args.Time);
        }
        if (camera is CameraOrtho orthoCam) 
        {
            var camVelocity = Vector3.Zero;
            if (KeyboardState.IsKeyDown(Keys.W)) camVelocity.Y = 1f;
            if (KeyboardState.IsKeyDown(Keys.S)) camVelocity.Y = -1f;
            if (KeyboardState.IsKeyDown(Keys.D)) camVelocity.X = 1f;
            if (KeyboardState.IsKeyDown(Keys.A)) camVelocity.X = -1f;
            camera.Velocity = camVelocity;

            var delta = MouseState.Delta;
            orthoCam.AngularZVelocity = delta.X * mouseSensitivity;
        }
        else if (camera is CameraOrbit orbitCam)
        {
            orbitCam.AngularVelocity = MouseState.Delta * mouseSensitivity;
            orbitCam.Velocity = new Vector3(0, 0, -MouseState.ScrollDelta.Y * 100f);

            if (KeyboardState.IsKeyDown(Keys.Equal))
                orbitCam.Fov = MathF.Max(0.1f, orbitCam.Fov - (float)args.Time);
            if (KeyboardState.IsKeyDown(Keys.Minus))
                orbitCam.Fov = MathF.Min(MathF.PI - 0.1f, orbitCam.Fov + (float)args.Time);
        }

        if (KeyboardState.IsKeyPressed(Keys.L))
        {
            lampModeAuto = !lampModeAuto;
        }
        if (!lampModeAuto && KeyboardState.IsKeyPressed(Keys.K))
        {
            lampOn = !lampOn;
        }

        if (KeyboardState.IsKeyPressed(Keys.H))
        {
            displayHud = !displayHud;
        }

        if (KeyboardState.IsKeyPressed(Keys.V))
        {
            if (VSync == VSyncMode.On)
            {
                VSync = VSyncMode.Off;
            }
            else if (VSync == VSyncMode.Off)
            {
                VSync = VSyncMode.Adaptive;
            }
            else
            {
                VSync = VSyncMode.On;
            }
        }

        camera.Update((float)args.Time);
        foreach (var obj in Objects) obj.Update((float)args.Time);

        var sun = Lights[0];

        Vector4 sunPos = sun.Position;
        sunPos.Y = -MathF.Cos(ElapsedTime);
        sunPos.X = MathF.Sin(ElapsedTime);
        sun.Position = sunPos;

        Vector3 sunColor = sun.BaseColor;
        sunColor *= sunlightMultiplier;
        sun.Color = sunColor;

        var lamp = Lights[1];
        if (lampModeAuto)
        {
            if (hours < 7 || hours > 17)
            {
                lamp.Intensity = 1f;
                Vector4 lampPos = lamp.Position;
                lampPos = new Vector4(camera.Position, 1.0f);
                lamp.Position = lampPos;
                lampOn = true;
            }
            else
            {
                lamp.Intensity = 0f;
                lampOn = false;
            }
        }
        else
        {

            if (lampOn)
            {
                lamp.Intensity = 1f;
                Vector4 lampPos = lamp.Position;
                lampPos = new Vector4(camera.Position, 1.0f);
                lamp.Position = lampPos;
            }
            else
            {
                lamp.Intensity = 0f;
            }
        }

        base.OnUpdateFrame(args);
    }

    /// <summary>
    /// Executes individual draw calls for the complete collection of active scene objects.
    /// Configures depth buffers, shader uniforms, and projection vectors.
    /// </summary>
    /// <param name="viewport">The target drawing boundaries.</param>
    /// <param name="camera">The primary camera mapping projection layout.</param>
    /// <param name="gizmo">Optional debugging objects requiring specialized rendering overlays.</param>
    void DrawScene(Viewport viewport, Camera camera, SceneObject gizmo)
    {
        var (pos, size) = viewport.GetPixelViewport();
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(pos.X, pos.Y, size.X, size.Y);
        GL.Viewport(pos.X, pos.Y, size.X, size.Y);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.DepthTest);

        shader.Use();

        shader.SetUniform("cameraPosWorld", camera.Position);
        shader.SetUniform("lightCount", Lights.Count);
        for (int i = 0; i < Lights.Count; i++)
        {
            shader.SetUniform($"lights[{i}].position", Lights[i].Position);
            shader.SetUniform($"lights[{i}].color", Lights[i].Color);
            shader.SetUniform($"lights[{i}].intensity", Lights[i].Intensity);
        }
        shader.SetUniform("diffuseTexture", 0);

        var projection = camera.GetProjectionMatrix(viewport.GetAspectRatio());
        shader.SetUniform("projection", projection);

        var view = camera.ViewMatrix;
        shader.SetUniform("view", view);

        foreach (var obj in Objects)
        {
            var modelMatrix = obj.ModelMatrix;
            shader.SetUniform("model", modelMatrix);

            shader.SetUniform("useTexture", 1);

            obj.Draw(shader);
        }
    }

    /// <summary>
    /// Orchestrates hardware rendering pipeline interactions necessary for projecting geometry onto the backbuffer prior to swap.
    /// </summary>
    /// <param name="args">Timing data associated with the current frame execution.</param>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        
        GL.ClearColor(.2f * sunlightMultiplier + .025f, .4f * sunlightMultiplier + .05f, 1f * sunlightMultiplier + .125f, 0);
        DrawScene(viewport, camera, camera);

        if (displayHud)
        {
            fontRenderer.DrawText($"Position: {camera.Position.X:0.0}, {camera.Position.Y:0.0}, {camera.Position.Z:0.0}", 10, 10, Vector3.One * 255);
            fontRenderer.DrawText($"Time: {(int)hours:D2}:{minutes:D2}", 10, 30, Vector3.One * 255);
            string lampMode = lampModeAuto ? "Auto" : "Manual";
            string lampStatus = Lights[1].Intensity > 0 ? "on" : "off";
            string lampHint = lampModeAuto ? "" : " (K)";
            fontRenderer.DrawText($"Lamp toggle: {lampMode} (L)", 10, 50, Vector3.One * 255);
            fontRenderer.DrawText($"Lamp {lampStatus}{lampHint}", 10, 70, Vector3.One * 255);
            fontRenderer.DrawText("(H) to hide hud", 10, 90, Vector3.One * 255);
        }



        SwapBuffers();

        string vsync;
        fps = 0.95 * fps + 0.05 * (1 / args.Time);
        if (VSync == VSyncMode.On)
        {
            vsync = " On";
        }
        else if (VSync == VSyncMode.Off)
        {
             vsync = " Off";
        }
        else
        {
             vsync = " Adaptive";
        }
        Title = $"FPS: {fps:0} (VSync {vsync})";
    }

    /// <summary>
    /// Executes final memory cleanup commands before context destruction.
    /// Ensures that graphics processor resources are actively freed to prevent leaks.
    /// </summary>
    override protected void OnUnload()
    {
        base.OnUnload();
        shader.Dispose();
        fontRenderer?.Dispose();

        foreach (var obj in Objects) obj.Dispose();
    }
}