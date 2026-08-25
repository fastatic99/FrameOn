using System;

namespace FrameonVideoUtility.Service.Downloads;

public static class VideoUrlNormalizer
{
    public static string Normalize(Uri uri)
    {
        if (!IsYouTubeHost(uri.Host) ||
            !uri.AbsolutePath.TrimEnd('/').Equals("/watch", StringComparison.OrdinalIgnoreCase) ||
            !TryGetQueryParameter(uri.Query, "v", out string videoId))
        {
            return uri.ToString();
        }

        // A YouTube watch URL needs only its video id. Playlist position and
        // tracking parameters are unnecessary for a single-video request.
        return $"https://www.youtube.com/watch?v={Uri.EscapeDataString(videoId)}";
    }

    public static bool IsYouTubeHost(string host)
    {
        return host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("music.youtube.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("m.youtube.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetQueryParameter(string query, string parameterName, out string value)
    {
        value = "";

        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        foreach (string part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pieces = part.Split('=', 2);
            if (pieces.Length != 2 ||
                !string.Equals(Uri.UnescapeDataString(pieces[0]), parameterName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = Uri.UnescapeDataString(pieces[1]).Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }
}
