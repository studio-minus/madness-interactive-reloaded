using BigGustave;
using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Decodes the most common image formats (PNG, JPEG, BMP, etc.)
/// </summary>
public class PngDecoder : IImageDecoder
{
    private static readonly byte[] magic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public DecodedImage Decode(in ReadOnlySpan<byte> bytes, bool flipY)
    {
        var png = Png.Open([.. bytes]);
        var colors = new Color[png.Width * png.Height];

        for (int i = 0; i < colors.Length; i++)
        {
            var x = i % png.Width;
            var y = (int)float.Floor(i / png.Width);
            if (flipY)
                y = png.Height - y - 1;
            var pixel = png.GetPixel(x, y);
            colors[i] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
        }

        return new DecodedImage(png.Width, png.Height, colors);
    }

    public DecodedImage Decode(in byte[] bytes, int count, bool flipY) => Decode(bytes.AsSpan(0, count), flipY);

    public bool CanDecode(in string filename) => filename.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase);

    public bool CanDecode(ReadOnlySpan<byte> raw)
    {
        return raw.StartsWith(magic);
    }
}

