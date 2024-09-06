using MIR.Controls;
using System.Numerics;
using Walgelijk;
using Walgelijk.Localisation;
using Walgelijk.Onion;

namespace MIR;

public class ExperimentSelectableWeapon : ExperimentModeComponent.IExperimentSelectable
{
    public WeaponComponent Component;

    public ExperimentSelectableWeapon(WeaponComponent component)
    {
        Entity = component.Entity;
        Component = component;
    }

    public Entity Entity { get; }
    public Rect BoundingBox => Component.GetBoundingBox(Game.Main.Scene);
    public string Name => Component.Data.Name;
    public int RaycastOrder { get; set; }
    public bool Disabled => false;
    public int UiHeight => 166;

    public void OnDrop(Scene scene)
    {
        if (Component.Wielder.TryGet(scene, out var wielder))
            RoutineScheduler.Start(ExperimentSelectableCharacter.FallToGroundRoutine(wielder).GetEnumerator());
        else
            foreach (var item in scene.GetAllComponentsOfType<CharacterComponent>())
            {
                var box = item.GetBoundingBox(scene);
                if (box.ContainsPoint(scene.Game.State.Input.WorldMousePosition))
                {
                    item.EquipWeapon(scene, Component);
                    break;
                }
            }
    }

    public void ProcessUi(Scene scene, ExperimentModeComponent exp)
    {
        if (Component.Data.WeaponType is WeaponType.Firearm)
        {
            Ui.Layout.FitWidth().Height(32).StickLeft();
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.5f, 1, false);
                Ui.TextRect(Localisation.Get("ammo"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                if (Component.InfiniteAmmo)
                {
                    Ui.Theme.Text(Ui.Theme.Base.Text.Default.WithAlpha(0.5f)).Once();
                    Ui.TextRect(Localisation.Get("infinite"), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                }
                else
                    Ui.IntStepper(ref Component.RemainingRounds, (0, Component.Data.RoundsPerMagazine), 1);
            }
            Ui.End();

            Ui.Layout.FitWidth().Height(32).StickLeft();
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer(0.5f, 1, false);
                Ui.TextRect(Localisation.Get("infinite-ammo"), HorizontalTextAlign.Left, VerticalTextAlign.Middle);

                Ui.Layout.FitContainer(0.5f, 1, false).StickRight(false);
                Toggle.Start(ref Component.InfiniteAmmo);
            }
            Ui.End();
        }

        if (!Component.Wielder.IsValid(scene))
        {
            Ui.Layout.Height(32).FitWidth().StickLeft();
            if (Ui.ClickButton(Localisation.Get("flip")))
            {
                Component.IsFlipped = !Component.IsFlipped;
                var weaponTransform = scene.GetComponentFrom<TransformComponent>(Component.Entity);
                weaponTransform.Scale = new Vector2(1, Component.IsFlipped ? -1 : 1);
                weaponTransform.Rotation += 180;
            }
        }
        Ui.Layout.Height(32).FitWidth().StickLeft();
        Ui.Theme.Foreground((Appearance)Colors.Transparent).Text(new(Colors.Red, Colors.White)).OutlineWidth(2).Once();
        if (Ui.ClickButton(Localisation.Get("delete")))
        {
            MadnessUtils.Delay(0.1f, () =>
            {
                exp.SelectionManager.DeselectAll();
                foreach (var b in scene.GetComponentFrom<DespawnComponent>(Entity).AlsoDelete!)
                    scene.RemoveEntity(b);
                scene.RemoveEntity(Entity);
                exp.CopySceneToSelectionManager(scene);
                exp.SelectionManager.UpdateOrder();
            });
        }
    }

    public void Rotate(Scene scene, float th)
    {
        if (Component.Wielder.IsValid(scene))
            return;
        if (scene.TryGetComponentFrom<TransformComponent>(Component.Entity, out var transform))
            transform.Rotation += th;
    }

    public void Translate(Scene scene, Vector2 offset)
    {
        // if the weapon is being carried, we should move the wielder and not the weapon
        if (Component.Wielder.TryGet(scene, out var wielder))
            wielder.Positioning.GlobalTarget += offset;
        // otherwise, just move the weapon
        else if (scene.TryGetComponentFrom<TransformComponent>(Component.Entity, out var transform))
            transform.Position += offset;
    }
}
