using System;
using System.Collections.Generic;
using System.IO;
using Walgelijk;

namespace MIR;

/// <summary>
/// Directories for saving data such as settings, player look, experiment mode characters, etc.
/// </summary>
public static class UserData
{
    public static class Paths
    {
        public static string BaseDir = Path.Combine(Game.Main.AppDataDirectory, "userdata/");
        public static string ExperimentDir => BaseDir + "experiment/";

        public static string Settings => BaseDir + "settings.json";
        public static string CampaignStatsDir => BaseDir + "campaigns/";
        public static string PlayerLookFile => BaseDir + "player.look";
        public static string ExperimentCharacterPresets => ExperimentDir + "character_presets/";

        public static string ArenaModeSaves => BaseDir + "arena.json";

        public static string UnlockedImprobabilityDisk => BaseDir + "disks.txt";
    }

    /// <summary>
    /// Instances for player info.
    /// </summary>
    public static class Instances
    {
        /// <summary>
        /// The player's look preset that they have configured.
        /// </summary>
        public static readonly CharacterLook PlayerLook = new() { Name = "Player" };

        /// <summary>
        /// The player's game settings.
        /// </summary>
        public static Settings Settings = new();

        /// <summary>
        /// The player's arena mode save data
        /// </summary>
        public static ArenaModeSaves ArenaMode = new();

        /// <summary> 
        /// All disk IDs unlocked by the player
        /// </summary>
        public static HashSet<string> UnlockedImprobabilityDisks = [];
    }
}
