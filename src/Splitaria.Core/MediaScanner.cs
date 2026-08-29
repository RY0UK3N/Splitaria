using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Splitaria.Core;

public sealed partial class MediaScanner
{
    private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".bmp", ".gif", ".tif", ".tiff" };
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mov", ".m4v", ".avi", ".mkv", ".wmv", ".webm", ".3gp", ".mts", ".m2ts" };
    private static readonly string[] MonthNames =
        { "01 - Janeiro", "02 - Fevereiro", "03 - Março", "04 - Abril", "05 - Maio", "06 - Junho",
          "07 - Julho", "08 - Agosto", "09 - Setembro", "10 - Outubro", "11 - Novembro", "12 - Dezembro" };

    public Task<IReadOnlyList<MediaItem>> ScanAsync(ScanOptions options, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default) => Task.Run<IReadOnlyList<MediaItem>>(() =>
    {
        Validate(options);
        var searchOption = options.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = options.SourceFolders.Where(Directory.Exists)
            .SelectMany(folder => EnumerateSafely(folder, searchOption))
            .Distinct(StringComparer.OrdinalIgnoreCase).Where(path => TryGetKind(path, out _)).ToArray();
        var result = new List<MediaItem>(files.Length);
        var sourceHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < files.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = files[index];
            TryGetKind(source, out var kind);
            var (date, dateSource) = ResolveDate(source, kind);
            var destinationRoot = kind == MediaKind.Photo ? options.PhotoDestination : options.VideoDestination;
            var destination = BuildDestination(destinationRoot, date, Path.GetFileName(source), options.FolderPattern);
            var sourceHash = ComputeHash(source);
            var duplicateKind = DuplicateKind.None;
            string? duplicateOf = null;

            if (sourceHashes.TryGetValue(sourceHash, out var firstSource))
            {
                duplicateKind = DuplicateKind.InSource;
                duplicateOf = firstSource;
            }
            else
            {
                sourceHashes[sourceHash] = source;
                if (File.Exists(destination))
                {
                    duplicateOf = destination;
                    duplicateKind = FilesAreEqual(source, destination)
                        ? DuplicateKind.IdenticalAtDestination : DuplicateKind.NameConflictAtDestination;
                }
            }

            result.Add(new MediaItem(source, kind, date, dateSource, destination, duplicateKind, duplicateOf));
            progress?.Report((index + 1) * 100 / Math.Max(files.Length, 1));
        }
        return result;
    }, cancellationToken);

    public static bool TryGetKind(string path, out MediaKind kind)
    {
        var extension = Path.GetExtension(path);
        if (PhotoExtensions.Contains(extension)) { kind = MediaKind.Photo; return true; }
        if (VideoExtensions.Contains(extension)) { kind = MediaKind.Video; return true; }
        kind = default; return false;
    }

    public static (DateTime Date, string Source) ResolveDate(string path, MediaKind kind)
    {
        if (kind == MediaKind.Photo && TryReadPhotoTakenDate(path, out var captured)) return (captured, "Data de captura");
        var match = DateInNameRegex().Match(Path.GetFileNameWithoutExtension(path));
        if (match.Success && DateTime.TryParseExact(
            $"{match.Groups["year"].Value}-{match.Groups["month"].Value}-{match.Groups["day"].Value}",
            "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return (parsed, "Nome do arquivo");
        return (File.GetLastWriteTime(path), "Data de modificação");
    }

    private static bool TryReadPhotoTakenDate(string path, out DateTime date)
    {
        date = default;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            if (decoder.Frames.FirstOrDefault()?.Metadata is not BitmapMetadata metadata) return false;
            var candidates = new object?[] { metadata.DateTaken, metadata.GetQuery("/app1/ifd/exif/{uint=36867}"), metadata.GetQuery("/app1/ifd/exif/{uint=36868}") };
            foreach (var text in candidates.Select(value => value?.ToString()).Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                foreach (var format in new[] { "yyyy:MM:dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss" })
                    if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)) return true;
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)) return true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or FileFormatException) { }
        return false;
    }

    private static string ComputeHash(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool FilesAreEqual(string first, string second) =>
        new FileInfo(first).Length == new FileInfo(second).Length && ComputeHash(first) == ComputeHash(second);

    private static string BuildDestination(string root, DateTime date, string fileName, FolderPattern pattern)
    {
        var year = date.Year.ToString(CultureInfo.InvariantCulture);
        var month = date.Month.ToString("00", CultureInfo.InvariantCulture);
        var namedMonth = MonthNames[date.Month - 1];
        return pattern switch
        {
            FolderPattern.YearOnly => Path.Combine(root, year, fileName),
            FolderPattern.YearAndNumericMonth => Path.Combine(root, year, month, fileName),
            FolderPattern.YearAndYearMonth => Path.Combine(root, year, $"{year}-{month}", fileName),
            FolderPattern.YearAndFullDate => Path.Combine(root, year, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), fileName),
            FolderPattern.FlatYearAndNamedMonth => Path.Combine(root, $"{year}-{month} - {namedMonth[5..]}", fileName),
            FolderPattern.FlatYearAndNumericMonth => Path.Combine(root, $"{year}-{month}", fileName),
            _ => Path.Combine(root, year, namedMonth, fileName)
        };
    }

    private static IEnumerable<string> EnumerateSafely(string folder, SearchOption option)
    {
        try { return Directory.EnumerateFiles(folder, "*", option).ToArray(); }
        catch (UnauthorizedAccessException) { return []; }
        catch (IOException) { return []; }
    }

    private static void Validate(ScanOptions options)
    {
        if (options.SourceFolders.Count == 0) throw new ArgumentException("Selecione ao menos uma pasta de origem.");
        if (string.IsNullOrWhiteSpace(options.PhotoDestination)) throw new ArgumentException("Selecione o destino das fotos.");
        if (string.IsNullOrWhiteSpace(options.VideoDestination)) throw new ArgumentException("Selecione o destino dos vídeos.");
    }

    [GeneratedRegex(@"(?<!\d)(?<year>19\d{2}|20\d{2})[-_\.]?(?<month>0[1-9]|1[0-2])[-_\.]?(?<day>0[1-9]|[12]\d|3[01])(?!\d)")]
    private static partial Regex DateInNameRegex();
}
