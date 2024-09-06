using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion.Controls;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Animates the flashing lights for the club level.
/// </summary>
public class ClubLightsSystem : Walgelijk.System
{
    private int lastFlashIndex = 0; //TODO stateful systeem 💀

    public override void Render()
    {
        if (!Scene.FindAnyComponent<DjComponent>(out var dj)
            || DjComponent.CurrentMusic == null
            || DjComponent.PlaybackState is not DjComponent.State.Playing)
            return;

        Color color;
        var actualPlayTime = Audio.GetTime(PersistentSoundHandles.DjMusic!);
        // we have passed the initial beat
        if (DjComponent.CurrentMusic.StartOffset < actualPlayTime)
        {
            var flashTimer = (DjComponent.Time * 6);
            var flashIndex = (int)flashTimer;

            if (lastFlashIndex != flashIndex)
            {
                //a beat occurred
                //Logger.Debug("beat " + flashIndex);
                MadnessUtils.Shake(10 * DjComponent.BeatEnergy);
            }
            lastFlashIndex = flashIndex;

            DjComponent.ClubLightColour = new Color(
                (byte)int.Max(123, (flashIndex * 23) % 256),
                (byte)int.Max(123, (flashIndex * -23) % 256),
                (byte)int.Max(123, (flashIndex * 644) % 256),
                (byte)255
            );
            color = DjComponent.ClubLightColour * Utilities.Lerp(0.5f, ((1 - flashTimer % 1) * 1.5f + 0.5f), DjComponent.BeatEnergy);
            color.A = 1;
        }
        else
        {
            color = Color.FromHsv(DjComponent.Time * 0.2f, 0.5f, 0.4f);
        }

        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundInFront.OffsetLayer(1);
        Draw.ScreenSpace = true;
        Draw.BlendMode = BlendMode.Multiply;
        Draw.Colour = color;
        Draw.Quad(Vector2.Zero, Window.Size);
        Draw.Reset();
    }
}
