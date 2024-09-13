using MIR.Controls;
using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class ExperimentSelectableCharacter : ExperimentModeComponent.IExperimentSelectable
{
    public CharacterComponent Component;

    public ExperimentSelectableCharacter(CharacterComponent component)
    {
        Entity = component.Entity;
        Component = component;
    }

    public Entity Entity { get; }
    public Rect BoundingBox => Component.GetBoundingBox(Game.Main.Scene);
    public string Name => Component.Name;
    public int RaycastOrder { get; set; }
    public bool Disabled => !canDrag || !Component.IsAlive;
    public int UiHeight => 350;

    public bool LookEditorOpen, StatsEditorOpen;

    private bool canDrag = true;

    public void OnDrop(Scene scene)
    {
        if (!canDrag)
            return;

        canDrag = false;
        RoutineScheduler.Start(r());

        IEnumerator<IRoutineCommand> r()
        {
            canDrag = false;
            foreach (var item in FallToGroundRoutine(Component))
                yield return item;
            canDrag = true;
        }
    }

    public static IEnumerable<IRoutineCommand> FallToGroundRoutine(CharacterComponent c)
    {
        float t = 0;
        var a = c.Positioning.GlobalTarget;
        var b = (Level.CurrentLevel?.GetFloorLevelAt(a.X) ?? 0)
            + CharacterConstants.GetFloorOffset(c.Positioning.Scale)
            + c.Positioning.FlyingOffset;

        float duration = Utilities.Clamp(float.Abs(a.Y - b) / 512, 0.01f, .5f);
        while (t < 1)
        {
            c.Positioning.GlobalTarget.Y = Utilities.Lerp(a.Y, b, Easings.Quad.InOut(t));
            yield return new RoutineFrameDelay();
            t += Game.Main.State.Time.DeltaTime / duration;
            if (Game.Main.Scene.FindAnyComponent<ExperimentModeComponent>(out var exp) && !exp.IsEditMode)
                yield break;
        }
        c.Positioning.GlobalTarget.Y = b;
    }

    public void ProcessUi(Scene scene, ExperimentModeComponent exp)
    {
        if (!scene.HasEntity(Component.Entity))
            return;

        if (Component.Stats.DodgeAbility >= float.Epsilon)
        {
            Ui.Layout.FitWidth().Height(32).StickLeft();

            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.5f, 1, false);
                Ui.TextRect(Localisation.Get("dodge"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                Ui.FloatSlider(ref Component.DodgeMeter, Direction.Horizontal, (0, Component.Stats.DodgeAbility), 0.01f, "{0:P0}");
            }
            Ui.End();
        }

        Ui.Layout.FitWidth().Height(32).StickLeft();
        Ui.StartGroup(false);
        {
            Ui.Layout.FitContainer(0.5f, 1, false);
            Ui.TextRect(Localisation.Get("experiment-head-health"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            var head = scene.GetComponentFrom<BodyPartComponent>(Component.Positioning.Head.Entity);
            Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
            Ui.FloatSlider(ref head.Health, Direction.Horizontal, (0.001f, head.MaxHealth), 0.05f, "{0:0.#}");
        }
        Ui.End();

        Ui.Layout.FitWidth().Height(32).StickLeft();
        Ui.StartGroup(false);
        {
            Ui.Layout.FitContainer(0.5f, 1, false);
            Ui.TextRect(Localisation.Get("experiment-body-health"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);
            var body = scene.GetComponentFrom<BodyPartComponent>(Component.Positioning.Body.Entity);
            Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
            Ui.FloatSlider(ref body.Health, Direction.Horizontal, (0.001f, body.MaxHealth), 0.05f, "{0:0.#}");
        }
        Ui.End();

        if (scene.TryGetComponentFrom<CharacterPresetComponent>(Component.Entity, out var characterPresetComp))
        {
            bool hasChanged = false;

            // TODO look & stats editor?
            var stats = characterPresetComp.Preset.Stats;

            //Ui.Layout.FitWidth().Height(32).StickLeft();
            //Ui.StartGroup(false);
            //{
            //    Ui.Layout.FitContainer(0.5f, 1, false);
            //    Ui.TextRect(Localisation.Get("max-dodge"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

            //    Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
            //    if (Ui.FloatInputBox(ref stats.DodgeAbility))
            //        hasChanged = true;
            //}
            //Ui.End();     

            Ui.Layout.Height(32).FitWidth().StickLeft();
            if (Ui.Button("Edit appearance"))
                LookEditorOpen = true;

            Ui.Layout.Height(32).FitWidth().StickLeft();
            if (Ui.Button("Edit stats"))
                StatsEditorOpen = true;

            if (hasChanged)
                try
                {
                    characterPresetComp.SaveChanges();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save character preset: " + e);
                }
        }
        else
        {
            //Ui.Spacer(8);
            //int index = Array.IndexOf(exp.AvailableLooks, Component.Look);
            //Ui.Layout.StickLeft();
            //Ui.Label("Look");
            //Ui.Layout.Height(32).FitWidth().StickLeft();
            //if (Ui.Dropdown(exp.AvailableLooks, ref index))
            //{
            //    Component.Look = exp.AvailableLooks[index];
            //    Component.NeedsLookUpdate = true;
            //}

            //index = Array.IndexOf(exp.AvailableStats, Component.Stats);
            //Ui.Spacer(8);
            //Ui.Layout.StickLeft();
            //Ui.Label("Stats");
            //Ui.Layout.Height(32).FitWidth().StickLeft();
            //if (Ui.Dropdown(exp.AvailableStats, ref index))
            //    Component.Stats = exp.AvailableStats[index];

            if (Component.EquippedWeapon.IsValid(scene))
            {
                Ui.Layout.Height(32).FitWidth().StickLeft();
                if (Ui.ClickButton(Localisation.Get("experiment-drop-weapon")))
                    MadnessUtils.Delay(0.1f, () =>
                    {
                        Component.DropWeapon(scene);
                    });
            }
        }

        Ui.Spacer(8);
        Ui.Layout.FitWidth().Height(48).StickLeft();
        var a = Colors.White.WithAlpha(Utilities.MapRange(-1, 1, 0.6f, 1, float.Sin(scene.Game.State.Time * 12)));
        Ui.Theme.OutlineWidth(0).ForegroundColor(Colors.White).Image(new(Colors.White, a)).Once();
        Ui.Decorate(new FancyButtonDecorator());
        if (LayeredImageButton.Start(Assets.Load<Texture>("textures/ui/experimentmode/kill_bg.png").Value, Assets.Load<Texture>("textures/ui/experimentmode/kill_fg.png").Value))
        {
            MadnessUtils.Delay(0, () =>
            {
                Component.Kill();
                exp.SelectionManager.DeselectAll();
                // we ignore animation constraints
                MadnessUtils.TurnIntoRagdoll(scene, Component, Utilities.RandomPointInCircle() * 20, Utilities.RandomFloat(-1, 1));
                if (!MadnessUtils.IsPlayerAlive(scene))
                    scene.GetSystem<ExperimentModeSystem>().ExitEditMode(exp);
            });
        }
    }

    public void Rotate(Scene scene, float th)
    {
        bool o = Component.Positioning.IsFlipped;
        Component.Positioning.IsFlipped = th > 0;
        if (o != Component.Positioning.IsFlipped)
        {
            // offset aim so that its 
            Component.AimTargetPosition.X -= Component.Positioning.GlobalCenter.X;
            Component.AimTargetPosition.X *= -1;
            Component.AimTargetPosition.X += Component.Positioning.GlobalCenter.X;
        }
        Component.NeedsLookUpdate = true;
    }

    public void Translate(Scene scene, Vector2 offset)
    {
        Component.Positioning.GlobalTarget += offset;
    }
}
