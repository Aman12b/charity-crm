using CharityCrm.Core.Abstractions;

namespace CharityCrm.Api.Setup;

public static class DownloadName
{
    /// <summary>"Mitglieder_2026-10-18.csv"</summary>
    public static string For(IClock clock, string prefix, string extension) => $"{prefix}_{clock.Today:yyyy-MM-dd}.{extension}";
}
