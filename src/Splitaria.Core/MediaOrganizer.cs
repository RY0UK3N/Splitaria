using System.IO;

namespace Splitaria.Core;

public enum DuplicateAction { Skip, KeepBoth }
public sealed record OrganizationResult(int Copied, int Skipped, int Failed);

public sealed class MediaOrganizer
{
    public async Task<OrganizationResult> CopyAsync(IReadOnlyList<MediaItem> items, DuplicateAction duplicateAction,
        IProgress<(int Current, int Total)>? progress = null, CancellationToken cancellationToken = default)
    {
        var selected = items.Where(item => item.IsSelected).ToArray();
        var copied = 0; var skipped = 0; var failed = 0;
        for (var index = 0; index < selected.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = selected[index];
            try
            {
                if (item.DuplicateKind == DuplicateKind.IdenticalAtDestination ||
                    item.DuplicateKind == DuplicateKind.InSource && duplicateAction == DuplicateAction.Skip) skipped++;
                else
                {
                    var destinationPath = item.DestinationPath;
                    if (File.Exists(destinationPath))
                    {
                        if (duplicateAction == DuplicateAction.Skip) { skipped++; progress?.Report((index + 1, selected.Length)); continue; }
                        destinationPath = GetAvailableName(destinationPath);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    await using var source = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                    await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                    await source.CopyToAsync(destination, cancellationToken);
                    File.SetLastWriteTime(destinationPath, File.GetLastWriteTime(item.SourcePath));
                    copied++;
                }
            }
            catch (IOException) { failed++; }
            catch (UnauthorizedAccessException) { failed++; }
            progress?.Report((index + 1, selected.Length));
        }
        return new OrganizationResult(copied, skipped, failed);
    }

    private static string GetAvailableName(string path)
    {
        var folder = Path.GetDirectoryName(path)!; var name = Path.GetFileNameWithoutExtension(path); var extension = Path.GetExtension(path);
        for (var index = 2; ; index++)
        {
            var candidate = Path.Combine(folder, $"{name} ({index}){extension}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
