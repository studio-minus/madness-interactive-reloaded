using Walgelijk;

namespace MIR;

/// <summary>
/// Increments the campaign stats timer
/// </summary>
public class CampaignStatsTimerSystem : Walgelijk.System
{
    public override void FixedUpdate()
    {
        var lvl = Level.CurrentLevel;
        if (lvl == null)
            return;

        if (!MadnessUtils.IsPaused(Scene))
            if (CampaignProgress.TryGetCurrentStats(out var stats))
                stats.IncrementTimer(lvl, Time.FixedInterval);
    }
}
