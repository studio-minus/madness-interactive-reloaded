using System;
using System.Numerics;

namespace MIR;

/// <summary>
/// Extensions for functions involving <see cref="CharacterComponent"/> and animations.
/// </summary>
public static class AnimatedCharacterExtensions
{
    /// <summary>
    /// Return the accumulated <see cref="AnimationConstraint"/> of all playing animations
    /// </summary>
    public static AnimationConstraint GetAnimationConstraints(this CharacterComponent character)
    {
        var f = AnimationConstraint.AllowAll;

        foreach (var item in character.Animations)
        {
            for (int i = 0; i < item.Animation.Constraints.Count; i++)
            {
                // TODO the time ranges for this should be precomputed during serialisation. e.g the ConstraintKeyframe should have a Duration field
                var current = item.Animation.Constraints[i];
                ConstraintKeyframe? next = (i + 1 == item.Animation.Constraints.Count) ? null : item.Animation.Constraints[i + 1];

                if (item.UnscaledTimer >= current.Time && (!next.HasValue || (item.UnscaledTimer < next?.Time)))
                {
                    f |= current.Constraints;
                    break;
                }
            }
        }

        return f | character.AdditionalConstraints;
    }

    /// <summary>
    /// Returns true if the currently playing animations include all of the given constaints
    /// </summary>
    public static bool AnimationConstrainsAll(this CharacterComponent character, AnimationConstraint mask)
    {
        var v = GetAnimationConstraints(character);
        return (v & mask) == mask;
    }

    /// <summary>
    /// Returns true if the currently playing animations include any of the given constaints
    /// </summary>
    public static bool AnimationConstrainsAny(this CharacterComponent character, AnimationConstraint mask)
    {
        var v = GetAnimationConstraints(character);
        return (v & mask) != 0;
    }

    /// <summary>
    /// Returns true if the given animation group (parent folder) is playing
    /// </summary>
    public static bool IsPlayingAnimationGroup(this CharacterComponent character, in string group)
    {
        foreach (var item in character.Animations)
            if (item.Animation.Group.Equals(group, StringComparison.InvariantCultureIgnoreCase))
                return true;
        return false;
    }
        
    
    /// <summary>
    /// Returns true if the given animation type (as defined in the animation file itself) is playing
    /// </summary>
    public static bool IsPlayingAnimation(this CharacterComponent character, CharacterAnimation anim)
    {
        foreach (var a in character.Animations)
            if (a.Animation == anim)
                return true;
        return false;
    }

    /// <summary>
    /// Add the animation to the animation list
    /// </summary>
    /// <param name="character"></param>
    /// <param name="anim"></param>
    /// <param name="speed"></param>
    /// <returns>The active animation.</returns>
    public static ActiveCharacterAnimation PlayAnimation(this CharacterComponent character, CharacterAnimation anim,
        float speed = 1)
    {
        ResetAnimation(character);
        character.AnimationMixProgress = 0;
        var a = new ActiveCharacterAnimation(anim, speed);
        character.Animations.Add(a);
        return a;
    }

    /// <summary>
    /// Stop animating this character.
    /// </summary>
    /// <param name="character"></param>
    public static void StopAllAnimations(this CharacterComponent character)
    {
        character.Animations.Clear();
        ResetAnimation(character);
    }

    /// <summary>
    /// Reset animation positioning and angles to 0.
    /// </summary>
    /// <param name="character"></param>
    public static void ResetAnimation(this CharacterComponent character)
    {
        character.Positioning.Head.AnimationAngle = 0;
        character.Positioning.Head.AnimationPosition = Vector2.Zero;

        character.Positioning.Body.AnimationAngle = 0;
        character.Positioning.Body.AnimationPosition = Vector2.Zero;

        foreach (var hand in character.Positioning.Hands)
        {
            hand.AnimationAngle = 0;
            hand.AnimationPosition = Vector2.Zero;
            hand.AnimatedHandLook = null;
        }
    }

    public static bool IsHeadAnimated(this CharacterComponent character)
        => character.MainAnimation?.Animation.HeadAnimation != null;

    public static bool IsBodyAnimated(this CharacterComponent character)
        => character.MainAnimation?.Animation.BodyAnimation != null;

    public static bool IsHandAnimated(this CharacterComponent character, HandAnimatedLimb hand)
    {
        if (!character.IsPlayingAnimation)
            return false;

        var anim = character.MainAnimation!.Animation;

        if (anim.HandAnimations == null)
            return false;

        int i = hand.IsLeftHand ? 1 : 0;
        var animationIndex = character.Positioning.IsFlipped ? character.Positioning.Hands.Length - 1 - i : i;

        return anim.HandAnimations.Length - 1 >= animationIndex;
    }
}