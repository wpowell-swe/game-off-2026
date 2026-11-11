# ShiftJam

Our entry for [GitHub Game Off 2026](https://itch.io/jam/game-off-2026). Theme: **SHIFT**.

- **Play the latest build:** https://wpowell-swe.github.io/game-off-2026/ (rebuilt on every push to `main`)
- **itch.io page:** _coming soon_

Built in C# with [KNI](https://github.com/kniEngine/kni), a MonoGame/XNA-compatible framework that can also target the web.

## Setup

Full install steps for Linux and Windows, as paste-into-the-terminal blocks, are in [docs/INSTALL.md](docs/INSTALL.md). In short:

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). The projects target .NET 8, but KNI's content builder needs the .NET 10 runtime.
2. (Optional) Install the KNI templates: `dotnet new install nkast.Kni.Templates::4.3.9001`
3. Clone and build:

   ```bash
   git clone git@github.com:wpowell-swe/game-off-2026.git
   cd game-off-2026
   dotnet build ShiftJam.sln
   ```

## Running

| What | Command |
|---|---|
| Desktop (fastest to iterate) | `dotnet run --project ShiftJam.SDL2.GL` |
| Web build | `dotnet publish ShiftJam.Blazor.GL -c Release -o publish/web` |
| Serve the web build locally | `python3 -m http.server 8766 --directory publish/web/wwwroot`, then open http://localhost:8766 |

Opening `index.html` straight from disk won't work; the web build has to be served over HTTP.

## Changing the game without compiling

Balance numbers, levels, on-screen text, the stage order and sprite sizes are plain files in `ShiftJamContent/Data/`. Edit them and press F11 in the game to reload. See [docs/DATA_GUIDE.md](docs/DATA_GUIDE.md).

## Project layout

| Folder | Contents |
|---|---|
| `ShiftJam.Game/` | All game code, shared by every platform. Start at `ShiftJamGame.cs`. |
| `ShiftJamContent/` | Art, audio and data (`Data/`: numbers, levels, text), listed in `ShiftJamContent.mgcb`. |
| `ShiftJam.SDL2.GL/` | Desktop launcher (Windows/Linux/macOS). |
| `ShiftJam.Blazor.GL/` | Web launcher (WebAssembly + WebGL). |

## Conventions

- Code style is defined in `.editorconfig` (standard .NET conventions). Run `dotnet format ShiftJam.sln` before committing.
- Keep commits small and push often. Use short-lived branches for anything that touches shared files.
- Add every third-party asset to [CREDITS.md](CREDITS.md) when you add it.
