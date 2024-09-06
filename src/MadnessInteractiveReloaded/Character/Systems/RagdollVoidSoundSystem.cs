using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class RagdollVoidSoundSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsPaused(Scene) || Level.CurrentLevel == null)
            return;

        foreach (var ragdoll in Scene.GetAllComponentsOfType<RagdollComponent>())
        {
            if (!ragdoll.ShouldMakeVoidSound)
                continue;

            if (ragdoll.Nodes[0].TryGet(Scene, out var node) && !Level.CurrentLevel.LevelBounds.Expand(100).ContainsPoint(node.Position))
            {
                var asset = Assets.Load<FixedAudioData>("sounds/" + Utilities.PickRandom("lego.wav", "lego2.wav"));
                Audio.PlayOnce(SoundCache.Instance.LoadSoundEffect(asset));
                ragdoll.ShouldMakeVoidSound = false;
            }
        }
    }
}
