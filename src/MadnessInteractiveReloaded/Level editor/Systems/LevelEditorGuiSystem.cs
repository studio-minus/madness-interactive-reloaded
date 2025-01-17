using MIR.LevelEditor.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Assets;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor;

/// <summary>
/// Process the <see cref="LevelEditor"/> GUI.
/// </summary>
public class LevelEditorGuiSystem : Walgelijk.System
{
    public delegate LevelObject CreateDelegate(LevelEditorComponent editor, Vector2 position);
    private readonly AssetBrowserControl browserControl = new();

    public readonly struct Creatable
    {
        public readonly string Name;
        public readonly CreateDelegate Creator;

        public Creatable(string name, CreateDelegate creator)
        {
            Name = name;
            Creator = creator;
        }
    }

    // TODO make sure to document this thing
    public static List<Creatable> Creatables =
    [
        new("Background", (editor, pos) => new Objects.Background(editor, AssetRef<Texture>.None, pos)),
        new("Wall (rect)", (editor, pos) => new RectWall(editor, new Rect(pos, new Vector2(256)))),
        new("Wall (line)", (editor, pos) => new LineWall(editor, pos - new Vector2(128), pos + new Vector2(128), 8)),
        new("Door", (editor, pos) => new Door(editor, new DoorProperties
        {
            TopLeft = pos + new Vector2(-130, 0),
            TopRight = pos + new Vector2(130, 0),
            BottomLeft = pos + new Vector2(-130, -300),
            BottomRight = pos + new Vector2(130, -300),
        })),
        new("Weapon", (editor, pos) => new LevelWeapon(editor, Registries.Weapons.GetRandomKey()) { Position = pos }),
        new("EnemySpawner", (editor, pos) => new EnemySpawner(editor) { Position = pos }),
        new("NPC", (editor, pos) => new NPC(editor,new NpcInstructions(
            Registries.Looks.GetRandomKey(),
            Registries.Stats.GetRandomKey(),
            "aahw") { BottomCenter = pos })),
        new("LevelProgressTrigger", (editor, pos) => new LevelProgressTrigger(editor, new Rect(pos, new Vector2(512)))),
        new("CutscenePlayer", (editor, pos) => new CutscenePlayer(editor) { Position = pos } ),
        new("AnimationTrigger", (editor, pos) => new AnimationTrigger(editor, new Rect(pos, new Vector2(512)))),
        new("CameraSection", (editor, pos) => new CameraSection(editor,new Rect(pos, new Vector2(512)))),
        new("DecalZone", (editor, pos) => new DecalZone(editor,new Rect(pos, new Vector2(256)))),
        new("TrainMovingBackground", (editor, pos) => new TrainMovingBackground(editor,new Rect(pos, new Vector2(256)))),
        new("PlayerPoster", (editor, pos) => new PlayerPoster(editor,PlayerPoster.PosterType.Ugly, pos)),
        new("FlipbookRect", (editor, pos) => new FlipbookRect(editor,pos)),
        new("DjScreen", (editor, pos) => new DjScreen(editor,new Rect(pos, new Vector2(256)))),
        new("GameSystem", (editor, pos) => new GameSystem(editor){ Position = pos }),
        new("Script", (editor, pos) => new LevelScript(editor, pos)),
        new("Improbability disk", (editor, pos) => new Disk(editor, pos)),
        new("Turret", (editor, pos) => new Turret(editor, pos)),
    ];

    public override void OnActivate()
    {
        Creatables = [.. Creatables.OrderBy(s => s.Name)];
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<LevelEditorComponent>(out var editor))
            return;

