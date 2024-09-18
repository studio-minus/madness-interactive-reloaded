using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Manages <see cref="AccurateShotComponent"/>s.
/// </summary>
public class AccurateShotSystem : Walgelijk.System
{
    public override void Update()
    {
        var paused = MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene);

        foreach (var comp in Scene.GetAllComponentsOfType<AccurateShotComponent>())
        {
            if (!comp.OriginCharacter.TryGet(Scene, out var origin))
                continue;

            if (!comp.TargetCharacter.TryGet(Scene, out var target))
                continue;

            var ai = Scene.GetComponentFrom<AiComponent>(comp.OriginCharacter.Entity);
            // TODO why can't we make accurate shots for non-ai characters?
            ai.IsDoingAccurateShot = true;
            ai.WantsToIronSight.Value = true;
            origin.AttacksCannotBeAutoDodged = true;

            if (!paused)
                comp.Time += Time.DeltaTime;

            if (!target.IsAlive || !origin.IsAlive || target.HasBeenRagdolled || origin.HasBeenRagdolled || !origin.HasWeaponEquipped)
            {
                Game.Main.AudioRenderer.Stop(Sounds.AccurateShotWarning);
                AbortAccurateShot(comp.Entity, ai, origin);
            }

            if (comp.Time >= comp.Lifespan && origin.EquippedWeapon.TryGet(Scene, out var wpn))
            {
                wpn.IsFiring = true;
                comp.Finished = true;
                MadnessUtils.Flash(Colors.White.WithAlpha(0.5f), 0.05f);
                MadnessUtils.Flash(Colors.Red.WithAlpha(0.8f), 0.1f);
                AbortAccurateShot(comp.Entity, ai, origin);
            }
        }
    }

    private void AbortAccurateShot(Entity entity, AiComponent ai, CharacterComponent origin)
    {
        Scene.RemoveEntity(entity);
        ai.IsDoingAccurateShot = false;
        MadnessUtils.DelayPausable(0.05f, () => origin.AttacksCannotBeAutoDodged = false);
    }

    public override void Render()
    {
        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundInFront.WithOrder(100);
        foreach (var comp in Scene.GetAllComponentsOfType<AccurateShotComponent>())
        {
            if (!comp.OriginCharacter.TryGet(Scene, out var origin))
                continue;

            if (!comp.TargetCharacter.TryGet(Scene, out var target))
                continue;

            if (!origin.HasWeaponEquipped || !Scene.TryGetComponentFrom<TransformComponent>(origin.EquippedWeapon.Entity, out var weaponTransform))
                continue;

            var targetPos = origin.AimTargetPosition;

            float progress = comp.Time / comp.Lifespan;
            var pl = progress * progress;
            var progressWithStartFlash = MathF.Max(Utilities.Clamp(1 - (progress * 10)), pl);

            Draw.Colour = Utilities.Lerp(Colors.White, Colors.White.WithAlpha(0), Utilities.Clamp(pl * 40));
            Draw.Line(weaponTransform.Position, targetPos, 5);

            var c = progress > 0.1f ? Colors.Red : Colors.White;
            Draw.Colour = Utilities.Lerp(c.WithAlpha(0), c, progressWithStartFlash);

            drawCross(targetPos, 3000, progress > 0.1f ? progress : 1);

            //if (progress < 0.1f)
            //    Draw.Circle(targetPos, new Vector2(256));
        }

        static void drawCross(Vector2 center, float size, float fillPercentage)
        {
            var fill = size * (1 - fillPercentage);

            //top line ⌖
            Draw.Line(center + new Vector2(0, fill), center + new Vector2(0, size), 9);
            //bottom line ⌖
            Draw.Line(center - new Vector2(0, fill), center - new Vector2(0, size), 9);

            //right line ⌖
            Draw.Line(center + new Vector2(fill, 0), center + new Vector2(size, 0), 9);
            //left line ⌖
            Draw.Line(center - new Vector2(fill, 0), center - new Vector2(size, 0), 9);
        }
    }
}