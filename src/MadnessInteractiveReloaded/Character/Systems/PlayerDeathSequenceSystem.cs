using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Localisation;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Managed the player's death sequence.
/// </summary>
public class PlayerDeathSequenceSystem : Walgelijk.System, IDisposable
{
    // TODO should really be stored somewhere else but it doesn't matter that much, really
    public static readonly Texture TrickyStatic;

    static PlayerDeathSequenceSystem()
    {
        TrickyStatic = TexGen.Noise(256, 256, 9.523f, 62.5123f, Colors.Black, Colors.White);
        TrickyStatic.FilterMode = FilterMode.Linear;
    }

    public override void Update()
    {
        if (Scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var ent) &&
            Scene.TryGetComponentFrom<PlayerDeathSequenceComponent>(ent, out var comp))
        {
            comp.Time += Time.DeltaTime;
        }
    }

    public override void FixedUpdate()
    {
        if (ImprobabilityDisks.IsEnabled("tricky"))
            if (Scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var ent) &&
                Scene.TryGetComponentFrom<PlayerDeathSequenceComponent>(ent, out var comp))
            {
                MadnessUtils.Shake(comp.Time); // because we wait 2 seconds before rivival in Prefabs.cs
            }
    }

    public override void Render()
    {
        if (Scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var ent) &&
            Scene.TryGetComponentFrom<PlayerDeathSequenceComponent>(ent, out var comp)/* && comp.Time > 1*/)
        {
            bool isTricky = ImprobabilityDisks.IsEnabled("tricky");
            if (isTricky)
            {
                Draw.Reset();
                Draw.ScreenSpace = true;
                Draw.Order = RenderOrders.UserInterfaceTop;
                float r = float.Max(Window.Width, Window.Height) * 2;
                Draw.Colour = Colors.Red * comp.Time * 0.5f; // because we wait 2 seconds before rivival in Prefabs.cs
                Draw.BlendMode = BlendMode.Screen;
                Draw.Image(TrickyStatic, new Rect(0, 0, r, r).Translate(-Window.Width * Utilities.RandomFloat(), -Window.Height * Utilities.RandomFloat()), ImageContainmentMode.Stretch);

                return;
            }

            Draw.Reset();
            Draw.ScreenSpace = true;

            Draw.Order = RenderOrders.UserInterface.WithOrder(-1);

            if (comp.Time < 1 / 3f)
            {
                Draw.Colour = Colors.Red.WithAlpha(1 - Utilities.Clamp(comp.Time * 3f));
                Draw.Quad(Vector2.Zero, Window.Size);
            }

            float delayedTime = comp.Time - 1;
            float clampedDelayedTime = Utilities.Clamp(delayedTime);
            var heartbeat = Utilities.MapRange(0, 1, 0.5f, 1, HeartbeatCurve((Time.SecondsSinceLoad * 0.5f) % 1));

            if (delayedTime > 0)
            {
                bool flickerDone = clampedDelayedTime > 0.25f;
                bool flicker = flickerDone || (delayedTime % 0.1f > .05f);
                float instructionBarY = Window.Height - 256;

                //background bar
                if (flicker)
                {
                    Draw.OutlineColour = Colors.Red;
                    Draw.OutlineWidth = 8;
                    Draw.Colour = Colors.Black;
                    Draw.Quad(new Vector2(0, instructionBarY - 40), new Vector2(Window.Width, 80));
                }

                if (flickerDone)
                {
                    var tt = Easings.Quad.Out((Time.SecondsSinceLoad * 0.5f) % 1);
                    Draw.OutlineWidth = 0;
                    Draw.Colour = Colors.Red.WithAlpha(heartbeat * (1 - tt));
                    float lineOffset = Utilities.MapRange(0, 1, 40, 60, tt);

                    Draw.Line(new Vector2(0, instructionBarY - lineOffset), new Vector2(Window.Width, instructionBarY - lineOffset), 4, 0);
                    Draw.Line(new Vector2(0, instructionBarY + lineOffset), new Vector2(Window.Width, instructionBarY + lineOffset), 4, 0);

                    Draw.Colour = Utilities.Lerp(Colors.White, Colors.Red, Utilities.Clamp(delayedTime * 4f - 1f));
                    Draw.OutlineWidth = 0;
                    Draw.Font = Fonts.Toxigenesis;
                    Draw.FontSize = 32;
                    string toDraw = Scene.FindAnyComponent<GameModeComponent>(out var gameModeComp) && gameModeComp.Mode == GameMode.Campaign ?
                        $"<color=#ffffff>[R]</color> {Localisation.Get("retry").ToUpper()}" :
                        $"<color=#ffffff>[R]</color> {Localisation.Get("revive").ToUpper()} ";

                    Draw.Text(toDraw, new(Window.Width / 2, instructionBarY), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                }

                Draw.Colour = Colors.Red;
                Draw.Colour.A = heartbeat;
                Draw.Quad(new Vector2(Window.Width / 2 - 350, instructionBarY - 40), new Vector2(4, 80), 0, 0);
                Draw.Quad(new Vector2(Window.Width / 2 - 370, instructionBarY - 40), new Vector2(4, 80), 0, 0);

                Draw.Quad(new Vector2(Window.Width / 2 + 350, instructionBarY - 40), new Vector2(4, 80), 0, 0);
                Draw.Quad(new Vector2(Window.Width / 2 + 370, instructionBarY - 40), new Vector2(4, 80), 0, 0);

                Draw.Order = RenderOrders.PlayerRagdoll.WithOrder(-100);
                Draw.Colour = new Color(15, 0, 0).WithAlpha(Utilities.Clamp(delayedTime / 5, 0, 0.9f));
                Draw.Quad(Vector2.Zero, Window.Size);
                Draw.Order = RenderOrders.UserInterface.WithOrder(-1);
            }

            Draw.OutlineWidth = 0;
            Draw.Colour = Colors.Red;

            var t = 1 - (1 - clampedDelayedTime) * (1 - clampedDelayedTime) * (1 - clampedDelayedTime);

            const float barHeight = 130;
            Draw.Quad(new Vector2(0), new Vector2(Window.Width, barHeight * t));
            Draw.Quad(new Vector2(0, Window.Height - barHeight * t), new Vector2(Window.Width, barHeight * t));

            const float barWidth = 32;
            Draw.Quad(new Vector2(0, 0), new Vector2(barWidth * t, Window.Height));
            Draw.Quad(new Vector2(Window.Width - barWidth * t, 0), new Vector2(barWidth * t, Window.Height));
        }
    }

    public static float HeartbeatCurve(float x)
    {
        if (x < 0 || x > 1)
            return 0;

        if (x < 0.5f)
        {
            var pulse1 = MathF.Exp(-200 * ((x - 0.15f) * (x - 0.15f)));
            var pulse2 = MathF.Exp(-100 * ((x - 0.35f) * (x - 0.35f)));
            return MathF.Max(pulse1, pulse2);
        }
        return 0;
    }

    public void Dispose()
    {
        TrickyStatic.Dispose();
    }
}
