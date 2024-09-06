using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Walgelijk;

namespace MIR;

/// <summary>
/// Statistics for a played campaign
/// </summary>
public class CampaignStats
{
    /// <summary>
    /// The associated campaign
    /// </summary>
    public string CampaignId = string.Empty;

    /// <summary>
    /// Current level progress
    /// </summary>
    public int LevelIndex;

    /// <summary>
    /// Current level progress, clamped to be inside the bounds. This is useful because <see cref="LevelIndex"/> can equal the total amount of levels if the campaign was completed.
    /// </summary>
    [JsonIgnore]
    public int LevelIndexClamped
    {
        get
        {
            if (Registries.Campaigns.TryGet(CampaignId, out var c))
                return int.Clamp(LevelIndex, 0, c.Levels.Length - 1);
            return LevelIndex;
        }
    }

    /// <summary>
    /// Stats per level
    /// </summary>
    public Dictionary<string, LevelStats> ByLevel = new();

    public CampaignStats()
    {

    }

    public CampaignStats(string campaignId)
    {
        CampaignId = campaignId;
    }

    [JsonIgnore]
    public TimeSpan TotalTimeSpent => TimeSpan.FromTicks(ByLevel.Values.Sum(static l => l.TotalTimeSpent.Ticks));

    [JsonIgnore]
    public int TotalKills => ByLevel.Values.Sum(static l => l.Kills);

    [JsonIgnore]
    public int TotalDeaths => ByLevel.Values.Sum(static l => l.Deaths);

    [JsonIgnore]
    public int LevelsPassedFirstTry => ByLevel.Values.Count(static l => l.Attempts == 1);

    /// <summary>
    /// Path to the file for this instance. Note that this file might not exist yet.
    /// </summary>
    public string GetFilePath() => UserData.Paths.CampaignStatsDir + CampaignId + ".json";

    public void IncrementKills(Level level)
    {
        var entry = ByLevel.Ensure(level.Id);
        entry.Kills++;
    }

    public void IncrementAttempts(Level level)
    {
        var entry = ByLevel.Ensure(level.Id);
        entry.Attempts++;
    }

    public void IncrementDeaths(Level level)
    {
        var entry = ByLevel.Ensure(level.Id);
        entry.Deaths++;
    }

    public void IncrementTimer(Level level, float dtSeconds)
    {
        var entry = ByLevel.Ensure(level.Id);
        entry.TotalTimeSpent += TimeSpan.FromSeconds(dtSeconds);
    }

    public bool IsLevelLocked(string levelId)
    {
        if (Registries.Campaigns.TryGet(CampaignId, out var c))
        {
            var levelIndex = Array.IndexOf(c.Levels, levelId);
            return levelIndex == -1 || levelIndex > LevelIndex;
        }
        return true;
    }

    /// <summary>
    /// Create a <see cref="CampaignStats"/> registry (<see cref="Registries.CampaignStats"/>) entry and instance for the given campaign ID
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static CampaignStats CreateFor(string campaignId)
    {
        if (Registries.CampaignStats.Has(campaignId))
            throw new InvalidOperationException("The registry already contains a campaign stats entry for " + campaignId);

        var path = UserData.Paths.CampaignStatsDir + campaignId + ".json";
        if (File.Exists(path))
            throw new InvalidOperationException($"The registry does not contain a campaign stats entry for {campaignId}, but a file for it already exists at {path}");

        var stats = new CampaignStats(campaignId);
        Registries.CampaignStats.Register(campaignId, stats);

        return stats;
    }

    /// <summary>
    /// Save the changes to the file at <see cref="GetFilePath"/>
    /// </summary>
    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(GetFilePath(), json);
    }
}
