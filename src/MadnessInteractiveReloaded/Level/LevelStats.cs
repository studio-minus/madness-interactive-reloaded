using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MIR;

/// <summary>
/// Stats per level for within a <see cref="CampaignStats"/>
/// </summary>
public class LevelStats : IPersistentLevelData
{
    /// <summary>
    /// Amount of characters the player killed during this level
    /// </summary>
    public int Kills;
    /// <summary>
    /// Amount of times the player died during this level
    /// </summary>
    public int Deaths;
    /// <summary>
    /// Amount of times that this level has been started
    /// </summary>
    public int Attempts;

    /// <summary>
    /// Total time spent during this level (unpaused)
    /// </summary>
    public TimeSpan TotalTimeSpent;

    /// <summary>
    /// Weapon equipped at the start of the level
    /// </summary>
    public PersistentEquippedWeapon? EquippedWeapon;

    /// <summary>
    /// Allies that should persist across levels (if they survive)
    /// </summary>
    public List<SerialisedBuddy> Buddies = [];

    /// <summary>
    /// Store arbitrary data by key
    /// </summary>
    public Dictionary<string, JObject> Extra = [];

    PersistentEquippedWeapon? IPersistentLevelData.EquippedWeapon => EquippedWeapon;
    IEnumerable<SerialisedBuddy>? IPersistentLevelData.Buddies => Buddies;
}