namespace MIR;

public static class SharedLevelData
{
    /// <summary>
    /// When a portal door is entered, this value is set to be read upon level load to figure out where to spawn the player
    /// </summary>
    public static string? TargetPortalID = null;

    /// <summary>
    /// When a portal door is entered, this value is set to be read upon level load to figure out what weapon the player needs to equip
    /// </summary>
    public static PersistentEquippedWeapon? EquippedWeaponPortal = null;
}

public struct PersistentEquippedWeapon
{
    public string Key;
    public int Ammo;
    public bool InfiniteAmmo;
}