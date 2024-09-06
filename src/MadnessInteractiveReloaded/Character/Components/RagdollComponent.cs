using System.Collections.Generic;
using Walgelijk;

namespace MIR;

/// <summary>
/// A <see cref="Component"/> for a physics-based ragdoll.
/// </summary>
public class RagdollComponent : Component
{
    /// <summary>
    /// The <see cref="Entity"/> the ragdoll is create from. Typically created on Entities with <see cref="CharacterComponent"/>s.
    /// </summary>
    public Entity Main;

    public readonly List<ComponentRef<VerletNodeComponent>> Nodes = new();
    public readonly List<ComponentRef<VerletLinkComponent>> Links = new();
    public readonly List<ComponentRef<VerletTransformComponent>> TransformLinks = new();

    public bool ShouldDelete = true;

    /// <summary>
    /// How long has this ragdoll been around? (in seconds)
    /// </summary>
    public float Lifetime = 0;

    /// <summary>
    /// For how long this ragdoll's physics have been essentially settled and not moving much.
    /// </summary>
    public float SleepingTime = 0;
    public bool ShouldMakeVoidSound = Utilities.RandomFloat() < 0.0025f;

    /// <summary>
    /// Delete this entity and cleanup all the physics entities.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="stamp">Whether or not to stamp the character to the background. <see cref="Stamper.Stamp(Scene, CharacterPositioning)"/></param>
    public void Delete(Scene scene, bool stamp = true)
    {
        if (!scene.HasEntity(Main))
            return;

        scene.RemoveEntity(Main);

        var pos = scene.GetComponentFrom<CharacterComponent>(Main).Positioning;

        if (stamp)
            Stamper.Stamp(scene, pos);

        foreach (var ent in Nodes)
            scene.RemoveEntity(ent.Entity);
        foreach (var ent in Links)
            scene.RemoveEntity(ent.Entity);
        foreach (var ent in TransformLinks)
            scene.RemoveEntity(ent.Entity);

        //scene.RemoveEntity(phys.Empty);
        scene.RemoveEntity(pos.Body.Entity);
        scene.RemoveEntity(pos.Head.Entity);

        if (scene.HasTag(Main, Tags.Player) && scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var entity))
            scene.RemoveEntity(entity);

        foreach (var item in pos.BodyDecorations)
            scene.RemoveEntity(item);
        foreach (var item in pos.HeadDecorations)
            scene.RemoveEntity(item);
        foreach (var h in pos.Hands)
            scene.RemoveEntity(h.Entity);
        foreach (var h in pos.Feet)
            scene.RemoveEntity(h.Entity);
    }
}
