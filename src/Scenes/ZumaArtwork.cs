using System.Text.Json;
using com.radiant.engine.core;
using Microsoft.Xna.Framework.Graphics;

namespace RaytracingZuma;

/// <summary>Loads the sprite atlas and draws marble, temple, and launcher artwork.</summary>
public sealed class ZumaArtwork
{
    public Texture2D Frog { get; }
    public Texture2D Marbles { get; }
    public Texture2D Courtyard { get; }
    public Texture2D Props { get; }
    private readonly Rectangle[] marbleFrames;
    private readonly Rectangle[] propFrames;

    /// <summary>Loads the atlas textures and frame metadata through the renderer.</summary>
    public ZumaArtwork(Renderer renderer)
    {
        Frog = renderer.GetTexture("art/frog-guardian");
        Marbles = renderer.GetTexture("art/marbles-roll");
        Courtyard = renderer.GetTexture("art/temple-courtyard");
        Props = renderer.GetTexture("art/temple-props");
        string manifest = Path.Combine(AppContext.BaseDirectory, "Content", "art", "atlas.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        marbleFrames = ReadFrames(document.RootElement.GetProperty("marbleFrames"), Marbles);
        propFrames = ReadFrames(document.RootElement.GetProperty("props"), Props);
        if (marbleFrames.Length != 4 * MarbleAnimation.Frames || propFrames.Length != 4)
            throw new InvalidDataException("The artwork atlas must contain 32 marble frames and four temple props.");
    }

    /// <summary>Parses atlas frame rectangles and validates them against the texture bounds.</summary>
    private static Rectangle[] ReadFrames(JsonElement frames, Texture2D texture)
    {
        return frames.EnumerateArray().Select(frame =>
        {
            var rectangle = new Rectangle(frame.GetProperty("x").GetInt32(), frame.GetProperty("y").GetInt32(),
                frame.GetProperty("width").GetInt32(), frame.GetProperty("height").GetInt32());
            if (rectangle.Width <= 0 || rectangle.Height <= 0 || !new Rectangle(0, 0, texture.Width, texture.Height).Contains(rectangle))
                throw new InvalidDataException("An artwork frame falls outside its texture.");
            return rectangle;
        }).ToArray();
    }

    /// <summary>Draws the marble's atlas frame selected by color and traveled distance.</summary>
    public void DrawMarble(Renderer renderer, Vector2 position, float radius, int color, float travel = 0, float rotation = 0)
    {
        var source = marbleFrames[color * MarbleAnimation.Frames + MarbleAnimation.FrameAt(travel)];
        var destination = new Rectangle((int)position.X, (int)position.Y, (int)(radius * 2), (int)(radius * 2));
        renderer.DrawSprite(Marbles, destination, source, Color.White, rotation,
            new Vector2(source.Width / 2f, source.Height / 2f));
    }

    /// <summary>Draws the selected temple prop at the requested position and size.</summary>
    public void DrawProp(Renderer renderer, int index, Vector2 position, float radius)
    {
        renderer.DrawSprite(Props, new Rectangle((int)(position.X - radius), (int)(position.Y - radius),
            (int)(radius * 2), (int)(radius * 2)), propFrames[index], Color.White);
    }

    /// <summary>Draws the launcher artwork at its current aim angle.</summary>
    public void DrawFrog(Renderer renderer, Vector2 position, float rotation)
    {
        renderer.DrawSprite(Frog, new Rectangle((int)position.X, (int)position.Y, 160, 160), null, Color.White,
            rotation, new Vector2(Frog.Width / 2f, Frog.Height / 2f));
    }
}
