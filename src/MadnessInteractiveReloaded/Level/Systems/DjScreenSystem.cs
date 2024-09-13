using System;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Updates the dj screen visualiser.
/// </summary>
public class DjScreenSystem : Walgelijk.System
{
    private readonly ActionRenderTask rt;

    public DjScreenSystem()
    {
        rt = new ActionRenderTask(g =>
        {
            foreach (var comp in Scene.GetAllComponentsOfType<DjScreenComponent>())
            {
                // TODO  
                // this bar graph should use instanced rendering

                Graphics.CurrentTarget = comp.Target;
                comp.Target.ProjectionMatrix = comp.Target.OrthographicMatrix;
                {
                    Graphics.Clear(Colors.Transparent);
                    if (comp.Visualiser != null)
                    {
                        var bars = comp.Visualiser.GetVisualiserData();
                        for (int i = 0; i < bars.Length * 2; i++)
                        {
                            float x = (int)Utilities.Lerp(0, comp.Target.Width, i / (float)((bars.Length * 2)));
                            int freqIndex = i % bars.Length;
                            if (i < bars.Length)
                                freqIndex = bars.Length - freqIndex;
                            freqIndex = Math.Clamp(freqIndex, 0, bars.Length - 1);
                            var r = new Rect(x, (1 - bars[freqIndex]) * comp.Target.Height, x + 5, comp.Target.Height);
                            Graphics.DrawQuadScreenspace(r, comp.BarMaterial);
                        }
                    }
                }
                Graphics.CurrentTarget = Game.Main.Window.RenderTarget;
            }
        });
    }

    public override void Render()
    {
        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundDecals;
        foreach (var comp in Scene.GetAllComponentsOfType<DjScreenComponent>())
        {
            if (Scene.FindAnyComponent<DjComponent>(out var dj)
                && DjComponent.CurrentMusic != null
                && DjComponent.PlaybackState == DjComponent.State.Playing)
            {
                if (comp.Visualiser == null || comp.Visualiser.Sound.Data != DjComponent.CurrentMusic.Sound.Value)
                    comp.Visualiser = new AudioVisualiser(PersistentSoundHandles.DjMusic!, 512, 256, 16)
                    {
                        MinFreq = 100,
                        MaxFreq = 16000,
                        OutputBlurIterations = 1,
                        OutputBlurIntensity = 0.2f,
                        Smoothing = 0,
                        MinDb = -20,
                        MaxDb = 90,
                        InputBlurIntensity = 0,
                        InputBlurIterations = 0,
                        OverlapWindow = true,
                    };

                if (!MadnessUtils.IsPaused(Scene))
                    comp.Visualiser.Update(Audio, Time.DeltaTime);
                var bars = comp.Visualiser.GetVisualiserData();
                DjComponent.BeatEnergy = Utilities.SmoothApproach(DjComponent.BeatEnergy, Utilities.Clamp(bars[0] * 2), 15, Time.DeltaTime);

                Draw.Texture = comp.Target;
                Draw.Quad(comp.WorldRect);
            }
        }
    }

    public override void PreRender()
    {
        RenderQueue.Add(rt);
    }
}
// ✨✨✨