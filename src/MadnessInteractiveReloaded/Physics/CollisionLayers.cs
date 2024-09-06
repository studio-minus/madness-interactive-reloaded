namespace MIR;

/// <summary>
/// Which layers a physics body can collide with.
/// </summary>
public static class CollisionLayers
{
    /// <summary>
    /// Collide with nothing.
    /// </summary>
    public const uint None = 0;

    /// <summary>
    /// The default collision layer.
    /// </summary>
    public const uint Default = 1;

    /// <summary>
    /// Objects that collide with physics objects.
    /// </summary>
    public const uint BlockPhysics = 2;

    /// <summary>
    /// Objects that are taken into consideration when determining where characters can move.
    /// </summary>
    public const uint BlockMovement = 4;

    /// <summary>
    /// Objects that are taken into consideration when determining what bullets will hit.
    /// </summary>
    public const uint BlockBullets = 8;

    /// <summary>
    /// When a bullet impacts a surface, it checks if that point overlaps with an object on this layer. If it does, a decal is placed.
    /// </summary>
    public const uint DecalZone = 16;

    /// <summary>
    /// This is the layer after which character factions are assigned their own 
    /// layer (see <see cref="Faction"/>). Do not use values beyond this.
    /// </summary>
    public const uint CharacterStart = 16;

    /// <summary>
    /// All characters, regardless of faction
    /// </summary>
    public const uint AllCharacters = All & ~BlockAll & ~DecalZone;

    /// <summary>
    /// Block physics objects, character movement, and bullets.
    /// </summary>
    public const uint BlockAll = BlockPhysics | BlockMovement | BlockBullets;

    /// <summary>
    /// Every collision layer.
    /// </summary>
    public const uint All = uint.MaxValue;
}
