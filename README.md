# FrameOn VideoUtility

<p align="center">
  <img src="FrameonVideoUtility/FrameonVideoUtility/Assets/frameon_icon_master_cleaned_1024.png" alt="FrameOn VideoUtility" width="180">
</p>

FrameOn is a desktop media downloading and conversion application for Windows and macOS. It provides one interface around yt-dlp, FFmpeg, FFprobe and Deno.

FrameOn can download online media, process prepared lists of video links, convert local audio and video files, and optimize videos for sharing or storage.

## Download 📦

Download the latest signed release from the [official FrameOn website](https://frameon.choatehome.com/#download).

Older releases are retained primarily for troubleshooting and maintenance. Use the newest stable release unless you specifically need an earlier version.

### Windows

1. Download the FrameOn file ending in `.msix`.
2. Open the downloaded package and select `Install`.
3. Launch FrameOn from the Start menu.
4. On first launch, choose whether FrameOn should create a desktop shortcut.

FrameOn installs as a Windows application. It can be updated or removed later from Windows Settings.

### macOS

1. Download the FrameOn file ending in `.pkg`.
2. Open the package and follow the installer.
3. Launch FrameOn from the Applications folder.

## Main Workspaces

FrameOn separates its main operations into five workspaces.

### Download

Use **Download** when you have one supported video URL.

1. Paste the video URL.
2. Keep `MP4 (Recommended)` and `Auto`, or choose another available format and quality.
3. Choose a destination folder.
4. Select `Download`.

The Download Summary shows the selected URL, format, quality, destination, and enabled advanced options.

#### Advanced Options

Advanced Options provide additional control over a single download:

- Quality inspection
- Filename handling
- Selected-format preference
- Subtitle downloading
- Metadata retention

The available media streams and final result still depend on what the source website provides.

### Bulk Download

Use **Bulk Download** when you have multiple video URLs stored in a prepared file.

Supported import formats include:

- TXT
- CSV
- JSON
- JSONL
- M3U
- M3U8

To start a batch:

1. Select `Import from File`.
2. Choose the prepared links file.
3. Review the detected links.
4. Choose the output format, batch quality, and batch speed.
5. Choose a destination folder.
6. Select `Download`.

FrameOn validates the imported links and places valid entries in the queue. The Queue Summary reports total, completed, running, queued, and failed items.

### Audio Converter

Use **Audio Converter** to convert a local audio file or extract audio from a local video.

1. Browse for or drag in a supported audio or video file.
2. Choose an output format such as MP3, AAC, M4A, WAV, FLAC, OGG, or Opus.
3. Choose a destination folder.
4. Select `Convert`.

Enable `Bulk Operation` to select and convert multiple compatible files.

### Video Converter

Use **Video Converter** when the main goal is changing a local video's format for compatibility, such as converting AVI to MP4.

1. Browse for or drag in a supported video file.
2. Choose an output format such as MP4, MOV, AVI, WMV, MKV, FLV, or WebM.
3. Choose the desired quality and destination folder.
4. Select `Convert`.

Enable `Bulk Operation` to select and convert multiple compatible videos.

### Video Optimizer

Use **Video Optimizer** when the main goal is reducing file size or adjusting resolution, frame rate, codec, quality, or file format for sharing and storage.

1. Select a video for inspection.
2. Review the source information and preview.
3. Choose the output settings.
4. Keep `Never upscale` and `Preserve aspect ratio` enabled when appropriate.
5. Choose a destination folder.
6. Select `Optimize`.

The Optimization Summary compares the original video with the estimated or completed output.

## Settings ⚙️

Open **Settings** to configure application-wide behavior.

### General

- Choose the workspace FrameOn opens to at startup.
- Review the installed FrameOn version and bundled tool versions.

### Cookies

Select a browser cookie profile only when a website requires an authenticated browser session.

> Browser cookies can grant access to signed-in website sessions. Select only a browser profile you trust and use this option only when a download requires it.

FrameOn passes the selected browser profile to yt-dlp for the applicable download operation. If the chosen profile is unavailable, select another valid profile in Settings.

### Performance

Hardware Acceleration controls video encoding and decoding for compatible Video Converter and Video Optimizer operations.

- When disabled, FrameOn uses the CPU.
- When enabled, FrameOn uses supported video hardware such as Apple VideoToolbox or an available platform encoder.
- If compatible hardware processing fails, FrameOn can retry using the CPU.

Downloads and Audio Converter operations are not affected by this setting.

### Updates

FrameOn checks for application updates and exposes update controls in Settings. When an update is available, an `Update Available` indicator also appears in the main navigation.

### Appearance

Choose a theme and accent color. The default appearance uses the Graphite theme with a violet accent.

## Help and Support

For guides, frequently asked questions, error codes, and troubleshooting steps, visit the [FrameOn Help Center](https://frameon.choatehome.com/help/).

### Debug Tools

Open the Debug menu to:

- Open the Debug window
- Save logs for troubleshooting

Use `Report a Bug` in the footer when you need to report a problem. Review diagnostic information before sharing it because logs can contain URLs, local paths, and account or profile names.

## Updates

FrameOn checks for updates when it opens and also supports manual checks from Settings.

- On macOS, FrameOn uses the signed macOS update flow.
- On Windows, FrameOn uses its built-in updater to download, verify, and install the signed package.

## Uninstalling

### Windows

Open `Settings > Apps > Installed apps`, find FrameOn, and select `Uninstall`.

You can also remove the installed package with PowerShell:

```powershell
Get-AppxPackage *FrameOn* | Remove-AppxPackage
```

### macOS

Remove the application and installed support files if you want a complete uninstall:

```bash
sudo rm -rf "/Applications/FrameOn.app"
sudo rm -rf "/Library/Application Support/FrameOn"
sudo rm -rf "/Library/Logs/FrameOn"
sudo pkgutil --forget com.choatehome.frameon
```

To remove per-user settings and logs as well:

```bash
rm -rf "$HOME/Library/Application Support/FrameOn"
rm -rf "$HOME/Library/Logs/FrameOn"
rm -rf "${TMPDIR:-/tmp}/FrameOnSandbox"
```

## Source Code 🧰

Browse the latest [public FrameOn source](https://github.com/fastatic99/FrameOn/tree/main).

### Build Locally

Install the .NET 10 SDK, then run:

```bash
dotnet restore FrameonVideoUtility/FrameonVideoUtility.slnx
dotnet build FrameonVideoUtility/FrameonVideoUtility.slnx
```

Bundled third-party tools are platform-specific. A local source build may require the expected tool files and integrity metadata before every media operation is available.

## Legal

- [License](FrameonVideoUtility/LICENSE.md)
- [Terms of Use](FrameonVideoUtility/TERMS_OF_USE.md)
- [Privacy Policy](FrameonVideoUtility/PRIVACY.md)
- [Third-Party Notices](FrameonVideoUtility/THIRD_PARTY_NOTICES.md)

## Support

- [FrameOn website](https://frameon.choatehome.com/)
- [Help Center](https://frameon.choatehome.com/help/)
- Use `Report a Bug` inside FrameOn for application problems.
