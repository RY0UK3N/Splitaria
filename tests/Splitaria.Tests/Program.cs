using Splitaria.Core;

var failures = new List<string>();

Check("Reconhece foto", MediaScanner.TryGetKind("foto.JPG", out var photo) && photo == MediaKind.Photo);
Check("Reconhece vídeo", MediaScanner.TryGetKind("video.mp4", out var video) && video == MediaKind.Video);
Check("Ignora arquivo comum", !MediaScanner.TryGetKind("notas.txt", out _));

var testRoot = Path.Combine(Path.GetTempPath(), $"splitaria-tests-{Guid.NewGuid():N}");
var source = Path.Combine(testRoot, "source");
var destination = Path.Combine(testRoot, "destination");
Directory.CreateDirectory(source);
try
{
    var photoPath = Path.Combine(source, "IMG_20260827_120000.jpg");
    await File.WriteAllTextAsync(photoPath, "test");
    var scanner = new MediaScanner();
    var result = await scanner.ScanAsync(new ScanOptions([source], destination, destination));
    Check("Analisa um arquivo", result.Count == 1);
    Check("Extrai data do nome", result[0].Date.Date == new DateTime(2026, 8, 27));
    Check("Monta pasta por ano e mês", result[0].DestinationPath.Contains(Path.Combine("2026", "08 - Agosto")));
    var numericMonth = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.YearAndNumericMonth));
    Check("Monta pasta com mês numérico", numericMonth[0].DestinationPath.Contains(Path.Combine("2026", "08")));
    var yearOnly = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.YearOnly));
    Check("Monta pasta somente por ano", Path.GetDirectoryName(yearOnly[0].DestinationPath) == Path.Combine(destination, "2026"));
    var yearAndYearMonth = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.YearAndYearMonth));
    Check("Monta pasta por ano e ano-mês", yearAndYearMonth[0].DestinationPath.Contains(Path.Combine("2026", "2026-08")));
    var yearAndFullDate = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.YearAndFullDate));
    Check("Monta pasta por ano e data completa", yearAndFullDate[0].DestinationPath.Contains(Path.Combine("2026", "2026-08-27")));
    var flatNamedMonth = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.FlatYearAndNamedMonth));
    Check("Monta pasta ano-mês com nome na raiz", Path.GetDirectoryName(flatNamedMonth[0].DestinationPath) == Path.Combine(destination, "2026-08 - Agosto"));
    var flatNumericMonth = await scanner.ScanAsync(new ScanOptions([source], destination, destination,
        FolderPattern: FolderPattern.FlatYearAndNumericMonth));
    Check("Monta pasta ano-mês na raiz", Path.GetDirectoryName(flatNumericMonth[0].DestinationPath) == Path.Combine(destination, "2026-08"));

    var organizer = new MediaOrganizer();
    var organized = await organizer.CopyAsync(result, DuplicateAction.Skip);
    Check("Copia arquivo", organized.Copied == 1 && File.Exists(result[0].DestinationPath));
    Check("Detalha fotos copiadas", organized.CopiedPhotos == 1 && organized.CopiedVideos == 0);
    Check("Não sobrescreve", (await organizer.CopyAsync(result, DuplicateAction.Skip)).Skipped == 1);

    var duplicatePath = Path.Combine(source, "copia_20260827.jpg");
    File.Copy(photoPath, duplicatePath);
    var duplicates = await scanner.ScanAsync(new ScanOptions([source], Path.Combine(testRoot, "photos-2"), destination));
    Check("Detecta conteúdo repetido na origem", duplicates.Count(item => item.DuplicateKind == DuplicateKind.InSource) == 1);
    Check("Desmarca duplicado da origem", duplicates.Single(item => item.DuplicateKind == DuplicateKind.InSource).IsSelected == false);

    var secondSource = Path.Combine(testRoot, "second-source");
    Directory.CreateDirectory(secondSource);
    await File.WriteAllTextAsync(Path.Combine(secondSource, "VID_20260710.mp4"), "video");
    var multipleSources = await scanner.ScanAsync(new ScanOptions([source, secondSource],
        Path.Combine(testRoot, "photos-3"), Path.Combine(testRoot, "videos-3")));
    Check("Analisa múltiplas pastas de origem", multipleSources.Count == 3);
}
finally
{
    if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} teste(s) falharam:");
    foreach (var failure in failures) Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine("Todos os testes passaram.");
return 0;

void Check(string name, bool condition)
{
    Console.WriteLine($"{(condition ? "OK" : "FALHOU")} — {name}");
    if (!condition) failures.Add(name);
}