        // process editor ui
        if (editor.Level != null)
        {
            // add context menu
            if (!Onion.Navigator.HoverControl.HasValue && Input.IsKeyHeld(Key.LeftShift) && Input.IsKeyPressed(Key.A))
            {
                var ctx = new ContextMenu();
                ctx.Title = "Add";
                ctx.Position = Input.WindowMousePosition;
                ctx.Buttons = new (string, Action<LevelEditorComponent, Scene>)[Creatables.Count];
                for (int i = 0; i < Creatables.Count; i++)
                {
                    var c = Creatables[i];
                    ctx.Buttons[i] = (c.Name, (editor, scene) =>
                    {
                        if (editor.Level == null)
                            return;

                        editor.RegisterAction();

                        var created = c.Creator(editor, editor.ContextMenu.HasValue ? Window.WindowToWorldPoint(editor.ContextMenu.Value.Position) : GetCameraCenter());
                        editor.Level.Objects.Add(created);
                        editor.Dirty = true;
                    }
                    );
                }
                editor.ContextMenu = ctx;
            }

            Ui.Layout.FitWidth().Height(32).StickBottom().StickLeft();
            Ui.TextRect(editor.Level.Id, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);

            // Ui.Layout.Size(400, 250).Center().Resizable();
            // Ui.Theme.Foreground((Appearance)new Color(25, 25, 25, 250)).Once();
            // Ui.StartDragWindow("Select sound");
            // {
            //     Ui.Layout.FitContainer(1, 1, false);
            //     MadnessUi.ResourceBrowser();
            // }
            // Ui.End();
        }

        // task bar
        Ui.Layout.FitWidth(false).Height(32).HorizontalLayout();
        Ui.StartScrollView(true);
        {
            if (editor.Level != null)
            {
                Ui.Layout.FitHeight(true).Width(150).StickTop();
                if (Ui.Button("New"))
                {
                    var tag = new Tag(13460); // random number to identify the dialog box later

                    if (!Scene.TryGetEntityWithTag(tag, out _)) // do nothing if its already there
                    {
                        var e = Scene.CreateEntity();
                        Scene.SetTag(e, tag);
                        var a = Scene.AttachComponent(e, new ConfirmationDialogComponent("You will lose unsaved progress. Are you sure you want to create a new level?", () => NewLevel(editor)));
                    }
                }
            }

            Ui.Layout.FitHeight(true).Width(150).StickTop();
            if (Ui.Button("Load"))
                ImportLevel(editor);

            if (editor.Level != null)
            {
                Ui.Layout.FitHeight(true).Width(150).StickTop();
                if (Ui.Button(Input.IsKeyHeld(Key.LeftShift) ? "Save as..." : "Save"))
                    try
                    {
                        ExportLevel(editor);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                        Scene.AttachComponent(Scene.CreateEntity(), new ConfirmationDialogComponent("Failed to save level", e.Message + "\nTry again?", () => ExportLevel(editor)));
                    }

                Ui.Layout.FitHeight(true).Width(150).StickTop();
                Ui.Theme.Foreground((Appearance)Colors.White).Text(new StateDependent<Color>(Colors.Black, Colors.Red)).Once();
                if (Ui.Button("Test"))
                    MadnessUtils.TransitionScene(game => LevelEditorTestScene.Create(Game.Main, editor.Level));
            }

            Ui.Layout.FitHeight(true).Width(150).StickTop();
            if (Ui.Button("Configure level"))
                editor.LevelSettingsMenuOpen = !editor.LevelSettingsMenuOpen;
        }
        Ui.End();

        // sidebar
        Ui.Layout.FitHeight(false).Scale(0, -32).Width(300).StickBottom(false).StickRight(false);
        Ui.Theme.Foreground((Appearance)new Color(25, 25, 25, 250)).Once();
        Ui.StartScrollView(true);
        {
            // outliner
            Ui.Layout.FitContainer(1, 0.3f, false).VerticalLayout();
            Ui.StartScrollView(true);
            {
                DrawOutliner(editor);
            }
            Ui.End();

            if (editor.SelectionManager.SelectedObject != null)
            {
                Ui.Layout.FitContainer(0.9f, 0.7f, true).CenterHorizontal().StickBottom().VerticalLayout().Overflow(false, true);
                Ui.StartScrollView();
                {
                    Ui.Spacer(8);
                    Ui.Theme.FontSize(23).Once();
                    Ui.Label(editor.SelectionManager.SelectedObject.GetType().Name);
                    Ui.Spacer(8);
                    if (editor.SelectionManager.SelectedObject is ITagged tt)
                    {
                        Ui.Layout.FitWidth(false).Height(32);
                        if (tt.Tag.HasValue)
                        {
                            if (Ui.Button("Remove tag"))
                                tt.Tag = null;
                            else
                            {
                                var tag = tt.Tag.Value;
                                Ui.Layout.FitWidth(false).Height(32);
                                if (Ui.IntInputBox(ref tag.Value))
                                    tt.Tag = tag;
                            }
                        }
                        else
                        {
                            if (Ui.Button("Add tag"))
                                tt.Tag = new();
                        }
                    }
                    var obj = editor.SelectionManager.SelectedObject;
                    obj.ProcessPropertyUi();
                }
                Ui.End();
            }
        }
        Ui.End();

