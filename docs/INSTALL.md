# Install guide (Linux and Windows)

Everything needed to build and run the game, as blocks you can paste into the VS Code terminal
(**Terminal > New Terminal**). Do the section for your system, then "Check it worked" and "Build and run".

What you are installing:

| Needed for | What |
|---|---|
| Building and running the game | .NET SDK **8** and **10** (the game targets 8; KNI's content builder runs on 10), git |
| Building art and sound (Linux only) | FreeImage (PNGs) and FFmpeg (WAVs) |
| The art tools in `tools/` | Python 3 and Pillow |
| Editing | VS Code with the **C# Dev Kit** extension (optional but nice) |

You do **not** need the AI art tools (ComfyUI, Stable Diffusion) or the story pipeline to build or play the game.

---

## Linux

Pick your distribution. Each block is one paste.

### Fedora / Nobara

```bash
sudo dnf install -y git dotnet-sdk-8.0 dotnet-sdk-10.0 freeimage python3 python3-pip python3-pillow
sudo dnf install -y ffmpeg-free || sudo dnf install -y ffmpeg
```

(Nobara and RPM Fusion users may already have full `ffmpeg`; the second line then does nothing.)

### Ubuntu / Debian

```bash
sudo apt-get update
sudo apt-get install -y git curl libfreeimage3 ffmpeg python3 python3-pip python3-pil
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0
bash /tmp/dotnet-install.sh --channel 10.0
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
```

(If your Ubuntu release already ships `dotnet-sdk-8.0` and `dotnet-sdk-10.0` in its repositories you can `apt-get install` those instead of using the script.)

### Linux, both: make FreeImage findable (one time)

.NET looks for the library under the name `libFreeImage.so`, but Linux installs it as `libfreeimage.so.3`. This links one to the other and tells the shell where to look:

```bash
FI=$(ldconfig -p | awk '/libfreeimage\.so\.3 /{print $NF; exit}')
mkdir -p ~/.local/lib/freeimage
ln -sf "$FI" ~/.local/lib/freeimage/libFreeImage.so
echo 'export LD_LIBRARY_PATH=$HOME/.local/lib/freeimage:$LD_LIBRARY_PATH' >> ~/.bashrc
source ~/.bashrc
```

Close the VS Code terminal and open a new one afterwards, so it picks up the changes.

---

## Windows

Open the VS Code terminal in **PowerShell** (the default). `winget` comes with Windows 10 and 11.

```powershell
winget install --id Git.Git -e
winget install --id Microsoft.DotNet.SDK.8 -e
winget install --id Microsoft.DotNet.SDK.10 -e
winget install --id Python.Python.3.12 -e
```

**Close VS Code completely and reopen it** so the new programs are on the PATH. Then, in a new terminal:

```powershell
py -m pip install pillow
```

Windows does not need FreeImage. If the build later complains about importing a `.wav` file, install FFmpeg and open a new terminal:

```powershell
winget install --id Gyan.FFmpeg -e
```

On Windows use `py` where this repo's docs say `python3`, for example `py tools/sync_content.py`.

---

## Check it worked

The same on both systems (Windows: use `py` instead of `python3`):

```bash
git --version
dotnet --list-sdks
python3 --version
python3 -c "import PIL; print('Pillow', PIL.__version__)"
```

`dotnet --list-sdks` must list a version starting with `8.` **and** one starting with `10.`.

## Build and run

```bash
git clone git@github.com:wpowell-swe/game-off-2026.git
cd game-off-2026
dotnet build ShiftJam.sln
dotnet run --project ShiftJam.SDL2.GL
```

If you cloned already, just `cd` into the folder. The first build downloads packages and builds all the art and sound, so it takes a minute or two.

To build the web version instead, and serve it:

```bash
dotnet publish ShiftJam.Blazor.GL -c Release -o publish/web
python3 -m http.server 8766 --directory publish/web/wwwroot
```

Then open http://localhost:8766 (it does not work opened straight from disk).

## Optional: the story pipeline

Only if you will work on the writing pipeline in `pipeline/`. It needs Python 3.11 or newer and an Anthropic API key:

```bash
python3 -m pip install -e "pipeline[dev]"
```

## If something goes wrong

| Symptom | Fix |
|---|---|
| `dotnet: command not found` | Open a new terminal (Windows: restart VS Code). On Ubuntu/Debian check the two `export` lines were added to `~/.bashrc`. |
| `The SDK 'Microsoft.NET.Sdk' ... .NET 10` or content-build errors | `dotnet --list-sdks` is missing 10.x. Install it from the section above. |
| Build fails on a PNG, e.g. `TextureImporter` or `libFreeImage` (Linux) | Redo "make FreeImage findable" and open a new terminal. |
| Build fails on a `.wav`, e.g. "Failed to open file" | FFmpeg is missing. Install it (see your section above). |
| `python3` not found (Windows) | Use `py` instead. |
