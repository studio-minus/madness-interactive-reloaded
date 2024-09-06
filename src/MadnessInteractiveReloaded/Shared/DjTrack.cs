namespace MIR;
using Walgelijk;
using Walgelijk.AssetManager;

/// <summary>
/// Music tracks that can be used in-game in the club level.
/// </summary>
public class DjTrack
{
    /// <summary>
    /// The track name to show to the player.
    /// </summary>
    public string Name;

    /// <summary>
    /// Whose track is it?
    /// </summary>
    public string Author;

    /// <summary>
    /// The temp of the track.
    /// Used for lights and other music visualisation fx.
    /// </summary>
    public float BPM;

    /// <summary>
    /// If the track has some silence in the beginning of the file, you can set this to offset the start of the actual audio.
    /// </summary>
    public float StartOffset;

    /// <summary>
    /// The audio data.
    /// </summary>
    public AssetRef<StreamAudioData> Sound;

    public DjTrack(string name, string author, float bPM, float startOffset, AssetRef<StreamAudioData> sound)
    {
        Name = MadnessUtils.Ellipsis(name, 25);
        Author = author;
        BPM = bPM;
        StartOffset = startOffset;
        Sound = sound;
    }
}