        // process context menu
        int contextMenuId = -1;
        if (editor.ContextMenu != null && editor.ContextMenu.Value.Buttons != null)
        {
            Ui.Layout.MoveAbs(editor.ContextMenu.Value.Position.X, editor.ContextMenu.Value.Position.Y).Size(200, 300).VerticalLayout();
            //Ui.Theme.ShowScrollbars(false).Once();
            Ui.StartScrollView(true);
            contextMenuId = Onion.Tree.CurrentNode?.Identity ?? -1;
            if (!string.IsNullOrEmpty(editor.ContextMenu.Value.Title))
            {
                Ui.Layout.FitWidth().Height(32);
                Ui.Theme.Text(Colors.White.WithAlpha(0.5f)).Once();
                Ui.TextRect(editor.ContextMenu.Value.Title, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            }
            foreach (var button in editor.ContextMenu.Value.Buttons)
            {
                Ui.Layout.FitWidth(true).Height(32);
                if (Ui.HoldButton(button.label, identity: button.GetHashCode()))
                {
                    button.action(editor, Scene);
                    editor.ContextMenu = null;
                }
            }
            Ui.End();

        }

        if (Input.IsKeyPressed(Key.Escape) || (Input.AnyMouseButton && !Onion.Navigator.RaycastAll(Input.WindowMousePosition.X, Input.WindowMousePosition.Y, CaptureFlags.All).Contains(contextMenuId)))
            editor.ContextMenu = null;

        if (editor.AutospawnMenuOpen && editor.Level != null)
        {
            Ui.Layout.Size(300, 400).Center().Resizable();
            Ui.Theme.Background((Appearance)new Color(25, 25, 25)).Once();
            Ui.StartDragWindow("Autospawn settings", ref editor.AutospawnMenuOpen);
            {
                Ui.Layout.FitContainer().StickLeft().StickTop().VerticalLayout();
                Ui.StartScrollView();
                {
                    Ui.Label("Enemy spawn interval");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.FloatSlider(ref editor.Level.EnemySpawnInterval, Direction.Horizontal, (0, 10), 0.1f, "{0:0.0} seconds");

                    Ui.Label("Max enemy count");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.IntStepper(ref editor.Level.MaxEnemyCount, (0, 50));

                    Ui.Spacer(8);

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Button("Add new spawn instructions"))
                        editor.Level.EnemySpawnInstructions.Add(new EnemySpawnInstructions("grunt", "grunt", "aahw")); // TODO what if grunt does not exist?

                    for (int i = 0; i < editor.Level.EnemySpawnInstructions.Count; i++)
                    {
                        var item = editor.Level.EnemySpawnInstructions[i];
                        Ui.Layout.FitWidth().Height(108).StickLeft();
                        Ui.Theme.Foreground((Appearance)Colors.Black.WithAlpha(0.5f)).Once();
                        Ui.StartGroup(true, i);
                        {
                            Ui.Layout.FitContainer(0.8f, null).Height(32).StickLeft().StickTop();
                            int ii = Array.IndexOf(editor.Stats, item.StatsKey);
                            Ui.Decorators.Tooltip("NPC stats");
                            if (Ui.Dropdown(editor.Stats, ref ii))
                                item.StatsKey = editor.Stats[ii];
                            Ui.Decorators.Clear(); // TODO why is this necessary

                            Ui.Layout.FitContainer(0.8f, null).Height(32).StickLeft().StickTop().Move(0, 35);
                            ii = Array.IndexOf(editor.Looks, item.LookKey);
                            Ui.Decorators.Tooltip("NPC look");
                            if (Ui.Dropdown(editor.Looks, ref ii))
                                item.LookKey = editor.Looks[ii];

                            Ui.Layout.FitContainer(0.8f, null).Height(32).StickLeft().StickTop().Move(0, 35 * 2);
                            ii = Array.IndexOf(editor.Factions, item.FactionKey);
                            Ui.Decorators.Tooltip("NPC faction");
                            if (Ui.Dropdown(editor.Factions, ref ii))
                                item.FactionKey = editor.Factions[ii];

                            Ui.Layout.Size(32, 32).StickRight().StickTop();
                            Ui.Theme.Text(Colors.Red).Once();
                            Ui.Decorators.Tooltip("Remove");
                            if (Ui.ImageButton(BuiltInAssets.Icons.Exit, ImageContainmentMode.Center))
                            {
                                editor.Level.EnemySpawnInstructions.RemoveAt(i);
                                Ui.End();
                                break;
                            }
                        }
                        Ui.End();
                    }
                }
                Ui.End();
            }
            Ui.End();
        }

