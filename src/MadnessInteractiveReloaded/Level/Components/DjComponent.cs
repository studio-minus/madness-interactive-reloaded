using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Data for the <see cref="DjSystem"/>.
/// </summary>
public class DjComponent : Component, IDisposable
{
    // This component is fucking insane, everything is static because the experiment mode persists across scenes :(

    /// <summary>
    /// What track are we currently playing.
    /// </summary>
    public static DjTrack? CurrentMusic;

    /// <summary>
    /// Is music playing or not?
    /// </summary>
    public static State PlaybackState = State.Stopped;

    /// <summary>
    /// How is the music played?
    /// </summary>
    public static Mode PlaybackMode = Mode.Autoplay;

    /// <summary>
    /// Whether or not to randomly pick a next track from the playlist
    /// </summary>
    public static bool Shuffle = true;

    /// <summary>
    /// Play as soon as the Scene is loaded.
    /// </summary>
    public static bool Autoplay = true;

    /// <summary>
    /// The time from the <see cref="AudioRenderer"/> scaled by playspeed.
    /// </summary>
    public static float Time;

    /// <summary>
    /// The speed for characters to dance to the music. 
    /// Uses <see cref="DjTrack.BPM"/>.
    /// </summary>
    public static float CalculatedAnimationSpeed;

    /// <summary>
    /// The color of the club lights.
    /// </summary>
    public static Color ClubLightColour = Colors.Purple;
    
    /// <summary>
    /// Energy of the lower frequencies currently playing
    /// </summary>
    public static float BeatEnergy;

    public void Play(DjTrack track, AudioRenderer audio)
    {
        PlaybackState = State.Playing;
        CurrentMusic = track;
        audio.StopAll(AudioTracks.Music);
        audio.Play(SoundCache.Instance.LoadMusicNonLoop(CurrentMusic.Sound));
        Time = 0;
    }

    public void Stop(AudioRenderer audio)
    {
        if (PlaybackState != State.Stopped)
        {
            PlaybackState = State.Stopped;
            audio.StopAll(AudioTracks.Music);
            CurrentMusic = null;
            Time = 0;
        }
    }

    public void Pause(AudioRenderer audio)
    {
        if (PlaybackState == State.Playing && CurrentMusic != null)
        {
            PlaybackState = State.Paused;
            audio.Pause(SoundCache.Instance.LoadMusicNonLoop(CurrentMusic.Sound));
        }
    }

    public void Resume(AudioRenderer audio)
    {
        if (PlaybackState == State.Paused && CurrentMusic != null)
        {
            PlaybackState = State.Playing;
            audio.Play(SoundCache.Instance.LoadMusicNonLoop(CurrentMusic.Sound));
        }
    }

    public void Dispose()
    {
        Stop(Game.Main.AudioRenderer);
    }

    public enum State
    {
        Stopped,
        Paused,
        Playing
    }

    public enum Mode
    {
        None,
        RepeatTrack,
        Autoplay
    }
}
