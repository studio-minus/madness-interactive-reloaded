using System.Linq;
using Walgelijk;

namespace MIR;

/// <summary>
/// Plays music from the <see cref="MusicPlaylistComponent"/>
/// </summary>
public class MusicPlaylistSystem : Walgelijk.System
{
    private static bool hooked = false;

    public override void Initialise()
    {
        //return;

        if (hooked)
            return;
        hooked = true;
        Game.Main.OnSceneChange.AddListener(static v =>
        {
            if (v.Old != null && v.Old.FindAnyComponent<MusicPlaylistComponent>(out var oldPlaylist))
            {
                // oldPlaylist.Stop();
            }
        });
    }

    public override void Update()
    {
        //return;

        if (!Scene.FindAnyComponent<MusicPlaylistComponent>(out var c) || c.Playlist.Count == 0)
            return;

        switch (c.State)
        {
            case PlaylistState.Playing:
                {
                    if (c.Playlist.Any(Audio.IsPlaying))
                        return;

                    c.CurrentIndex = Utilities.RandomInt(0, c.Playlist.Count);
                    var s = c.Playlist[c.CurrentIndex];
                    Audio.Play(s);
                }
                break;
            case PlaylistState.Stopped:
                {
                    if (!c.Playlist.Any(Audio.IsPlaying))
                        return;

                    foreach (var item in c.Playlist)
                        Audio.Stop(item);
                }
                break;
        }
    }
}
