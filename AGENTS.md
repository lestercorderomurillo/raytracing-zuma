# Agent instructions

- Keep Zuma gameplay, scenes, and tests here. Shared engine code and shaders belong in the sibling `radiant-game-engine` repository.
- Match the Pac-Man stack: .NET 8, Radiant public APIs, MonoGame 3.8.6 DesktopVK, HRCGI and UDR4. Keep Balanced as the default quality.
- Use Renderer APIs for textures and drawing. Do not allocate GPU resources directly in game systems.
- Run graphical checks only on an isolated display. Do not open windows or send input on the user's desktop.
- Keep deterministic gameplay independent of rendering and test meaningful behavior in `tests/Zuma.Tests`.
- Preserve proportional world fitting and use the same world/screen mapping for aiming.
- Build and validation instructions live in BUILD.md. Keep README focused on the game.
