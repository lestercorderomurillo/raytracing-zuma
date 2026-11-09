
namespace RaytracingZuma;

/// <summary>Running, won, or lost state of a Zuma level.</summary>
public enum PlayState { Playing, Won, Lost }

/// <summary>A colored chain element positioned by distance along the marble path.</summary>
public sealed class Marble
{
    public int Id { get; init; }
    public int Color { get; init; }
    public float Distance { get; set; }
}

/// <summary>A launched colored marble with world position and velocity.</summary>
public sealed class Shot
{
    public int Id { get; init; }
    public int Color { get; init; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; init; }
}

/// <summary>Match event containing the world position, color, and combo multiplier.</summary>
public readonly record struct Burst(Vector2 Position, int Color, int Combo);

/// <summary>Deterministic marble-chain simulation independent of rendering and input devices.</summary>
public sealed class ZumaGame
{
    public const float Radius = 16;
    public const float Spacing = 33;
    public static readonly Vector2 Launcher = new(640, 393);
    public static readonly string[] LevelNames = ["Jade sanctuary", "Serpent's coil", "Sunken temple"];
    private readonly Random random;
    private int nextId;
    private float accumulator;
    private float cooldown;
    private readonly List<Marble> marbles = [];
    private readonly List<Shot> shots = [];
    public IReadOnlyList<Marble> Marbles => marbles;
    public IReadOnlyList<Shot> Shots => shots;
    public List<Burst> Bursts { get; } = [];
    public MarblePath Path { get; private set; }
    public PlayState State { get; private set; }
    public bool Paused { get; set; }
    public int Level { get; private set; }
    public int Score { get; private set; }
    public int Remaining { get; private set; }
    public int LoadedColor { get; private set; }
    public int NextColor { get; private set; }
    public int BestCombo { get; private set; }
    public float Danger => marbles.Count == 0 ? 0 : marbles[^1].Distance / Path.Length;

    /// <summary>Creates a seeded simulation and starts the first temple.</summary>
    public ZumaGame(int seed = 1459)
    {
        random = new Random(seed);
        StartLevel(0);
    }

    /// <summary>Resets the selected temple's chain and ammunition, optionally retaining the score.</summary>
    public void StartLevel(int level, bool keepScore = false)
    {
        Level = Math.Clamp(level, 0, LevelNames.Length - 1);
        Path = MarblePath.ForLevel(Level);
        if (!keepScore) Score = 0;
        State = PlayState.Playing;
        Paused = false;
        BestCombo = 0;
        cooldown = accumulator = 0;
        Remaining = 34 + Level * 10;
        marbles.Clear();
        shots.Clear();
        Bursts.Clear();

        for (int marbleIndex = 0; marbleIndex < 24; marbleIndex++)
            marbles.Add(new Marble { Id = nextId++, Color = marbleIndex / 2 % 4, Distance = marbleIndex * Spacing });

        LoadedColor = PickColor();
        NextColor = PickColor();
    }

    /// <summary>Chooses a color still present in the chain, falling back to any color when empty.</summary>
    private int PickColor()
    {
        if (marbles.Count == 0) return random.Next(4);
        return marbles[random.Next(marbles.Count)].Color;
    }

    /// <summary>Exchanges loaded and next ammunition while play is active and unpaused.</summary>
    public void Swap()
    {
        if (!Paused && State == PlayState.Playing) (LoadedColor, NextColor) = (NextColor, LoadedColor);
    }

    /// <summary>Launches a shot toward the target if play state, cooldown, and direction allow it.</summary>
    public bool Fire(Vector2 target)
    {
        Vector2 direction = target - Launcher;
        if (Paused || State != PlayState.Playing || cooldown > 0 || direction.LengthSquared() < 1) return false;
        direction = Vector2.Normalize(direction);
        shots.Add(new Shot { Id = nextId++, Color = LoadedColor, Position = Launcher + direction * 42, Velocity = direction * 1050 });
        LoadedColor = NextColor;
        NextColor = PickColor();
        cooldown = 0.2f;
        return true;
    }

    /// <summary>Accumulates elapsed time and advances active play in fixed 120 Hz steps.</summary>
    public void Update(float elapsed)
    {
        if (Paused || State != PlayState.Playing) return;
        accumulator += Math.Clamp(elapsed, 0, 0.25f);

        while (accumulator >= 1f / 120 && State == PlayState.Playing)
        {
            Step(1f / 120);
            accumulator -= 1f / 120;
        }
    }

