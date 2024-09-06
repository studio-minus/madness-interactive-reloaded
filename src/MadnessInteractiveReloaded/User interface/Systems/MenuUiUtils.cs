using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

public static class MenuUiUtils
{
    public static void DrawBackground(Scene scene, float introAnimationProgress)
    {
        var window = scene.Game.Window;
        Draw.Colour = Utilities.Lerp(Colors.White, new Color(0.7f, 0.3f, 0.3f), introAnimationProgress);
        Draw.Texture = Assets.Load<Texture>("textures/main_menu_background.qoi").Value;
        float backgroundAspect = Draw.Texture.Width / (float)Draw.Texture.Height;
        var backgroundTargetSize = new Vector2(backgroundAspect * window.Height, window.Height);
        Draw.Quad(
            new Vector2(-(backgroundTargetSize.X - window.Width) * MadnessUtils.NormalisedSineWave(scene.Game.State.Time.SecondsSinceLoad * 0.005f), 0),
            new Vector2(backgroundTargetSize.X, backgroundTargetSize.Y));
        Draw.ResetMaterial();
    }

    public static void DrawBorderGradients(Scene scene, float introAnimationProgress)
    {
        var window = scene.Game.Window;

        const float topGradientSize = 200;
        const float bottomGradientSize = 100;

        var tex = Assets.Load<Texture>("textures/black-transparent-gradient.png").Value;
        tex.WrapMode = WrapMode.Mirror;
        Draw.Texture = tex;

        Draw.Colour = Colors.White.WithAlpha(0.5f * introAnimationProgress);

        Draw.Quad(new Vector2(0, topGradientSize), new Vector2(window.Width, -topGradientSize));
        Draw.Quad(new Vector2(0, window.Height - bottomGradientSize), new Vector2(window.Width, bottomGradientSize));
    }

    public static void DrawLogo(Scene scene, float introAnimationProgress, int windowHeightResponsiveThreshold)
    {
        var window = scene.Game.Window;

        Draw.Colour = Colors.White.WithAlpha(introAnimationProgress);
        Draw.Texture = Textures.UserInterface.Logo.Value;
        var logoAspectRatio = Draw.Texture.Size.X / Draw.Texture.Size.Y;
        float targetLogoWidth = 600;

        var s = new Vector2(targetLogoWidth, targetLogoWidth / logoAspectRatio);
        var p = new Vector2(20);

        if (window.Height <= windowHeightResponsiveThreshold)
        {
            s *= 0.8f;
            p.X = window.Width - s.X - 30;
            p.Y = (window.Height - s.Y) / 2;
        }

        Draw.Quad(p, s);
    }

    public static void StartFullMenuPanel(Scene scene, [CallerLineNumber] int site = 0)
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        MenuUiUtils.DrawBackground(scene, 1);
        //MenuUiUtils.DrawLogo(Scene, 1, 0);
        Ui.Theme.Foreground(new Appearance(Colors.White, Assets.Load<Texture>("textures/border-top-bottom.png").Value, ImageMode.Slice)).Once();
        Ui.Layout.FitWidth().StickLeft().FitHeight().Scale(-20, -70).CenterHorizontal().StickTop().Move(0, 10);
        Ui.StartGroup(true, identity: site); // main panel
    }

    public static bool BackButton([CallerLineNumber] int site = 0)
    {
        Ui.Theme.OutlineWidth(2).Once();
        Ui.Layout.FitWidth().MaxWidth(150).Height(40).StickLeft().StickBottom().Move(10, -10);
        if (Ui.Button(Localisation.Get("back"), identity: site))
        {
            MadnessUtils.Flash(Colors.Black, 0.2f);
            return true;
        }
        return false;
    }
}
