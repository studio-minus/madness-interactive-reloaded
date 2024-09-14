using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Renders thumbnails for things like clothes.
/// </summary>
public static class ThumbnailRenderer
{
    public const int ThumbnailResolution = 256;
    private static IGraphics Graphics => Game.Main.Window.Graphics;

    private static IReadableTexture? playerThumbnail = null;
    private static readonly IReadableTexture?[] playerPosters =
    {
        null,
        null,
        null,
    };

    private static RenderTexture PrepareEnvironment(int width = ThumbnailResolution, int height = ThumbnailResolution)
    {
        Game.Main.RenderQueue.RenderAndReset(Graphics);
        var rt = new RenderTexture(width, height, WrapMode.Clamp, FilterMode.Linear, flags: RenderTargetFlags.None);
        Draw.Reset();
        Draw.ScreenSpace = true;
        Graphics.CurrentTarget = rt;
        return rt;
    }

    private static void EndEnvironment()
    {
        Game.Main.RenderQueue.RenderAndReset(Graphics);
        Graphics.CurrentTarget = Game.Main.Window.RenderTarget;
    }

    public static IReadableTexture CreateThumbnailFor(WeaponInstructions instr)
    {
        var rt = PrepareEnvironment();
        Graphics.Clear(Colors.Black);
        var center = rt.Size / 2;
        const int targetSize = ThumbnailResolution - 25;

        Draw.Texture = Textures.UserInterface.ApparelButtonBackground.Value;
        Draw.Quad(Vector2.Zero, rt.Size);

        var baseTexture = instr.BaseTexture.Value;
        var scaling = (float)targetSize / int.Max(baseTexture.Height, baseTexture.Width);
        var baseTextureSize = baseTexture.Size * scaling;
        Draw.Texture = baseTexture;
        Draw.Quad(center - baseTextureSize / 2, baseTextureSize);

        if (instr.AnimatedParts != null)
            foreach (var animated in instr.AnimatedParts)
            {
                if (animated.VisibilityCurve != null && animated.VisibilityCurve.Evaluate(0) < 0.5)
                    continue;

                Draw.Texture = animated.Texture.Value;
                var size = Draw.Texture.Size * scaling;
                size.X *= animated.Scale.X;
                size.Y *= animated.Scale.Y;
                var offset = animated.TranslationCurve != null ? animated.TranslationCurve.Evaluate(0) * scaling : default;
                offset.Y *= -1;
                Draw.Quad(center - size / 2 + offset, size);
            }

        EndEnvironment();
        return rt;
    }

    public static IReadableTexture CreateThumbnailFor(CharacterLook look, Color backgroundColor, IReadableTexture? backgroundTexture)
    {
        var rt = PrepareEnvironment();
        var center = rt.Size / 2;
        const int targetHeadHeight = 161;

        Graphics.Clear(backgroundColor);
        if (backgroundTexture != null)
        {
            Draw.Texture = backgroundTexture;// Textures.UI.ApparelButtonBackground;
            Draw.Quad(Vector2.Zero, rt.Size);
        }

        var headTexture = look.Head.Right.Value;
        var scaling = (float)targetHeadHeight / headTexture.Height;
        var bodyOffset = new Vector2(101, 260);

        Draw.Material = BodyPartMaterialPool.Instance.RequestObject(new BodyPartMaterialParams
        {
            SkinTexture = look.Body.Right.Value,
            FleshTexture = Textures.Character.FleshBody,
            GoreTexture = Texture.White,
            BloodColour = look.BloodColour,
            Scale = 1,
        });
        drawBodyLayer(look.Body);

        Draw.ResetMaterial();
        drawBodyLayer(look.BodyLayer1);
        drawBodyLayer(look.BodyLayer2);

        Draw.Material = BodyPartMaterialPool.Instance.RequestObject(new BodyPartMaterialParams
        {
            SkinTexture = look.Head.Right.Value,
            FleshTexture = Textures.Character.FleshHead,
            GoreTexture = Texture.White,
            BloodColour = look.BloodColour,
            Scale = 1,
        });
        drawHeadLayer(look.Head);

        Draw.ResetMaterial();
        drawHeadLayer(look.HeadLayer1);
        drawHeadLayer(look.HeadLayer2);
        drawHeadLayer(look.HeadLayer3);

        void drawBodyLayer(ArmourPiece? piece)
        {
            if (piece == null)
                return;

            const float angleDegrees = 6;

            var tex = piece.Right.Value;
            var size = tex.Size * scaling;
            var center = bodyOffset - size / 2;
            var rotatedOffset = Vector2.Transform(piece.OffsetRight, Matrix3x2.CreateRotation(angleDegrees * Utilities.DegToRad, center));
            var p = center + rotatedOffset * scaling;

            Draw.Texture = tex;
            Draw.Quad(p, size, angleDegrees);
        }

        void drawHeadLayer(ArmourPiece? piece)
        {
            if (piece == null)
                return;

            var tex = piece.Right.Value;
            var size = tex.Size * scaling;
            var p = center - size / 2 + new Vector2(piece.OffsetRight.X, piece.OffsetRight.Y) * scaling;
            Draw.Texture = tex;
            Draw.Quad(p, size);
        }

        EndEnvironment();
        return rt;
    }

    public static void ResetPlayerThumbnail()
    {
        playerThumbnail?.Dispose();
        playerThumbnail = null;
    }

    /// <summary>
    /// This already calls <see cref="ResetPlayerThumbnail"/>
    /// </summary>
    public static void ResetAllPosters()
    {
        for (int i = 0; i < playerPosters.Length; i++)
        {
            var p = playerPosters[i];
            if (p != null && SpriteMaterialCreator.Instance.Has(p))
                SpriteMaterialCreator.Instance.Unload(p);

            p?.Dispose();
            playerPosters[i] = null;
        }
        ResetPlayerThumbnail();
    }

    public static IReadableTexture GetOrCreatePlayerThumbnail()
    {
        return playerThumbnail ??= CreateThumbnailFor(UserData.Instances.PlayerLook, new Color(21, 21, 21), null);
    }

    private static IReadableTexture CreatePlayerPoster(int index)
    {
        playerPosters[index]?.Dispose();
        playerPosters[index] = null;

        var overlay = Textures.PlayerPosterOverlays[index].Value;
        var player = GetOrCreatePlayerThumbnail();
        var rt = PrepareEnvironment(overlay.Width, overlay.Height);

        var pos = Vector2.Zero;
        var size = player.Size;
        switch (index)
        {
            case 0:
                pos = new Vector2(86, 12);
                size = new Vector2(113);
                break;
            case 1:
                pos = new Vector2(8 + 109, 29);
                size = new Vector2(-109, 109);
                break;
            case 2:
                pos = new Vector2(-23, -10);
                size = new Vector2(94);
                break;
        }

        Draw.Colour = Colors.White;
        Draw.Texture = player;
        Draw.Quad(pos, size);

        Draw.Texture = overlay;
        Draw.Quad(Vector2.Zero, overlay.Size);

        EndEnvironment();

        return rt;
    }

    public static IReadableTexture GetOrGeneratePlayerPoster(int index)
    {
        return playerPosters[index] ??= CreatePlayerPoster(index);
    }
}
