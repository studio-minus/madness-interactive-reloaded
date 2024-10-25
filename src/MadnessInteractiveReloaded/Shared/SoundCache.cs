namespace MIR;

using System;
using System.Collections.Generic;
using Walgelijk;
using Walgelijk.AssetManager;

/// <summary>
/// Caches sounds instead of always loading them immediately.
/// </summary>
public class SoundCache : Cache<CachedSoundProfile, Sound>
{
    public static readonly SoundCache Instance = new();

    private int cachedCount = 0;

    protected override Sound CreateNew(CachedSoundProfile raw)
    {
        cachedCount++;
        Logger.Log($"Sound cached: {raw.GetHashCode()} {cachedCount}");
        return new Sound(raw.Data, raw.Loops, raw.Spatial, raw.Track);
    }

    public Sound LoadMusic(AudioData data)
    {
        return Load(new(data, true, null, AudioTracks.Music));
    }

    public Sound LoadMusicNonLoop(AudioData data)
    {
        return Load(new(data, false, null, AudioTracks.Music));
    }

    public Sound LoadSoundEffect(AudioData data) => LoadSoundEffect(data, out _);
    public Sound LoadSoundEffect(string s) => LoadSoundEffect(Assets.Load<FixedAudioData>(s), out _);

    public Sound LoadSoundEffect(AudioData data, out CachedSoundProfile b)
    {
        b = new(data, false, data.ChannelCount == 2 ? null : Sounds.DefaultSpatialParams, AudioTracks.SoundEffects);
        return Load(b);
    }

    public Sound LoadUISoundEffect(AudioData data)
    {
        return Load(new(data, false, null, AudioTracks.UserInterface));
    }

    protected override void DisposeOf(Sound loaded) { }
}

public readonly struct CachedSoundProfile : IEquatable<CachedSoundProfile>
{
    public readonly AudioData Data;
    public readonly bool Loops;
    public readonly SpatialParams? Spatial;
    public readonly AudioTrack? Track;

    public CachedSoundProfile(AudioData data, bool loops, SpatialParams? spatial, AudioTrack? track)
    {
        Data = data;
        Loops = loops;
        Spatial = spatial;
        Track = track;
    }

    public override bool Equals(object? obj)
    {
        return obj is CachedSoundProfile profile && Equals(profile);
    }

    public bool Equals(CachedSoundProfile other)
    {
        return EqualityComparer<AudioData>.Default.Equals(Data, other.Data) &&
               Loops == other.Loops &&
               EqualityComparer<SpatialParams?>.Default.Equals(Spatial, other.Spatial) &&
               EqualityComparer<AudioTrack?>.Default.Equals(Track, other.Track);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Data, Loops, Spatial, Track);
    }

    public static bool operator ==(CachedSoundProfile left, CachedSoundProfile right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CachedSoundProfile left, CachedSoundProfile right)
    {
        return !(left == right);
    }
}