using com.radiant.engine.bundle;
using com.radiant.engine.core;

namespace RaytracingZuma;

/// <summary>Connects the Zuma simulation, presentation, and Radiant lighting systems.</summary>
public sealed class ZumaScene : Scene
{
    public ZumaGame Game { get; } = new();

    /// <inheritdoc/>
    public override void SetupECS()
    {
        UDRQuality.Set(1);
        Renderer.RenderScale = UDRQuality.ScaleNormalized;
        Renderer.FitWorld(new Vector2(640, 360), new Vector2(1280, 720));
        Renderer.LoadFont("Temple", "fonts/PressStart2P.ttf");
        ECS.AddSystem<Inspector>();
        ECS.AddSystem<PerformanceMonitor>();
        ECS.AddSystem<Geometry>();
        var lighting = ECS.AddSystem<HRCGI>();
        var color = ECS.AddSystem<ColorManagement>();
        color.SetInputSource(lighting.GetOutput);
        var upscaler = ECS.AddSystem<UDR4>();
        upscaler.SetInputSource(color.GetOutput);
        ECS.AddSystem<ZumaPresentation>();
    }

    /// <inheritdoc/>
    public override void SetupScene()
    {
        Inspector.CreateWindow("zuma", "Zuma", 2, Visible: false);
        Inspector.AddButton("zuma", "cameraControls", "Camera controls", () => Inspector.OpenCameraControls());
        Inspector.AddButton("zuma", "restart", "Restart level", () => Game.StartLevel(Game.Level));
        Inspector.AddToggle("zuma", "pause", "Pause gameplay", false, paused => Game.Paused = paused);
        Inspector.AddDropdown("zuma", "level", "Level", ZumaGame.LevelNames, 0, level => Game.StartLevel(level));
    }
}
