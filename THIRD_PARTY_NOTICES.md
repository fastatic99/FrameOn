# Third-Party Notices

FrameOn bundles separate third-party tools to provide download and conversion features. These tools are not owned by FrameOn and remain under their own licenses.

## yt-dlp

FrameOn bundles yt-dlp for media download support. FrameOn obtains the binaries and published checksums from the official yt-dlp project releases. yt-dlp is distributed under the Unlicense.

Releases, source, and license information: https://github.com/yt-dlp/yt-dlp/releases

## Deno

FrameOn bundles Deno as the JavaScript runtime used by supported media-download workflows. FrameOn obtains the binaries from the official Deno project releases.

Releases, source, and license information: https://github.com/denoland/deno/releases

## FFmpeg and FFprobe

FrameOn bundles FFmpeg and FFprobe for audio/video conversion, probing, and container remuxing. FrameOn v1.0.4 uses checksum-verified Gyan Windows x64 builds and OpenPGP-verified Evermeet macOS Intel builds linked from FFmpeg's download page. These are third-party builds, not official FFmpeg-project binaries. The bundled FFmpeg/FFprobe builds are GPL-family builds where marked by the platform package.

Source and license information: https://ffmpeg.org

## Runtime Dependencies

Some platform bundles include runtime dependencies used by yt-dlp, including Python/runtime libraries and packages such as curl_cffi and websockets. These dependencies keep their own open-source licenses, including MIT and BSD-style licenses where included.
