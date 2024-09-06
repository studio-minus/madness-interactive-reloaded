using Walgelijk;

namespace MIR;

/// <summary>
/// Makes characters dance if they have the <see cref="DancingCharacterComponent"/>.
/// </summary>
public class DancingCharacterSystem : Walgelijk.System
{
    public override void Update()
    {
        if (!Scene.FindAnyComponent<DjComponent>(out var dj))
            return;

        foreach (var dancing in Scene.GetAllComponentsOfType<DancingCharacterComponent>())
        {
            if (!Scene.TryGetComponentFrom<CharacterComponent>(dancing.Entity, out var character))
                continue;

            dancing.Life += Time.DeltaTime;

            if (dancing.Enabled && DjComponent.PlaybackState == DjComponent.State.Playing && dancing.Life > dancing.LifeThreshold)
            {
                if (Scene.TryGetComponentFrom<AiComponent>(dancing.Entity, out var ai))
                    ai.IsDocile = true;

                character.AllowWalking = false;
                character.Faction = Registries.Factions.Get("civil");

                if (!character.IsPlayingAnimation)
                {
                    var dance = dancing.Dance;
                    character.DropWeapon(Scene);
                    character.PlayAnimation(dance,
                        DjComponent.CalculatedAnimationSpeed * dance.TotalDuration * 3);
                }
            }
        }
    }
}

