using System;
using System.Diagnostics.CodeAnalysis;
using Walgelijk;

namespace MIR;

/// <summary>
/// Saved progress for the campaign, like which level the player is on.
/// </summary>
public static class CampaignProgress
{
    public static Campaign? CurrentCampaign { get; private set; }

    public static CampaignStats? GetCurrentStats()
    {
        if (CurrentCampaign == null)
            return null;

        if (Registries.CampaignStats.TryGet(CurrentCampaign.Id, out var stats))
            return stats;

        stats = CampaignStats.CreateFor(CurrentCampaign.Id);
        return stats;
    }

    [MemberNotNullWhen(true, nameof(CurrentCampaign))]
    public static bool TryGetCurrentStats([NotNullWhen(true)] out CampaignStats? stats)
    {
        stats = GetCurrentStats();
        return stats != null;
    }

    /// <summary>
    /// Sets the current campaign
    /// </summary>
    public static void SetCampaign(Campaign? campaign)
    {
        CurrentCampaign = campaign;
        Logger.Log($"Set campaign to {campaign?.Name ?? null}");

        if (campaign != null && campaign.Levels.Length == 0)
        {
            Logger.Error("Campaign level list is empty");
            CurrentCampaign = null;
        }
    }

    public static LevelEntry? GetLevelAtProgress()
    {
        var stats = GetCurrentStats();
        if (stats == null)
            return null;

        var key = GetLevelKeyAt(stats.LevelIndex);
        if (key != null)
        {
            if (Registries.Levels.TryGet(key, out var level))
                return level;
        }
        return null;
    }

    public static ReadOnlySpan<string> GetCampaignLevelList() => CurrentCampaign?.Levels ?? ReadOnlySpan<string>.Empty;

    public static string? GetLevelKeyAt(int index) => (GetCampaignLevelList().Length <= index) ? null : (GetCampaignLevelList()[index] ?? null);

    /// <summary>
    /// Increments the level progress index.
    /// </summary>
    public static void SetProgressToNextLevel()
    {
        if (TryGetCurrentStats(out var stats))
        {
            stats.LevelIndex++;
            if (!CurrentCampaign.Temporary)
                stats.Save();
        }
    }
}
