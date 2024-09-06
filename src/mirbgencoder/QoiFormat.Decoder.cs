using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace mirbgencoder;

public partial class QoiFormat
{
    public class Decoder : ImageDecoder
    {
        public static Decoder Instance { get; private set; } = new();

        public bool LinearColourSpace { get; set; }
        public bool UseAlpha { get; set; }

        protected override Image<TPixel> Decode<TPixel>(DecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            var core = new DecoderCore(UseAlpha, LinearColourSpace);
            var buffer = new byte[stream.Length];
            int c = stream.Read(buffer);
            buffer = buffer[0..c];
            if (!core.Decode(buffer, buffer.Length))
            {
                stream.Dispose();
                throw new SixLabors.ImageSharp.InvalidImageContentException("QOI could not be decoded for unknown reasons");
            }

            TPixel[] result = new TPixel[core.GetWidth() * core.GetHeight()];

            for (int i = 0; i < core.GetPixels().Length; i++)
            {
                var v = core.GetPixels()[i];

                var b = new Rgba32((byte)((v & 0xFF_00_00) >> 48),
                        (byte)((v & 0xFF_00) >> 40),
                        (byte)(v & 0xFF),
                        (byte)(((uint)v & 0xFF_00_00_00) >> 56));

                result[i].FromRgba32(b);
            }

            return Image.WrapMemory(result.AsMemory(), core.GetWidth(), core.GetHeight());
        }

        protected override Image Decode(DecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            return Decode<Rgba32>(options, stream, cancellationToken);
        }

        protected override ImageInfo Identify(DecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[12];
            stream.Read(buffer);

            int width = buffer[4] << 24 | buffer[5] << 16 | buffer[6] << 8 | buffer[7];
            int height = buffer[8] << 24 | buffer[9] << 16 | buffer[10] << 8 | buffer[11];

            Size size = new(width, height);
            PixelTypeInfo pixelType = new(sizeof(int), PixelAlphaRepresentation.Unassociated);

            return new ImageInfo(pixelType, size, null);
        }
    }

    private class DecoderCore
    {
        int Width;

        int Height;

        int[]? Pixels;

        bool Alpha;

        bool LinearColorspace;

        public DecoderCore(bool alpha, bool linearColorspace)
        {
            Alpha = alpha;
            LinearColorspace = linearColorspace;
        }

        /// <summary>Decodes the given QOI file contents.</summary>
        /// <remarks>Returns <see langword="true" /> if decoded successfully.</remarks>
        /// <param name="encoded">QOI file contents. Only the first <c>encodedSize</c> bytes are accessed.</param>
        /// <param name="encodedSize">QOI file length.</param>
        public bool Decode(byte[] encoded, int encodedSize)
        {
            if (encodedSize < 23 || encoded[0] != 'q' || encoded[1] != 'o' || encoded[2] != 'i' || encoded[3] != 'f')
                return false;
            int width = encoded[4] << 24 | encoded[5] << 16 | encoded[6] << 8 | encoded[7];
            int height = encoded[8] << 24 | encoded[9] << 16 | encoded[10] << 8 | encoded[11];
            if (width <= 0 || height <= 0 || height > 2147483647 / width)
                return false;
            switch (encoded[12])
            {
                case 3:
                    Alpha = false;
                    break;
                case 4:
                    Alpha = true;
                    break;
                default:
                    return false;
            }
            switch (encoded[13])
            {
                case 0:
                    LinearColorspace = false;
                    break;
                case 1:
                    LinearColorspace = true;
                    break;
                default:
                    return false;
            }
            int pixelsSize = width * height;
            int[] pixels = new int[pixelsSize];
            encodedSize -= 8;
            int encodedOffset = 14;
            int[] index = new int[64];
            int pixel = -16777216;
            for (int pixelsOffset = 0; pixelsOffset < pixelsSize;)
            {
                if (encodedOffset >= encodedSize)
                    return false;
                int e = encoded[encodedOffset++];
                switch (e >> 6)
                {
                    case 0:
                        pixels[pixelsOffset++] = pixel = index[e];
                        continue;
                    case 1:
                        pixel = pixel & -16777216 | pixel + ((e >> 4) - 4 - 2 << 16) & 16711680 | pixel + ((e >> 2 & 3) - 2 << 8) & 65280 | pixel + (e & 3) - 2 & 255;
                        break;
                    case 2:
                        e -= 160;
                        int rb = encoded[encodedOffset++];
                        pixel = pixel & -16777216 | pixel + (e + (rb >> 4) - 8 << 16) & 16711680 | pixel + (e << 8) & 65280 | pixel + e + (rb & 15) - 8 & 255;
                        break;
                    default:
                        if (e < 254)
                        {
                            e -= 191;
                            if (pixelsOffset + e > pixelsSize)
                                return false;
                            Array.Fill(pixels, pixel, pixelsOffset, e);
                            pixelsOffset += e;
                            continue;
                        }
                        if (e == 254)
                        {
                            pixel = pixel & -16777216 | encoded[encodedOffset] << 16 | encoded[encodedOffset + 1] << 8 | encoded[encodedOffset + 2];
                            encodedOffset += 3;
                        }
                        else
                        {
                            pixel = encoded[encodedOffset + 3] << 24 | encoded[encodedOffset] << 16 | encoded[encodedOffset + 1] << 8 | encoded[encodedOffset + 2];
                            encodedOffset += 4;
                        }
                        break;
                }
                pixels[pixelsOffset++] = index[(pixel >> 16) * 3 + (pixel >> 8) * 5 + (pixel & 63) * 7 + (pixel >> 24) * 11 & 63] = pixel;
            }
            if (encodedOffset != encodedSize)
                return false;
            Width = width;
            Height = height;
            Pixels = pixels;
            return true;
        }

        /// <summary>Returns the width of the decoded image in pixels.</summary>
        public int GetWidth() => Width;

        /// <summary>Returns the height of the decoded image in pixels.</summary>
        public int GetHeight() => Height;

        /// <summary>Returns the pixels of the decoded image, top-down, left-to-right.</summary>
        /// <remarks>Each pixel is a 32-bit integer 0xAARRGGBB.</remarks>
        public int[] GetPixels() => Pixels ?? Array.Empty<int>();

        /// <summary>Returns the information about the alpha channel from the file header.</summary>
        public bool HasAlpha() => Alpha;

        /// <summary>Returns the color space information from the file header.</summary>
        /// <remarks><see langword="false" /> = sRGB with linear alpha channel.
        /// <see langword="true" /> = all channels linear.</remarks>
        public bool IsLinearColorspace() => LinearColorspace;
    }
}
