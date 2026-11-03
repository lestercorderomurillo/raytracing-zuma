using Microsoft.Xna.Framework;

namespace RaytracingZuma;

public sealed class MarblePath
{
    private readonly Vector2[] points;
    private readonly float[] distances;
    public float Length => distances[^1];
    public IReadOnlyList<Vector2> Points => points;

    public MarblePath(IEnumerable<Vector2> samples)
    {
        points = samples.ToArray();
        if (points.Length < 2) throw new ArgumentException("A path needs at least two points.");
        distances = new float[points.Length];

        for (int pointIndex = 1; pointIndex < points.Length; pointIndex++)
            distances[pointIndex] = distances[pointIndex - 1] + Vector2.Distance(points[pointIndex - 1], points[pointIndex]);

        if (Length <= 0) throw new ArgumentException("A path needs a positive length.");
    }

    public Vector2 At(float distance)
    {
        distance = Math.Clamp(distance, 0, Length);
        int index = Array.BinarySearch(distances, distance);
        if (index >= 0) return points[index];
        index = ~index;
        float fraction = (distance - distances[index - 1]) / (distances[index] - distances[index - 1]);
        return Vector2.Lerp(points[index - 1], points[index], fraction);
    }

    public Vector2 Tangent(float distance) => Vector2.Normalize(At(Math.Min(Length, distance + 2)) - At(Math.Max(0, distance - 2)));

    public static MarblePath ForLevel(int level)
    {
        // A narrowing temple spiral leaves the launcher clear in its central chamber.
        var samples = new List<Vector2>();
        float turns = level switch { 0 => 1.18f, 1 => 1.38f, _ => 1.58f };

        for (int sampleIndex = 0; sampleIndex <= 600; sampleIndex++)
        {
            float progress = sampleIndex / 600f;
            float angle = MathHelper.Pi + progress * MathHelper.TwoPi * turns;
            float radiusX = MathHelper.Lerp(510, 180, progress);
            float radiusY = MathHelper.Lerp(245, 115, progress);
            float ripple = level == 2 ? 10 * MathF.Sin(progress * MathHelper.TwoPi * 5) : 0;
            samples.Add(new Vector2(640 + (radiusX + ripple) * MathF.Cos(angle), 393 + (radiusY + ripple) * MathF.Sin(angle)));
        }

        return new MarblePath(samples);
    }
}
