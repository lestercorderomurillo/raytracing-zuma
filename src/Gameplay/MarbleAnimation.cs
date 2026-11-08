namespace RaytracingZuma;

/// <summary>Maps signed travel distance to looping marble rotation frames.</summary>
public static class MarbleAnimation
{
    public const int Frames = 8;

    /// <summary>Returns the rolling sprite frame for signed travel around the marble's circumference.</summary>
    public static int FrameAt(float travel)
    {
        float cycle = travel / (MathF.Tau * ZumaGame.Radius);
        float phase = cycle - MathF.Floor(cycle);
        return Math.Min(Frames - 1, (int)(phase * Frames));
    }
}
