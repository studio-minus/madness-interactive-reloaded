using System.Collections.Generic;

namespace MIR;

/// <summary>
/// When a portal door is entered, this singleton object is set to be read on level load to figure out where to spawn, which weapon to equip, and so on.
/// </summary>
public class PersistentPortalData : IPersistentLevelData
{
    public static PersistentPortalData Shared = new();

    /// <summary>
    /// When a portal door is entered, this value is set to be read on level load to figure out where to spawn the player
    /// </summary>
    public string? TargetPortalID = null;

    /// <summary>
    /// When a portal door is entered, this value is set to be read on level load to figure out what weapon the player needs to equip
    /// </summary>
    public PersistentEquippedWeapon? EquippedWeapon = null;

    /// <summary>
    /// When a portal door is entered, this value is set to be read on level load to figure out which allies to spawn
    /// </summary>
    public List<SerialisedBuddy> Buddies = [];

    IEnumerable<SerialisedBuddy>? IPersistentLevelData.Buddies => Buddies;
    PersistentEquippedWeapon? IPersistentLevelData.EquippedWeapon => EquippedWeapon;
}
