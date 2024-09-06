using System;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Decorators;

using static MIR.ExperimentModeComponent;

namespace MIR;

public readonly struct WeaponContextMenuDecorator : IDecorator
{
    private readonly string wpn;
    private readonly ExperimentModeComponent exp;

    public WeaponContextMenuDecorator(string wpn, ExperimentModeComponent exp)
    {
        this.wpn = wpn;
        this.exp = exp;
    }

    public void RenderAfter(in ControlParams p)
    {
        var s = Game.Main.Scene;
        if (!s.FindAnyComponent<EnemySpawningComponent>(out var spawn))
            return;

        var wpn = this.wpn;
        var exp = this.exp;
        if (Game.Main.State.Input.IsButtonPressed(MouseButton.Right)) // TODO UI input should have different mouse buttons
            if (p.Instance.Rects.ComputedDrawBounds.ContainsPoint(Game.Main.State.Input.WindowMousePosition))
            {
                exp.ContextMenuInstance = new ContextMenu(p.Input.MousePosition, () =>
                {
                    // TODO cache this, probably
                    // makes everything so messy though
                    // is there a more elegant way to speed this up? keep a hashmap?
                    bool autospawn = spawn.WeaponsToSpawnWith?.Contains(wpn) ?? false;
                    bool c = autospawn;
                    Ui.Layout.FitWidth().Height(32).StickLeft();

                    if (Ui.Checkbox(ref c, " Autospawn"))
                    {
                        if (autospawn)
                        {
                            spawn.WeaponsToSpawnWith?.Remove(wpn);
                        }
                        else
                        {
                            spawn.WeaponsToSpawnWith ??= [];
                            spawn.WeaponsToSpawnWith.Add(wpn);
                        }
                    }

                });
            }
    }

    public void RenderBefore(in ControlParams p)
    {

    }
}
