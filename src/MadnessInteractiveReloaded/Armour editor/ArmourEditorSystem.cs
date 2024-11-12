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
    const float rightBarWidth = 400;
    const float topBarHeight = 32;
    private static Texture checkerboard = TexGen.Checkerboard(32, 32, 16, Colors.White, Colors.Gray);

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
            new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Stretch);

        if (!Scene.FindAnyComponent<ArmourEditorComponent>(out var editor))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character))
            return;

        DrawTopBar(editor);
        DrawArmourEditorUi(editor);

        if (player != null)
        {
            Ui.Layout.Size(Window.Width - rightBarWidth, 30).StickLeft(false).StickBottom(false);
            Ui.TextRect(player.RespondToUserInput ? "Press F to freeze character" : "Press F to unfreeze character", HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        }

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

        Ui.Layout.Size(w - rightBarWidth, topBarHeight).HorizontalLayout();
        Ui.StartGroup();
        {
            Ui.Layout.Size(100, topBarHeight);
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

            Ui.Layout.Size(100, topBarHeight);
            if (Ui.Button("Load"))
            {
                if (FileDialog.OpenFile(new[] { ("armor file", "armor"), ("All files", "*") }, out var chosenPath)
                    && !string.IsNullOrWhiteSpace(chosenPath))
                {
                    var loaded = ArmourDeserialiser.LoadFromPath(chosenPath);
                    if (loaded != null)
                    {
                        editor.CurrentPiecePath = chosenPath;
                        SetPiece(editor, loaded);
                    }
                }
            }

            if (editor.CurrentPiece != null)
            {
                Ui.Layout.Size(100, topBarHeight);
                if (Ui.Button("Save") && editor.CurrentPiece.Type != ArmourPieceType.Hand && editor.CurrentPiece != null)
                {
                    if (string.IsNullOrWhiteSpace(editor.CurrentPiecePath))
                    {
                        // We do this label and goto shit because the CurrentPiecePath is remembered, so that when Save is pressed, the file is overwritten.
                        // This behaviour matches basically every other program but does not match any other editor in this game.

                        // TODO: should we make every other editor behave this way? i dont know!! probably!!???

                        // UPDATE: the behaviour in the level editor is preferred:
                        // your save overwrites by default if you have a file currently open (no prompt, just a green flash if successful)
                        // if you hold shift, the button changes to say "Save as..." and opens a file dialog

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
            }

            if (editor.CurrentPiecePath != null)
            {
                Ui.Layout.Size(2900, topBarHeight);
                Ui.TextRect(editor.CurrentPiecePath, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            }
        }
        Ui.End();
    }

    private void DrawArmourEditorUi(ArmourEditorComponent editor)
    {
        var w = Window.Size.X;
        var h = Window.Size.Y;

        Ui.Layout.Size(rightBarWidth, h).StickRight(false).VerticalLayout();
        Ui.Theme.Foreground((Appearance)new Color(45, 45, 45)).Once();
        Ui.StartScrollView(true);
        {
            if (editor.CurrentPiece == null)
            {
                Ui.Layout.Size(100, 100);
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

        if (editor.CurrentPiece == null) // appease the nullability gods
            return;

        Ui.Spacer(5);

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Name", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                Ui.StringInputBox(ref editor.CurrentPiece.Name, default);
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Type", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                Ui.EnumDropdown(ref editor.CurrentPiece.Type);
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Scale", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                Ui.FloatStepper(ref editor.CurrentPiece.TextureScale, (0.01f, 10000), 0.05f);
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Deflect chance", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                Ui.FloatSlider(ref editor.CurrentPiece.DeflectChance, Direction.Horizontal, (0, 1), label: "{0:P0}");
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(60).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Offset (left)", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                VectorControl(ref editor.CurrentPiece.OffsetLeft, Input.IsKeyHeld(Key.LeftShift), true);
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(60).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.4f, 1, false);
                Ui.TextRect("Offset (right)", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(0.6f, 1, false);
                VectorControl(ref editor.CurrentPiece.OffsetRight, Input.IsKeyHeld(Key.LeftShift), false);
            }
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(1, 1, false);
                Ui.TextRect("Texture (left)", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(1, 1, false);
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
            Ui.End();
        }

        {
            Ui.Layout.FitWidth().Height(32).CenterHorizontal().EnqueueLayout(new FractionLayout(0.4f, 0.6f));
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(1, 1, false);
                Ui.TextRect("Texture (right)", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                Ui.Layout.FitContainer(1, 1, false);
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
            Ui.End();
        }

        Ui.Spacer(10);

        {
            var a = new Appearance(checkerboard ?? Textures.Black, ImageMode.Tiled);
            Ui.Layout.FitWidth().Height(160).CenterHorizontal().EnqueueLayout(new DistributeChildrenLayout());
            Ui.Theme.Foreground(a).Once();
            Ui.StartGroup(true);
            {
                var left = Textures.Transparent;
                var right = Textures.Transparent;

                if (editor.CurrentPiece.Left.IsValid && Assets.TryLoad<Texture>(editor.CurrentPiece.Left.Id, out var ll))
                    left = ll;
                if (editor.CurrentPiece.Right.IsValid && Assets.TryLoad<Texture>(editor.CurrentPiece.Right.Id, out var rr))
                    right = rr;

                Ui.Layout.FitContainer(1, 1, false);
                Ui.Image(left, ImageContainmentMode.Contain);
                Ui.Layout.FitContainer(1, 1, false);
                Ui.Image(right, ImageContainmentMode.Contain);
            }
            Ui.End();
        }
    }

    private static void VectorControl(ref Vector2 v, in bool hold, in bool inverse = false, [CallerLineNumber] int identity = 0)
    {
        float sign = inverse ? -1 : 1;
        bool h = hold;

        Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.3f)).Once();
        Ui.StartGroup(true, identity: identity);
        {
            Ui.Layout.FitContainer(1, 0.5f, false);
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.5f, 1, false).Scale(-2, 0);
                Ui.FloatInputBox(ref v.X);
                Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                Ui.FloatInputBox(ref v.Y);
            }
            Ui.End();

            Ui.Layout.FitContainer(1, 0.5f, false).Scale(0, -2).StickBottom(false).EnqueueLayout(new DistributeChildrenLayout());
            Ui.Theme.Foreground(default).Push();
            Ui.StartGroup(false);
            {
                Ui.Layout.FitHeight(false);
                if (Interpret(Ui.Button("<")))
                    v.X += -1 * sign;
                Ui.Layout.FitHeight(false);
                if (Interpret(Ui.Button(">")))
                    v.X += 1 * sign;
                Ui.Layout.FitHeight(false);
                if (Interpret(Ui.Button("^")))
                    v.Y -= 1;
                Ui.Layout.FitHeight(false);
                if (Interpret(Ui.Button("v")))
                    v.Y -= -1;
            }
            Ui.End();
            Ui.Theme.Pop();
        }
        Ui.End();

        bool Interpret(InteractionReport i) => h ? i.Held : i;
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
