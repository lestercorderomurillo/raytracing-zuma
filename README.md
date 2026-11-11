<h1 align="center">Raytracing Zuma</h1>

<p align="center">
  <a href="https://github.com/lestercorderomurillo/raytracing-zuma/releases/tag/v0.4.0"><img src="https://img.shields.io/badge/release-v0.4.0-E73C00?style=flat-square" alt="Release v0.4.0"></a>
  <img src="https://img.shields.io/badge/Linux-Vulkan-8B5CF6?style=flat-square" alt="Linux with Vulkan">
</p>

[![Raytracing Zuma: jade frog and carved marbles on a spiral track](docs/screenshots/gameplay.png)](docs/screenshots/gameplay.png)

Aim the jade frog and fire into the moving chain. Match at least three marbles of the same color to clear them. Joined groups can trigger chain reactions and multiply your score. Clear every marble before the chain reaches the glowing temple exit.

- **Three temples:** spiral tracks with increasing speed and marble counts.
- **Four marble designs:** rolling animation, glowing stone channels, and match particles.
- **Chain reactions:** join matching groups, multiply your score, and swap your loaded marble.
- **Dynamic lighting:** a jade-and-bronze guardian, painted courtyard, and carved temple props lit by Radiant.

## Play

**[Download v0.4.0 for Linux x64](https://github.com/lestercorderomurillo/raytracing-zuma/releases/download/v0.4.0/raytracing-zuma-v0.4.0-linux-x64.tar.gz)** · [Release notes](https://github.com/lestercorderomurillo/raytracing-zuma/releases/tag/v0.4.0) · [Checksums](https://github.com/lestercorderomurillo/raytracing-zuma/releases/download/v0.4.0/SHA256SUMS)

```sh
tar -xzf raytracing-zuma-v0.4.0-linux-x64.tar.gz
cd raytracing-zuma
./raytracing-zuma
```

Requires Linux x64, Vulkan graphics drivers, and a graphical desktop. Keep the bundled runtime and assets with the executable.

## Controls

| Control | Action |
| --- | --- |
| Mouse | Aim |
| Left click / hold | Fire |
| Right click / Q | Swap loaded and next marble |
| P / Space | Pause / resume |
| R | Restart current temple and reset score |
| Enter after a round | Advance after winning, retry after losing |
| F1 | Radiant inspector, lighting and UDR quality settings |
| Esc | Exit |

## Editor

Press **F1** to open the Radiant inspector, lighting controls and UDR quality settings. Balanced is the default quality. The board and aiming stay proportional when you resize the window.

[Build and test instructions](BUILD.md) · [Art sources and prompts](Content/art/README.md).

Built with [Radiant](https://github.com/lestercorderomurillo/radiant-game-engine), C# / .NET 8, and MonoGame DesktopVK. Lighting uses Radiant's 2D HRC global illumination and UDR4 upscaling. The artwork was generated from text descriptions without original-game assets or visual references.

Press **F1**, then choose **View → Workspace Mode → Panels** for docked tools and a framed game view. Drag tabs to reorder or move them between docks; choose **Windows** to restore floating tools.
