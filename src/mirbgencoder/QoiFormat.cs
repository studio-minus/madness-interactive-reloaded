using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace mirbgencoder;

public partial class QoiFormat : IImageFormat, IImageFormatDetector
{
    public static QoiFormat Instance { get; private set; } = new();

    public string Name => "Quite OK Image Format";
    public string DefaultMimeType => "image/x-qoi";

    public IEnumerable<string> MimeTypes
    {
        get
        {
            yield return DefaultMimeType;
            yield return "image/qoi";
        }
    }

    public IEnumerable<string> FileExtensions
    {
        get
        {
            yield return "qoi";
        }
    }

    public int HeaderSize => Encoder.HeaderSize;

    public bool TryDetectFormat(ReadOnlySpan<byte> header, [NotNullWhen(true)] out IImageFormat? format)
    {
        if (header.Length < 14 || header[0] != 'q' || header[1] != 'o' || header[2] != 'i' || header[3] != 'f')
        {
            format = null;
            return false;
        }
        format = Instance;
        return true;
    }
}

public static class QoiFormatExtensions
{
    private static IImageEncoder GetEncoder(IImageEncoder? encoder, Image source)
        => encoder ?? source.Configuration.ImageFormatsManager.GetEncoder(QoiFormat.Instance);

    public static Task SaveAsQoiAsync(this Image source, string path, QoiFormat.Encoder? encoder = null)
        => source.SaveAsync(path, GetEncoder(encoder, source));

    public static Task SaveAsQoiAsync(this Image source, Stream stream, QoiFormat.Encoder? encoder = null)
    => source.SaveAsync(stream, GetEncoder(encoder, source));

    public static void SaveAsQoi(this Image source, string path, QoiFormat.Encoder? encoder = null)
        => source.Save(path, GetEncoder(encoder, source));

    public static void SaveAsQoi(this Image source, Stream stream, QoiFormat.Encoder? encoder = null)
        => source.Save(stream, GetEncoder(encoder, source));
}