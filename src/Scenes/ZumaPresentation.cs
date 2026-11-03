using com.radiant.engine.bundle;
using com.radiant.engine.core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RaytracingZuma;

[RunBefore(typeof(Geometry))]
[SystemTag("Zuma")]
public sealed class ZumaPresentation : com.radiant.engine.core.System
{
    public static readonly Color[] Palette = [new(242, 72, 58), new(43, 194, 220), new(247, 188, 54), new(110, 221, 93)];
    private ZumaGame game;
    private MarblePath builtPath;
    private readonly Dictionary<int, int> marbleEntities = [];
    private readonly List<int> architecture = [];
    private readonly List<(int entity, Vector2 position, Vector2 velocity, float life, Color color)> particles = [];
    private KeyboardState previousKeys;
    private MouseState previousMouse;
    private Vector2 aim = ZumaGame.Launcher - Vector2.UnitY * 200;
    private int loadedEntity;
    private int nextEntity;
    private ZumaArtwork artwork;
    private readonly Dictionary<int, (int color, float travel, float rotation)> marbleSprites = [];
    private Vector2 facing = -Vector2.UnitY;
    private Geometry geometry;

    public override void Initialize()
    {
        game = ((ZumaScene)Scene).Game;
        geometry = Scene.ECS.GetSystem<Geometry>();
        artwork = new ZumaArtwork(Renderer);
        previousKeys = Keyboard.GetState();
        previousMouse = Mouse.GetState();
    }

    private int Orb(Vector2 position, float radius, Color emission, float depth, bool blocksLight = false)
    {
        return LightFactory.CreateLight(Scene.ECS, position, radius,
            blocksLight ? Color.White : Color.Transparent, emission, depth);
    }

    private void BuildTemple()
    {
        foreach (int entity in architecture)
            Scene.ECS.DestroyEntity(entity);

        foreach (int entity in marbleEntities.Values)
            Scene.ECS.DestroyEntity(entity);

        foreach (var particle in particles)
            Scene.ECS.DestroyEntity(particle.entity);

        architecture.Clear();
        marbleEntities.Clear();
        marbleSprites.Clear();
        particles.Clear();
        builtPath = game.Path;
        Scene.ECS.GetSystem<UDR4>().ResetHistory();

        // Layer stone channels, brass inlays, and the unlit recessed track beneath the marbles.
        for (int sampleIndex = 0; sampleIndex < builtPath.Points.Count; sampleIndex += 3)
        {
            var point = builtPath.Points[sampleIndex];
            architecture.Add(Orb(point, 25, new Color(31, 47, 43), 10));
            if (sampleIndex % 6 == 0)
            {
                var tangent = builtPath.Tangent(builtPath.Length * sampleIndex / 600f);
                var normal = new Vector2(-tangent.Y, tangent.X);
                architecture.Add(Orb(point + normal * 22, 2, new Color(109, 92, 46), 12));
                architecture.Add(Orb(point - normal * 22, 2, new Color(109, 92, 46), 12));
            }
            architecture.Add(Orb(point, 18, new Color(8, 18, 17), 15));
        }

        Vector2 entrance = builtPath.At(0);
        Vector2 exit = builtPath.At(builtPath.Length);
        architecture.Add(Orb(entrance, 28, new Color(30, 90, 75), 20));
        architecture.Add(Orb(exit, 30, new Color(165, 60, 22), 20));
        architecture.Add(Orb(ZumaGame.Launcher, 48, new Color(26, 73, 58), 30, true));
        loadedEntity = Orb(ZumaGame.Launcher, ZumaGame.Radius, Palette[game.LoadedColor], 41);
        nextEntity = Orb(ZumaGame.Launcher + new Vector2(0, 73), 10, Palette[game.NextColor], 42);
        architecture.AddRange([loadedEntity, nextEntity]);
    }

