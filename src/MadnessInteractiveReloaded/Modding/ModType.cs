namespace MIR;

/// <summary>
/// The mod type represents the abilties of a mod
/// </summary>
public enum ModType
{
    /// <summary>
    /// The mod contains an assembly
    /// </summary>
    Script,
    /// <summary>
    /// The mod only replaces/adds data-based content
    /// </summary>
    Data,
    /// <summary>
    /// Reserved for ??????
    /// </summary>
    Unknown
}
