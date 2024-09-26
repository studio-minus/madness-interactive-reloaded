using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk.Onion.Controls;
using System;

using System.Linq;
using static MIR.ExperimentModeComponent;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Icons = MIR.Textures.UserInterface.ExperimentMode.MusicPlayer;
using OpenTK.Graphics.ES20;
using Walgelijk.Onion.Animations;
using MIR.Controls;
using Walgelijk.Onion.Layout;

namespace MIR;

/// <summary>
/// Manages the sandbox experiment mode.
/// </summary>
public class ExperimentModeSystem : Walgelijk.System
{
    public const float MenuWidth = 300;
    public const float TabBarHeight = 60;
    public const float ControlBlockHeight = 256;

    private Vector2 worldMenuPos;
    private float cameraZoom = default;

    public override void OnActivate()
    {
        if (Scene.FindAnyComponent<ExperimentModeComponent>(out var exp))
        {
            exp.WeaponFilter = ExperimentModePersistentData.CurrentFilter;
            exp.SelectedFaction = ExperimentModePersistentData.SelectedFaction;

            //if (exp.SortedWeaponCache.Length == 0)
            exp.SortedWeaponCache = [.. Registries.Weapons.GetAllKeys().OrderBy(key => {
                var wpn = Registries.Weapons[key];
                return wpn.WeaponData.WeaponType + wpn.WeaponData.Name;
            })];

            exp.FactionCache = [.. Registries.Factions.GetAllKeys().Select(id => new FactionOption(id))];
        }

        if (Scene.FindAnyComponent<EnemySpawningComponent>(out var sp))
        {
            var old = ExperimentModePersistentData.SpawningComponent;
            if (old != null)
            {
                sp.SpawnInstructions = [.. old.SpawnInstructions];
                sp.Enabled = old.Enabled;
                sp.Interval = old.Interval;
                sp.WeaponChance = old.WeaponChance;
                sp.WeaponsToSpawnWith = [.. (old.WeaponsToSpawnWith ?? [])];
                sp.MaxEnemyCount = old.MaxEnemyCount;
            }
        }

        AiCharacterSystem.AutoSpawn = ExperimentModePersistentData.AutoSpawn;
    }

