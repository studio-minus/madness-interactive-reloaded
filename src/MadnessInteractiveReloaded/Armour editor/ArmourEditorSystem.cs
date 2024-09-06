using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class ArmourEditorSystem : Walgelijk.System
{
    const float rightBarWidth = 450;
    const float topBarHeight = 32;

    private bool TryResetLook()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return false;

        Registries.Looks.Get("grunt").CopyTo(character.Look);
        character.NeedsLookUpdate = true;
        return true;
    }

    public override void Initialise()
    {
        TryResetLook();
    }

    public override void Update()
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.BackgroundBehind;
        Draw.Image(
            Assets.Load<Texture>("textures/red-black-gradient.png").Value, 
            new Rect(0,0,Window.Width, Window.Height), ImageContainmentMode.Stretch);

        if (!Scene.FindAnyComponent<ArmourEditorComponent>(out var editor))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        DrawTopBar(editor);
        DrawArmourEditorUi(editor);

        character.NeedsLookUpdate = true;

        // Toggle freezing
        if (Input.IsKeyReleased(Key.F) && player != null)
            player.RespondToUserInput = !player.RespondToUserInput;
    }

    private void DrawTopBar(ArmourEditorComponent editor)
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        var w = Window.Size.X;
        var h = Window.Size.Y;

        Ui.Layout.Size(w, topBarHeight).HorizontalLayout();
        Ui.StartGroup();
        {
            Ui.Layout.Size(64, topBarHeight);
            if (Ui.Button("New"))
            {
                var tag = new Tag(234578); // random number to identify the dialog box later

                if (!Scene.TryGetEntityWithTag(tag, out _)) // do nothing if its already there
                {
                    var e = Scene.CreateEntity();
                    Scene.SetTag(e, tag);
                    var a = Scene.AttachComponent(e, new ConfirmationDialogComponent("You will lose unsaved progress. Are you sure you want to create a new armour piece?", () =>
                    {
                        var newPiece = new ArmourPiece
                        {
                            Name = "New armour piece",
                            Type = ArmourPieceType.Unknown,
                            Left = AssetRef<Texture>.None,
                            Right = AssetRef<Texture>.None
                        };

                        SetPiece(editor, newPiece);
                    }));
                }

            }

            Ui.Layout.Size(64, topBarHeight);
            if (Ui.Button("Load"))
            {
                if (FileDialog.OpenFile(new[] { ("armor file", "armor"), ("All files", "*") }, out var chosenPath)
                    && !string.IsNullOrWhiteSpace(chosenPath))
                {
                    var loaded = ArmourDeserialiser.Load(chosenPath);
                    if (loaded != null)
                    {
                        editor.CurrentPiecePath = chosenPath;
                        SetPiece(editor, loaded);
                    }
                }
            }

            Ui.Layout.Size(400, topBarHeight);
            if (player != null)
                Onion.Theme.Text(player.RespondToUserInput ? Colors.White : Colors.Green).Once();
            Ui.TextRect("Press F to freeze/unfreeze character.", HorizontalTextAlign.Center, VerticalTextAlign.Middle);

            if (editor.CurrentPiecePath != null)
            {
                Ui.Layout.Size(2900, topBarHeight);
                if (player != null)
                    Onion.Theme.Text(Colors.White.WithAlpha(0.5f)).Once();
                Ui.TextRect(editor.CurrentPiecePath, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            }
        }
        Ui.End();
    }

    private void DrawArmourEditorUi(ArmourEditorComponent editor)
    {
        var layout = Onion.Layout;
        var w = Window.Size.X;
        var h = Window.Size.Y;

        layout.Size(rightBarWidth, h).Move(w - rightBarWidth, topBarHeight).VerticalLayout().CenterVertical();
        Ui.Theme.Foreground((Appearance)new Color(25, 25, 25)).Once();
        Ui.StartGroup();
        {
            if (editor.CurrentPiece == null)
            {
                layout.Size(100, 100);
                Ui.Label("Select an armour piece to edit.");
            }
            else
                DrawPropertyPanel(editor);

        }
        Ui.End();
    }

    private void DrawPropertyPanel(ArmourEditorComponent editor)
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        if (editor.CurrentPiece == null)
            return;

        Onion.Theme.Padding(8);
        var layout = Onion.Layout;

        layout.FitWidth().Height(42).CenterHorizontal();
        Onion.Theme.OutlineColour(Colors.Green).Text(new(Colors.White, Colors.Green)).Once();
        if (Ui.ClickButton("Save") && editor.CurrentPiece.Type != ArmourPieceType.Hand && editor.CurrentPiece != null)
        {
            if (string.IsNullOrWhiteSpace(editor.CurrentPiecePath))
            {
                // We do this label and goto shit because the CurrentPiecePath is remembered, so that when Save is pressed, the file is overwritten.
                // This behaviour matches basically every other program but does not match any other editor in this game.

                // TODO: should we make every other editor behave this way? i dont know!! probably!!???

                if (FileDialog.SaveFile(new[] { ("armor file", "armor"), ("All files", "*") }, 
                    defaultName: editor.CurrentPiece.Name, defaultPath: Program.BaseDirectory, out var selectedPath))
                    editor.CurrentPiecePath = selectedPath;
                else goto skip;
            }

            try
            {
                ArmourDeserialiser.Save(editor.CurrentPiece, editor.CurrentPiecePath);
                Audio.PlayOnce(Sounds.UiBad);
                MadnessUtils.Flash(Colors.Green.WithAlpha(0.2f), 0.25f);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MadnessUtils.Flash(Colors.Red, 0.5f);
                Audio.PlayOnce(Sounds.UiBad);
            }

        skip:;
        }

        if (editor.CurrentPiece == null) // appease the nullability gods
            return;

        Ui.Spacer(35);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Name:");

            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.StringInputBox(ref editor.CurrentPiece.Name, new TextBoxOptions());
        }

        Ui.Spacer(8);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Type:");

            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.EnumDropdown(ref editor.CurrentPiece.Type);
        }

        Ui.Spacer(8);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Category:");

            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.EnumDropdown(ref editor.CurrentPiece.Category);
        }

        Ui.Spacer(8);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Texture scale:");

            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.FloatInputBox(ref editor.CurrentPiece.TextureScale);
        }

        Ui.Spacer(8);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Bullet deflect chance:");

            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.FloatInputBox(ref editor.CurrentPiece.DeflectChance, (0, 1));
        }

        Ui.Spacer(8);

        static void dPad(ref Vector2 v, in LayoutQueue layout, in bool hold, in bool inverse = false, [CallerLineNumber] int site = 0)
        {
            var delta = inverse ? -1 : 1;
            Ui.StartDummy(identity: site);
            {
                if (hold)
                {
                    layout.Size(32, 32).Move(-32, 0);
                    if (Ui.HoldButton("<", identity: site))
                        v.X -= delta;

                    layout.Size(32, 32).Move(0, -32);
                    if (Ui.HoldButton("^", identity: site))
                        v.Y--;

                    layout.Size(32, 32).Move(32, 0);
                    if (Ui.HoldButton(">", identity: site))
                        v.X += delta;

                    layout.Size(32, 32).Move(0, 32);
                    if (Ui.HoldButton("v", identity: site))
                        v.Y++;
                }
                else
                {
                    layout.Size(32, 32).Move(-32, 0);
                    if (Ui.ClickButton("<", identity: site))
                        v.X -= delta;

                    layout.Size(32, 32).Move(0, -32);
                    if (Ui.ClickButton("^", identity: site))
                        v.Y--;

                    layout.Size(32, 32).Move(32, 0);
                    if (Ui.ClickButton(">", identity: site))
                        v.X += delta;

                    layout.Size(32, 32).Move(0, 32);
                    if (Ui.ClickButton("v", identity: site))
                        v.Y++;
                }
            }
            Ui.End();
        }

        layout.FitWidth().Height(150).HorizontalLayout();
        Ui.StartGroup(false);
        {
            layout.Size(32, 16);
            Ui.Label("OffsetLeft");

            layout.Size(64, 32);
            Ui.FloatInputBox(ref editor.CurrentPiece.OffsetLeft.X);

            layout.Size(64, 32);
            Ui.FloatInputBox(ref editor.CurrentPiece.OffsetLeft.Y);

            layout.FitWidth().Height(64).Move(0, 64);
            dPad(ref editor.CurrentPiece.OffsetLeft, layout, Input.IsKeyHeld(Key.LeftShift), true);
        }
        Ui.End();

        Ui.Spacer(32);

        layout.FitWidth().Height(150).HorizontalLayout();
        Ui.StartGroup(false);
        {
            layout.Size(32, 16);
            Ui.Label("OffsetRight");

            layout.Size(64, 32);
            Ui.FloatInputBox(ref editor.CurrentPiece.OffsetRight.X);

            layout.Size(64, 32);
            Ui.FloatInputBox(ref editor.CurrentPiece.OffsetRight.Y);

            layout.FitWidth().Height(64).Move(0, 64);
            dPad(ref editor.CurrentPiece.OffsetRight, layout, Input.IsKeyHeld(Key.LeftShift));
        }
        Ui.End();

        Ui.Spacer(8);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Left Texture");
            layout.FitWidth().Height(32).CenterHorizontal();

            MadnessUi.AssetPicker(
                editor.CurrentPiece.Left.Id,
                c =>
                {
                    editor.CurrentPiece.Left = new(c);
                    character.NeedsLookUpdate = true;
                    SetPiece(editor, editor.CurrentPiece);
                },
                static c => c.MimeType.Contains("image"));
        }

        Ui.Spacer(4);

        {
            layout.FitWidth().Height(32).CenterHorizontal();
            Ui.Label("Right Texture");
            layout.FitWidth().Height(32).CenterHorizontal();

            MadnessUi.AssetPicker(
                editor.CurrentPiece.Right.Id,
                c =>
                {
                    editor.CurrentPiece.Right = new(c);
                    character.NeedsLookUpdate = true;
                    SetPiece(editor, editor.CurrentPiece);
                },
                static c => c.MimeType.Contains("image"));
        }
    }

    public void SetPiece(ArmourEditorComponent editor, ArmourPiece? piece)
    {
        if (!MadnessUtils.FindPlayer(Scene, out _, out var character))
            return;

        TryResetLook();
        editor.CurrentPiece = piece;
        editor.CurrentPiecePath = null;

        // (duston): We reset the look already so we won't bother setting
        // the layers to null.
        if (editor.CurrentPiece == null)
            return;

        switch (editor.CurrentPiece.Type)
        {
            case ArmourPieceType.Head:
                character.Look.SetHeadLayer(-1, piece);
                break;
            case ArmourPieceType.HeadAccessory:
                character.Look.SetHeadLayer(0, piece);
                break;
            case ArmourPieceType.Body:
                character.Look.SetBodyLayer(-1, piece);
                break;
            case ArmourPieceType.BodyAccessory:
                character.Look.SetBodyLayer(0, piece);
                break;
        }
    }
}
