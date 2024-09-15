namespace MIR;

using Walgelijk;

/// <summary>
/// A <see cref="System"/> that animates each <see cref="Entity"/> with a <see cref="AnimationComponent"/>
/// </summary>
public class AnimationSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if(!MadnessUtils.IsPaused(Scene))
        foreach (var animation in Scene.GetAllComponentsOfType<AnimationComponent>())
        {
            var transform = Scene.GetComponentFrom<TransformComponent>(animation.Entity);
            var t = (animation.CurrentPlaybackTime / animation.Duration);

            if (animation.Loops)
                t %= 1;
            else if (t > 1)
            {
                t = 1;
                animation.IsPlaying = false;
                animation.CurrentPlaybackTime = 0;
            }

            if (animation.Reversed)
                t = 1 - t;

            t = Utilities.Clamp(t);

            switch (animation.InterpolationMode)
            {
                case AnimationInterpolationMode.EaseOut:
                    //var r = 1 - t;
                    //t = 1f - r * r * r;
                    t = Easings.Cubic.Out(t);
                    break;
                case AnimationInterpolationMode.EaseIn:
                    //t = t * t * t;
                    t = Easings.Cubic.In(t);
                    break;
                case AnimationInterpolationMode.EaseInOut:
                    t = Easings.Cubic.InOut(t);
                    break;
                default:
                case AnimationInterpolationMode.Linear:
                    //doe niks
                    break;
            }

            if (animation.Translational != null && animation.Translational.Keys.Length > 0)
                transform.Position = animation.Translational.Evaluate(t);

            if (animation.Rotational != null && animation.Rotational.Keys.Length > 0)
                transform.Rotation = animation.Rotational.Evaluate(t);

            if (animation.Scaling != null && animation.Scaling.Keys.Length > 0)
                transform.Scale = animation.Scaling.Evaluate(t);

            var hasVisibilityAnim = animation.Visibility != null && animation.Visibility.Keys.Length > 0;
            var hasTintAnim = animation.Tint != null && animation.Tint.Keys.Length > 0;
            if ((hasVisibilityAnim || hasTintAnim) && Scene.TryGetComponentFrom<ShapeComponent>(animation.Entity, out var shape))
            {
                if (hasVisibilityAnim)
                    shape.Visible = animation.Visibility?.Evaluate(t) > 0.5f;

                if (hasTintAnim)
                    shape.RenderTask.Material.SetUniform(animation.TintUniformName, animation.Tint?.Evaluate(t) ?? Colors.White);
            }

            if (animation.IsPlaying)
                animation.CurrentPlaybackTime += Time.DeltaTime;
        }

        foreach (var animation in Scene.GetAllComponentsOfType<BackgroundOffsetAnimationComponent>())
        {
            if (MadnessUtils.IsPaused(Scene) && animation.AffectedByTimeScale)
                continue;
            var background = Scene.GetComponentFrom<Background.BackgroundComponent>(animation.Entity);
            var t = Utilities.Clamp(animation.CurrentPlaybackTime / animation.Duration);

            if (animation.Loops)
                t %= 1;
            else if (t > 1)
            {
                t = 1;
                animation.IsPlaying = false;
                animation.CurrentPlaybackTime = animation.FillForwards ? 1 : 0;
            }

            if (animation.Reversed)
                t = 1 - t;

            if (animation.OffsetCurve != null && animation.OffsetCurve.Keys.Length > 0)
                background.Offset = animation.OffsetCurve.Evaluate(t);

            if (animation.IsPlaying)
                animation.CurrentPlaybackTime += animation.AffectedByTimeScale ? Time.DeltaTime : Time.DeltaTimeUnscaled;
        }
    }
}
