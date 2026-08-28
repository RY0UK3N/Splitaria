using LibVLCSharp.Shared;

namespace Splitaria.App;

internal static class VideoEngine
{
    private static readonly Lazy<LibVLC> Instance = new(CreatePlayerEngine);

    public static LibVLC Shared => Instance.Value;

    private static LibVLC CreatePlayerEngine()
    {
        LibVLCSharp.Shared.Core.Initialize();
        return new LibVLC("--no-video-title-show", "--quiet");
    }
}
