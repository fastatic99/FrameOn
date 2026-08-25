#!/usr/bin/env bash
set -euo pipefail

missing=0
for tool in ffmpeg ffprobe yt-dlp; do
  if command -v "$tool" >/dev/null 2>&1; then
    printf '%s\n' "Found $tool: $(command -v "$tool")"
  else
    printf '%s\n' "Missing $tool. Install or provide it before running media workflows." >&2
    missing=1
  fi
done

exit "$missing"