    public override void OnDeactivate()
    {
        if (Scene.FindAnyComponent<ExperimentModeComponent>(out var exp))
        {
            ExperimentModePersistentData.CurrentFilter = exp.WeaponFilter;
            ExperimentModePersistentData.SelectedFaction = exp.SelectedFaction;
        }

        if (Scene.FindAnyComponent<EnemySpawningComponent>(out var sp))
            ExperimentModePersistentData.SpawningComponent = sp.Clone() as EnemySpawningComponent;

        ExperimentModePersistentData.AutoSpawn = AiCharacterSystem.AutoSpawn;
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene))
            return;

        if (!Scene.FindAnyComponent<ExperimentModeComponent>(out var exp))
            return;

        if (!Scene.FindAnyComponent<PlayerComponent>(out var player) ||
            !Scene.TryGetComponentFrom<CharacterComponent>(player.Entity, out var playerCharacter) || playerCharacter == null)
            return;

        // (duston): Set this here because we want the campaign's AI disabled parameter completely split from this
        // while also having a persistent experiment mode parameter so it's not reset every time you go back to exp mode.
        // But it also needs to change when the persistentdata's paramter changes :)
        AiCharacterSystem.DisableAI = ExperimentModePersistentData.AIDisabled;

        if (Input.IsKeyReleased(Key.Tab) && playerCharacter.IsAlive)
            if (exp.IsEditMode)
                ExitEditMode(exp);
            else
                EnterEditMode(exp);

        if (playerCharacter.IsAlive)
            ProcessUi(exp);

        if (exp.IsEditMode)
        {
            exp.TimeSinceMenuOpened += Time.DeltaTimeUnscaled;
            if (Input.IsButtonReleased(MouseButton.Left) && exp.CurrentlyPlacing.HasValue)
            {
                if (!Onion.Navigator.HoverControl.HasValue)
                {
                    exp.CurrentlyPlacing?.SpawnFunction?.Invoke(Scene, Input.WorldMousePosition);
                    exp.CopySceneToSelectionManager(Scene);
                }
                exp.CurrentlyPlacing = null;
            }

            if (!Onion.Navigator.IsBeingUsed)
            {
                // update selection
                {
                    ISelectable? selected = exp.SelectionManager.SelectedObject;
                    exp.SelectionManager.UpdateState(Input.WorldMousePosition, Input.IsButtonPressed(MouseButton.Left));
                    if (selected != exp.SelectionManager.SelectedObject)
                        worldMenuPos = new Vector2(float.NaN);
                }

                if (exp.SelectionManager.SelectedObject != null)
                {
                    float rotateSpeed = 180 * Time.DeltaTime;

                    // dragging
                    if (Input.IsButtonHeld(MouseButton.Left))
                    {
                        worldMenuPos = new Vector2(float.NaN);
                        exp.DrawPropertyMenu = false;
                        exp.SelectionManager.SelectedObject.Translate(Scene, Input.WorldMouseDelta);

                        // rotating
                        if (Input.IsKeyHeld(Key.A))
                            exp.SelectionManager.SelectedObject.Rotate(Scene, rotateSpeed);
                        else if (Input.IsKeyHeld(Key.D))
                            exp.SelectionManager.SelectedObject.Rotate(Scene, -rotateSpeed);
                    }
                }
            }

            if (Input.IsButtonReleased(MouseButton.Left))
            {
                if (exp.SelectionManager.SelectedObject != null)
                    exp.SelectionManager.SelectedObject.OnDrop(Scene);
                exp.DrawPropertyMenu = true;
            }

            DrawWorldSelectionMenus(exp);
        }
        else
        {
            exp.TimeSinceMenuOpened = 0;
            exp.CurrentlyPlacing = null;
            if (Input.IsKeyReleased(Key.R) && !playerCharacter.IsAlive)
                MadnessCommands.Revive();
        }
    }

    private void ProcessObjectMenu(ExperimentModeComponent exp, IExperimentSelectable obj, int i)
    {
        Draw.Reset();
        Draw.ScreenSpace = false;
        Draw.Colour = Colors.Red;

        const float selectExpand = 15;
        float pixel = 1;
        if (Scene.FindAnyComponent<CameraComponent>(out var camera))
            pixel = camera.OrthographicSize;

        var offset = Utilities.Hash(i * 0.6523f);
        var selected = exp.SelectionManager.IsSelected(obj);

        if (selected)
        {
            Draw.Colour = Colors.Red.WithAlpha(0.2f);
            Draw.OutlineWidth = pixel * 4;
            var pos = Window.WorldToWindowPoint(obj.BoundingBox.TopRight + new Vector2(selectExpand));

            if (float.IsNaN(worldMenuPos.X))
                worldMenuPos = pos;
            else
                worldMenuPos = Utilities.SmoothApproach(worldMenuPos, pos, 15, Time.DeltaTime);
            pos = worldMenuPos;
            if (exp.DrawPropertyMenu)
            {
                Ui.Theme.Base.Font = Fonts.CascadiaMono;
                Ui.Layout.Size(200, obj.UiHeight).FitContent().Width(200).MaxHeight(512).Scale(0, 4).MoveAbs(pos.X, pos.Y).VerticalLayout();
                Ui.Theme.Foreground((Appearance)Colors.Black.WithAlpha(0.6f)).OutlineColour(Colors.Red).OutlineWidth(1).Once();
                Ui.StartScrollView(true, obj.Entity);
                {
                    Ui.Layout.StickLeft();
                    Ui.Theme.FontSize(20).Once();
                    Ui.Label(obj.Name);
                    Ui.Spacer(8);

                    obj.ProcessUi(Scene, exp);

                    Draw.OutlineColour = Colors.Red;
                    Draw.Quad(obj.BoundingBox.Expand(selectExpand));
                }
                Ui.End();
                Ui.Theme.Base.Font = Fonts.Oxanium;
            }
        }
        else
        {
            Draw.Order = RenderOrders.Default;
            Draw.Colour = obj.Disabled ? Colors.Black : (exp.SelectionManager.IsHovering(obj) ? Colors.White : Colors.Red);
            DrawCrosshairEffect(obj.BoundingBox, 3 * pixel, pixel * 15, float.Clamp(exp.TimeSinceMenuOpened * 4 - offset * 3, 0, 1), i);
        }
    }

    private void DrawWorldSelectionMenus(ExperimentModeComponent exp)
    {
        int index = 0;
        foreach (var item in exp.SelectionManager.Selectables)
            ProcessObjectMenu(exp, item, index++);
    }

    private void DrawCrosshairEffect(Rect r, float w, float e, float animationTime, int i)
    {
        e = float.Min(e, r.Width);
        e = float.Min(e, r.Height);

        Draw.Order = RenderOrders.Effects;
        Draw.OutlineWidth = 0;

        var windowWorld = Window.WorldBounds;
        var center = r.GetCenter();

        // TODO this is not very performant
        Draw.Line(r.TopLeft, new Vector2(r.TopLeft.X + e, r.TopLeft.Y), w, 0);
        Draw.Line(r.TopLeft, new Vector2(r.TopLeft.X, r.TopLeft.Y - e), w, 0);

        Draw.Line(r.TopRight, new Vector2(r.TopRight.X - e, r.TopRight.Y), w, 0);
        Draw.Line(r.TopRight, new Vector2(r.TopRight.X, r.TopRight.Y - e), w, 0);

        Draw.Line(r.BottomLeft, new Vector2(r.BottomLeft.X + e, r.BottomLeft.Y), w, 0);
        Draw.Line(r.BottomLeft, new Vector2(r.BottomLeft.X, r.BottomLeft.Y + e), w, 0);

        Draw.Line(r.BottomRight, new Vector2(r.BottomRight.X - e, r.BottomRight.Y), w, 0);
        Draw.Line(r.BottomRight, new Vector2(r.BottomRight.X, r.BottomRight.Y + e), w, 0);

        Draw.Order = RenderOrders.CharacterLower.OffsetLayer(-1);
        Draw.Colour.A = 0.5f;

        if (i % 2 == 0)
            Draw.Line(new Vector2(center.X, windowWorld.MaxY), new Vector2(center.X, float.Lerp(windowWorld.MaxY, r.MaxY, animationTime)), w, 0);
        else
            Draw.Line(new Vector2(windowWorld.MaxX, center.Y), new Vector2(float.Lerp(windowWorld.MaxX, r.MaxX, animationTime), center.Y), w, 0);

        if (i % 3 == 1)
            Draw.Line(new Vector2(windowWorld.MinX, center.Y), new Vector2(float.Lerp(windowWorld.MinX, r.MinX, animationTime), center.Y), w, 0);
        else
            Draw.Line(new Vector2(center.X, windowWorld.MinY), new Vector2(center.X, float.Lerp(windowWorld.MinY, r.MinY, animationTime)), w, 0);
    }

    public override void Render()
    {
        if (!Scene.FindAnyComponent<ExperimentModeComponent>(out var exp))
            return;

        if (exp.CurrentlyPlacing.HasValue)
        {
            var p = exp.CurrentlyPlacing.Value;
            var r = new Rect(Input.WorldMousePosition, p.DraggingTexture.Size);
            Draw.ResetDrawBounds();
            Draw.ResetMaterial();
            Draw.ScreenSpace = false;
            Draw.Order = RenderOrders.UserInterface.WithOrder(5);
            Draw.Texture = p.DraggingTexture;
            Draw.Colour = Colors.White.WithAlpha(0.5f);
            Draw.Quad(r.TopLeft, r.GetSize());
        }
    }

    private void ProcessUi(ExperimentModeComponent exp)
    {
        const float h = 32;

        if (exp.IsEditMode)
        {
            Ui.Theme.Once();
            Ui.Layout.StickLeft(false).StickTop(false).FitHeight(false).Width(MenuWidth);
            Ui.StartGroup();
            {
                Ui.Layout.StickLeft().StickTop().FitWidth().Height(60).HorizontalLayout();
                Ui.StartScrollView();
                {
                    tabButton(Localisation.Get("experiment-tab-characters"), Tab.Characters, Textures.UserInterface.ExperimentMode.CharactersIcon);
                    tabButton(Localisation.Get("experiment-tab-weapons"), Tab.Weapons, Textures.UserInterface.ExperimentMode.WeaponsIcon);
                    tabButton(Localisation.Get("experiment-tab-music"), Tab.Music, Textures.UserInterface.ExperimentMode.MusicIcon);
                    tabButton(Localisation.Get("experiment-tab-game-rules"), Tab.GameRules, Textures.UserInterface.ExperimentMode.GameRulesIcon);
                    //tabButton("Settings", Tab.Settings, Textures.UserInterface.ExperimentMode.SettingsIcon);

                    void tabButton(in string name, Tab tab, in AssetRef<Texture> texture)
                    {
                        Ui.Decorators.Tooltip(name);
                        Ui.Layout.FitHeight().AspectRatio(1).StickLeft().StickTop();
                        Ui.Decorate(new FancyButtonDecorator());
                        if (exp.CurrentTab == tab)
                            Ui.Decorate(new CrosshairDecorator());
                        if (Ui.ClickImageButton(texture.Value, identity: tab.GetHashCode()))
                            exp.CurrentTab = tab;
                        Ui.Spacer(15, identity: tab.GetHashCode());
                    }
                }
                Ui.End();

                Ui.Layout.FitWidth().CenterHorizontal().Height(Window.Height - TabBarHeight - Ui.Theme.Base.Padding * 2).StickBottom();
                Ui.StartGroup(false);
                switch (exp.CurrentTab)
                {
                    case Tab.Characters:
                        ProcessCharacterTab(exp, h);
                        break;
                    case Tab.Weapons:
                        ProcessWeaponsTab(exp, h);
                        break;
                    case Tab.GameRules:
                        ProcessGameRulesTab(exp, h);
                        break;
                    case Tab.Music:
                        ProcessMusicTab(exp, h);
                        break;
                    case Tab.Settings:
                        break;
                }
                Ui.End();
            }
            Ui.End();

            if (AiCharacterSystem.AutoSpawn && exp.AutoSpawnSettingsOpen && Scene.FindAnyComponent<EnemySpawningComponent>(out var spawning))
                ProcessAutospawnWindow(exp, spawning);

            if (exp.ImprobabilityDisksOpen)
                ProcessModifiersWindow(exp);

            if (exp.ActivePresetEditor != null)
            {
                Ui.Layout.Size(300, 400).Center();
                Ui.Theme.Background((Appearance)Colors.Red.WithAlpha(0.2f)).OutlineWidth(1).OutlineColour(new(Colors.Red, Colors.White)).Text(Colors.White).Once();
                bool isOpen = true;
                Ui.StartDragWindow(exp.ActivePresetEditor.Name ?? Localisation.Get("experiment-preset-editor"), ref isOpen);
                {
                    // tab bar
                    Ui.Layout.FitWidth(false).Height(40).EnqueueLayout(new DistributeChildrenLayout());
                    Ui.StartGroup(false);
                    {
                        if (exp.PresetEditorTab != 0)
                            Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.5f)).Once();
                        Ui.Layout.FitContainer(1, 1, false).Inflate(0, -5).StickBottom(false);
                        if (Ui.Button(Localisation.Get("experiment-look")))
                            exp.PresetEditorTab = 0;

                        if (exp.PresetEditorTab != 1)
                            Ui.Theme.ForegroundColor(Colors.Black.WithAlpha(0.5f)).Once();
                        Ui.Layout.FitContainer(1, 1, false).Inflate(0, -5).StickBottom(false);
                        if (Ui.Button(Localisation.Get("experiment-stats")))
                            exp.PresetEditorTab = 1;
                    }
                    Ui.End();

                    // things
                    Ui.Layout.FitContainer(1, 1, false).Scale(0, -40).StickBottom(false).VerticalLayout();
                    Ui.StartScrollView(true);
                    {
                        bool changed = false;
                        var preset = exp.ActivePresetEditor;
                        preset.Name ??= string.Empty;
                        switch (exp.PresetEditorTab)
                        {
                            case 0:
                                {
                                    //Ui.Layout.FitContainer().Center().Inflate(-10);
                                    //Ui.TextRect("how am i supposed to display this much information and so many settings in this tiny little window??", default, default);
                                    Ui.Label("Base layer", HorizontalTextAlign.Center);
                                    Ui.Layout.FitWidth().Height(100).CenterHorizontal().VerticalLayout();
                                    Ui.StartScrollView(false);
                                    {
                                        foreach (var k in Registries.Armour.Head.GetAllKeys())
                                        {
                                            var a = Registries.Armour.Head[k];
                                            Ui.Layout.FitWidth(false).Height(30);
                                            Ui.Theme.Font(Fonts.CascadiaMono).Once();
                                            if (LeftAlignedButton.Start(a.Name, a.GetHashCode()))
                                            {
                                                preset.Look.Head = a;
                                                changed = true;
                                            }
                                        }
                                    }
                                    Ui.End();

                                    Ui.Spacer(10);
                                    Ui.Label("Layer 1", HorizontalTextAlign.Center);
                                    Ui.Layout.FitWidth().Height(100).CenterHorizontal().VerticalLayout();
                                    Ui.StartScrollView(false);
                                    {
                                        foreach (var k in Registries.Armour.HeadAccessory.GetAllKeys())
                                        {
                                            var a = Registries.Armour.HeadAccessory[k];
                                            Ui.Layout.FitWidth(false).Height(30);
                                            Ui.Theme.Font(Fonts.CascadiaMono).Once();
                                            if (LeftAlignedButton.Start(a.Name, a.GetHashCode()))
                                            {
                                                preset.Look.HeadLayer1 = a;
                                                changed = true;
                                            }
                                        }
                                    }
                                    Ui.End();

                                    Ui.Spacer(10);
                                    Ui.Label("Layer 2", HorizontalTextAlign.Center);
                                    Ui.Layout.FitWidth().Height(100).CenterHorizontal().VerticalLayout();
                                    Ui.StartScrollView(false);
                                    {
                                        foreach (var k in Registries.Armour.HeadAccessory.GetAllKeys())
                                        {
                                            var a = Registries.Armour.HeadAccessory[k];
                                            Ui.Layout.FitWidth(false).Height(30);
                                            Ui.Theme.Font(Fonts.CascadiaMono).Once();
                                            if (preset.Look.HeadLayer2 == a)
                                                Ui.Decorate(new CrosshairDecorator());
                                            if (LeftAlignedButton.Start(a.Name, a.GetHashCode()))
                                            {
                                                preset.Look.HeadLayer2 = a;
                                                changed = true;
                                            }
                                        }
                                    }
                                    Ui.End();
                                }
                                break;
                            case 1:
                                changed |= StatsEditor(preset);
                                break;
                        }

                        if (changed)
                        {
                            updateAllRelevantCharacters();
                            preset.SaveChanges();
                        }
                    }
                    Ui.End();
                }
                Ui.End();

                if (!isOpen)
                    exp.ActivePresetEditor = null;

                void updateAllRelevantCharacters()
                {
                    foreach (var presetCharacters in Scene.GetAllComponentsOfType<CharacterPresetComponent>())
                        if (presetCharacters.Preset == exp.ActivePresetEditor)
                        {
                            if (Scene.TryGetComponentFrom<CharacterComponent>(presetCharacters.Entity, out var character))
                                character.NeedsLookUpdate = true;
                        }
                }
            }
        }
        else
        {
            //// Edit mode button
            //Ui.Layout.Size(MenuWidth, h);
            //if (Ui.ClickButton("Edit mode"))
            //    EnterEditMode(exp);
        }

        if (exp.ContextMenuInstance != null)
        {
            var p = exp.ContextMenuInstance.Value;
            var rect = new Rect();
            Ui.Layout.Size(200, 200).VerticalLayout().FitContent().Width(200).MoveAbs(p.ScreenPosition.X, p.ScreenPosition.Y);
            Ui.Layout.EnqueueConstraint(new AlwaysOnTop());
            Ui.Animate(new SlideInRelativeAnimation(-Vector2.UnitY)).Add(new FadeAnimation());
            Ui.Animation.SetDuration(0.1f);
            Ui.Theme.OutlineColour(Colors.White).Padding(8).Foreground((Appearance)Colors.Black).OutlineWidth(1);
            Ui.StartGroup(true, identity: exp.ContextMenuInstance.Value.ProcessUi.GetHashCode());
            {
                p.ProcessUi();
                Ui.Spacer(0); // for padding
                rect = Onion.Tree.CurrentNode!.GetFinalDrawBounds(Onion.Tree).Expand(Input.WindowMouseDelta.LengthSquared() + 40);
            }
            Ui.End();
            Ui.Theme.Reset();

            if (Input.IsButtonReleased(MouseButton.Left) && !rect.ContainsPoint(Input.WindowMousePosition))
                exp.ContextMenuInstance = null;
        }
    }

    private static bool StatsEditor(ExperimentCharacterPreset preset)
    {
        const int controlHeight = 35;
        bool changed = false;

        Ui.Theme.Padding(5).Push();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Name", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.StringInputBox(ref preset.Name, TextBoxOptions.TextInput);
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Head HP", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.HeadHealth, (0.01f, 1000));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Body HP", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.BodyHealth, (0.01f, 1000));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Scale", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.Scale, (0.1f, 3f));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Dodge", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.DodgeAbility, (0.1f, 3f));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Agility", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.EnumDropdown(ref preset.Stats.AgilitySkillLevel);
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Melee skill", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.MeleeSkill, (0, 100));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Melee force", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Ui.FloatInputBox(ref preset.Stats.MeleeKnockback, (0, 100));
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Can deflect", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Toggle.Start(ref preset.Stats.CanDeflect);
        }
        Ui.End();

        Ui.Layout.Height(controlHeight).FitWidth(false).EnqueueLayout(new DistributeChildrenLayout());
        Ui.StartGroup(false);
        {
            Ui.Layout.FitHeight(false);
            Ui.TextRect("Can deflect", HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            Ui.Layout.FitHeight(false); Ui.Theme.Font(Fonts.CascadiaMono).Once();
            changed |= Toggle.Start(ref preset.Stats.CanDeflect);
        }
        Ui.End();

        Ui.Theme.Pop();

        return changed;
    }

    private void ProcessMusicTab(ExperimentModeComponent exp, float h)
    {
        if (!Scene.FindAnyComponent<DjComponent>(out var dj))
            return;


        Ui.Layout.FitWidth(true).Scale(-8, 0).Height(h).CenterHorizontal().StickTop();
        Ui.StartGroup(false);
        {
            //Ui.Layout.Size(h, h);
            //if (Ui.ImageButton(Icons.PlayButton.Value, ImageContainmentMode.Stretch))
            //    dj.Play(DjComponent.CurrentMusic ?? Registries.Dj.GetRandomValue(), Audio);

            //Ui.Layout.Size(h, h).Move(h, 0);
            //if (Ui.ImageButton(Icons.StopButton.Value, ImageContainmentMode.Stretch))
            //    dj.Stop(Audio);

            var tex = DjComponent.PlaybackMode switch
            {
                DjComponent.Mode.Autoplay => Icons.RepeatAllButton,
                DjComponent.Mode.RepeatTrack => Icons.RepeatOneButton,
                _ => Icons.RepeatNoneButton,
            };

            Ui.Layout.Size(h, h);
            if (Ui.ImageButton(tex.Value, ImageContainmentMode.Stretch))
            {
                DjComponent.PlaybackMode = DjComponent.PlaybackMode switch
                {
                    DjComponent.Mode.Autoplay => DjComponent.Mode.RepeatTrack,
                    DjComponent.Mode.RepeatTrack => DjComponent.Mode.None,
                    _ => DjComponent.Mode.Autoplay,
                };
            }

            tex = DjComponent.Shuffle ?
                Assets.Load<Texture>("textures/ui/experimentmode/shuffleon_icon.png")
                : Assets.Load<Texture>("textures/ui/experimentmode/shuffleoff_icon.png");
            Ui.Layout.Size(h, h).Move(h + 6, 0);
            if (Ui.ImageButton(tex.Value, ImageContainmentMode.Stretch))
            {
                DjComponent.Shuffle = !DjComponent.Shuffle;
            }

            Ui.Layout.FitWidth(false).Scale(-h * 2 - 12, 0).StickRight(false).Height(h);
            Ui.StringInputBox(ref exp.MusicFilter, new(Localisation.Get("experiment-filter")));
        }
        Ui.End();

        Ui.Layout.FitContainer().StickTop().StickLeft().VerticalLayout().Move(0, h).Scale(0, -h);
        Ui.StartScrollView(false);
        {
            float bh = h * 1.8f;
            Ui.Theme.OutlineWidth(0);
            int i = 0;
            bool showAll = string.IsNullOrWhiteSpace(exp.MusicFilter);
            foreach (var track in Registries.Dj)
            {
                if (!showAll
                    && !track.Name.Contains(exp.MusicFilter, StringComparison.InvariantCultureIgnoreCase)
                    && !track.Author.Contains(exp.MusicFilter, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var isCurrent = DjComponent.CurrentMusic == track;

                Ui.Spacer(0, i);

                if (isCurrent)
                {
                    Ui.Theme.OutlineWidth(2).ForegroundColor(Colors.Red.WithAlpha(0.2f)).Once();
                }
                Ui.Layout.FitWidth(true).Height(bh).CenterHorizontal();
                Ui.StartGroup(true, identity: i++);
                {
                    const int padding = 3;

                    Ui.Decorate(new FancyButtonDecorator());
                    Ui.Layout.FitHeight(false).Scale(padding * -2, padding * -2).Move(padding, 0).CenterVertical().AspectRatio(1);
                    if (Ui.ImageButton(isCurrent ?
                        Icons.StopButton.Value :
                        Icons.PlayButton.Value, ImageContainmentMode.Stretch))
                    {
                        if (isCurrent)
                            dj.Stop(Audio);
                        else
                            dj.Play(track, Audio);

                    }

                    Ui.Layout.StickLeft().Height(h).FitWidth().StickTop().Move(bh - padding, padding);
                    Ui.Label(track.Name);

                    Ui.Theme.FontSize(14).Text(Colors.White.WithAlpha(0.5f)).Once();
                    Ui.Layout.StickLeft().Height(h).FitWidth().StickBottom().Move(bh - padding, padding);
                    Ui.Label(track.Author);
                }
                Ui.End();

                //if (DjComponent.CurrentMusic == item)
                //    Ui.Decorate(new CrosshairDecorator());
                //Ui.Decorate(new FancyButtonDecorator());
                //Ui.Theme.OutlineWidth(0);
                //Ui.Layout.FitWidth(true).Height(h * 2).CenterHorizontal();
                //if (LeftAlignedButton.Start(item.Name, identity: i++))
                //{
                //    dj.Play(item, Audio);
                //}
            }
            Ui.Theme.Pop();
        }
        Ui.End();
    }

    private static void ProcessAutospawnWindow(ExperimentModeComponent exp, EnemySpawningComponent spawning)
    {
        Ui.Layout.Size(MenuWidth + 100, 593 + 40).Center().Resizable().MaxHeight(593 + 40).MinSize(100, 100);
        Ui.Theme.Background((Appearance)Colors.Red.WithAlpha(0.2f)).OutlineWidth(1).OutlineColour(new(Colors.Red, Colors.White)).Text(Colors.White).Once();
        Ui.StartDragWindow(Localisation.Get("experiment-autospawn-settings"), ref exp.AutoSpawnSettingsOpen);
        {
            Ui.Layout.FitContainer(1, 1, false).VerticalLayout();
            Ui.StartScrollView();
            {
                Ui.Label(Localisation.Get("experiment-max-enemies"));
                Ui.Layout.Height(32).FitWidth().StickLeft();
                Ui.IntStepper(ref spawning.MaxEnemyCount, (0, 30), 1);
                Ui.Spacer(5);
                Ui.Label(Localisation.Get("experiment-spawn-interval"));
                Ui.Layout.Height(32).FitWidth().StickLeft();
                Ui.FloatStepper(ref spawning.Interval, (0.1f, 60), 0.1f);
                const float size = 40;
                spawning.SpawnInstructions ??= [];
                spawning.WeaponsToSpawnWith ??= [];
                if (spawning.SpawnInstructions != null)
                {
                    Ui.Layout.FitWidth().StickLeft().StickTop().Height(216).VerticalLayout();
                    Ui.StartScrollView(true);
                    if (spawning.SpawnInstructions.Count == 0)
                    {
                        Ui.Layout.PreferredSize().FitWidth().StickLeft().StickTop();
                        Ui.Theme.Text(Colors.White.WithAlpha(0.5f)).Once();
                        Ui.TextRect(Localisation.Get("experiment-autospawn-help-weapon"), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                    }
                    else
                        for (int i = 0; i < spawning.SpawnInstructions.Count; i++)
                        {
                            var item = spawning.SpawnInstructions[i];
                            if (item is not ExperimentCharacterPreset preset)
                                continue;
                            Ui.Layout.Height(size).FitWidth().StickLeft();
                            Ui.Theme.OutlineColour(Colors.Gray * 0.5f).OutlineWidth(1).Once();
                            Ui.StartGroup(true, identity: i);
                            {
                                Ui.Layout.StickLeft().FitContainer(0.5f, 1).Move(size + 8, 2);
                                Ui.TextRect(preset.Name, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                                Ui.Layout.Size(size, size);
                                Ui.Image(CharacterThumbnailCache.Instance.Load(preset.Look), ImageContainmentMode.Stretch);
                                Ui.Layout.Size(12, 12).StickRight().CenterVertical().Move(-8, 0);
                                Ui.Theme.Image(new(Colors.White, Colors.Red)).OutlineWidth(0).Once();
                                if (Ui.ClickImageButton(Textures.UserInterface.SmallExitClose.Value))
                                {
                                    MadnessUtils.Delay(0, () => spawning.SpawnInstructions.Remove(item));
                                }
                            }
                            Ui.End();
                        }
                    Ui.End();
                }

                if (spawning.WeaponsToSpawnWith != null)
                {
                    Ui.Layout.FitWidth().Height(35).StickLeft().StickTop().EnqueueLayout(new DistributeChildrenLayout());
                    Ui.StartGroup();
                    {
                        // TODO this should be cached! it's kind of scary
                        // how fast it is without caching but it really
                        // should be cached
                        var meleeSet = Registries.Weapons.GetAllValues().Where(t => t.WeaponData.WeaponType is WeaponType.Melee).Select(t => t.Id);
                        var firearmSet = Registries.Weapons.GetAllValues().Where(t => t.WeaponData.WeaponType is WeaponType.Firearm).Select(t => t.Id);

                        bool melee = spawning.WeaponsToSpawnWith.Intersect(meleeSet).Count() == meleeSet.Count();
                        Ui.Layout.FitContainer(1 / 3f, 1).StickLeft().StickTop();
                        if (Ui.Checkbox(ref melee, Localisation.Get("experiment-melee")))
                        {
                            if (!melee)
                                spawning.WeaponsToSpawnWith = [.. spawning.WeaponsToSpawnWith.Concat(meleeSet).Distinct()];
                            else
                                spawning.WeaponsToSpawnWith = [.. spawning.WeaponsToSpawnWith.Except(meleeSet).Distinct()];
                        }

                        bool firearms = spawning.WeaponsToSpawnWith.Intersect(firearmSet).Count() == firearmSet.Count();
                        Ui.Layout.FitContainer(1 / 3f, 1).StickLeft().StickTop();
                        if (Ui.Checkbox(ref firearms, Localisation.Get("experiment-firearms")))
                        {
                            if (!firearms)
                                spawning.WeaponsToSpawnWith = [.. spawning.WeaponsToSpawnWith.Concat(firearmSet).Distinct()];
                            else
                                spawning.WeaponsToSpawnWith = [.. spawning.WeaponsToSpawnWith.Except(firearmSet).Distinct()];
                        }

                        bool all = spawning.WeaponsToSpawnWith.Count == Registries.Weapons.Count;
                        Ui.Layout.FitContainer(1 / 3f, 1).StickLeft().StickTop();
                        if (Ui.Checkbox(ref all, Localisation.Get("experiment-all")))
                        {
                            if (!all)
                                spawning.WeaponsToSpawnWith = [.. Registries.Weapons.GetAllValues().Select(d => d.Id)];
                            else
                                spawning.WeaponsToSpawnWith = [];
                        }
                    }
                    Ui.End();

                    Ui.Layout.FitWidth().StickLeft().StickTop().Height(216).VerticalLayout();
                    Ui.StartScrollView(true);
                    if (spawning.WeaponsToSpawnWith.Count == 0)
                    {
                        Ui.Layout.PreferredSize().FitWidth().StickLeft().StickTop();
                        Ui.Theme.Text(Colors.White.WithAlpha(0.5f)).Once();
                        Ui.TextRect(Localisation.Get("experiment-autospawn-help-weapon"), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                    }
                    else
                        for (int i = 0; i < spawning.WeaponsToSpawnWith.Count; i++)
                        {
                            var item = spawning.WeaponsToSpawnWith[i];
                            var wpn = Registries.Weapons.Get(item);
                            Ui.Layout.Height(size).FitWidth().StickLeft();
                            Ui.Theme.OutlineColour(Colors.Gray * 0.5f).OutlineWidth(1).Once();
                            Ui.StartGroup(true, identity: i);
                            {
                                Ui.Layout.StickLeft().FitContainer(0.5f, 1).Move(size + 8, 2);
                                Ui.TextRect(wpn.WeaponData.Name, HorizontalTextAlign.Left, VerticalTextAlign.Middle);
                                Ui.Layout.Size(size, size);
                                Ui.Image(WeaponThumbnailCache.Instance.Load(wpn), ImageContainmentMode.Stretch);
                                Ui.Layout.Size(12, 12).StickRight().CenterVertical().Move(-8, 0);
                                Ui.Theme.Image(new(Colors.White, Colors.Red)).OutlineWidth(0).Once();
                                if (Ui.ClickImageButton(Textures.UserInterface.SmallExitClose.Value))
                                {
                                    MadnessUtils.Delay(0, () => spawning.WeaponsToSpawnWith.Remove(item));
                                }
                            }
                            Ui.End();
                        }
                    Ui.End();
                }
            }
            Ui.End();
        }
        Ui.End();
    }

    private void ProcessGameRulesTab(ExperimentModeComponent exp, float controlHeight)
    {
        Ui.Layout.FitContainer().StickTop().StickLeft().VerticalLayout();
        Ui.StartScrollView(false);
        {
            // TODO i am not sure, but the player needs to be able to toggle modifiers

            //Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            //Ui.Checkbox(ref GameModifiers.GodPlayer, "God mode");

            //Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            //Ui.Checkbox(ref GameModifiers.InfiniteAmmoPlayer, "Infinite ammo");

            Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            Ui.Checkbox(ref ExperimentModePersistentData.AIDisabled, "AI disabled");

            Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            Ui.Checkbox(ref AiCharacterSystem.AutoSpawn, Localisation.Get("experiment-autospawn"));
            if (AiCharacterSystem.AutoSpawn)
            {
                Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
                if (Ui.ClickButton(Localisation.Get("experiment-autospawn-configure")))
                    exp.AutoSpawnSettingsOpen = !exp.AutoSpawnSettingsOpen;
            }

            Ui.Spacer(10);

            Ui.Label(Localisation.Get("experiment-timescale"));
            float timeScale = Time.TimeScale;
            Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            if (Ui.FloatSlider(ref timeScale, Direction.Horizontal, (0.05f, 1), label: "{0:P0}"))
                Time.TimeScale = timeScale;

            Ui.Spacer(8);
            Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            if (Ui.Button(Localisation.Get("experiment-cleanup")))
                ClearAll(exp);

            //Ui.Layout.Height(controlHeight).FitWidth().StickLeft();
            //if (Ui.Button("Configure improbability..."))
            //    exp.ImprobabilityDisksOpen = !exp.ImprobabilityDisksOpen;
        }
        Ui.End();
    }

    private void ProcessModifiersWindow(ExperimentModeComponent exp)
    {
        Ui.Layout.Size(300, 400).Center();
        Ui.Theme.Background((Appearance)Colors.Red.WithAlpha(0.2f)).OutlineWidth(1).OutlineColour(new(Colors.Red, Colors.White)).Text(Colors.White).Once();
        Ui.StartDragWindow(Localisation.Get("experiment-modifiers-disks"), ref exp.ImprobabilityDisksOpen);
        {
            Ui.Layout.FitContainer(1, 1, false).VerticalLayout();
            Ui.StartScrollView(false);
            {
                foreach (var pair in ImprobabilityDisks.All)
                {
                    var disk = pair.Value;
                    var incompatible = ImprobabilityDisks.IsIncompatibleWithEnabled(pair.Key);

                    Ui.Layout.Height(64).FitWidth().StickLeft();

                    if (incompatible)
                    {
                        Ui.Theme.Image(Colors.White.WithAlpha(0.4f)).OutlineWidth(0).Text(Colors.White).Once();
                    }
                    else
                    {
                        Ui.Decorate(new FancyButtonDecorator());
                        if (disk.Enabled)
                            Ui.Decorate(new CrosshairDecorator());
                    }

                    Ui.Decorators.Tooltip(disk.Description);
                    if (ExperimentModeButton.Click(disk.DisplayName, disk.Texture, identity: disk.GetHashCode()))
                    {
                        if (!incompatible)
                            disk.Enabled = !disk.Enabled;
                    }
                }
            }
            Ui.End();
        }
        Ui.End();
    }

    private void ProcessCharacterTab(ExperimentModeComponent exp, float controlHeight)
    {
        Ui.Layout.FitWidth().Scale(-32, 0).Height(32).StickTop().StickLeft();
        Ui.StringInputBox(ref exp.NPCFilter, new TextBoxOptions(Localisation.Get("experiment-filter")));

        Ui.Layout.Size(32, 32).StickTop().StickRight();
        if (Ui.ImageButton(Textures.UserInterface.SmallExitClose.Value, ImageContainmentMode.Center))
            exp.NPCFilter = string.Empty;

        int factionIndex = int.Max(0, Array.IndexOf(exp.FactionCache, exp.SelectedFaction));
        Ui.Layout.FitWidth().Height(32).StickLeft().Move(0, 36);
        Ui.Decorators.Tooltip(Localisation.Get("experiment-npc-faction"));
        Ui.Theme.OutlineWidth(1).Once();
        if (Ui.Dropdown(exp.FactionCache, ref factionIndex))
            exp.SelectedFaction = exp.FactionCache[factionIndex];

        Ui.Layout.FitContainer().Scale(0, -64).StickTop().StickLeft().Move(0, 64).VerticalLayout();
        Ui.StartScrollView(false);
        {
            int i = 0;
            foreach (var preset in Registries.Experiment.CharacterPresets.GetAllValues())
            {
                if (!string.IsNullOrEmpty(exp.NPCFilter) && !preset.Name.Contains(exp.NPCFilter, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                i++;
                Ui.Layout.Height(controlHeight * 2).FitWidth().StickLeft();
                Ui.Decorate(new CharacterContextMenuDecorator(preset, exp));
                if (ExperimentModeButton.Hold(preset.Name, CharacterThumbnailCache.Instance.Load(preset.Look), i) && !exp.CurrentlyPlacing.HasValue)
                {
                    exp.CurrentlyPlacing = new ExperimentPlacableObject((scene, pos) =>
                    {
                        var faction = Registries.Factions[exp.SelectedFaction.Id];

                        var character = Prefabs.CreateCharacter(scene, new CharacterPrefabParams
                        {
                            Name = preset.Name,
                            Bottom = pos,
                            Faction = faction,
                            Look = preset.Look,
                            Stats = preset.Stats,
                            Tag = Tags.EnemyAI,
                        });
                        if (preset.Mutable)
                            scene.AttachComponent(character.Entity, new CharacterPresetComponent(preset));
                        scene.AttachComponent(character.Entity, new AiComponent());

                        RoutineScheduler.Start(ExperimentSelectableCharacter.FallToGroundRoutine(character).GetEnumerator());

                    }, preset.Look.Head.Right.Value);
                }
            }

        }
        Ui.End();
    }

    private void ProcessWeaponsTab(ExperimentModeComponent exp, float controlHeight)
    {
        Ui.Layout.FitWidth().Scale(-32, 0).Height(32).StickTop().StickLeft();
        Ui.StringInputBox(ref exp.WeaponFilter, new TextBoxOptions(Localisation.Get("experiment-filter")));

        Ui.Layout.Size(32, 32).StickRight().StickTop();
        if (Ui.ImageButton(Textures.UserInterface.SmallExitClose.Value, ImageContainmentMode.Center))
            exp.WeaponFilter = string.Empty;

        Ui.Layout.FitContainer().Scale(0, -32).StickBottom().StickLeft().VerticalLayout();
        Ui.StartScrollView(false);
        {
            int i = 0;
            foreach (var item in exp.SortedWeaponCache)
            {
                if (!Registries.Weapons.TryGet(item, out var wpn))
                    continue;

                if (!string.IsNullOrEmpty(exp.WeaponFilter) && !wpn.WeaponData.Name.Contains(exp.WeaponFilter, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                Ui.Layout.Height(controlHeight * 3).FitWidth().StickLeft();
                Ui.Decorate(new WeaponContextMenuDecorator(item, exp));
                if (ExperimentModeButton.Hold(wpn.WeaponData.Name, WeaponThumbnailCache.Instance.Load(wpn), i) && !exp.CurrentlyPlacing.HasValue)
                    exp.CurrentlyPlacing = new ExperimentPlacableObject((scene, pos) =>
                    {
                        Prefabs.CreateWeapon(scene, pos, Registries.Weapons.Get(item));
                    }, wpn.BaseTexture.Value);
                i++;
            }
        }
        Ui.End();
    }

    public void EnterEditMode(ExperimentModeComponent exp)
    {
        exp.IsEditMode = true;
        MadnessUtils.Shake(90);
        MadnessUtils.Flash(Colors.Red.WithAlpha(0.5f), 2f);
        Audio.PlayOnce(Sounds.TimeFreezeStart);
        Audio.Play(Sounds.TimeFreezeLoop);
        exp.CopySceneToSelectionManager(Scene);

        foreach (var comp in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            var floorLevel = (Level.CurrentLevel?.GetFloorLevelAt(comp.Positioning.GlobalCenter.X) ?? 0) + CharacterConstants.GetFloorOffset(comp.Positioning.Scale);
            comp.Positioning.GlobalTarget.Y = floorLevel + comp.Positioning.FlyingOffset;
        }

        if (Scene.FindAnyComponent<CameraMovementComponent>(out var cm))
        {
            cameraZoom = cm.ComputedOrthographicSize;
            cm.States.Clear();
            cm.Targets = [exp.FreeMoveCameraTarget];
        }
    }

    public void ExitEditMode(ExperimentModeComponent exp)
    {
        exp.IsEditMode = false;

        MadnessUtils.Flash(Colors.Red.WithAlpha(0.25f), 0.25f);
        Audio.Stop(Sounds.TimeFreezeLoop);

        if (Scene.FindAnyComponent<CameraMovementComponent>(out var cm))
        {
            cm.TransitionTo(exp.PlayerCameraTarget, 0.5f);
        }
    }

    private void ClearAll(ExperimentModeComponent exp)
    {
        // Clear the stamped stuff
        if (Level.CurrentLevel != null && Scene.FindAnyComponent<StampCanvasComponent>(out var canvas))
            canvas.Clear(RenderQueue);

        // Delete all the other characters
        var characters = Scene.GetAllComponentsOfType<CharacterComponent>().ToArray();
        for (int i = 0; i < characters.Length; i++)
        {
            if (Scene.HasTag(characters[i].Entity, Tags.Player))
                continue;

            characters[i].Delete(Scene);
        }

        // Delete all weapons
        var weapons = Scene.GetAllComponentsOfType<WeaponComponent>().ToArray();
        for (int i = 0; i < weapons.Length; i++)
        {
            var weaponEntity = weapons[i].Entity;
            Scene.RemoveEntity(weaponEntity);
            if (!Scene.TryGetComponentFrom<DespawnComponent>(weaponEntity, out var despawn))
                continue;

            var alsoDelete = despawn.AlsoDelete!;
            for (int k = 0; k < alsoDelete.Length; k++)
                Scene.RemoveEntity(alsoDelete[k]);
        }

        // Clear decals since they stick around unlike bullet casings, which go away pretty quickly.
        if (Scene.TryGetSystem<DecalSystem>(out var decalSystem))
            decalSystem.RemoveAllDecals();

        exp.SelectionManager.DeselectAll();
        exp.CopySceneToSelectionManager(Scene);
    }
}
