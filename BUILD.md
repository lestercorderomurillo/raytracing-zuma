# Build and verification

Use a .NET 8 SDK or newer and the matching Radiant development checkout with `com.radiant.engine.graphics` texture, render-target and effect contracts (after v0.3.0). Zuma v0.2.0 remains paired with Radiant v0.3.0; Zuma v0.1.0 with Radiant v0.2.0. Released engine v0.3.0 cannot compile the new resource aliases. Keep `raytracing-zuma` and `radiant-game-engine` as sibling directories, matching the Pac-Man checkout layout. The engine's `eng/Radiant.Desktop.props` pins MonoGame DesktopVK 3.8.6 and restores its content tool. The game consumes the engine's compiled shaders and public Scene/ECS/Renderer APIs, including the current `Transform2D` component. Its own MGCB pipeline compiles the PNG artwork with premultiplied alpha; the atlas manifest is copied alongside the compiled textures.

```sh
dotnet build raytracing-zuma.csproj -c Release
dotnet run --project raytracing-zuma.csproj -c Release
```

For a different engine location, append `-p:RadiantRoot=/absolute/path/to/radiant-game-engine` to build/run commands. Linux is the verified target; other platforms need runtime verification.

## Checks

Deterministic gameplay tests cover all three path bounds, launcher clearance, match removal, combo collapse, insertion spacing, projectile collision, pause, restart, ammo swapping, terminal states, fixed timesteps, and distance-driven rolling frame loops (including reverse movement):

```sh
dotnet run --project tests/Zuma.Tests -c Release
```

In restricted environments where .NET build-server sockets are unavailable, build sequentially without the compiler server:

```sh
dotnet build tests/Zuma.Tests/Zuma.Tests.csproj -c Release -m:1 -nr:false -p:UseSharedCompilation=false
dotnet tests/Zuma.Tests/bin/Release/net8.0/Zuma.Tests.dll
```

Vulkan checks must run on an isolated display, never on the user's desktop. They exercise three temples, square/portrait/wide resizing, the three UDR quality levels, nonempty light emission, and world/screen aim mapping. They save unedited game captures under `artifacts/smoke`:

```sh
xvfb-run -a -s '-screen 0 1440x1000x24 -extension MIT-SHM' dotnet tests/Zuma.Tests/bin/Release/net8.0/Zuma.Tests.dll --graphics
```

Use `--capture` instead of `--graphics` to capture only the first temple at 1280×720.

The graphics check requires Xvfb and an accessible Vulkan device/driver. If the hardware driver cannot present to Xvfb, select an installed software Vulkan ICD with `VK_ICD_FILENAMES=/path/to/vk_swiftshader_icd.json`. Disabling MIT-SHM avoids shared-memory access errors in isolated environments. Software-driver checks validate rendering correctness, not hardware performance.

## Publish locally

```sh
dotnet publish raytracing-zuma.csproj -c Release -r linux-x64 --self-contained true -o artifacts/linux-x64
./artifacts/linux-x64/raytracing-zuma
```

Keep the complete output together, including runtime, native libraries, fonts, and engine shaders. No hosting service or web framework is involved.

## Shared camera controller

With Radiant v0.2.1, open F1 > ECS > Open Camera Controls, or use the Camera controls button in the game settings. The single window searches camera IDs/types and selects instances independently of the entity inspector. This game uses the fitted 2D World view: enable Navigate camera and hold RMB with WASD to pan, or use Zoom in / Zoom out. Adjust movement speed for world units. Closing the controller stops navigation. Changing levels restores the game framing where the scene calls FitWorld.

## Radiant input and value types

Gameplay uses `Time.DeltaSeconds`, `Controls.Mouse` and `Controls.WasPressed`.
The runtime owns keyboard/mouse edge detection, so held pause/swap buttons do not
repeat. `src/RadiantTypes.cs` selects public .NET/Radiant types and is linked into
the tests without depending on the engine's private compilation aliases.

After building, run the input regression in addition to the graphics checks:

```sh
xvfb-run -a -s '-screen 0 1600x1200x24 -extension MIT-SHM' dotnet tests/Zuma.Tests/bin/Release/net8.0/Zuma.Tests.dll --input
```

It checks pause edges, held keys, keyboard/right-mouse ammo swaps and restart through
the actual presentation system using an injected Radiant input source.

## Radiant graphics resources

Gameplay uses Radiant textures and render targets through `src/RadiantTypes.cs`;
resource creation stays in `Renderer`. Region readback uses Radiant rectangles.
The engine owns native conversion and cached resources. See the engine's
[resource contracts](../radiant-game-engine/docs/GraphicsResources.md) for ownership
and effect lifetime rules. Run `python ../radiant-game-engine/eng/check-consumers.py`
to build the engine and both games together before the isolated graphical checks.

## Renderer and window contracts

Current sources also require Radiant's `IRenderer` and `IWindow` migration. Scene/ECS
renderers and game drawing helpers consume that interface; native MonoGame window
subclasses remain confined to graphical test harnesses. Fluent calls still work, and
window closure is `Renderer.Window.RequestClose()`. See the engine's
[renderer contracts](../radiant-game-engine/docs/RendererContracts.md).

The engine now delegates fitting, world/screen mapping and scaled metrics to its
backend-independent `RenderView`. Game calls still use `IRenderer`. The `--graphics`
check additionally compares sprite transforms with input coordinates and verifies
scaled metrics after resizing; asset caches are tested through Radiant's injected
`IGraphicsAssetLoader`.

Fonts now use Radiant `FontFace`/`IFontLoader` contracts, and shape accumulation
uses the shared CPU `ShapeCollection` before backend submission. Gameplay keeps
using named `DrawString` and `IRenderer` shape commands. The graphics checks verify
the game's HUD font metrics and repeated font lookup identity as well as rendering.

Radiant now routes mesh and colored-triangle submission through `IGeometryBackend`.
Game code continues to use `IRenderer.DrawMesh` / `DrawTriangles`; GPU buffer ownership
and upload caching stay in the engine backend. Use the matching sibling engine checkout.
