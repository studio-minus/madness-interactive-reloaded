using Walgelijk;

namespace MIR;

/// <summary>
/// Will delete the entity when the Character dies.
/// </summary>
[RequiresComponents(typeof(CharacterComponent))]
public class RemoveEntityOnKillComponent : Component
{
    /// <summary>
    /// The entity with a <see cref="CharacterComponent"/>
    /// to delete when it dies.
    /// </summary>
    public Entity Character;
}