using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace mirbgencoder;

public partial class QoiFormat
{
    public class Encoder : ImageEncoder
    {
        public static Encoder Instance { get; private set; } = new();

        public bool LinearColourSpace { get; set; }
        public bool UseAlpha { get; set; } = true;

        internal const int HeaderSize = 14;
        internal const int PaddingSize = 8;

        private byte[]? encodedBytes;
        private int encodedSize;

        //source.CopyPixelDataTo(data);
        //source.cop
        //if (!encoder.Encode(source.Width, source.Height, EncodeImage(data, source.Width, source.Height), true, false))
        //    throw new Exception("failed to encode image, reason unknown");

        //var encoded = encoder.GetEncoded().AsSpan(0, encoder.GetEncodedSize());
        //using var qoiWrite = File.OpenWrite(path);
        //qoiWrite.Write(encoded);
        //qoiWrite.Dispose();

        public static bool CanEncode(int width, int height, bool alpha) => width > 0 && height > 0 && height <= 2147483625 / width / (alpha ? 5 : 4);

        protected override void Encode<TPixel>(Image<TPixel> source, Stream stream, CancellationToken cancellationToken)
        {
            var data = new TPixel[source.Width * source.Height];
            source.CopyPixelDataTo(data);
            if (!Encode(source.Width, source.Height, EncodeImage(data, source.Width, source.Height), UseAlpha, LinearColourSpace))
                throw new Exception("failed to encode image, reason unknown");

            var e = encodedBytes.AsSpan(0, encodedSize);
            stream.Write(e);
            stream.Flush();
        }

        private static int[] EncodeImage<TPixel>(TPixel[] colors, int width, int height) where TPixel : unmanaged, IPixel<TPixel>
        {
            Rgba32 dest = new(0, 0, 0, 0);
            int[] pixels = new int[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                colors[i].ToRgba32(ref dest);
                int r = dest.R;
                int g = dest.G;
                int b = dest.B;
                int a = dest.A;

                pixels[x + y * width] = (a << 24) | (r << 16) | (g << 8) | b;
            }
            return pixels;
        }

        private bool Encode(int width, int height, int[] pixels, bool alpha, bool linearColorspace)
        {
            if (!CanEncode(width, height, alpha))
                return false;
            int pixelsSize = width * height;
            byte[] encoded = new byte[14 + pixelsSize * (alpha ? 5 : 4) + 8];
            encoded[0] = (byte)'q';
            encoded[1] = (byte)'o';
            encoded[2] = (byte)'i';
            encoded[3] = (byte)'f';
            encoded[4] = (byte)(width >> 24);
            encoded[5] = (byte)(width >> 16);
            encoded[6] = (byte)(width >> 8);
            encoded[7] = (byte)width;
            encoded[8] = (byte)(height >> 24);
            encoded[9] = (byte)(height >> 16);
            encoded[10] = (byte)(height >> 8);
            encoded[11] = (byte)height;
            encoded[12] = (byte)(alpha ? 4 : 3);
            encoded[13] = (byte)(linearColorspace ? 1 : 0);
            int[] index = new int[64];
            int encodedOffset = 14;
            int lastPixel = -16777216;
            int run = 0;
            for (int pixelsOffset = 0; pixelsOffset < pixelsSize;)
            {
                int pixel = pixels[pixelsOffset++];
                if (!alpha)
                    pixel |= -16777216;
                if (pixel == lastPixel)
                {
                    if (++run == 62 || pixelsOffset >= pixelsSize)
                    {
                        encoded[encodedOffset++] = (byte)(191 + run);
                        run = 0;
                    }
                }
                else
                {
                    if (run > 0)
                    {
                        encoded[encodedOffset++] = (byte)(191 + run);
                        run = 0;
                    }
                    int indexOffset = (pixel >> 16) * 3 + (pixel >> 8) * 5 + (pixel & 63) * 7 + (pixel >> 24) * 11 & 63;
                    if (pixel == index[indexOffset])
                        encoded[encodedOffset++] = (byte)indexOffset;
                    else
                    {
                        index[indexOffset] = pixel;
                        int r = pixel >> 16 & 255;
                        int g = pixel >> 8 & 255;
                        int b = pixel & 255;
                        int a = pixel >> 24 & 255;
                        if ((pixel ^ lastPixel) >> 24 != 0)
                        {
                            encoded[encodedOffset] = 255;
                            encoded[encodedOffset + 1] = (byte)r;
                            encoded[encodedOffset + 2] = (byte)g;
                            encoded[encodedOffset + 3] = (byte)b;
                            encoded[encodedOffset + 4] = (byte)a;
                            encodedOffset += 5;
                        }
                        else
                        {
                            int dr = r - (lastPixel >> 16 & 255);
                            int dg = g - (lastPixel >> 8 & 255);
                            int db = b - (lastPixel & 255);
                            if (dr >= -2 && dr <= 1 && dg >= -2 && dg <= 1 && db >= -2 && db <= 1)
                                encoded[encodedOffset++] = (byte)(106 + (dr << 4) + (dg << 2) + db);
                            else
                            {
                                dr -= dg;
                                db -= dg;
                                if (dr >= -8 && dr <= 7 && dg >= -32 && dg <= 31 && db >= -8 && db <= 7)
                                {
                                    encoded[encodedOffset] = (byte)(160 + dg);
                                    encoded[encodedOffset + 1] = (byte)(136 + (dr << 4) + db);
                                    encodedOffset += 2;
                                }
                                else
                                {
                                    encoded[encodedOffset] = 254;
                                    encoded[encodedOffset + 1] = (byte)r;
                                    encoded[encodedOffset + 2] = (byte)g;
                                    encoded[encodedOffset + 3] = (byte)b;
                                    encodedOffset += 4;
                                }
                            }
                        }
                    }
                    lastPixel = pixel;
                }
            }
            Array.Clear(encoded, encodedOffset, 7);
            encoded[encodedOffset + 8 - 1] = 1;
            encodedBytes = encoded;
            encodedSize = encodedOffset + 8;
            return true;
        }
    }
}
