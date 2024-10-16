using System;
using System.Numerics;

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

        //TODO this code is not pretty

        MaxKeyCount = 0;
        if (animation.HandAnimations != null)
            foreach (var item in animation.HandAnimations)
            {
                MaxKeyCount = int.Max(MaxKeyCount, item.TranslationCurve?.Keys.Length ?? 0);
                MaxKeyCount = int.Max(MaxKeyCount, item.RotationCurve?.Keys.Length ?? 0);
            }

        if (animation.HeadAnimation != null)
        {
            MaxKeyCount = int.Max(MaxKeyCount, animation.HeadAnimation.TranslationCurve?.Keys.Length ?? 0);
            MaxKeyCount = int.Max(MaxKeyCount, animation.HeadAnimation.RotationCurve?.Keys.Length ?? 0);
        }

        if (animation.BodyAnimation != null)
        {
            MaxKeyCount = int.Max(MaxKeyCount, animation.BodyAnimation.TranslationCurve?.Keys.Length ?? 0);
            MaxKeyCount = int.Max(MaxKeyCount, animation.BodyAnimation.RotationCurve?.Keys.Length ?? 0);
        }
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
            return default;
        var anim = Animation.HeadAnimation;
        return anim.ScaleCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
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
            return default;
        var anim = Animation.BodyAnimation;
        return anim.ScaleCurve?.Evaluate(UnscaledTimer / anim.Duration) ?? default;
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

