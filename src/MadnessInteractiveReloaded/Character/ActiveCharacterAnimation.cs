using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Used for an animation that is actually playing right now.
/// </summary>
public class ActiveCharacterAnimation
{
    public readonly CharacterAnimation Animation;

    /// <summary>
    /// The playbackrate of this animation.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Timer unaffected by <see cref="Speed"/>.
    /// </summary>
    public float UnscaledTimer;

    public ActiveCharacterAnimation(CharacterAnimation animation, float speed)
    {
        Animation = animation;
        Speed = speed;

        MaxKeyCount = 0;

        if (animation.HandAnimations != null)
            foreach (var item in animation.HandAnimations)
            {
                AdjustMax(ref MaxKeyCount, item.TranslationCurve);
                AdjustMax(ref MaxKeyCount, item.RotationCurve);
            }

        AdjustMax(ref MaxKeyCount, animation.HeadAnimation?.TranslationCurve);
        AdjustMax(ref MaxKeyCount, animation.HeadAnimation?.RotationCurve);
        AdjustMax(ref MaxKeyCount, animation.BodyAnimation?.TranslationCurve);
        AdjustMax(ref MaxKeyCount, animation.BodyAnimation?.RotationCurve);

        void AdjustMax<T>(ref int m, Curve<T>? curve) where T : notnull => m = int.Max(m, curve?.Keys.Length ?? 0);
    }

    public float ScaledTimer => UnscaledTimer * Speed;

    public float ScaledDuration => Speed * Animation.TotalDuration;
    public bool IsOver => UnscaledTimer > Animation.TotalDuration;
    public bool IsAlmostOver(float percentageOver = 0.95f) => UnscaledTimer > Animation.TotalDuration * percentageOver;

    public event Action? OnEnd;
    public readonly int MaxKeyCount;

    public float CalculateProgress(LimbAnimation anim) => UnscaledTimer / anim.Duration;

    public Vector2 GetHandPosition(int index)
    {
        if (Animation.HandAnimations == null)
            return default;
        var anim = Animation.HandAnimations[index];
        return anim.TranslationCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
    }

    public Vector2 GetHeadPosition()
    {
        if (Animation.HeadAnimation == null)
            return default;
        var anim = Animation.HeadAnimation;
        return anim.TranslationCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
    }

    public float GetHeadRotation()
    {
        if (Animation.HeadAnimation == null)
            return default;
        var anim = Animation.HeadAnimation;
        return anim.RotationCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
    }

    public Vector2 GetHeadScale()
    {
        if (Animation.HeadAnimation == null)
            return Vector2.One;
        var anim = Animation.HeadAnimation;
        return anim.ScaleCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? Vector2.One;
    }

    public Vector2 GetBodyPosition()
    {
        if (Animation.BodyAnimation == null)
            return default;
        var anim = Animation.BodyAnimation;
        return anim.TranslationCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
    }

    public float GetBodyRotation()
    {
        if (Animation.BodyAnimation == null)
            return default;
        var anim = Animation.BodyAnimation;
        return anim.RotationCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
    }

    public Vector2 GetBodyScale()
    {
        if (Animation.BodyAnimation == null)
            return Vector2.One;
        var anim = Animation.BodyAnimation;
        return anim.ScaleCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? Vector2.One;
    }

    public void Stop()
    {
        UnscaledTimer = float.MaxValue;
    }

    internal void InvokeOnEnd()
    {
        OnEnd?.Invoke();
    }
}