    public override void Update()
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool Pressed(Keys key) => keyboard.IsKeyDown(key) && previousKeys.IsKeyUp(key);
        if (Pressed(Keys.P) || Pressed(Keys.Space)) game.Paused = !game.Paused;
        if (Pressed(Keys.R)) game.StartLevel(game.Level);
        if (Pressed(Keys.Enter) && game.State != PlayState.Playing)
            game.StartLevel(game.State == PlayState.Won ? (game.Level + 1) % ZumaGame.LevelNames.Length : game.Level, game.State == PlayState.Won);

        if (!Inspector.IsMouseOverUI())
        {
            aim = Renderer.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
            if (Pressed(Keys.Q) || (mouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Released)) game.Swap();
            if (mouse.LeftButton == ButtonState.Pressed) game.Fire(aim);
        }
        previousKeys = keyboard;
        previousMouse = mouse;
        game.Update((float)GameTime.ElapsedGameTime.TotalSeconds);
        if (builtPath != game.Path) BuildTemple();
        SyncMarbles();
        UpdateParticles(game.Paused ? 0 : (float)GameTime.ElapsedGameTime.TotalSeconds);
        var direction = aim - ZumaGame.Launcher;
        if (direction.LengthSquared() < 1) direction = -Vector2.UnitY;
        direction.Normalize();
        facing = direction;
        Move(loadedEntity, ZumaGame.Launcher + direction * 50);
        Scene.ECS.GetComponent<Material>(loadedEntity).Emissive = Palette[game.LoadedColor];
        Scene.ECS.GetComponent<Material>(nextEntity).Emissive = Palette[game.NextColor];
    }

    private void Move(int entity, Vector2 position) => Scene.ECS.GetComponent<Transform2D>(entity).Position = position;

    private void SyncMarbles()
    {
        var alive = new HashSet<int>();

        foreach (var marble in game.Marbles)
        {
            alive.Add(marble.Id);
            Vector2 tangent = game.Path.Tangent(marble.Distance);
            SyncOrb(marble.Id, game.Path.At(marble.Distance), marble.Color, marble.Distance, MathF.Atan2(tangent.Y, tangent.X));
        }

        foreach (var shot in game.Shots)
        {
            alive.Add(shot.Id);
            SyncOrb(shot.Id, shot.Position, shot.Color, Vector2.Distance(ZumaGame.Launcher, shot.Position),
                MathF.Atan2(shot.Velocity.Y, shot.Velocity.X));
        }

        foreach (int id in marbleEntities.Keys.Where(id => !alive.Contains(id)).ToArray())
        {
            Scene.ECS.DestroyEntity(marbleEntities[id]);
            marbleEntities.Remove(id);
            marbleSprites.Remove(id);
        }
    }

    private void SyncOrb(int id, Vector2 position, int color, float travel, float rotation)
    {
        if (!marbleEntities.TryGetValue(id, out int entity))
        {
            entity = Orb(position, ZumaGame.Radius, Palette[color], 100, true);
            Scene.ECS.AddComponent<MotionTrackable>(entity);
            marbleEntities.Add(id, entity);
        }
        Move(entity, position);
        marbleSprites[id] = (color, travel, rotation);
    }

    private void UpdateParticles(float elapsed)
    {
        foreach (var burst in game.Bursts)
        {
            for (int sparkIndex = 0; sparkIndex < 7; sparkIndex++)
            {
                float angle = sparkIndex * MathHelper.TwoPi / 7;
                var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (65 + burst.Combo * 20);
                int entity = Orb(burst.Position, 3, Palette[burst.Color], 120);
                particles.Add((entity, burst.Position, velocity, 0.5f, Palette[burst.Color]));
            }
        }
        game.Bursts.Clear();

        for (int particleIndex = particles.Count - 1; particleIndex >= 0; particleIndex--)
        {
            var particle = particles[particleIndex];
            particle.life -= elapsed;
            if (particle.life <= 0)
            {
                Scene.ECS.DestroyEntity(particle.entity);
                particles.RemoveAt(particleIndex);
                continue;
            }
            particle.position += particle.velocity * elapsed;
            Move(particle.entity, particle.position);
            Scene.ECS.GetComponent<Material>(particle.entity).Emissive = particle.color * (particle.life * 2);
            particles[particleIndex] = particle;
        }
    }

    public override void LateRender()
    {
        if (geometry.IsDebugging) return;
        DrawSurfaces();
        Renderer.BeginDraw(blend: BlendState.AlphaBlend, sampler: SamplerState.LinearClamp);
        var solid = Renderer.GetSolidTexture(Color.White);
        Vector2 boardTopLeft = Renderer.WorldToScreen(Vector2.Zero);
        Vector2 boardBottomRight = Renderer.WorldToScreen(new Vector2(1280, 720));
        var marginColor = new Color(7, 16, 15);
        if (boardTopLeft.Y > 0)
        {
            Renderer.DrawSprite(solid, new Rectangle(0, 0, Renderer.ScreenWidth, (int)MathF.Ceiling(boardTopLeft.Y)), marginColor);
            Renderer.DrawSprite(solid, new Rectangle(0, (int)boardBottomRight.Y, Renderer.ScreenWidth,
                Renderer.ScreenHeight - (int)boardBottomRight.Y), marginColor);
        }
        if (boardTopLeft.X > 0)
        {
            Renderer.DrawSprite(solid, new Rectangle(0, 0, (int)MathF.Ceiling(boardTopLeft.X), Renderer.ScreenHeight), marginColor);
            Renderer.DrawSprite(solid, new Rectangle((int)boardBottomRight.X, 0,
                Renderer.ScreenWidth - (int)boardBottomRight.X, Renderer.ScreenHeight), marginColor);
        }
        Renderer.DrawSprite(solid, new Rectangle(0, 0, Renderer.ScreenWidth, 96), new Color(2, 10, 10, 190));
        Renderer.DrawSprite(solid, new Rectangle(0, Renderer.ScreenHeight - 45, Renderer.ScreenWidth, 45), new Color(2, 10, 10, 190));
        float scale = Math.Clamp(Renderer.ScreenWidth / 1280f, 0.48f, 1.4f);
        float margin = 24 * scale;
        Text("RAYTRACING ZUMA", new Vector2(margin, 24 * scale), 21 * scale, new Color(224, 220, 187));
        Text($"{game.Level + 1} / {ZumaGame.LevelNames.Length}  {ZumaGame.LevelNames[game.Level]}", new Vector2(margin, 59 * scale), 11 * scale, new Color(119, 174, 146));
        string score = game.Score.ToString("D6");
        Text(score, new Vector2(Renderer.ScreenWidth - margin - Renderer.MeasureString("Temple", 24 * scale, score).X, 24 * scale), 24 * scale, new Color(244, 200, 91));
        string remaining = $"{game.Remaining + game.Marbles.Count} marbles left";
        Text(remaining, new Vector2(Renderer.ScreenWidth - margin - Renderer.MeasureString("Temple", 10 * scale, remaining).X, 61 * scale), 10 * scale, new Color(162, 187, 166));
        string controls = Renderer.ScreenWidth < 700 ? "Click: fire  Q: swap  P: pause" : "Mouse: aim / fire    Right click / Q: swap    P: pause    R: restart    F1: inspector";
        CenterText(controls, Renderer.ScreenHeight - 29 * scale, 9 * scale, new Color(151, 171, 154));
        if (game.Danger > 0.8f && game.State == PlayState.Playing)
            CenterText("The temple is closing!", 94 * scale, 11 * scale, Palette[0]);
        if (game.BestCombo > 1)
            Text($"Best chain x{game.BestCombo}", new Vector2(margin, 89 * scale), 10 * scale, Palette[2]);

        if (game.Paused || game.State != PlayState.Playing)
        {
            var background = Renderer.GetSolidTexture(Color.White);
            Renderer.DrawSprite(background, new Rectangle(0, 0, Renderer.ScreenWidth, Renderer.ScreenHeight), new Color(3, 13, 10, 220));
            string title = game.Paused ? "PAUSED" : game.State == PlayState.Won ? "TEMPLE CLEARED" : "THE CHAIN ESCAPED";
            CenterText(title, Renderer.ScreenHeight / 2f - 40 * scale, 24 * scale, new Color(242, 212, 136));
            CenterText(game.Paused ? "P / Space to resume" : game.State == PlayState.Won ? "Enter: next temple   R: replay" : "Enter / R: try again", Renderer.ScreenHeight / 2f + 15 * scale, 11 * scale, Color.White);
        }
        Renderer.EndDraw();
    }

    private void DrawSurfaces()
    {
        // The painted floor lets a little of HRC's indirect light through.
        Renderer.BeginDraw(blend: BlendState.AlphaBlend, sampler: SamplerState.LinearClamp, transform: Renderer.WorldTransform);
        Renderer.DrawSprite(artwork.Courtyard, new Rectangle(0, 0, 1280, 720), new Color(190, 200, 193, 230));
        Renderer.DrawSprite(Renderer.GetSolidTexture(Color.White), new Rectangle(0, 0, 1280, 720), new Color(0, 5, 4, 45));

        foreach (int entity in architecture.Where(entity => Scene.ECS.GetComponent<Transform2D>(entity).Layer < 20)
                     .OrderBy(entity => Scene.ECS.GetComponent<Transform2D>(entity).Layer))
            DrawSurface(entity);

        artwork.DrawProp(Renderer, 1, game.Path.At(0), 37);
        artwork.DrawProp(Renderer, 2, game.Path.At(game.Path.Length), 40);
        artwork.DrawProp(Renderer, 0, ZumaGame.Launcher, 82);
        artwork.DrawProp(Renderer, 3, new Vector2(330, 635), 27);
        artwork.DrawProp(Renderer, 3, new Vector2(950, 635), 27);
        float frogAngle = MathF.Atan2(facing.Y, facing.X) + MathHelper.PiOver2;
        artwork.DrawFrog(Renderer, ZumaGame.Launcher, frogAngle);
        artwork.DrawMarble(Renderer, ZumaGame.Launcher + facing * 50, ZumaGame.Radius, game.LoadedColor);
        artwork.DrawProp(Renderer, 0, ZumaGame.Launcher + new Vector2(0, 73), 16);
        artwork.DrawMarble(Renderer, ZumaGame.Launcher + new Vector2(0, 73), 10, game.NextColor);

        if (game.State == PlayState.Playing && !game.Paused && !Inspector.IsMouseOverUI())
        {
            Vector2 direction = aim - ZumaGame.Launcher;
            if (direction.LengthSquared() >= 1)
            {
                direction.Normalize();

                for (int dotIndex = 0; dotIndex < 7; dotIndex++)
                {
                    Vector2 position = ZumaGame.Launcher + direction * (65 + dotIndex * 16);
                    Renderer.DrawSprite(Renderer.GetCircleTexture(16),
                        new Rectangle((int)position.X - 2, (int)position.Y - 2, 4, 4),
                        Palette[game.LoadedColor] * (0.8f - dotIndex * 0.08f));
                }
            }
        }

        foreach (var (id, entity) in marbleEntities)
        {
            var sprite = marbleSprites[id];
            var position = Scene.ECS.GetComponent<Transform2D>(entity).Position;
            artwork.DrawMarble(Renderer, new Vector2(position.X, position.Y), ZumaGame.Radius,
                sprite.color, sprite.travel, sprite.rotation);
        }

        foreach (var particle in particles)
            DrawSurface(particle.entity);

        Renderer.EndDraw();
    }

    private void DrawSurface(int entity)
    {
        var position = Scene.ECS.GetComponent<Transform2D>(entity).Position;
        float radius = Scene.ECS.GetComponent<Circle2D>(entity).Radius;
        var material = Scene.ECS.GetComponent<Material>(entity);
        var texture = material.Texture ?? Renderer.GetCircleTexture(64);
        Renderer.DrawSprite(texture, new Rectangle((int)(position.X - radius), (int)(position.Y - radius),
            (int)(radius * 2), (int)(radius * 2)), material.Emissive);
    }

    private void Text(string value, Vector2 position, float size, Color color) => Renderer.DrawString("Temple", size, value, position, color);
    private void CenterText(string value, float y, float size, Color color) => Text(value,
        new Vector2((Renderer.ScreenWidth - Renderer.MeasureString("Temple", size, value).X) / 2, y), size, color);
}
