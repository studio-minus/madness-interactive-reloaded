using System;
using System.IO;
using Walgelijk;
using Walgelijk.SimpleDrawing;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Decorators;

using static MIR.ExperimentModeComponent;
using System.Linq;
using System.Collections.Generic;
using Walgelijk.Localisation;

namespace MIR;

public readonly struct CharacterContextMenuDecorator : IDecorator
{
    private readonly ExperimentCharacterPreset preset;
    private readonly ExperimentModeComponent exp;

    public CharacterContextMenuDecorator(ExperimentCharacterPreset preset, ExperimentModeComponent exp)
    {
        this.preset = preset;
        this.exp = exp;
    }

    public void RenderAfter(in ControlParams p)
    {
        var s = Game.Main.Scene;
        if (!s.FindAnyComponent<EnemySpawningComponent>(out var spawn))
            return;

        var preset = this.preset;
        var exp = this.exp;
        if (Game.Main.State.Input.IsButtonPressed(MouseButton.Right)) // TODO UI input should have different mouse buttons
            if (p.Instance.Rects.ComputedDrawBounds.ContainsPoint(Game.Main.State.Input.WindowMousePosition))
            {
                exp.ContextMenuInstance = new ContextMenu(p.Input.MousePosition, () =>
                {
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.ClickButton(Localisation.Get("experiment-duplicate-preset")))
                    {
                        CharacterPresetDeserialiser.Save(
                            preset.Name + " (copy)",
                            new(preset.Look),
                            new(preset.Stats),
                            UserData.Paths.ExperimentCharacterPresets + Guid.NewGuid() + ".preset");
                        Registries.LoadCharacterPresets();
                        exp.ContextMenuInstance = null;
                    }


                    // TODO cache this, probably
                    // makes everything so messy though
                    // is there a more elegant way to speed this up? keep a hashmap?
                    bool autospawn = spawn.SpawnInstructions?.Contains(preset) ?? false;
                    bool c = autospawn;
                    Ui.Layout.FitWidth().Height(32).StickLeft();
                    if (Ui.Checkbox(ref c, Localisation.Get("experiment-autospawn")))
                    {
                        if (autospawn)
                        {
                            spawn.SpawnInstructions?.Remove(preset);
                        }
                        else
                        {
                            spawn.SpawnInstructions ??= [];
                            spawn.SpawnInstructions.Add(preset);
                        }
                    }

                    if (preset.Mutable)
                    {
                        Ui.Theme.Text(new(Colors.Red, Colors.White)).OutlineColour(Colors.Red).Foreground((Appearance)Colors.Transparent).OutlineWidth(1);
                        Ui.Layout.FitWidth().Height(32).StickLeft();
                        if (Ui.ClickButton(Localisation.Get("delete")))
                        {
                            if (Game.Main.State.Input.IsKeyHeld(Key.LeftShift))
                                remove();
                            else
                                Game.Main.Scene.AttachComponent(Game.Main.Scene.CreateEntity(), new ConfirmationDialogComponent(
                                    string.Format(Localisation.Get("frmt-confirm-delete"), preset.Name),
                                    remove
                                ));

                            exp.ContextMenuInstance = null;

                            void remove()
                            {
                                var path = Resources.GetPathAssociatedWith(preset);
                                if (string.IsNullOrEmpty(path))
                                    Logger.Error("Could not find file for preset " + preset.Name);
                                else
                                {
                                    File.Delete(path);
                                    Registries.LoadCharacterPresets();
                                    spawn.SpawnInstructions?.Remove(preset);
                                }
                            }
                        }
                    }
                });
            }

        if (!preset.Mutable)
        {
            Draw.Colour = Colors.White;
            var r = p.Instance.Rects.Rendered;
            Draw.Image(Textures.UserInterface.Locked.Value, r with { MinX = r.MaxX - r.Height }, ImageContainmentMode.Center);
        }
    }

    public void RenderBefore(in ControlParams p)
    {

    }
}