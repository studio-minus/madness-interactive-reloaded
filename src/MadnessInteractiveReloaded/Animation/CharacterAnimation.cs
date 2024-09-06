using System;
using System.Collections.Generic;

namespace MIR;

/// <summary>
/// Animation information specific to Characters.
/// See <see cref="AnimatedCharacterExtensions"/>
/// </summary>
public class CharacterAnimation
{
    public readonly string Name, Group;

    public CharacterAnimation(string name, string group)
    {
        Name = name;
        Group = group;
    }

    /// <summary>
    /// Constraints by keyframe, sorted chronologically
    /// </summary>
    public readonly List<ConstraintKeyframe> Constraints = new();

    public LimbAnimation? HeadAnimation = null;
    public LimbAnimation? BodyAnimation = null;
    public HandLimbAnimation[]? HandAnimations = null;
    public float TotalDuration = 0;
    public bool DoSmoothing = false;
    public bool RelativeHandPosition;

    public void CalculateTotalDuration()
    {
        TotalDuration = 0;

        if (HeadAnimation != null)
            TotalDuration = float.Max(HeadAnimation.Duration, TotalDuration);
        if (BodyAnimation != null)
            TotalDuration = float.Max(BodyAnimation.Duration, TotalDuration);
        if (HandAnimations != null)
            foreach (var handAnimation in HandAnimations)
                TotalDuration = float.Max(handAnimation.Duration, TotalDuration);
    }
}

public record struct ConstraintKeyframe(float Time, AnimationConstraint Constraints)
{
    public static implicit operator (float Time, AnimationConstraint Constraints)(ConstraintKeyframe value)
    {
        return (value.Time, value.Constraints);
    }

    public static implicit operator ConstraintKeyframe((float Time, AnimationConstraint Constraints) value)
    {
        return new ConstraintKeyframe(value.Time, value.Constraints);
    }
}