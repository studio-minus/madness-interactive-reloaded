using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// System for the loading scene.
/// </summary>
public class GameLoadingSystem : Walgelijk.System
{
    private static readonly string GameName = "MADNESS INTERACTIVE RELOADED " + GameVersion.Version;

    public override void Render()
    {
        if (!Scene.FindAnyComponent<GameLoadingComponent>(out var data))
            return;

        int padding = 18;
        int width = Window.Width - padding * 2;
        int lineHeight = 32;
        int scrollOffset = Math.Max(data.DisplayedText.Count * lineHeight - (Window.Height - padding * 2 - 24), 0);
        float flash = MadnessUtils.NormalisedSineWave(Time.SecondsSinceLoad * 2) * 0.5f + 0.5f;

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Font = Fonts.Toxigenesis;

        Draw.FontSize = 72;
        Draw.Colour = Colors.Red * 0.5f;
        Draw.Text(Math.Ceiling(Utilities.Clamp(data.Progress) * 100) + "%", Window.Size / 2, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);

        Draw.FontSize = 32;
        Draw.Colour = Colors.Red;
        Draw.Text(GameName, new Vector2(padding, padding - scrollOffset), Vector2.One, textBoxWidth: width);

        Draw.FontSize = 24;
        for (int i = 0; i < data.DisplayedText.Count; i++)
        {
            Draw.Colour = i == data.DisplayedText.Count - 1 ? (Colors.White * flash) : Colors.Red * 0.8f;
            Draw.Text(data.DisplayedText[i], new Vector2(padding, padding + (i + 1) * lineHeight - scrollOffset), Vector2.One, textBoxWidth: width);
        }

        if (data.FlashTime < 1)
        {
            Draw.Colour = Colors.Red * (1 - Utilities.Clamp(data.FlashTime));
            Draw.Image(Assets.Load<Texture>("textures/red_vignette.png").Value, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Stretch);
            data.FlashTime += Time.DeltaTime * 0.5f;
        }

        Draw.TransformMatrix = Matrix3x2.CreateRotation(Time * 0.2f, Window.Size);
        Draw.Colour = Colors.Red.WithAlpha(0.05f);
        Draw.Image(
            Assets.Load<Texture>("textures/loading_gear.png").Value, 
            new Rect(Window.Size, new Vector2(Window.Height * 2)), 
            ImageContainmentMode.Stretch);     
    }
}
