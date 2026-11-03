using Microsoft.Xna.Framework;
using RaytracingZuma;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

for (int level = 0; level < 3; level++)
{
    var path = MarblePath.ForLevel(level);
    Check(path.Length > 2000, "Level path is too short.");
    Check(path.At(-100) == path.Points[0] && path.At(path.Length + 100) == path.Points[^1], "Path clamping failed.");

    for (float distance = 0; distance < path.Length; distance += 5)
    {
        Vector2 point = path.At(distance);
        Check(point.X > 70 && point.X < 1210 && point.Y > 105 && point.Y < 670, "Path leaves the board.");
        Check(Vector2.Distance(point, ZumaGame.Launcher) > 85, "Path intersects launcher.");
        Check(Math.Abs(path.Tangent(distance).Length() - 1) < 0.001f, "Invalid path tangent.");
    }
}
Console.WriteLine("PASS: all level paths, bounds, sampling, launcher clearance.");

var game = new ZumaGame();
game.SetChain([0, 0], 100);
game.Insert(1, 0);
Check(game.Marbles.Count == 0 && game.Score == 300, "Match-three removal or score failed.");
game.Update(0.02f);
Check(game.State == PlayState.Won, "Empty final chain did not win.");

game.StartLevel(0);
game.SetChain([1, 1, 0, 0, 1], 150);
game.Insert(3, 0);
Check(game.Marbles.Count == 0 && game.Score == 900 && game.BestCombo == 2, "Chain reaction failed.");
Check(game.Bursts.Count == 6, "Match bursts missing.");
Console.WriteLine("PASS: matching, chain retraction, combo multiplier, victory.");

game.StartLevel(0);
game.SetChain([0, 1, 2], 0);
game.Insert(0, 3);
for (int index = 1; index < game.Marbles.Count; index++)
    Check(game.Marbles[index].Distance - game.Marbles[index - 1].Distance >= ZumaGame.Spacing - 0.01f, "Inserted chain overlaps.");
game.SetChain([0], game.Path.Length - 0.01f);
game.Update(0.02f);
Check(game.State == PlayState.Lost, "Chain reaching exit did not lose.");
Check(!game.Fire(Vector2.Zero), "Can fire after losing.");
Console.WriteLine("PASS: insertion spacing, exit loss, terminal input.");

game.StartLevel(1);
int loaded = game.LoadedColor;
int next = game.NextColor;
game.Swap();
Check(game.LoadedColor == next && game.NextColor == loaded, "Ammo swap failed.");
game.Paused = true;
float before = game.Marbles[^1].Distance;
game.Update(1);
Check(game.Marbles[^1].Distance == before && !game.Fire(Vector2.Zero), "Pause moved gameplay.");
game.Paused = false;
Check(game.Fire(Vector2.Zero) && !game.Fire(Vector2.Zero), "Shot cooldown failed.");
Check(game.Shots.Count == 1, "Projectile not created.");
game.StartLevel(2);
Check(game.State == PlayState.Playing && game.Shots.Count == 0 && game.Score == 0 && !game.Paused, "Restart failed.");
Console.WriteLine("PASS: ammo swap, pause, firing, cooldown, restart.");

// Fire at a known stationary cross-section and ensure a swept collision inserts a marble.
game.SetChain([0, 1], 800);
Vector2 target = game.Path.At(game.Marbles[0].Distance);
Check(game.Fire(target), "Test projectile not fired.");
for (int frame = 0; frame < 120 && game.Shots.Count > 0; frame++) game.Update(1f / 120);
Check(game.Marbles.Count == 3 && game.Shots.Count == 0, "Projectile missed the chain or failed to insert.");
Console.WriteLine("PASS: projectile travel and swept chain collision.");

var first = new ZumaGame(42);
var second = new ZumaGame(42);
for (int frame = 0; frame < 1200; frame++)
{
    first.Update(1f / 120);
    if (frame % 2 == 0) second.Update(1f / 60);
}
Check(first.Remaining == second.Remaining && first.Marbles.Count == second.Marbles.Count, "Simulation depends on frame rate.");
Check(Math.Abs(first.Marbles[^1].Distance - second.Marbles[^1].Distance) < 0.001f, "Fixed-step positions diverged.");
Console.WriteLine("PASS: deterministic fixed timestep across frame rates.");

float circumference = MathF.Tau * ZumaGame.Radius;
for (int animationFrame = 0; animationFrame < MarbleAnimation.Frames; animationFrame++)
{
    float travel = circumference * (animationFrame + 0.25f) / MarbleAnimation.Frames;
    Check(MarbleAnimation.FrameAt(travel) == animationFrame, "Rolling frame does not follow distance.");
    Check(MarbleAnimation.FrameAt(travel + circumference) == animationFrame, "Rolling animation does not loop.");
}
Check(MarbleAnimation.FrameAt(-circumference / 16) == 7, "Rolling cannot reverse through the loop.");
Console.WriteLine("PASS: eight rolling frames, full revolutions, and reverse travel.");

if (args.Contains("--graphics") || args.Contains("--capture"))
{
    using var window = new ZumaWindow(args.Contains("--capture"));
    window.Run();
}
