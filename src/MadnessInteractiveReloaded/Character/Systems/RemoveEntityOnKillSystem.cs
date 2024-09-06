namespace MIR;

using Walgelijk;

/// <summary>
/// Removes entity when their <see cref="CharacterComponent"/> dies.
/// </summary>
public class RemoveEntityOnKillSystem : Walgelijk.System
{
    public override void Update()
    {
        foreach (var comp in Scene.GetAllComponentsOfType<RemoveEntityOnKillComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(comp.Character);
            if (!character.IsAlive)
                Scene.RemoveEntity(comp.Entity);
        }
    }
}
