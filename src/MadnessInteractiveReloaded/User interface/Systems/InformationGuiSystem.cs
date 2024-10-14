using System.IO;
using Walgelijk;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// The game information and credits.
/// </summary>
public class InformationGuiSystem : Walgelijk.System
{
    private static readonly string infoText =
$@"<color=#ff0000>Madness Interactive Reloaded v{GameVersion.Version} for {BuildInfo.Runtime}</color>
<color=#ff0061>Studio Minus</color>

Programming
<color=#ffffff80>zooi</color>
<color=#ffffff80>duston</color>
<color=#ffffff80>Orsoniks</color>
<color=#ffffff80>Orongto</color>

Art and animations
<color=#ffffff80>Dikkiedik</color>
<color=#ffffff80>Koof</color>

Original soundtrack
<color=#ffffff80>Lothyde</color>
<color=#ffffff80>Additional music by Cheshyre and mmodule.</color>

<color=#ffffff80>Based on Madness Interactive by </color>Flecko (Max Abernethy)<color=#ffffff80> and Madness Combat by </color>Krinkels";

    public override void Update()
    {
        Ui.Layout.FitHeight().Width(Window.Width / 2).MinWidth(512).MaxWidth(1024).CenterHorizontal().VerticalLayout().Scale(0, -48).StickTop();
        Ui.StartScrollView();
        {
            Ui.Layout.FitWidth(true).Height(250).CenterHorizontal();
            Ui.Image(Textures.UserInterface.Logo.Value, ImageContainmentMode.Contain);

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
