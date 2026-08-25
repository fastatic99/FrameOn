# FrameOn VideoUtility

<p align="center">
  <img src="FrameonVideoUtility/Assets/frameon_icon_master_cleaned_1024.png" alt="FrameOn VideoUtility" width="180">
</p>

FrameOn VideoUtility is a desktop helper for downloading videos and converting audio/video files without making the workflow feel like a chore.

## Download 📦

Download FrameOn from the [official FrameOn website](https://frameon.choatehome.com/#download).

Use the newest version shown on the release page. Older builds are mainly kept for troubleshooting.

### Windows

Download the file ending in `.msix`. This installs FrameOn like a normal Windows app, adds it to the Start menu, and lets you remove it later from Windows Settings.

After installing, open FrameOn from the Start menu. On first launch, FrameOn asks whether you want a desktop shortcut. You can create one or skip it and continue using the Start menu.

On first launch, FrameOn places the video tools it needs, such as `yt-dlp` and `ffmpeg`, into your Windows app data folder. They are kept there so the app starts faster after the first launch.

#### If Windows Shows A Warning

Windows may ask you to confirm the app the first time you install or open a new release. That is expected for new downloads.

Before continuing, make sure:

1. You downloaded FrameOn from the release page above.
2. The file name starts with `FrameOn-VideoUtility-`.
3. The file name ends with `.msix`.

If those are true, choose `More info`, then `Run anyway` or `Install`. If the file came from anywhere else, delete it and download a fresh copy from the release page.

### macOS

Download the file ending in `.pkg`, then open it and follow the installer. The pkg installs FrameOn into Applications and places the required video tools in the system Application Support folder.

## Source Code 🧰

Want to inspect or build the public source? Browse the latest [sanitized public source](https://github.com/fastatic99/FrameOn/tree/main).

The public source archive removes private API and telemetry implementation files, then includes safe placeholders so the project can still build.

## Bundled Tool Provenance 🔍

FrameOn v1.0.4 obtains Deno binaries from the official [Deno project releases](https://github.com/denoland/deno/releases) and obtains yt-dlp binaries and published checksums from the official [yt-dlp project releases](https://github.com/yt-dlp/yt-dlp/releases).

FFmpeg and FFprobe come from platform builds linked by the [FFmpeg download page](https://ffmpeg.org/download.html): Gyan for Windows x64 and Evermeet for macOS Intel. FrameOn verifies the Windows package with its published SHA-256 digest and verifies the macOS packages with Evermeet's pinned OpenPGP signing key. These are third-party builds rather than official FFmpeg-project binaries; FFmpeg itself publishes source code. Each tool remains under its own license, and none of these upstream projects endorses FrameOn.

## Quick Help 🧭

- Need install files? Start with [Download](#download-).
- Want the source? See [Source Code](#source-code-).
- App feels busy while converting? See [Performance Selector](#performance-selector-%EF%B8%8F).
- Got an error message? Jump to [Troubleshooting Messages](#troubleshooting-messages-).
- Want the app terms? See [License](LICENSE), [Terms of Use](TERMS_OF_USE.md), [Privacy](PRIVACY.md), and [Third-Party Notices](THIRD_PARTY_NOTICES.md).

## Features ✨

- Download videos from supported URLs.
- Choose download quality and output format.
- Convert video files between common video formats.
- Convert or extract audio into common audio formats.
- Get installed app updates from inside FrameOn.
- Save application logs from the Debug menu.
- Choose a conversion performance mode to control ffmpeg CPU usage.

## How To Download A Video

1. Paste a supported video URL.
2. Choose the file format. `MP4 (Recommended)` is the safest choice for most videos.
3. Click `Download`.
4. Choose where to save the file.

If the download fails, open the Debug window and review the latest `yt-dlp` messages.

## App Updates

FrameOn checks for updates when it opens. If an update is available, an `Update Available` button appears near the top of the app.

On macOS, FrameOn uses the macOS update prompt to install the update.

On Windows, FrameOn uses its built-in updater to download and install the newer version.

## How To Convert Audio

1. In `Audio Converter`, click `Browse`.
2. Select a supported audio or video file.
3. Choose the output audio format.
4. Click `Convert`.
5. Choose the output folder.

Video inputs can be converted to audio-only outputs.

## How To Convert Video

1. In `Video Converter`, click `Browse`.
2. Select a supported video file.
3. Choose the output video format.
4. Click `Convert`.
5. Choose the output folder.

## Performance Selector ⚙️

Open `Settings` and choose `Conversion Impact`.

- `Auto - ffmpeg auto`: lets ffmpeg choose CPU thread usage. This is the default.
- `Low Impact`: uses fewer CPU threads.
- `Balanced`: uses about half of available CPU threads.
- `High Impact`: uses more CPU threads.
- `Max Impact`: uses all available CPU threads.

Use `Low Impact` or `Balanced` if the computer feels busy while converting or downloading. Downloads can also use ffmpeg during merge/remux post-processing, so this setting can affect both conversions and some downloads.

## Troubleshooting Messages 🔎

When something goes sideways, start here. Match the message you see in FrameOn, then follow the note underneath it.

### Link And Format Messages

**`Please enter a video link.`**  
The URL box is empty. Paste a video URL and try again.

**`Please enter a valid video URL.`**  
The link is not a valid `http` or `https` URL. Check the link and paste the full address.

**`Please select a valid output format.`**  
A format header or blank option was selected. Choose an actual video or audio format from the dropdown.

### Sign-In And Site Access Messages

**`This video appears to be age-restricted and may require sign-in.`**  
The site may require age verification or a signed-in session. Confirm the video opens in your browser first.

**`This video appears to be private and cannot be downloaded.`**  
The selected browser profile or account may not have access. Confirm the video opens in your browser first.

**`This video may require sign-in.`**  
The site likely needs an authenticated browser session. Confirm the video opens in your browser first.

**`HTTP 403 Forbidden: The video site refused access.`**


### Network And Site Support Messages

**`This link does not appear to be a supported video URL.`**  
The site or URL format was not recognized. Confirm the URL and check whether `yt-dlp` supports the site.

**`The video site could not be reached.`**  
Network, DNS, VPN, firewall, or site availability may be blocking access. Check your connection and try again.

### Conversion Messages

**`Audio conversion failed. Check the selected file and try again.`**  
The file may be corrupt, unsupported, locked, or unavailable. Choose the file again or try a different output format.

**`Video conversion failed. Check the selected file and try again.`**  
The file may be corrupt, unsupported, locked, or unavailable. Choose the file again or try a different output format.

**`The selected file no longer exists.`**  
The file was moved, deleted, or is on a disconnected drive. Browse for the file again.

**`Download cancelled / conversion cancelled.`**  
The active operation was cancelled. Temporary work files are cleaned up automatically when FrameOn closes normally. Start it again if needed.

## FAQ

### Why do I get HTTP 403 Forbidden for a video that opens in my browser?


### How do I uninstall FrameOn on Windows?

Open `Settings > Apps > Installed apps`, find FrameOn, and choose `Uninstall`.

You can also remove it with PowerShell:

```powershell
Get-AppxPackage *FrameOn* | Remove-AppxPackage
```

Windows installs FrameOn as an MSIX app, so Windows keeps FrameOn's per-user tools, settings, and logs under the app package data folder. The package suffix can be different on each machine, so use this command to find it:

```powershell
Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -Force |
  Where-Object { $_.Name -like "FrameOn.VideoUtility_*" } |
  Select-Object FullName
```

After first launch, the video tools are usually under:

```powershell
$env:LOCALAPPDATA\Packages\FrameOn.VideoUtility_*\LocalCache\Local\FrameOn\Tools\Windows X64
```

To fully clean up FrameOn's per-user Windows data after uninstalling, remove the matching package data folder:

```powershell
Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -Force |
  Where-Object { $_.Name -like "FrameOn.VideoUtility_*" } |
  Remove-Item -Recurse -Force
```

FrameOn also creates temporary work folders while it downloads or converts files. They normally clean themselves up when the app closes. If the app is force-closed, you can remove leftovers here:

```powershell
Remove-Item "$env:TEMP\FrameOnSandbox" -Recurse -Force
```

### How do I uninstall FrameOn on macOS?

Delete the app and remove the installed tools if you want a clean uninstall:

```bash
sudo rm -rf "/Applications/FrameOn.app"
sudo rm -rf "/Library/Application Support/FrameOn"
sudo rm -rf "/Library/Logs/FrameOn"
sudo pkgutil --forget com.choatehome.frameon
```

The macOS installer places its video tools here:

```bash
/Library/Application Support/FrameOn/Tools/Macos (Silicon)
```

FrameOn may also have per-user settings, logs, or temporary files. Remove these only if you want to reset FrameOn completely for that macOS user:

```bash
rm -rf "$HOME/Library/Application Support/FrameOn"
rm -rf "$HOME/Library/Logs/FrameOn"
rm -rf "${TMPDIR:-/tmp}/FrameOnSandbox"
```

Installing a newer pkg replaces the app and refreshes the tools.

## Logs 🧾

Open `Debug > Open Debug Window` to view logs.

Use `Debug > Save Logs` to export logs for troubleshooting. FrameOn also keeps a small rolling log on disk so recent app activity is still available after a restart.

Use `Report a Bug` at the bottom of FrameOn to send a problem report.

Windows log file:

```powershell
$env:LOCALAPPDATA\Packages\FrameOn.VideoUtility_*\LocalCache\Local\FrameOn\Logs\FrameOn.log
```

Find the exact Windows log path:

```powershell
Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -Force |
  Where-Object { $_.Name -like "FrameOn.VideoUtility_*" } |
  ForEach-Object {
    Get-ChildItem $_.FullName -Recurse -Force -Filter FrameOn.log -ErrorAction SilentlyContinue
  } |
  Select-Object FullName
```

macOS log file:

```bash
$HOME/Library/Logs/FrameOn/FrameOn.log
```

Logs include high-level app actions, selected download quality and format mode, the generated `yt-dlp` selector, selected performance mode changes, tool preparation, and summarized process output.
