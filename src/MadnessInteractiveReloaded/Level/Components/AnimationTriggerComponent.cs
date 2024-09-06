using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Plays an animation o a character when they enter this trigger.
/// </summary>
public class AnimationTriggerComponent : Component
{
    /// <summary>
    /// The trigger bounds to check overlaps with.
    /// </summary>
    public Rect WorldRect;

    /// <summary>
    /// The animation to play.
    /// </summary>
    public string Animation = string.Empty;

    /// <summary>
    /// What sequence of the level the animation is allowed to play.
    /// </summary>
    public TriggerType StartEvent;

    /// <summary>
    /// What should trigger the animation.
    /// </summary>
    public TargetType Target;

    public float ExistingLerpTime = 0;

    /// <summary>
    /// When <see cref="TargetType"/> is <see cref="TargetType.CreateNew"/>
    /// will create the character at this position.
    /// </summary>
    public Vector2 TargetPosition;

    /// <summary>
    /// If to flip the direction for the character.
    /// </summary>
    public bool TargetFlipped;

    /// <summary>
    /// If <see cref="Target"/> is <see cref="TargetType.CreateNew"/>
    /// what the <see cref="CharacterLook"/> is set to on the created character.
    /// </summary>
    public string? CreateNewLook;

    /// <summary>
    /// If <see cref="Target"/> is <see cref="TargetType.CreateNew"/>
    /// what the <see cref="CharacterStats"/> is set to on the created character.
    /// </summary>
    public string? CreateNewStats;

    /// <summary>
    /// If <see cref="Target"/> is <see cref="TargetType.CreateNew"/>
    /// what the <see cref="Faction"/> is set to on the created character.
    /// </summary>
    public string CreateNewFaction = "aahw";

    /// <summary>
    /// The animation has been triggered already and thus won't be triggered again.
    /// </summary>
    public bool IsExpired = false;

    /// <summary>
    /// Return a clone of this <see cref="AnimationTriggerComponent"/>.
    /// </summary>
    /// <returns></returns>
    public AnimationTriggerComponent Clone()
        => new()
        {
            WorldRect = WorldRect,

            Animation = Animation,
            StartEvent = StartEvent,
            Target = Target,

            ExistingLerpTime = ExistingLerpTime,

            TargetPosition = TargetPosition,
            TargetFlipped = TargetFlipped,

            CreateNewLook = CreateNewLook,
            CreateNewStats = CreateNewStats,
            CreateNewFaction = CreateNewFaction,

            IsExpired = IsExpired
        };

    /// <summary>
    /// <see cref="Nearest"/> whatever character is closest.
    /// <see cref="Player"/> only the player character.
    /// <see cref="CreateNew"/> spawns a character and plays the animation on them.
    /// </summary>
    public enum TargetType
    {
        Nearest,
        Player,
        CreateNew,
    }

    /// <summary>
    /// What point in the level this trigger is active.
    /// </summary>
    public enum TriggerType
    {
        LevelStart,
        LevelComplete,
        //Timed
    }
}
