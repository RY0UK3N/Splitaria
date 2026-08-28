using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Splitaria.Core;

public enum MediaKind { Photo, Video }
public enum DuplicateKind { None, InSource, IdenticalAtDestination, NameConflictAtDestination }

public sealed class MediaItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public MediaItem(string sourcePath, MediaKind kind, DateTime date, string dateSource,
        string destinationPath, DuplicateKind duplicateKind = DuplicateKind.None,
        string? duplicateOf = null, bool isSelected = true)
    {
        SourcePath = sourcePath;
        Kind = kind;
        Date = date;
        DateSource = dateSource;
        DestinationPath = destinationPath;
        DuplicateKind = duplicateKind;
        DuplicateOf = duplicateOf;
        _isSelected = duplicateKind is not DuplicateKind.InSource and not DuplicateKind.IdenticalAtDestination && isSelected;
    }

    public string SourcePath { get; }
    public MediaKind Kind { get; }
    public DateTime Date { get; }
    public string DateSource { get; }
    public string DestinationPath { get; }
    public DuplicateKind DuplicateKind { get; }
    public string? DuplicateOf { get; }
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); }
    }

    public string Name => Path.GetFileName(SourcePath);
    public string TypeLabel => Kind == MediaKind.Photo ? "Foto" : "Vídeo";
    public string DateLabel => Date.ToString("dd/MM/yyyy");
    public bool HasDuplicateIssue => DuplicateKind != DuplicateKind.None;
    public string StatusLabel => DuplicateKind switch
    {
        DuplicateKind.InSource => "Duplicado na origem",
        DuplicateKind.IdenticalAtDestination => "Já existe no destino",
        DuplicateKind.NameConflictAtDestination => "Nome já usado no destino",
        _ => "Pronto para copiar"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
