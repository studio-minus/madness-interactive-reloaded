using Walgelijk;
using Walgelijk.OpenTK;

namespace MIR;

/// <summary>
/// Sets the pitch of all sound effects to match the current timescale
/// </summary>
public class SoundEffectPitchSystem : Walgelijk.System
{
    public override void Update()
    {
        float pitch = float.Clamp(Time.TimeScale, 0.01f, 100);

        AudioTracks.SoundEffects.Pitch = pitch;

        if (Audio is OpenALAudioRenderer al)
            foreach (var item in al.TemporarySourceBuffer)
            {
                if (item.Sound != null && (item.Track == AudioTracks.SoundEffects))
                {
                    item.Sound.Pitch = pitch;
                    item.Sound.ForceUpdate();
                    OpenTK.Audio.OpenAL.AL.Source(item.Source, OpenTK.Audio.OpenAL.ALSourcef.Pitch, pitch);
                }
            }
    }
}