        if (editor.WeaponSpawnMenuOpen && editor.Level != null)
        {
            Ui.Layout.Size(300, 400).Center().Resizable();
            Ui.Theme.Background((Appearance)new Color(25, 25, 25)).Once();
            Ui.StartDragWindow("Weapon spawn settings", ref editor.WeaponSpawnMenuOpen);
            {
                Ui.Layout.FitContainer().StickLeft().StickTop().VerticalLayout();
                Ui.StartScrollView();
                {
                    Ui.Label("Enemy weapon chance");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.FloatSlider(ref editor.Level.WeaponChance, Direction.Horizontal, (0, 1), 0.01f, "{0:P0}");
                    Ui.Spacer(8);


                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Button("Add new weapon"))
                    {
                        if (editor.Level.Weapons == null)
                            editor.Level.Weapons = new();
                        else
                            editor.Level.Weapons.Add(Registries.Weapons.GetRandomKey());
                    }

                    if (editor.Level.Weapons != null)
                        for (int i = 0; i < editor.Level.Weapons.Count; i++)
                        {
                            var item = editor.Level.Weapons[i];
                            Ui.Layout.FitWidth().Height(42).StickLeft();
                            Ui.Theme.Foreground((Appearance)Colors.Black.WithAlpha(0.5f)).Once();
                            if (!Registries.Weapons.Has(editor.Level.Weapons[i]))
                                Ui.Theme.Foreground((Appearance)Colors.Red).Once();
                            Ui.StartGroup(true, i);
                            {
                                Ui.Layout.FitContainer().Scale(-32, 0).StickLeft().StickTop();
                                if (Ui.StringInputBox(ref item, TextBoxOptions.TextInput))
                                {
                                    editor.Level.Weapons[i] = item;
                                }

                                Ui.Layout.Width(32).FitHeight().StickRight().StickTop();
                                Ui.Theme.Text(Colors.Red).Once();
                                Ui.Decorators.Tooltip("Remove");
                                if (Ui.Button("X"))
                                {
                                    editor.Level.Weapons.RemoveAt(i);
                                    Ui.End();
                                    break;
                                }
                            }
                            Ui.End();
                        }
                }
                Ui.End();
            }
            Ui.End();
        }

        if (editor.LevelSettingsMenuOpen && editor.Level != null)
        {
            Ui.Layout.Size(300, 400).Center().Resizable();
            Ui.Theme.Background((Appearance)new Color(25, 25, 25)).Once();
            Ui.StartDragWindow("Level settings", ref editor.LevelSettingsMenuOpen);
            {
                Ui.Layout.FitContainer().StickLeft().StickTop().VerticalLayout();
                Ui.StartScrollView();
                {
                    Ui.Label("Level type");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.EnumDropdown(ref editor.Level.LevelType);
                    Ui.Spacer(8);

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.Checkbox(ref editor.Level.FullZoom, "Full zoom");
                    Ui.Spacer(8);

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.Checkbox(ref editor.Level.OpeningTransition, "Opening transition");
                    Ui.Spacer(8);

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    Ui.Checkbox(ref editor.Level.ExitingTransition, "Exiting transition");
                    Ui.Spacer(8);

                    if (editor.Level.LevelType is LevelType.Campaign)
                    {
                        Ui.Label("Progression behaviour");
                        Ui.Layout.FitWidth().Height(32).StickLeft();
                        Ui.EnumDropdown(ref editor.Level.ProgressionType);
                        Ui.Spacer(8);

                        if (editor.Level.ProgressionType is ProgressionType.BodyCount)
                        {
                            Ui.Label("Winning body count");
                            Ui.Layout.FitWidth().Height(32).StickLeft();
                            Ui.IntStepper(ref editor.Level.BodyCountToWin, (0, int.MaxValue));
                            Ui.Spacer(8);
                        }

                        if (editor.Level.ProgressionType is ProgressionType.Time)
                        {
                            Ui.Label("Level duration in seconds");
                            Ui.Layout.FitWidth().Height(32).StickLeft();
                            Ui.FloatStepper(ref editor.Level.TimeLimitInSeconds, (0, int.MaxValue));
                            Ui.Spacer(8);
                        }
                    }

                    Ui.Label("Music");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    MadnessUi.AssetPicker(
                        editor.Level.BackgroundMusic.Id,
                        c => editor.Level.BackgroundMusic = new(c),
                        static c => c.MimeType.Contains("audio"));

                    Ui.Label("Thumbnail");
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    MadnessUi.AssetPicker(
                        editor.Level.Thumbnail.Id,
                        c => editor.Level.Thumbnail = new(c),
                        static c => c.MimeType.Contains("image"));
                    if (editor.Level.Thumbnail.IsValid && Assets.HasAsset(editor.Level.Thumbnail.Id))
                    {
                        Ui.Layout.FitWidth().CenterHorizontal().Height(100);
                        Ui.Image(editor.Level.Thumbnail.Value, ImageContainmentMode.Contain);
                    }

                    Ui.Spacer(8);

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Button("Autospawn settings"))
                        editor.AutospawnMenuOpen = !editor.AutospawnMenuOpen;

                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Button("Weapon spawn settings"))
                        editor.WeaponSpawnMenuOpen = !editor.WeaponSpawnMenuOpen;
                }
                Ui.End();
            }
            Ui.End();
        }

        if (editor.Level != null)
        {
            Draw.Reset();
            Draw.ScreenSpace = false;
            Draw.Colour = Colors.Transparent;
            Draw.OutlineColour = Colors.Orange;
            Draw.OutlineWidth = editor.PixelSize * 1;
            Draw.Quad(editor.Level.LevelBounds);
        }
    }

    private static void DrawOutliner(LevelEditorComponent editor)
    {
        switch (editor.OutlinerMode)
        {
            case OutlinerMode.ByType:
                int i = 0;
                foreach (var item in editor.Filter.Keys)
                {
                    var a = editor.Filter[item];
                    if (editor.Level != null && editor.Level.Objects.Any(obj => item.IsInstanceOfType(obj)))
                    {
                        Ui.Layout.FitWidth(false).Height(32);
                        Ui.StartGroup(false, i);
                        {
                            Ui.Layout.FitContainer().Scale(-64, 0).StickLeft().StickTop();
                            Ui.TextRect(item.Name, HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                            Ui.Layout.Width(32).FitHeight(false).CenterVertical().StickRight();
                            if (Ui.ImageButton((a.Selectable ? Textures.UserInterface.Unlocked : Textures.UserInterface.Locked).Value, ImageContainmentMode.Center))
                            {
                                a.Selectable = !a.Selectable;
                                editor.Filter[item] = a;
                                editor.Dirty = true;
                            }

                            Ui.Layout.Width(32).FitHeight(false).CenterVertical().StickRight().Move(-32, 0);
                            if (Ui.ImageButton((a.Visible ? Textures.UserInterface.EyeOn : Textures.UserInterface.EyeOff).Value, ImageContainmentMode.Center))
                            {
                                a.Visible = !a.Visible;
                                editor.Filter[item] = a;
                                editor.Dirty = true;
                            }
                        }
                        Ui.End();
                    }
                    i++;
                }
                break;
            case OutlinerMode.ByObject:
                if (editor.Level != null)
                {
                    i = 0;
                    foreach (var item in editor.Level.Objects)
                    {
                        Ui.Layout.FitWidth(false).Height(32);
                        Ui.StartGroup(false, i);
                        {
                            Ui.Layout.FitContainer().Scale(-64, 0).StickLeft().StickTop();
                            Ui.TextRect(item.GetType().Name, HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                            Ui.Layout.Width(32).FitHeight(false).CenterVertical().StickRight();
                            if (Ui.ImageButton((item.Disabled ? Textures.UserInterface.Locked : Textures.UserInterface.Unlocked).Value, ImageContainmentMode.Center))
                            {
                                //item.Disabled = !item.Disabled;
                                editor.Dirty = true;
                            }
                        }
                        Ui.End();
                        i++;
                    }
                }
                break;
        }
    }

    public override void PreRender()
    {
        if (!Scene.FindAnyComponent<CameraComponent>(out var camera) || !Scene.TryGetComponentFrom<TransformComponent>(camera.Entity, out var cameraTransform))
            return;

        if (!Scene.FindAnyComponent<LevelEditorComponent>(out var editor))
            return;

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrder.CameraOperations.WithOrder(1000);
        Draw.Material = Materials.EditorBackground;
        Materials.EditorBackground.SetUniform("windowSize", Window.Size);
        Materials.EditorBackground.SetUniform("cameraTransform", new Vector3(cameraTransform.Position, camera.OrthographicSize));
        Draw.Quad(Vector2.Zero, Window.Size);

        Draw.Reset();
    }

    private void NewLevel(LevelEditorComponent editor)
    {
        editor.Level = new Level() { BackgroundMusic = AssetRef<StreamAudioData>.None };
        editor.Level.Objects.Add(new Objects.PlayerSpawn(editor));
        editor.Dirty = true;
    }

    private Vector2 GetCameraCenter()
    {
        if (Scene.FindAnyComponent<CameraComponent>(out var camera))
            return Scene.GetComponentFrom<TransformComponent>(camera.Entity).Position;
        return default;
    }

    private void ImportLevel(LevelEditorComponent editor)
    {
        if (FileDialog.OpenFile(new[] { ("JSON file", "json"), ("All files", "*") }, out var chosenPath))
            editor.ImportLevel(chosenPath ?? throw new Exception("chosen path cannot be null"));
    }

    private void ExportLevel(LevelEditorComponent editor)
    {
        if (editor.Level == null)
            throw new Exception("Current editor level is null, which can't be exported");

        if (!Input.IsKeyHeld(Key.LeftShift))
            if (editor.FileName != null)
            {
                var e = Scene.CreateEntity();
                var a = Scene.AttachComponent(e, new ConfirmationDialogComponent($"You are about to overwrite \"{Path.GetFileName(editor.FileName)}\"", () =>
                {
                    save(editor.FileName);
                }));
                return;
            }

        saveAs();

        void saveAs()
        {
            try
            {
                if (FileDialog.SaveFile(new[] { ("JSON file", "json"), ("All files", "*") }, null, null, out var chosenPath))
                    save(chosenPath);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MadnessUtils.Flash(Colors.Red, 0.5f);
                Audio.PlayOnce(Sounds.UiBad);
            }
        }

        void save(string chosenPath)
        {
            try
            {
                editor.ExportLevel(chosenPath ?? throw new Exception("chosen path cannot be null"));
                Audio.PlayOnce(Sounds.UiConfirm);
                MadnessUtils.Flash(Colors.Green.WithAlpha(0.2f), 0.25f);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MadnessUtils.Flash(Colors.Red, 0.5f);
                Audio.PlayOnce(Sounds.UiBad);
            }
        }
    }
}
