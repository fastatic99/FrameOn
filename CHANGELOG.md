# FrameOn Changelog

## v1.0.4

### New

- Added an Installed Tools section in Settings showing the bundled versions of FFmpeg, FFprobe, yt-dlp, and Deno.
- Added a Windows Update Status page.

### Enhancements

- Updated bundled tools:
  - yt-dlp: 2026.03.17 → 2026.08.19
  - Deno: 2.7.14 → 2.9.5
  - FFmpeg and FFprobe: 8.1.2 on macOS and 8.1.1 on Windows → 9.0.1 on both platforms
- Improved video-conversion reliability.

### Fixes

- Fixed duplicate FrameOn instances appearing after closing a macOS update notification.
- Fixed the macOS updater's Install and Relaunch window falling behind other windows after authentication.
- Fixed Windows update interface rendering problems.

### Security and Privacy

- Updated Deno with the security fix for CVE-2026-27190.
- Updated the Privacy Policy to explain the limited technical information collected when FrameOn connects to its services.

### Terms and Licensing

- Clarified acceptable use of FrameOn's update and connected services.
- Clarified warranty limitations and responsibility for misuse.
- Clarified that official FrameOn application builds and private components are proprietary.
- Clarified that FrameOn's sanitized public source export is separately available under the Apache License 2.0 and is provided without warranty.

## v1.0.3

### Highlights
- Improved app update support for installed macOS and Windows versions.
- Improved Advanced downloads so FrameOn can check available video quality options before saving.

## v1.0.2

### Highlights
- Improved YouTube download reliability by including the JavaScript runtime FrameOn needs.
- Windows installs can now use an auto-updating installer link, so future updates are easier to install.
- macOS installs now include the ability to auto-update.
- Deno runtime failures are now captured in the logs.

### Fixed
- Fixed macOS signed-tool issues that could prevent yt-dlp or Deno from running after installation.
- Fixed YouTube downloads that failed when yt-dlp needed a JavaScript runtime to inspect available formats.
- Fixed Windows MSIX installs missing the bundled Deno runtime used by yt-dlp for YouTube playback checks.
- Reduced noisy macOS permission warnings when FrameOn uses tools installed by the pkg installer.

## v1.0.1

### Added
- Added signed Windows MSIX installer support.
- Added signed macOS PKG installer support.
- Added crash reporting and user feedback reporting.
- Added Welcome Screen for Windows and the ability to add short-cut on first launch.

### Changed
- Improved installed-tool handling for Windows and macOS included a tool hashing function that ensures tools haven't been tampered, stores a secure cache of the hash using Windows DPAPI.
- Improved startup behavior so tools are prepared when needed instead of during app launch.
- Updated release packaging so installers include the app version in their artifact names.

### Fixed
- Fixed recommended quality selection and file type seleciton corrupting quick time player
