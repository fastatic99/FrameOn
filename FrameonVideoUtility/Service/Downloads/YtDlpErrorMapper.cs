using System;

namespace FrameonVideoUtility.Service.Downloads;

public static class YtDlpErrorMapper
{
    public static string GetFriendlyError(string rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError))
        {
            return "The video could not be downloaded. Check the link and try again.";
        }

        string error = rawError.ToLowerInvariant();

        if (error.Contains("http error 403") ||
            error.Contains("403 forbidden") ||
            error.Contains("(403) forbidden") ||
            error.Contains("status code 403") ||
            error.Contains("server returned 403"))
        {
            return "HTTP 403 Forbidden: The video site refused access. This video may require browser cookies. Open Settings > Downloads > Browser Cookies, select the signed-in profile, then try again.";
        }

        if (error.Contains("age-restricted") ||
            error.Contains("confirm your age") ||
            error.Contains("sign in to confirm") ||
            error.Contains("age restriction"))
        {
            return "This video appears to be age-restricted and may require sign-in.";
        }

        if (error.Contains("private video") ||
            error.Contains("this video is private"))
        {
            return "This video appears to be private and cannot be downloaded.";
        }

        if (error.Contains("unsupported url") ||
            error.Contains("no suitable extractor") ||
            error.Contains("not a valid url") ||
            error.Contains("unable to extract"))
        {
            return "This link does not appear to be a supported video URL.";
        }

        if (error.Contains("video unavailable") ||
            error.Contains("this video is unavailable") ||
            error.Contains("not available"))
        {
            return "This video appears to be unavailable.";
        }

        if (error.Contains("login") ||
            error.Contains("sign in") ||
            error.Contains("cookies") ||
            error.Contains("authentication"))
        {
            return "This video may require sign-in.";
        }

        if (error.Contains("network") ||
            error.Contains("timed out") ||
            error.Contains("connection") ||
            error.Contains("unable to download webpage"))
        {
            return "The video site could not be reached. Check your connection and try again.";
        }

        return "The video could not be downloaded. Check the link and try again.";
    }
}
