namespace MIR;

/// <summary>
/// How the link should behave and place each node.
/// </summary>
public enum VerletLinkMode
{
    /// <summary>
    /// Unimplemented.
    /// </summary>
    KeepDistance,

    /// <summary>
    /// Only pull the nodes together if distance between the them is greater than or equal to <see cref="VerletLinkComponent.TargetDistance"/>.
    /// </summary>
    MaxDistanceOnly,

    /// <summary>
    /// Only pull the nodes together when the distance beteween them is less than <see cref="VerletLinkComponent.TargetDistance"/>.
    /// </summary>
    MinDistanceOnly,

    /// <summary>
    /// Ensures the distance between the nodes is greater than <see cref="VerletLinkComponent.MinMaxDistance"/>.X and smaller than <see cref="VerletLinkComponent.MinMaxDistance"/>.Y
    /// </summary>
    MinMaxDistance,
}