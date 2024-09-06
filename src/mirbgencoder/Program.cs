using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace mirbgencoder;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
            throw new Exception($"Invalid use. Usage:\n\t{nameof(mirbgencoder)} [path to background image] [path to foreground image] [path to decal mask] [output file]");

        var backgroundPath = args[0];
        var foregroundPath = args[1];
        var decalMaskPath = args[2];
        var outputPath = (args.Length == 4) ?
            args[3] :
            ((Path.GetFileNameWithoutExtension(backgroundPath) + "_" +
            Path.GetFileNameWithoutExtension(foregroundPath) + "_" +
            Path.GetFileNameWithoutExtension(decalMaskPath)) + "_output.qoi");

        Configuration.Default.ImageFormatsManager.AddImageFormat(QoiFormat.Instance);
        Configuration.Default.ImageFormatsManager.AddImageFormatDetector(QoiFormat.Instance);
        Configuration.Default.ImageFormatsManager.SetEncoder(QoiFormat.Instance, QoiFormat.Encoder.Instance);
        Configuration.Default.ImageFormatsManager.SetDecoder(QoiFormat.Instance, QoiFormat.Decoder.Instance);

        if (!outputPath.EndsWith(".qoi"))
            throw new Exception("the output file path should end with .qoi");

        using var background = Image.Load<Rgba32>(backgroundPath);
        using var foreground = Image.Load<Rgba32>(foregroundPath);
        using var decalMask = Image.Load<Rgba32>(decalMaskPath);

        if (background.Size != foreground.Size || foreground.Size != decalMask.Size)
            throw new Exception("all provided images need to be the same size");

        using var target = new Image<Rgba32>(background.Width, background.Height);

        for (int y = 0; y < target.Height; y++)
            for (int x = 0; x < target.Width; x++)
            {
                var bg = background[x, y];
                var fg = foreground[x, y];
                var dm = decalMask[x, y];

                bool hasDecals = dm.R > 128;
                bool isForeground = fg.A > 128;
                bool isBackground = !isForeground && bg.A > 1;

                var colour = new Rgba32(
                    isForeground ? fg.R : bg.R,
                    isForeground ? fg.G : bg.G,
                    isForeground ? fg.B : bg.B);

                if (isBackground)
                    colour.A =  hasDecals ? (byte)100 : (byte)200;
                else if (isForeground)
                    colour.A = 255;
                else 
                    colour.A = 0;


                target[x, y] = colour;
            }

        target.SaveAsPng(Path.ChangeExtension(outputPath, "png"));
        target.SaveAsQoi(outputPath);

        static Rgba32 Lerp(Rgba32 a, Rgba32 b, float t)
        {
            var result = new Rgba32();
            result.FromVector4(Vector4.Lerp(a.ToVector4(), b.ToVector4(), t));
            return result;
        }
    }
}