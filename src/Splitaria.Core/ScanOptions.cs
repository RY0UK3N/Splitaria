namespace Splitaria.Core;

public sealed record ScanOptions(
    IReadOnlyCollection<string> SourceFolders,
    string PhotoDestination,
    string VideoDestination,
    bool IncludeSubfolders = true,
    FolderPattern FolderPattern = FolderPattern.YearAndNamedMonth);

public enum FolderPattern
{
    YearAndNamedMonth,
    YearAndNumericMonth,
    YearOnly
}
