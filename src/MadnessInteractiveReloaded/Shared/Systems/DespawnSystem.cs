namespace MIR;

using Walgelijk;

/// <summary>
/// System for despawning entities with <see cref="DespawnComponent"/>.
/// </summary>
public class DespawnSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var des in Scene.GetAllComponentsOfType<DespawnComponent>())
        {
            des.Timer += Time.DeltaTime;
            if (des.Timer > des.DespawnTime)
            {
                Scene.RemoveEntity(des.Entity);

                if (des.AlsoDelete != null)
                    foreach (var also in des.AlsoDelete)
                        Scene.RemoveEntity(also);
            }
        }
    }
}
