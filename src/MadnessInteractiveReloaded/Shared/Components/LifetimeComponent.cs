using Walgelijk;

namespace MIR;

/// <summary>
/// Component for storing how many seconds a component has existed for. 
/// </summary>
public class LifetimeComponent : Component
{
    /// <summary>
    /// How long (in seconds) this component has existed.
    /// </summary>
    public float Lifetime;
}