    /// <summary>Advances the chain and shots, resolves collisions, and updates win or loss state.</summary>
    private void Step(float elapsed)
    {
        cooldown = Math.Max(0, cooldown - elapsed);
        float advance = (27 + Level * 8) * elapsed;

        foreach (var marble in marbles)
            marble.Distance += advance;

        if (Remaining > 0 && (marbles.Count == 0 || marbles[0].Distance >= Spacing))
        {
            marbles.Insert(0, new Marble { Id = nextId++, Color = random.Next(4), Distance = 0 });
            Remaining--;
        }

        for (int shotIndex = shots.Count - 1; shotIndex >= 0; shotIndex--)
        {
            var shot = shots[shotIndex];
            Vector2 travel = shot.Velocity * elapsed;
            int hitIndex = -1;
            float hitTime = float.MaxValue;

            for (int marbleIndex = 0; marbleIndex < marbles.Count; marbleIndex++)
            {
                Vector2 offset = shot.Position - Path.At(marbles[marbleIndex].Distance);
                float time = Intersect(offset, travel, Radius * 2);
                if (time < hitTime)
                {
                    hitTime = time;
                    hitIndex = marbleIndex;
                }
            }

            if (hitIndex >= 0)
            {
                shot.Position += travel * hitTime;
                var hit = marbles[hitIndex];
                bool ahead = Vector2.Dot(shot.Position - Path.At(hit.Distance), Path.Tangent(hit.Distance)) >= 0;
                Insert(hitIndex + (ahead ? 1 : 0), shot.Color);
                shots.RemoveAt(shotIndex);
            }
            else
            {
                shot.Position += travel;
                if (shot.Position.X < -40 || shot.Position.X > 1320 || shot.Position.Y < 40 || shot.Position.Y > 760)
                    shots.RemoveAt(shotIndex);
            }
        }

        if (marbles.Count > 0 && marbles[^1].Distance >= Path.Length) State = PlayState.Lost;
        else if (Remaining == 0 && marbles.Count == 0) State = PlayState.Won;
        if (State != PlayState.Playing) shots.Clear();
    }

    /// <summary>Returns the earliest swept-circle hit fraction, or float.MaxValue when no hit occurs.</summary>
    private static float Intersect(Vector2 offset, Vector2 travel, float radius)
    {
        float squared = offset.LengthSquared() - radius * radius;
        if (squared <= 0) return 0;
        float projection = Vector2.Dot(offset, travel);
        float discriminant = projection * projection - travel.LengthSquared() * squared;
        if (discriminant < 0) return float.MaxValue;
        float time = (-projection - MathF.Sqrt(discriminant)) / travel.LengthSquared();
        return time is >= 0 and <= 1 ? time : float.MaxValue;
    }

    /// <summary>Inserts a shot into the chain, restores spacing, and resolves resulting matches.</summary>
    internal void Insert(int index, int color)
    {
        float distance = index == 0 ? Math.Max(0, marbles[0].Distance - Spacing) : marbles[index - 1].Distance + Spacing;
        marbles.Insert(index, new Marble { Id = nextId++, Color = color, Distance = distance });

        for (int marbleIndex = index + 1; marbleIndex < marbles.Count; marbleIndex++)
            marbles[marbleIndex].Distance = Math.Max(marbles[marbleIndex].Distance, marbles[marbleIndex - 1].Distance + Spacing);

        Resolve(index, 1);
        // Do not strand the player with ammunition for an extinct color after the final spawn.
        if (Remaining == 0 && marbles.Count > 0)
        {
            if (!marbles.Any(marble => marble.Color == LoadedColor)) LoadedColor = PickColor();
            if (!marbles.Any(marble => marble.Color == NextColor)) NextColor = PickColor();
        }
    }

    /// <summary>Removes matching runs and recursively scores combos formed by closing the gap.</summary>
    private void Resolve(int index, int combo)
    {
        if (index < 0 || index >= marbles.Count) return;
        int first = index;
        int last = index;
        int color = marbles[index].Color;
        while (first > 0 && marbles[first - 1].Color == color) first--;
        while (last + 1 < marbles.Count && marbles[last + 1].Color == color) last++;
        int count = last - first + 1;
        if (count < 3) return;

        for (int marbleIndex = first; marbleIndex <= last; marbleIndex++)
            Bursts.Add(new Burst(Path.At(marbles[marbleIndex].Distance), color, combo));

        marbles.RemoveRange(first, count);
        Score += count * 100 * combo;
        BestCombo = Math.Max(BestCombo, combo);

        // Retract the leading segment into the gap, then check the newly joined colors.
        for (int marbleIndex = first; marbleIndex < marbles.Count; marbleIndex++)
            marbles[marbleIndex].Distance = Math.Max(0, marbles[marbleIndex].Distance - count * Spacing);

        if (first > 0 && first < marbles.Count && marbles[first - 1].Color == marbles[first].Color)
            Resolve(first, combo + 1);
    }

    /// <summary>Replaces the marble chain with a deterministic arrangement for gameplay checks.</summary>
    internal void SetChain(int[] colors, float start = 0, int remaining = 0)
    {
        marbles.Clear();
        shots.Clear();
        Bursts.Clear();
        Remaining = remaining;

        for (int marbleIndex = 0; marbleIndex < colors.Length; marbleIndex++)
            marbles.Add(new Marble { Id = nextId++, Color = colors[marbleIndex], Distance = start + marbleIndex * Spacing });
    }
}
