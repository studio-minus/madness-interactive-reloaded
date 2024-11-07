using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Walgelijk;
using Walgelijk.AssetManager;
using System.Linq;

namespace MIR;

/// <summary>
/// Cache weapon thumbnails.
/// </summary>
public class WeaponThumbnailCache// : Cache<WeaponInstructions, IReadableTexture>
{
    public static readonly WeaponThumbnailCache Instance = new();
    public const int MaxWorkers = 4;

    private static readonly ConcurrentDictionary<string, Worker> workers = [];
    private const string CacheLocation = "cache/weapons/";

    public WeaponThumbnailCache()
    {
        string cachePath = Path.Combine(Game.Main.AppDataDirectory, CacheLocation);
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);
    }

    public IReadableTexture Load(WeaponInstructions obj)
    {
        if (workers.TryGetValue(obj.Id, out var worker))
        {
            if (worker.Finished)
                workers.Remove(obj.Id, out _);
            return Textures.UserInterface.ApparelButtonBackground.Value;
        }

        string cachedPath = Path.Combine(Game.Main.AppDataDirectory, CacheLocation, obj.Id + ".png");

        if (File.Exists(cachedPath))
        {
            var tex = Resources.Load<Texture>(cachedPath, true);
            return tex;
        }

        if (workers.Count >= MaxWorkers)
            return Textures.UserInterface.ApparelButtonBackground.Value;
        Resources.Unload(Resources.GetID(cachedPath));
        worker = new Worker(cachedPath, obj);
        workers.TryAdd(obj.Id, worker);
        Task.Run(worker.Start);
        return Textures.UserInterface.ApparelButtonBackground.Value;
    }

    private class Worker
    {
        public readonly string OutputPath;

        public readonly bool Tilt;
        public readonly AssetRef<Texture> BaseTexture;
        public readonly AnimatedPartIntermediate[] AnimatedParts;

        public bool Finished { get; private set; }

        public Worker(string outputPath, WeaponInstructions wpn)
        {
            OutputPath = outputPath;

            BaseTexture = wpn.BaseTexture;
            AnimatedParts = wpn.AnimatedParts == null ? [] : [.. wpn.AnimatedParts
                .Where(p => p.VisibilityCurve == null || p.VisibilityCurve.Evaluate(0)> 0.5f)
                .Select(p => {
                return new AnimatedPartIntermediate(
                    p.Scale,
                    p.Texture,
                    p.AngleCurve?.Evaluate(0) ?? default,
                    p.TranslationCurve?.Evaluate(0) ?? default
                );
            })];
            Tilt = wpn.WeaponData.WeaponType is WeaponType.Melee;
        }

        public void Start()
        {
            const int targetSize = ThumbnailRenderer.ThumbnailResolution;
            const int padding = 25;

            using var outStream = new FileStream(OutputPath, FileMode.Create);

            using var img = new SKBitmap(targetSize, targetSize);
            using var canvas = new SKCanvas(img);

            using var baseTexture = SKBitmap.Decode(Assets.LoadNoCache<byte[]>(BaseTexture.Id));

            var scaling = (float)(targetSize - padding) / int.Max(baseTexture.Height, baseTexture.Width);
            var baseTextureSize = new Vector2(baseTexture.Width, baseTexture.Height) * scaling;

            canvas.Clear(SKColors.Black);
            canvas.DrawRect(padding, padding, targetSize - padding * 2, targetSize - padding * 2, new SKPaint
            {
                Color = SKColors.Red.WithAlpha(64),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke
            });

            if (Tilt)
                canvas.RotateRadians(float.Pi / 4, targetSize / 2, targetSize / 2);

            var baseTexRect = new SKRect(0, 0, baseTextureSize.X, baseTextureSize.Y);
            baseTexRect.Offset(
                targetSize / 2 - baseTextureSize.X / 2,
                targetSize / 2 - baseTextureSize.Y / 2);
            canvas.DrawBitmap(baseTexture, baseTexRect);

            if (AnimatedParts != null)
                foreach (var part in AnimatedParts)
                {
                    using var tex = SKBitmap.Decode(Assets.LoadNoCache<byte[]>(part.Texture.Id));

                    var size = new Vector2(tex.Width, tex.Height) * scaling;
                    size.X *= part.Scale.X;
                    size.Y *= part.Scale.Y;
                    var offset = part.Position * scaling;
                    offset.Y *= -1;
                    //Draw.Quad(center - size / 2 + offset, size);
                    var r = new SKRect(0, 0, size.X, size.Y);
                    r.Offset(targetSize / 2 - size.X / 2 + offset.X, targetSize / 2 - size.Y / 2 + offset.Y);
                    canvas.DrawBitmap(tex, r);
                }

            canvas.Save();
            img.Encode(outStream, SKEncodedImageFormat.Png, 100);
            Finished = true;
        }

        public record AnimatedPartIntermediate(Vector2 Scale, AssetRef<Texture> Texture, float Angle, Vector2 Position);
    }
}

