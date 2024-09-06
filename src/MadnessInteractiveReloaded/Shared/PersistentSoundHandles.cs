using Walgelijk;

namespace MIR;

/// <summary>
/// Contains references for sounds that play across scenes, and need to be managed by objects in many different places
/// </summary>
public static class PersistentSoundHandles
{
    public static Sound? LevelMusic;
    public static Sound? PauseMusic;
    public static Sound? MainMenuMusic;

    public static Sound? DjMusic =>     DjComponent.CurrentMusic == null ? null : SoundCache.Instance.LoadMusicNonLoop(DjComponent.CurrentMusic.Sound);
}