using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kaguya.Utils;

public static class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/YujiSX/-Slay-The-Sprie2-CosmicPrincessKaguyaMod/releases/latest";
    private const string ModId = "Kaguya";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static bool _hasCheckedThisSession = false;
    private static readonly object _lock = new();

    public static string LatestVersion { get; private set; } = string.Empty;
    public static string ReleaseUrl { get; private set; } = string.Empty;

    public static string CurrentVersion => GetCurrentVersion();

    static UpdateChecker()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "KaguyaMod-UpdateChecker");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    private static string GetCurrentVersion()
    {
        string? version = null;

        try
        {
            version = GetVersionFromModManager();
        }
        catch { }

        if (string.IsNullOrEmpty(version))
        {
            try
            {
                version = GetVersionFromManifestFile();
            }
            catch { }
        }

        if (string.IsNullOrEmpty(version))
            return "v0.0.0";

        if (!version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            version = "v" + version;

        return version;
    }

    // ===== 路径 A：反射 ModManager =====
    private static string? GetVersionFromModManager()
    {
        var modManagerType = Type.GetType("MegaCrit.Sts2.Core.Modding.ModManager, sts2");
        if (modManagerType == null) return null;

        var modsProperty = modManagerType.GetProperty("Mods");
        if (modsProperty == null) return null;

        var mods = modsProperty.GetValue(null) as System.Collections.IEnumerable;
        if (mods == null) return null;

        foreach (var mod in mods)
        {
            var manifestField = mod.GetType().GetField("manifest");
            if (manifestField == null) continue;

            var manifest = manifestField.GetValue(mod);
            if (manifest == null) continue;

            var idField = manifest.GetType().GetField("id");
            var versionField = manifest.GetType().GetField("version");
            if (idField == null || versionField == null) continue;

            var id = idField.GetValue(manifest) as string;
            if (id != ModId) continue;

            var version = versionField.GetValue(manifest) as string;
            if (!string.IsNullOrEmpty(version))
                return version;
        }

        return null;
    }

    // ===== 路径 B：读 manifest 文件 =====
    private static string? GetVersionFromManifestFile()
    {
        string? manifestPath = FindManifestPath();
        if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
            return null;

        var jsonContent = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ModManifestData>(jsonContent);
        return manifest?.Version;
    }

    private static string? FindManifestPath()
    {
        string? path = TryGetManifestPathFromModManager();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = TryGetManifestPathFromExecutable();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        path = TryGetManifestPathFromAssembly();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return path;

        return null;
    }

    private static string? TryGetManifestPathFromModManager()
    {
        try
        {
            var modManagerType = Type.GetType("MegaCrit.Sts2.Core.Modding.ModManager, sts2");
            if (modManagerType == null) return null;

            var modsProperty = modManagerType.GetProperty("Mods");
            if (modsProperty == null) return null;

            var mods = modsProperty.GetValue(null) as System.Collections.IEnumerable;
            if (mods == null) return null;

            foreach (var mod in mods)
            {
                var manifestField = mod.GetType().GetField("manifest");
                if (manifestField == null) continue;

                var manifest = manifestField.GetValue(mod);
                if (manifest == null) continue;

                var idField = manifest.GetType().GetField("id");
                if (idField == null) continue;

                if ((idField.GetValue(manifest) as string) != ModId) continue;

                var pathField = manifest.GetType().GetField("path");
                if (pathField != null)
                {
                    var modPath = pathField.GetValue(manifest) as string;
                    if (!string.IsNullOrEmpty(modPath))
                        return Path.Combine(modPath, $"{ModId}.json");
                }

                var directoryField = manifest.GetType().GetField("directory");
                if (directoryField != null)
                {
                    var directory = directoryField.GetValue(manifest) as string;
                    if (!string.IsNullOrEmpty(directory))
                        return Path.Combine(directory, $"{ModId}.json");
                }
            }
        }
        catch { }

        return null;
    }

    private static string? TryGetManifestPathFromExecutable()
    {
        try
        {
            string executablePath = Godot.OS.GetExecutablePath();
            string? gameDirectory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrEmpty(gameDirectory)) return null;

            string modsDirectory = Path.Combine(gameDirectory, "mods");
            if (!Directory.Exists(modsDirectory)) return null;

            string directPath = Path.Combine(modsDirectory, ModId, $"{ModId}.json");
            if (File.Exists(directPath))
                return directPath;

            foreach (var subDir in Directory.GetDirectories(modsDirectory))
            {
                string manifestPath = Path.Combine(subDir, $"{ModId}.json");
                if (File.Exists(manifestPath))
                    return manifestPath;
            }
        }
        catch { }

        return null;
    }

    private static string? TryGetManifestPathFromAssembly()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            string? location = assembly.Location;
            if (string.IsNullOrEmpty(location)) return null;

            string? directory = Path.GetDirectoryName(location);
            if (string.IsNullOrEmpty(directory)) return null;

            string manifestPath = Path.Combine(directory, $"{ModId}.json");
            if (File.Exists(manifestPath))
                return manifestPath;

            var parentDir = Directory.GetParent(directory);
            if (parentDir != null)
            {
                manifestPath = Path.Combine(parentDir.FullName, $"{ModId}.json");
                if (File.Exists(manifestPath))
                    return manifestPath;
            }
        }
        catch { }

        return null;
    }

    // ===== GitHub 检测 =====
    public static async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        lock (_lock)
        {
            if (_hasCheckedThisSession)
                return new UpdateCheckResult { AlreadyChecked = true, CurrentVersion = CurrentVersion };
            _hasCheckedThisSession = true;
        }

        for (int retry = 0; retry < MaxRetries; retry++)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                    continue;

                LatestVersion = release.TagName;
                ReleaseUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3747548629";

                bool hasUpdate = CompareVersions(CurrentVersion, LatestVersion) < 0;

                return new UpdateCheckResult
                {
                    Success = true,
                    HasUpdate = hasUpdate,
                    CurrentVersion = CurrentVersion,
                    LatestVersion = LatestVersion,
                    ReleaseUrl = ReleaseUrl
                };
            }
            catch
            {
                if (retry < MaxRetries - 1)
                    await Task.Delay(RetryDelayMs);
            }
        }

        return new UpdateCheckResult { Success = false, CurrentVersion = CurrentVersion };
    }

    private static int CompareVersions(string version1, string version2)
    {
        string Normalize(string v) => v.TrimStart('v', 'V');

        var v1Parts = Normalize(version1).Split('.');
        var v2Parts = Normalize(version2).Split('.');

        int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
        for (int i = 0; i < maxLength; i++)
        {
            int v1 = i < v1Parts.Length && int.TryParse(v1Parts[i], out int p1) ? p1 : 0;
            int v2 = i < v2Parts.Length && int.TryParse(v2Parts[i], out int p2) ? p2 : 0;
            if (v1 != v2) return v1.CompareTo(v2);
        }
        return 0;
    }

    public static void ResetCheckState()
    {
        lock (_lock) { _hasCheckedThisSession = false; }
    }

    private class ModManifestData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}

public class UpdateCheckResult
{
    public bool AlreadyChecked { get; set; }
    public bool Success { get; set; }
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
}