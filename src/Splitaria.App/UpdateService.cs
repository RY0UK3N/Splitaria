using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Splitaria.App;

internal sealed record UpdateRelease(Version Version, string Tag, string PageUrl, UpdateAsset Installer);
internal sealed record UpdateAsset(string Name, Uri DownloadUrl, long Size, string Sha256);

internal static class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/RY0UK3N/Splitaria/releases/latest";

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

    public static async Task<UpdateRelease?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version) || version <= CurrentVersion)
            return null;

        var assetElement = root.GetProperty("assets").EnumerateArray().FirstOrDefault(asset =>
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            return name.StartsWith("Splitaria-Setup-", StringComparison.OrdinalIgnoreCase) &&
                   name.EndsWith("-win-x64.exe", StringComparison.OrdinalIgnoreCase);
        });
        if (assetElement.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("A release não contém o instalador do Splitaria para Windows x64.");

        var assetName = assetElement.GetProperty("name").GetString() ?? "";
        if (Path.GetFileName(assetName) != assetName)
            throw new InvalidDataException("O nome do instalador publicado é inválido.");
        var downloadUrl = new Uri(assetElement.GetProperty("browser_download_url").GetString()
                                  ?? throw new InvalidDataException("A release não contém um endereço de download."));
        if (downloadUrl.Scheme != Uri.UriSchemeHttps || !downloadUrl.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("O endereço do instalador não pertence ao GitHub.");

        var digest = assetElement.TryGetProperty("digest", out var digestElement)
            ? digestElement.GetString() ?? "" : "";
        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) || digest.Length != 71)
            throw new InvalidDataException("A release não contém uma assinatura SHA-256 válida.");

        var pageUrl = root.GetProperty("html_url").GetString() ?? "https://github.com/RY0UK3N/Splitaria/releases";
        return new UpdateRelease(version, tag, pageUrl,
            new UpdateAsset(assetName, downloadUrl, assetElement.GetProperty("size").GetInt64(), digest[7..]));
    }

    public static async Task<string> DownloadAsync(UpdateRelease release, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var updateFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Splitaria", "Updates", release.Tag);
        Directory.CreateDirectory(updateFolder);
        var destination = Path.Combine(updateFolder, release.Installer.Name);
        var partial = destination + ".download";

        try
        {
            using var client = CreateClient(TimeSpan.FromMinutes(20));
            using var response = await client.GetAsync(release.Installer.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None,
                             81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    received += read;
                    if (release.Installer.Size > 0)
                        progress?.Report((int)Math.Min(100, received * 100 / release.Installer.Size));
                }
            }

            await using var downloaded = new FileStream(partial, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(downloaded, cancellationToken));
            if (!actualHash.Equals(release.Installer.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A verificação de integridade do instalador falhou.");

            File.Move(partial, destination, true);
            progress?.Report(100);
            return destination;
        }
        catch
        {
            if (File.Exists(partial)) File.Delete(partial);
            throw;
        }
    }

    public static void StartInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true,
            Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /SP- /UPDATE"
        });
    }

    private static HttpClient CreateClient(TimeSpan? timeout = null)
    {
        var client = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Splitaria/{CurrentVersion.ToString(3)}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }
}
