using System;
using System.Collections.Generic;

namespace MIR;

/// <summary>
/// Stats per level for within a <see cref="CampaignStats"/>
/// </summary>
public class LevelStats
{
    public int Kills;
    public int Deaths;
    public int Attempts;
    public TimeSpan TotalTimeSpent;
    public PersistentEquippedWeapon? EquippedWeapon;
}