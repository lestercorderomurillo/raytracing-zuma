namespace RaytracingZuma;

public static class MarbleAnimation
{
    public const int Frames = 8;

    public static int FrameAt(float travel)
    {
        float cycle = travel / (MathF.Tau * ZumaGame.Radius);
        float phase = cycle - MathF.Floor(cycle);
        return Math.Min(Frames - 1, (int)(phase * Frames));
    }
}
