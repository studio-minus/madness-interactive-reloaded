using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;  
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk.Onion.Controls;
using static MIR.Prefabs;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// For editing weapons.
/// </summary>
public class WeaponEditorSystem : Walgelijk.System
{
    //todo dit kan mooier he
    private static int sharpRectToHighlight = -1;
    private static float rangeGizmoHeat = 0;
    private static IDraggableGizmo? currentGizmo = null;

    public bool TryOpenWithWeapon(string? startFilePath = default)
    {
        return TrySetWeapon(startFilePath);
    }

    /// <summary>
    /// Tries to load and start editing the weapon file from disk.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <exception cref="Exception"></exception>
    public bool TrySetWeapon(string? path = default)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            MadnessUtils.Delay(0.1f, () =>
            {
                SetWeapon(path);
            });
            return true;
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to read weapon: {e.Message}", nameof(WeaponEditorSystem));
            return false;
        }
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<CameraComponent>(out var camera) ||
            !Scene.TryGetComponentFrom<TransformComponent>(camera.Entity, out var cameraTransform) ||
            !Scene.FindAnyComponent<WeaponEditorComponent>(out var editor))
            return;

        DrawTaskBar(editor);
        DrawRightSidebar(editor);

        if (editor != null && editor.Weapon != null && editor.CurrentSelectedAnimatedPart != null)
            DrawKeyframeEditor(editor, editor.Weapon, editor.CurrentSelectedAnimatedPart);

        if (Input.IsKeyReleased(Key.Home))
        {
            camera.OrthographicSize = 1;
            Scene.GetSystem<CameraMovementSystem>().ResetCameraPosition();
        }

        // (duston): use Pressed so we don't instantly re-enter test mode when exiting test mode
        if (editor != null && Input.IsKeyPressed(Key.F5) && editor.Weapon != null)
            MadnessUtils.TransitionScene(game => WeaponEditorTestScene.Create(Game.Main, editor.Weapon));

        rangeGizmoHeat -= Time.DeltaTime;
    }

    private void SetWeapon(string path)
    {
        if (!Scene.FindAnyComponent<CameraComponent>(out var camera) ||
            !Scene.FindAnyComponent<WeaponEditorComponent>(out var editor))
            throw new InvalidOperationException("Necessary components (CameraComponent, WeaponEditorComponent) could not be found");

        var w = WeaponDeserialiser.LoadFromPath(path);
        editor.Weapon = w;
        editor.File = new FileInfo(path);
        camera.OrthographicSize = 1;
        Scene.GetSystem<CameraMovementSystem>().ResetCameraPosition();
        editor.CurrentSelectedAnimatedPart = null;
        editor.CurrentPropertyTab = WeaponEditorComponent.PropertyTab.BasicInfo;
    }

    public override void Render()
    {
        if (!Scene.FindAnyComponent<WeaponEditorComponent>(out var editor))
            return;

        if (currentGizmo != null && !Onion.Navigator.IsBeingUsed)
        {
            if (Input.IsButtonHeld(MouseButton.Left))
                currentGizmo.OnDrag(Input.WorldMouseDelta);

            if (Input.IsButtonReleased(MouseButton.Left))
                currentGizmo = null;
        }

        if (editor != null && editor.Weapon != null)
            DrawWeapon(editor, editor.Weapon);
    }

    private void DrawTaskBar(WeaponEditorComponent editor)
    {
        var layout = Onion.Layout;
        Ui.Theme.OutlineWidth(0).Padding(0);
        layout.Height(WeaponEditorComponent.TopbarHeight).FitWidth().Scale(-WeaponEditorComponent.SidebarWidth, 0).HorizontalLayout();
        Ui.StartScrollView(true);
        {
            layout.FitHeight().Width(128).CenterVertical();
            if (Ui.ClickButton("New"))
            {
                editor.Weapon = new WeaponInstructions(string.Empty, new WeaponData(), new("textures/error.png"), false, new(-15), new(15), new List<Vector2>(), null);
                editor.Weapon.HoldPoints.Add(new Vector2(-15, 15));
            }

            layout.FitHeight().Width(128).CenterVertical();
            if (Ui.ClickButton("Load"))
            {
                if (FileDialog.OpenFile(new[] { ("JSON files", "json") }, out var chosenPath) && !string.IsNullOrWhiteSpace(chosenPath))
                {
                    if (TrySetWeapon(chosenPath))
                        Audio.PlayOnce(Sounds.UiConfirm);
                    else
                        Audio.PlayOnce(Sounds.UiBad);
                }
            }

            // (duston): Don't process any of this UI if no weapon is loeaded.
            if (editor.Weapon == null)
            {
                Ui.End();
                return;
            }

            layout.FitHeight().Width(128).CenterVertical();
            if (Ui.ClickButton("Save"))
            {
                try
                {
                    if (FileDialog.SaveFile(new[] { ("JSON files", "json"), ("All files", "*") }, (editor.File?.FullName ?? editor.Weapon.Id), null, out var chosenPath) && !string.IsNullOrWhiteSpace(chosenPath))
                    {
                        WeaponDeserialiser.SaveWeapon(editor.Weapon, chosenPath);
                        editor.File = new FileInfo(chosenPath);
                        Audio.PlayOnce(Sounds.UiConfirm);
                        MadnessUtils.Flash(Colors.Green.WithAlpha(0.2f), 0.25f);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    MadnessUtils.Flash(Colors.Red, 0.5f);
                    Audio.PlayOnce(Sounds.UiBad);
                }

            }

            layout.FitHeight().Width(200).CenterVertical();
            Ui.Theme.Foreground(new StateDependent<Appearance>(Colors.White)).Text(new(Colors.Black, Colors.Red)).Once();
            if (Ui.ClickButton("Test in world"))
                MadnessUtils.TransitionScene(game => WeaponEditorTestScene.Create(Game.Main, editor.Weapon));
        }
        Ui.End();
        Ui.Theme.Reset();
        return;
    }

    private void DrawRightSidebar(WeaponEditorComponent editor)
    {
        const float sidebarWidth = WeaponEditorComponent.SidebarWidth;
        const float tabsWidth = WeaponEditorComponent.SidebarTabWidth;
        const float propertyPanelWidth = sidebarWidth - tabsWidth - 8;

        var layout = Onion.Layout;
        Ui.Theme.Padding(0).Foreground((Appearance)Colors.Black.WithAlpha(0.5f)).Once();
        layout.Width(sidebarWidth).FitHeight().StickTop().StickRight();
        Ui.StartScrollView(true);
        {
            if (editor.Weapon == null)
            {
                layout.Height(32).FitWidth().Move(10).Scale(-10, 0);
                Ui.TextRect("Create a new weapon or load an existing one", HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
                Ui.End();
                return;
            }

            // Tabs
            layout.VerticalLayout().Width(tabsWidth).FitHeight();
            Ui.StartScrollView();
            {
                bool tabButton(string text, in WeaponEditorComponent.PropertyTab tab, [CallerLineNumber] int site = 0, int optionalId = 0)
                {
                    Onion.Layout.Size(tabsWidth, tabsWidth);
                    Onion.Decorators.Tooltip(text);
                    if (Ui.ClickImageButton(WeaponEditorComponent.GetTabIcon(tab), ImageContainmentMode.Center, optionalId, site))
                    {
                        editor.CurrentPropertyTab = tab;
                        return true;
                    }

                    return false;
                }

                tabButton("Basic", WeaponEditorComponent.PropertyTab.BasicInfo);
                tabButton("Damage", WeaponEditorComponent.PropertyTab.WeaponDamage);

                if (editor.Weapon.WeaponData.WeaponType == WeaponType.Firearm)
                    tabButton("Firearm Info", WeaponEditorComponent.PropertyTab.FirearmSettings);

                if (editor.Weapon.WeaponData.WeaponType is WeaponType.Firearm or WeaponType.Melee)
                    tabButton("Melee Info", WeaponEditorComponent.PropertyTab.MeleeSettings);

                if (editor.Weapon.WeaponData.WeaponType == WeaponType.Firearm)
                    tabButton("Sounds", WeaponEditorComponent.PropertyTab.SoundSettings);

                tabButton("Anims", WeaponEditorComponent.PropertyTab.AnimatedParts);
                tabButton("Hold Points", WeaponEditorComponent.PropertyTab.HoldPointsEditor);
            }
            Ui.End();

            // Properties panel
            layout.Width(propertyPanelWidth).FitHeight().StickRight().VerticalLayout().StickTop();
            Ui.StartScrollView();
            {
                switch (editor.CurrentPropertyTab)
                {
                    case WeaponEditorComponent.PropertyTab.BasicInfo:
                        DrawBasicInfo(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.WeaponDamage:
                        DrawDamageSettings(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.FirearmSettings:
                        DrawFirearmSettings(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.MeleeSettings:
                        DrawMeleeSettings(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.SoundSettings:
                        DrawFirearmSoundSettings(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.AnimatedParts:
                        DrawAnimatedPartSettings(editor, editor.Weapon);
                        break;
                    case WeaponEditorComponent.PropertyTab.HoldPointsEditor:
                        DrawHoldPointSettings(editor, editor.Weapon);
                        break;
                }
            }
            Ui.End();
        }
        Ui.End();
    }

    private void DrawBasicInfo(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        const int h = WeaponEditorComponent.ControlHeight;
        const int s = WeaponEditorComponent.ControlSpacing;

        var weaponData = weapon.WeaponData;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Basic Properties");
        Ui.Spacer(16);

        Ui.Label("Base texture");
        Ui.Layout.FitWidth(false).Height(h);
        MadnessUi.AssetPicker(
            weapon.BaseTexture.Id,
            c => weapon.BaseTexture = new(c),
            static c => c.MimeType.Contains("image"));

        Ui.Spacer(s);

        Ui.Label("Codename", HorizontalTextAlign.Left);
        Ui.Layout.FitWidth().Height(h);
        Ui.StringInputBox(ref weapon.Id, new TextBoxOptions(placeholder: "Codename", maxLength: 50));
        Ui.Spacer(s);

        Ui.Label("Display name");
        Ui.Layout.FitWidth().Height(h);
        Ui.StringInputBox(ref weaponData.Name, new TextBoxOptions(placeholder: "Codename", maxLength: 50));
        Ui.Spacer(s);

        Ui.Label("Weapon type");
        Ui.Layout.FitWidth().Height(h);
        Ui.EnumDropdown(ref weaponData.WeaponType);
        Ui.Spacer(s);

        Ui.Layout.FitWidth().Height(h);
        Ui.Checkbox(ref weapon.HoldForGrip, "Grip handle");
        Ui.Spacer(s);

        Ui.Layout.FitWidth().Height(h);
        Ui.Checkbox(ref weapon.HoldStockHandPose, "Hold stock hand pose");
        Ui.Spacer(s);

        Ui.Label("Delay / automatic interval");
        Ui.Layout.FitWidth().Height(h);
        Ui.FloatStepper(ref weaponData.UseDelay, (0, 2), 0.1f);
        Ui.Spacer(s);

        Ui.Label("Max distance from player");
        Ui.Layout.FitWidth().Height(h);
        Ui.FloatInputBox(ref weaponData.MaxHandRangeMultiplier, (0.1f, 5f));
        Ui.Spacer(s);

        Ui.Label("Floor angle");
        Ui.Layout.FitWidth().Height(h);
        Ui.FloatInputBox(ref weapon.OnFloorAngle, (0f, 360f));
    }

    private void DrawDamageSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;

        Ui.Label("Damage");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.Damage, new MinMax<float>(0, 10));
        Ui.Spacer(8);

        Ui.Label("Throwable damage");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.ThrowableDamage, new MinMax<float>(0, 50));
        Ui.Spacer(8);

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.Checkbox(ref weaponData.ThrowableHeavy, "Heavy when thrown");
    }

    private void DrawFirearmSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Firearm settings");
        Ui.Spacer(16);

        Ui.Label("Ejection particle");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.EnumDropdown(ref weaponData.EjectionParticle);
        Ui.Spacer(8);

        Ui.Label("Ejection particle size");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.CasingSize, new MinMax<float>(0.1f, 2f));
        Ui.Spacer(8);

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.Checkbox(ref weaponData.IsPumpAction, "Pump action");

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.Checkbox(ref weaponData.CanBulletsBeDeflected, "Deflectable bullets");

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.Checkbox(ref weaponData.Automatic, "Automatic");
        Ui.Spacer(8);

        Ui.Label("Max. ammo");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.IntInputBox(ref weaponData.RoundsPerMagazine, (1, 120));
        Ui.Spacer(8);

        Ui.Label("Bullets per shot");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.IntInputBox(ref weaponData.BulletsPerShot, (1, 12));
        Ui.Spacer(8);
        // from this point forward you may notice manual IDs; i dont know why but it seems like theres a few collisions without it
        Ui.Label("Burst fire count");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.IntInputBox(ref weaponData.BurstFireCount, (0, 5), identity: 9034);
        Ui.Spacer(8, identity: 4625);

        Ui.Label("Accuracy");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatSlider(ref weaponData.Accuracy, Direction.Horizontal, (0, 1), 0.01f, label: "{0:0.#}", identity: 3658);
        Ui.Spacer(8, identity: 4);

        Ui.Label("Recoil");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.Recoil, (0, 3), identity: 46728);
        Ui.Spacer(8, identity: 4725);

        Ui.Label("Rotational recoil intensity");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.RotationalRecoilIntensity, (0, 3), identity: 34625);
        Ui.Spacer(8, identity: 342);

        Ui.Label("Recoil handling");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.FloatInputBox(ref weaponData.RecoilHandling, (0, 2), identity: 500);
        Ui.Spacer(8, identity: 6795);
    }

    private void DrawMeleeSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Melee settings");
        Ui.Spacer(16);

        Ui.Label("Melee damage type");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.EnumDropdown(ref weaponData.MeleeDamageType);
        Ui.Spacer(8);

        Ui.Label("Range");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        if (Ui.FloatInputBox(ref weaponData.Range, new MinMax<float>(10, 1000)))
            rangeGizmoHeat = 2;
        Ui.Spacer(8);

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        Ui.Checkbox(ref weaponData.CanDeflectBullets, "Bullet deflection");
        Ui.Spacer(16);

        Ui.Label("Melee size");
        Ui.Layout.FitWidth().Height(WeaponEditorComponent.ControlHeight);
        Ui.EnumDropdown(ref weaponData.MeleeSize);
        Ui.Spacer(WeaponEditorComponent.ControlSpacing);

        Ui.Label("\"Special\" weapon");
        Ui.Layout.FitWidth().Height(WeaponEditorComponent.ControlHeight);
        Ui.Checkbox(ref weaponData.SpecialMelee);
        Ui.Spacer(WeaponEditorComponent.ControlSpacing);

        Ui.Label("Throwable sharp rects");
        sharpRectToHighlight = -1;

        Ui.Decorators.Tooltip("These rectangles determine what parts of the weapon are sharp when thrown. They are shown in yellow in the editor view.");
        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        if (Ui.ClickButton("Add sharp rect"))
        {
            Audio.PlayOnce(Sounds.UiConfirm);
            weapon.WeaponData.ThrowableSharpBoxes.Add(new Rect(Vector2.Zero, new Vector2(64)));
        }

        for (int i = 0; i < weapon.WeaponData.ThrowableSharpBoxes.Count; i++)
        {
            var rect = weapon.WeaponData.ThrowableSharpBoxes[i];
            var c = rect.GetCenter();

            Ui.Layout.FitWidth().Height(64);
            Ui.StartGroup(identity: i);
            {
                if (Onion.Tree.CurrentNode != null && Onion.Tree.GetControlInstance(Onion.Tree.CurrentNode.Identity).Rects.ComputedGlobal.ContainsPoint(Input.WindowMousePosition))
                    sharpRectToHighlight = i;
                Ui.Layout.StickTop();
                Ui.Label($"X: {MathF.Round(c.X, 2)}, Y: {MathF.Round(c.Y, 2)}, W: {MathF.Round(rect.Width, 2)}. H: {MathF.Round(rect.Height, 2)}", HorizontalTextAlign.Center, identity: i);

                Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth().StickBottom().CenterHorizontal();
                if (Ui.ClickButton("Remove", identity: i))
                {
                    Game.Main.AudioRenderer.PlayOnce(Sounds.UiBad);
                    weapon.WeaponData.ThrowableSharpBoxes.RemoveAt(i);
                }
            }
            Ui.End();
        }
    }

    private void DrawFirearmSoundSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;

        var layout = Onion.Layout;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Sound properties");
        Ui.Spacer(16);

        layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        if (Ui.ClickButton("Add sound..."))
        {
            var v = Sounds.Empty;
            weaponData.ShootSounds ??= [];
            weaponData.ShootSounds.Add(default);
        }

        if (weaponData.ShootSounds != null)
        {
            Ui.Spacer(8);
            for (int i = 0; i < weaponData.ShootSounds.Count; i++)
            {
                Ui.Layout.FitWidth().Height(110).VerticalLayout();
                Ui.StartGroup(true, i);
                {
                    int index = i;
                    layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
                    MadnessUi.AssetPicker(
                        weaponData.ShootSounds[index].Id,
                        (id) => weaponData.ShootSounds[index] = new(id),
                        a => a.MimeType.Contains("audio"), title: "Select a sound", identity: i);

                    if (Assets.HasAsset(weaponData.ShootSounds[i].Id))
                    {
                        layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
                        if (Ui.ClickButton("Listen", identity: i))
                            Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(weaponData.ShootSounds[i]));
                    }

                    layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
                    if (Ui.ClickButton("Remove", identity: i))
                    {
                        MadnessUtils.Delay(0.01f, () =>
                        {
                            if (weaponData.ShootSounds.Count > index)
                            {
                                Game.Main.AudioRenderer.PlayOnce(Sounds.UiBad);
                                weaponData.ShootSounds.RemoveAt(index);
                            }
                        });
                    }
                }
                Ui.End();
            }
        }
    }

    private void DrawAnimatedPartSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;

        var layout = Onion.Layout;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Animated parts");
        Ui.Spacer(16);

        layout.Height(WeaponEditorComponent.ControlHeight).FitWidth();
        if (Ui.ClickButton("Add animated part..."))
        {
            var v = new AnimatedWeaponPart("textures/error.png");
            weapon.AnimatedParts ??= new List<AnimatedWeaponPart>();
            weapon.AnimatedParts.Add(v);
        }

        Ui.Spacer(8);
        if (weapon.AnimatedParts != null)
            for (int i = 0; i < weapon.AnimatedParts.Count; i++)
            {
                var item = weapon.AnimatedParts[i];

                Ui.Layout.Height(246).FitWidth().VerticalLayout();
                if (editor.CurrentSelectedAnimatedPart == item)
                    Ui.Theme.OutlineWidth(2).OutlineColour(Colors.White).Once();
                Ui.StartGroup(true, i);
                {
                    layout.Size(100, 100).CenterHorizontal();
                    Ui.Image(item.Texture.Value, ImageContainmentMode.Contain);

                    layout.FitWidth().Height(32).CenterHorizontal();
                    MadnessUi.AssetPicker(
                        item.Texture.Id,
                        (id) => item.Texture = new(id),
                        a => a.MimeType.Contains("image"), title: "Select an image");

                    layout.Height(WeaponEditorComponent.ControlHeight).FitWidth().CenterHorizontal();
                    if (Ui.FloatInputBox(ref item.Duration, null, null))
                        editor.CurrentSelectedAnimatedPart = item;

                    layout.Height(WeaponEditorComponent.ControlHeight).FitWidth().CenterHorizontal();
                    if (Ui.ClickButton("Edit keyframes"))
                        editor.CurrentSelectedAnimatedPart = item;

                    layout.Height(WeaponEditorComponent.ControlHeight).FitWidth().CenterHorizontal();
                    if (Ui.ClickButton("Remove"))
                    {
                        int index = i;
                        MadnessUtils.Delay(0.01f, () =>
                        {
                            if (weapon.AnimatedParts.Count > index)
                            {
                                Game.Main.AudioRenderer.PlayOnce(Sounds.UiBad);
                                weapon.AnimatedParts.RemoveAt(index);
                            }
                        });
                    }
                }
                Ui.End();
            }
    }

    private void DrawHoldPointSettings(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        var weaponData = weapon.WeaponData;
        Ui.Theme.FontSize(18).Once();
        Ui.Label("Hold points");
        Ui.Spacer(16);

        Ui.Layout.Height(WeaponEditorComponent.ControlHeight).FitWidth().FitWidth();
        if (Ui.ClickButton("Add point..."))
            weapon.HoldPoints.Add(new Vector2(0));
        Ui.Spacer(16);
        for (int i = 0; i < weapon.HoldPoints.Count; i++)
        {
            Ui.Layout.FitWidth().Height(68);
            Ui.Theme.Foreground((Appearance)Colors.Black.WithAlpha(0.4f)).OutlineWidth(0).Once();
            Ui.StartGroup(true, identity: i);
            {
                var holdPoint = weapon.HoldPoints[i];
                bool canDelete = i != 0;

                float x = holdPoint.X;
                float y = holdPoint.Y;
                Ui.Layout.FitWidth(false).Height(WeaponEditorComponent.ControlHeight);
                Ui.StartGroup(false, identity: i);
                {
                    Ui.Layout.FitContainer(0.5f, 1).CenterVertical().StickLeft().StickTop();
                    if (Ui.FloatInputBox(ref x))
                        weapon.HoldPoints[i] = new Vector2(x, y);
                    Ui.Layout.FitContainer(0.5f, 1).CenterVertical().StickRight().StickTop();
                    if (Ui.FloatInputBox(ref y))
                        weapon.HoldPoints[i] = new Vector2(x, y);
                }
                Ui.End();


                if (holdPoint != Vector2.Zero)
                {
                    Ui.Layout.FitContainer(0.5f, null).Height(WeaponEditorComponent.ControlHeight).StickRight().StickBottom();
                    if (Ui.ClickButton("Reset"))
                        weapon.HoldPoints[i] = Vector2.Zero;
                }

                if (canDelete)
                {
                    Ui.Layout.FitContainer(0.5f, null).Height(WeaponEditorComponent.ControlHeight).StickLeft().StickBottom();
                    if (Ui.ClickButton("Remove"))
                    {
                        int index = i;
                        MadnessUtils.Delay(0.01f, () =>
                        {
                            if (weapon.HoldPoints.Count > index)
                            {
                                Game.Main.AudioRenderer.PlayOnce(Sounds.UiBad);
                                weapon.HoldPoints.RemoveAt(index);
                            }
                        });
                    }
                }
                Ui.End();
            }
        }
    }

    private void DrawWeapon(WeaponEditorComponent editor, WeaponInstructions weapon)
    {
        Draw.Reset();

        //draw person guide
        Draw.Texture = Textures.UserInterface.EditorNpcPlaceholder.Value;
        Draw.Colour = Colors.White.WithAlpha(0.1f);
        var pS = Draw.Texture.Size * 2.3f;
        var personPlaceholderPos = -weapon.HoldPoints.FirstOrDefault() - new Vector2(pS.X - 50, pS.Y / -2);
        Draw.Quad(personPlaceholderPos, pS);
        Draw.Colour = Colors.White;

        //draw base texture
        Draw.Texture = weapon.BaseTexture.Value;
        var hS = Draw.Texture.Size / 2;
        var offset = new Vector2(-hS.X, hS.Y);
        Draw.Quad(offset, Draw.Texture.Size);

        //draw animated parts
        if (weapon.AnimatedParts != null)
            foreach (var item in weapon.AnimatedParts)
            {
                var time = (Time.SecondsSinceLoad / item.Duration) % 1f;

                var visibility = true;
                if (item.VisibilityCurve != null)
                    visibility = item.VisibilityCurve.Evaluate(time) > 0f;
                if (!visibility)
                    continue;

                Draw.Texture = item.Texture.Value;

                var pos = new Vector2(-Draw.Texture.Size.X, Draw.Texture.Size.Y) * 0.5f;

                if (item.TranslationCurve != null)
                {
                    var o = item.TranslationCurve.Evaluate(time);
                    pos += o;
                }

                Draw.Quad(pos, Draw.Texture.Size * item.Scale);
            }

        //draw hold points
        Draw.ResetTexture();
        for (int i = 0; i < weapon.HoldPoints.Count; i++)
        {
            var p = weapon.HoldPoints[i];
            bool firstPoint = i == 0;
            bool canEdit = true;

            if (firstPoint) //TODO waarom is dit zo lmao
                p *= -1;

            if (weapon.WeaponData.IsPumpAction && weapon.AnimatedParts != null && weapon.AnimatedParts.Count > 0 && i == 1)
            {
                var curve = weapon.AnimatedParts[0]?.TranslationCurve;
                if (curve != null)
                {
                    canEdit = false;
                    p = curve.Evaluate((Time.SecondsSinceLoad / weapon.AnimatedParts[0].Duration) % 1f) - new Vector2(0, 15);
                }
            }

            bool isHovering = canEdit && Vector2.Distance(p, Input.WorldMousePosition) < 8;

            if (isHovering && Input.IsButtonPressed(MouseButton.Left))
                currentGizmo = new HoldPointGizmo { Weapon = weapon, HoldPointIndex = i };

            HandLook look = HandLook.HoldPistol;

            if (i != 0)
            {
                if (weapon.HoldForGrip)
                    look = HandLook.HoldRifle;
                else
                    look = HandLook.HoldUnderside;
            }
            else if (i == 0)
            {
                if (weapon.HoldStockHandPose)
                    look = HandLook.HoldStock;
            }

            Draw.Texture = Textures.Character.GetTextureForHandLook(null, look, i > 0 && !weapon.HoldForGrip, weapon.WeaponData.WeaponType);
            Draw.Colour = Color.White.WithAlpha(0.2f);
            Draw.Quad(p + new Vector2(Draw.Texture.Size.X / -2, Draw.Texture.Size.Y / 2), Draw.Texture.Size);
            Draw.ResetTexture();
            if (canEdit)
            {
                Draw.Colour = firstPoint ? Colors.Green : Colors.White;
                Draw.Colour.A = isHovering ? 1 : 0.2f;
                Draw.Circle(p, new Vector2(8));
            }
        }

        //draw firearm guides
        if (weapon.WeaponData.WeaponType is WeaponType.Firearm)
        {
            {
                bool isHovering = Vector2.Distance(weapon.BarrelEndPoint, Input.WorldMousePosition) < 4;

                if (isHovering && Input.IsButtonPressed(MouseButton.Left))
                    currentGizmo = new BarrelGizmo { Weapon = weapon };

                Draw.Colour = Colors.Yellow;
                Draw.Colour.A = isHovering ? 1 : 0.2f;
                Draw.Circle(weapon.BarrelEndPoint, new Vector2(4));
                if (isHovering)
                    Draw.Text("Barrel exit", weapon.BarrelEndPoint + new Vector2(0, 8), new Vector2(0.4f), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                Draw.Colour = Colors.White.WithAlpha(0.1f);
                Draw.Line(weapon.BarrelEndPoint, weapon.BarrelEndPoint + Vector2.UnitX * ((Time.SecondsSinceLoad % 0.5f) * 100 + 32), 2);
            }

            {
                bool isHovering = Vector2.Distance(weapon.CasingEjectionPoint, Input.WorldMousePosition) < 4;

                if (isHovering && Input.IsButtonPressed(MouseButton.Left))
                    currentGizmo = new CasingEjectionPointGizmo { Weapon = weapon };

                Draw.Colour = Colors.Magenta;
                Draw.Colour.A = isHovering ? 1 : 0.2f;
                Draw.Circle(weapon.CasingEjectionPoint, new Vector2(4));
                if (isHovering)
                    Draw.Text("Casing ejection", weapon.CasingEjectionPoint + new Vector2(0, 8), new Vector2(0.4f), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                Draw.Colour = Colors.White.WithAlpha(0.1f);
                Draw.Line(weapon.CasingEjectionPoint, weapon.CasingEjectionPoint + Vector2.UnitY * ((Time.SecondsSinceLoad % 0.5f) * 100 + 32), 2);
            }
        }
        else if (rangeGizmoHeat > 0 && weapon.WeaponData.WeaponType is WeaponType.Melee && weapon.HoldPoints.Count > 0)
        {
            Draw.Colour = Colors.Red.WithAlpha(0.1f * Utilities.Clamp(rangeGizmoHeat));
            Draw.Circle(personPlaceholderPos + new Vector2(pS.X / 2, pS.Y / -2), new Vector2(weapon.WeaponData.Range));
        }

        //draw sharpboxes
        Draw.Reset();
        Draw.Colour = Colors.Cyan.WithAlpha(0.1f);
        bool alreadyHovering = false;
        for (int i = 0; i < weapon.WeaponData.ThrowableSharpBoxes.Count; i++)
        {
            var rect = weapon.WeaponData.ThrowableSharpBoxes[i];

            var s = rect.GetSize();
            var p = new Vector2(rect.TopLeft.X - hS.X, (rect.TopLeft.Y + hS.Y) - s.Y);
            var r = new Rect(p.X, p.Y - s.Y, p.X + s.X, p.Y);

            Draw.OutlineWidth = 2;
            var dist = DistanceToRect(Input.WorldMousePosition - r.GetCenter(), r.GetSize() / 2) * 2;
            if (!alreadyHovering && dist < 5)
            {
                alreadyHovering = true;
                Draw.OutlineColour = Colors.Yellow;
                Draw.OutlineWidth = 3;

                if (Input.IsButtonHeld(MouseButton.Left))
                {
                    if (dist < -5) // center edit, move entire rect
                    {
                        rect.MinX += Input.WorldMouseDelta.X;
                        rect.MaxX += Input.WorldMouseDelta.X;

                        rect.MaxY += Input.WorldMouseDelta.Y;
                        rect.MinY += Input.WorldMouseDelta.Y;
                    }
                    else
                    {
                        var top = DistanceToLineSegment(Input.WorldMousePosition, r.TopLeft, r.TopRight);
                        var bottom = DistanceToLineSegment(Input.WorldMousePosition, r.BottomLeft, r.BottomRight);
                        var right = DistanceToLineSegment(Input.WorldMousePosition, r.BottomRight, r.TopRight);
                        var left = DistanceToLineSegment(Input.WorldMousePosition, r.TopLeft, r.BottomLeft);

                        if (bottom < 5)
                            rect.MaxY -= Input.WorldMouseDelta.Y;
                        if (top < 5)
                        {
                            rect.MaxY += Input.WorldMouseDelta.Y * 2;
                            rect.MinY += Input.WorldMouseDelta.Y;
                        }
                        if (right < 5)
                            rect.MaxX += Input.WorldMouseDelta.X;
                        if (left < 5)
                            rect.MinX += Input.WorldMouseDelta.X;
                    }

                    weapon.WeaponData.ThrowableSharpBoxes[i] = rect;
                }
            }
            else
            {
                Draw.OutlineColour = Colors.Cyan;
                Draw.OutlineWidth = sharpRectToHighlight == i ? 5 : 1;
            }

            Draw.Quad(p, s, 0, 0);
        }
    }

    private static float DistanceToRect(in Vector2 p, in Vector2 b)
    {
        var d = Vector2.Abs(p) - b;
        return (Vector2.Max(d, default)).Length() + MathF.Min(MathF.Max(d.X, d.Y), 0);
    }

    private static float DistanceToLineSegment(in Vector2 p, in Vector2 a, in Vector2 b)
    {
        var pa = p - a;
        var ba = b - a;
        float h = Utilities.Clamp(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba), 0.0f, 1.0f);
        return (pa - ba * h).Length();
    }

    float keyframeEditorHeight = -1;
    private void DrawKeyframeEditor(WeaponEditorComponent editor, WeaponInstructions weapon, AnimatedWeaponPart animatedPart)
    {
        if (keyframeEditorHeight < 0)
            keyframeEditorHeight = WeaponEditorComponent.CurveEditorHeight;
        float h = editor.ExpandedCurveEditor ? WeaponEditorComponent.TallCurveEditorHeight : WeaponEditorComponent.CurveEditorHeight;
        keyframeEditorHeight = Utilities.SmoothApproach(keyframeEditorHeight, h, 15, Time.DeltaTime);

        const float toolbarHeight = 32;
        Ui.Theme.Padding(0).Once();
        Ui.Layout.FitWidth().Scale(-WeaponEditorComponent.SidebarWidth, 0).Height(keyframeEditorHeight).StickBottom();
        Ui.Animate(new SlideAnimation(new Vector2(0, 1)));
        Ui.StartGroup();
        {
            Ui.Layout.FitWidth(false).Height(toolbarHeight).HorizontalLayout();
            Ui.StartScrollView(true);
            {
                Ui.Layout.FitHeight().Width(100).CenterVertical();
                if (Ui.ClickButton("Close"))
                    editor.CurrentSelectedAnimatedPart = null;

                Ui.Layout.FitHeight().Width(100).CenterVertical();
                if (Ui.ClickButton(editor.ExpandedCurveEditor ? "Collapse" : "Expand"))
                    editor.ExpandedCurveEditor = !editor.ExpandedCurveEditor;
            }
            Ui.End();


            Ui.Layout.FitWidth(false).Height(keyframeEditorHeight - toolbarHeight).Move(0, toolbarHeight).VerticalLayout();
            Ui.StartScrollView(false);
            {
                // translation curve
                if (animatedPart.TranslationCurve != null)
                {
                    Ui.Layout.FitWidth(false).Height(114).HorizontalLayout().StickLeft();
                    Ui.StartScrollView(false);
                    {
                        bool somethingRemoved = false;
                        for (int i = 0; i < animatedPart.TranslationCurve.Keys.Length; i++)
                        {
                            var key = animatedPart.TranslationCurve.Keys[i];
                            Ui.Layout.FitHeight().Width(200).StickTop();
                            Ui.Theme.Foreground((Appearance)new Color(25, 25, 25)).Once();
                            Ui.StartGroup(true, i);
                            {
                                Ui.Layout.FitWidth().Height(32).StickLeft().StickTop();
                                Ui.FloatSlider(ref key.Position, Direction.Horizontal, (0, 1), 0.05f, "Time: {0:0.##}");

                                Ui.Layout.FitContainer(.5f, null).Scale(-Ui.Theme.Base.Padding / 2, 0).Height(32).StickLeft().StickTop().Move(0, 35);
                                Ui.Decorators.Tooltip("X");
                                Ui.FloatInputBox(ref key.Value.X);

                                Ui.Layout.FitContainer(.5f, null).Scale(-Ui.Theme.Base.Padding / 2, 0).Height(32).StickRight().StickTop().Move(0, 35);
                                Ui.Decorators.Tooltip("Y");
                                Ui.FloatInputBox(ref key.Value.Y);

                                Ui.Layout.FitWidth().Height(32).StickLeft().StickBottom();
                                if (Ui.ClickButton("Remove"))
                                {
                                    somethingRemoved = true;
                                    animatedPart.TranslationCurve.Keys[i] = null!;
                                }
                            }
                            Ui.End();
                        }
                        if (somethingRemoved)
                            animatedPart.TranslationCurve.Keys = animatedPart.TranslationCurve.Keys.Where(a => a != null).ToArray();
                        Ui.Layout.FitHeight().Width(200).StickTop();
                        if (Ui.ClickButton("+ Add keyframe"))
                        {
                            var newKey = new Curve<Vector2>.Key(default, 1);
                            if (animatedPart.TranslationCurve.Keys.Length > 0)
                            {
                                var l = animatedPart.TranslationCurve.Keys.Last();
                                newKey.Position = l.Position;
                                newKey.Value = l.Value;
                            }
                            animatedPart.TranslationCurve.Keys = animatedPart.TranslationCurve.Keys.Append(newKey).ToArray();
                        }
                    }
                    Ui.End();
                }
                else
                {
                    Ui.Layout.Size(250, 32).StickLeft().StickTop();
                    if (Ui.ClickButton("+ Create translation curve"))
                        animatedPart.TranslationCurve = new Vec2Curve();
                }
            }
            Ui.End();
        }
        Ui.End();
    }
}
