using com.radiant.engine.bundle;
using com.radiant.engine.runtime;
using Microsoft.Xna.Framework;
using RaytracingZuma;

/// <summary>Checks temple rendering and resizing or captures a single game frame.</summary>
sealed class ZumaWindow : Window
{
    private readonly ZumaScene scene = new();
    private int frame;
    private int testCase;
    private readonly bool captureOnly;
    private readonly (int width, int height, int level, int quality)[] cases =
        [(1280, 720, 0, 1), (960, 960, 1, 0), (800, 1000, 2, 2), (1440, 600, 0, 1)];

    /// <summary>Creates the test fixture with the supplied scene or window configuration.</summary>
    public ZumaWindow(bool captureOnly = false) : base(new GameLoop(), new() { Title = "Zuma isolated graphics checks" })
    {
        this.captureOnly = captureOnly;
        GameLoop.AddScene(scene);
        GameLoop.ThrottleUnfocused = false;
        GameLoop.SetTargetFps(0);
    }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (frame % 12 == 1)
        {
            var current = cases[testCase];
            scene.Game.StartLevel(current.level);
            scene.Game.Paused = true;
            UDRQuality.Set(current.quality);
            GraphicsDeviceManager.PreferredBackBufferWidth = current.width;
            GraphicsDeviceManager.PreferredBackBufferHeight = current.height;
            GraphicsDeviceManager.ApplyChanges();
        }
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        frame++;
        if (frame % 12 != 0) return;
        var current = cases[testCase];
        var renderer = scene.Renderer;
        if (renderer.ScreenWidth != current.width || renderer.ScreenHeight != current.height)
            throw new Exception("Resize failed.");
        var world = new Vector2(420, 310);
        if (Vector2.Distance(renderer.ScreenToWorld(renderer.WorldToScreen(world)), world) > 0.01f)
            throw new Exception("Aim coordinate mapping failed.");
        var lighting = scene.ECS.GetSystem<HRCGI>().GetOutput();
        if (lighting.Width != renderer.ScaledSize.X || lighting.Height != renderer.ScaledSize.Y)
            throw new Exception("Lighting quality size mismatch.");
        var emission = scene.ECS.GetSystem<Geometry>().EmissiveTexture;
        var pixels = new Color[emission.Width * emission.Height];
        emission.GetData(pixels);
        if (pixels.Count(pixel => pixel.R > 80 || pixel.G > 80 || pixel.B > 80) < 1000)
            throw new Exception("Temple or marble lighting is empty.");
        scene.Game.Paused = false;
        GameLoop.Render();
        Directory.CreateDirectory("artifacts/smoke");
        using (var output = File.Create($"artifacts/smoke/level-{current.level + 1}-{current.width}x{current.height}.png"))
            renderer.SceneRT.SaveAsPng(output, renderer.SceneRT.Width, renderer.SceneRT.Height);
        scene.Game.Paused = true;
        Console.WriteLine($"PASS: Vulkan level {current.level + 1}, {current.width}x{current.height}, quality {current.quality}, lighting and aim mapping.");
        testCase++;
        if (captureOnly || testCase == cases.Length) Exit();
    }
}
