namespace MIR;

/// <summary>
/// The agility level used to decide what animations a character will play when being agile.
/// These don't necessarily map to <see cref="CharacterStats.DodgeAbility"/>, it just determines how cool they look while
/// doing it
/// TODO this should be gone and the stats object should have an assetref to a animations
/// </summary>
public enum AgilitySkillLevel
{
    /// <summary>
    /// I can't do shit
    /// </summary>
    None,

    /// <summary>
    /// I can jump a little :)
    /// </summary>
    Novice,

    /// <summary>
    /// Engineers and equivalent
    /// </summary>
    Adept,

    /// <summary>
    /// The player's and soldats agility level
    /// </summary>
    Master,

    // /// <summary>
    // /// Hank etc.
    // /// </summary>
    // FuckingUnhinged
}