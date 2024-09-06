namespace MIR;

using Walgelijk;

/// <summary>
/// Static <see cref="Tag"/>s for our game's entities.<br></br>
/// Useful for grouping entities, singling others out, and finding them.
/// </summary>
public static class Tags
{
    /// <summary>
    /// The player ore player related entities.
    /// </summary>
    public static readonly Tag Player = new(1);

    /// <summary>
    /// Enemy guys.
    /// </summary>
    public static readonly Tag EnemyAI = new(2);

    public static readonly Tag AccurateShotHUD = new(3);
    public static readonly Tag CharacterCreationPreviewPanel = new(4);
    public static readonly Tag PlayerDeathSequence = new(5);

    //public static readonly Tag CasingParticles = new(6);
    //public static readonly Tag MediumCasingParticles = new(7);
    //public static readonly Tag ShotgunShellParticles = new(8);
    //public static readonly Tag RifleCasingParticles = new(9);

    public static readonly Tag BackgroundDecals = new(10);
    public static readonly Tag ForegroundDecals = new(11);

    public static readonly Tag BulletImpact = new(12);

    public static readonly Tag TrainEngine = new(13);
    public static readonly Tag TrainEngineBulletHole = new(14);

    public static readonly Tag NotificationText = new(15);
}