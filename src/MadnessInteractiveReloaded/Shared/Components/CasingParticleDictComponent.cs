using System.Collections.Generic;
using Walgelijk;

namespace MIR;

/// <summary>
/// Used for tying an entity to an <see cref="EjectionParticle"/> type.
/// </summary>
public class CasingParticleDictComponent : Component
{
    public readonly Dictionary<EjectionParticle, Entity> EntityByParticle = new();
}