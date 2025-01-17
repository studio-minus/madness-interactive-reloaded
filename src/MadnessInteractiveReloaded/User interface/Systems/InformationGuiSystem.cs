using System;
using System.IO;
using System.Runtime.InteropServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// The game information and credits.
/// </summary>
public class InformationGuiSystem : Walgelijk.System
{
    private static string startText = $"<color=#ff0000>Madness Interactive Reloaded v{GameVersion.Version}</color>\n<color=#ff0061>Studio Minus</color>";
    private static string infoText => Assets.Load<string>("base:data/credits.txt");

    public override void Update()
    {
        Ui.Layout.FitHeight().Width(Window.Width / 2).MinWidth(512).MaxWidth(1024).CenterHorizontal().VerticalLayout().Scale(0, -48).StickTop();
        Ui.StartScrollView();
        {
            Ui.Layout.FitWidth(true).Height(250).CenterHorizontal();
            Ui.Image(Textures.UserInterface.Logo.Value, ImageContainmentMode.Contain);

            Ui.Layout.PreferredSize().FitWidth(true).CenterHorizontal();
            Ui.Theme.FontSize(24).Once();
            Ui.TextRect(startText, HorizontalTextAlign.Center, VerticalTextAlign.Top);   
             
            Ui.Layout.PreferredSize().FitWidth(true).CenterHorizontal();
            Ui.Theme.FontSize(24).Once();
            Ui.TextRect(infoText, HorizontalTextAlign.Center, VerticalTextAlign.Top);
        }
        Ui.End();

        // back button
        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);

        Ui.Theme.OutlineWidth(2).Once();
        Ui.Layout.Size(170, 40).StickRight().StickBottom().Move(-10, -10);
        if (Ui.ClickButton(Localisation.Get("Open source libraries")))
            MadnessUtils.OpenExplorer('"' + Path.Combine(Game.ExecutableDirectory, "NOTICE.txt") + '"');
    }
}
