using System;
using System.Collections.Generic;
using System.Linq;
using Walgelijk;

namespace MIR;

/// <summary>
/// Holds the current music playlist. There can only be one of these in the scene
/// </summary>
[SingleInstance]
public class MusicPlaylistComponent : Component, IDisposable
{
    // We don't want anyone to add or remove music while it's playing bceause it could cause "rogue" music; when calling Stop() it would only stop the tracks it can find in the list.
    public readonly IReadOnlyList<Sound> Playlist;
    public int CurrentIndex;
    public PlaylistState State { get; private set; }

    public MusicPlaylistComponent(IEnumerable<Sound> playlist)
    {
        Playlist = playlist.ToList().AsReadOnly();
    }

    public void Play()
    {
        State = PlaylistState.Playing;
    }

    public void Stop()
    {
        State = PlaylistState.Stopped;
    }

    public void Dispose()
    {
        // hack lol
        foreach (var item in Playlist)
            Game.Main.AudioRenderer.Stop(item);
    }
}

public enum PlaylistState
{
    Playing,
    Stopped
}