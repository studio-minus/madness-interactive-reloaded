using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

//😀 😀

public class IncidentSystem : Walgelijk.System
{
    public static IncidentConfig? CurrentConfig;
    public static IncidentState CurrentState;

    private int startKillCount;
    private float animTime;

    public override void OnActivate()
    {
        if (CurrentConfig == null)
        {
            Logger.Error("Started Incident mode without an IncidentConfig");
            return;
        }

        if (CampaignProgress.CurrentCampaign == null)
        {
            Logger.Error("Started Incident mode without a campaign set");
            return;
        }

        if (!CampaignProgress.CurrentCampaign.Temporary)
        {
            Logger.Error("Started Incident mode with a non-temporary campaign (it was probably not generated on-the-fly)");
            return;
        }

        if (!CampaignProgress.TryGetCurrentStats(out var campaignStats))
        {
            Logger.Error("Can't get campaign stats for generated incident mode campaign");
            return;
        }

        CurrentState.LastLevelIndex = CurrentState.LevelIndex;
        CurrentState.LevelIndex = campaignStats.LevelIndex;

        if (CurrentState.LevelIndex != CurrentState.LastLevelIndex)
        {
            // we progressed recently so we should store the killcount in case the player dies
            CurrentState.KillCountAtLevelStart = CurrentState.KillCount;
        }
        else
        {
            // we restarted so we mustve died
            CurrentState.KillCount = CurrentState.KillCountAtLevelStart;
        }

        startKillCount = CurrentState.KillCount;
    }

    public override void Update()
    {
        if (CurrentConfig != null 
            && !MadnessUtils.IsCutscenePlaying(Scene) 
            && MadnessUtils.IsPlayerAlive(Scene) 
            && !MadnessUtils.IsPaused(Scene))
        {
            CurrentState.KillCount = startKillCount;
            if (Scene.FindAnyComponent<LevelProgressComponent>(out var lvl))
                CurrentState.KillCount += lvl.BodyCount.Current;

            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrders.UserInterface;
            Draw.Font = Fonts.Impact;
            Draw.FontSize = 100;

            var str = $"{CurrentConfig.KillTarget - CurrentState.KillCount}";
            var width = Draw.CalculateTextWidth(str);
            var height = Draw.CalculateTextHeight(str);

            var pos = new Vector2(Window.Width, Window.Height);
            pos.X -= width / 2 + 30;
            pos.Y -= height / 2 + 20;

            if (CurrentState.LevelIndex == 0)
            {
                animTime += Time.DeltaTimeUnscaled * 0.5f;
                //animTime %= 1;

                Draw.Colour = Colors.Black.WithAlpha(Easings.Quad.Out(1 - animTime));
                Draw.Quad(new Rect(0,0,Window.Width, Window.Height));
                Draw.Colour = Colors.White;

                // first level!!
                var p = float.Clamp(animTime , 0, 1);
                pos = Vector2.Lerp(Window.Size * 0.5f, pos, Easings.Quad.InOut(p));
            }

            Draw.Text(str, pos, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        }
    }
}
