using Window = com.radiant.engine.backends.monogame.MonoGameWindow;
using System;
using com.radiant.engine.runtime;
using com.radiant.engine.input;
using Microsoft.Xna.Framework;
using RaytracingZuma;

/// <summary>Exercises gameplay through Radiant input snapshots on an isolated window.</summary>
internal sealed class InputRegressionWindow : Window
{
    private sealed class ScriptedInput : IInputSource
    {
        public InputSnapshot Snapshot;
        public InputSnapshot Capture() => Snapshot;
    }
    private readonly ScriptedInput input = new();
    private readonly ZumaScene scene = new();
    private int step;

    public InputRegressionWindow() : base(new GameLoop(), new() { Title = "Game input regression", Width = 640, Height = 480 })
    {
        GameLoop.AddScene(scene);
        GameLoop.InputSource = input;
        GameLoop.ThrottleUnfocused = false;
    }

    protected override void Update(GameTime gameTime)
    {
        Key[] keys = step switch { 1 or 2 or 4 => new[] { Key.P }, 6 or 7 => new[] { Key.Q }, 11 => new[] { Key.R }, _ => Array.Empty<Key>() };
        var mouse = new com.radiant.engine.input.MouseState(320, 180,
            RightButton: step is 9 or 10 ? com.radiant.engine.input.ButtonState.Pressed : com.radiant.engine.input.ButtonState.Released);
        input.Snapshot = new(new KeyboardState(keys), mouse);
        int loaded = scene.Game.LoadedColor, next = scene.Game.NextColor;
        if (step == 11)
        {
            if (!scene.Game.Fire(ZumaGame.Launcher - Vector2.UnitY * 150) || scene.Game.Shots.Count == 0)
                throw new Exception("Could not prepare a live shot for the restart input check.");
        }
        base.Update(gameTime);

        if (step is 1 or 2 or 3 && !scene.Game.Paused) throw new Exception("Pause key edge/hold failed.");
        if (step >= 4 && scene.Game.Paused) throw new Exception("Pause release/repress failed.");
        if (step is 6 or 9 && (scene.Game.LoadedColor != next || scene.Game.NextColor != loaded))
            throw new Exception("Keyboard/mouse ammo swap failed.");
        if (step is 7 or 10 && (scene.Game.LoadedColor != loaded || scene.Game.NextColor != next))
            throw new Exception("Held swap repeated.");
        if (step == 11 && scene.Game.Shots.Count != 0) throw new Exception("Restart did not clear shots.");
        if (++step == 13)
        {
            Console.WriteLine("PASS: Zuma Radiant input, pause edges, held keys, keyboard/mouse ammo swapping and restart.");
            Exit();
        }
    }
}
