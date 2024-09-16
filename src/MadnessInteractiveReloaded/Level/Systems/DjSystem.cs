using OpenTK.Windowing.Common.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Manages the <see cref="DjTrack"/> related components.
/// Plays music in the club level, basically.
/// </summary>
public class DjSystem : Walgelijk.System
{
    public override void Update()
    {
        if (!Scene.FindAnyComponent<DjComponent>(out var dj))
        {
            Scene.AttachComponent(Scene.CreateEntity(), new DjComponent());//
            return;
        }

#if DEBUG
        if (Input.IsKeyReleased(Key.F8))
            dj.Play(Utilities.PickRandom(Registries.Dj), Audio);
#endif

        if (DjComponent.PlaybackState == DjComponent.State.Playing && PersistentSoundHandles.DjMusic != null)
        {
            if (MadnessUtils.IsPaused(Scene))
            {
                Audio.Pause(PersistentSoundHandles.DjMusic);
            }
            else
            {
                if (PersistentSoundHandles.DjMusic.State == SoundState.Paused) // we resume when paused because it can only pause when the pause menu is open
                    Audio.Play(PersistentSoundHandles.DjMusic);

                if (PersistentSoundHandles.DjMusic.State == SoundState.Stopped)
                {
                    switch (DjComponent.PlaybackMode)
                    {
                        case DjComponent.Mode.RepeatTrack:
                            {
                                //var track = DjComponent.CurrentMusic!;
                                //var m = PersistentSoundHandles.DjMusic;
                                //Audio.SetTime(m, 0);
                                //Audio.Play(m);
                            }
                            break;
                        case DjComponent.Mode.Autoplay:
                            {
                                var track = DjComponent.CurrentMusic!;
                                dj.Stop(Audio);
                                if (DjComponent.Shuffle)
                                    dj.Play(Utilities.PickRandom(Registries.Dj), Audio);
                                else
                                {
                                    var next = (Registries.Dj.IndexOf(track) + 1) % Registries.Dj.Count;
                                    dj.Play(Registries.Dj[next], Audio);
                                }
                            }
                            break;
                        default:
                        case DjComponent.Mode.None:
                            dj.Stop(Audio);
                            break;
                    }
                }
                else if (DjComponent.CurrentMusic != null)
                    ProcessSpeed(dj, DjComponent.CurrentMusic);
            }
        }

        if (DjComponent.PlaybackState == DjComponent.State.Playing && Scene.FindAnyComponent<PlayerDeathSequenceComponent>(out _))
        {
            dj.Pause(Audio);
            RoutineScheduler.Start(restart());

            IEnumerator<IRoutineCommand> restart()
            {
                yield return new RoutineWaitUntil(() => !Scene.FindAnyComponent<PlayerDeathSequenceComponent>(out _));
                dj.Resume(Audio);
            }
        }
    }

    private void ProcessSpeed(DjComponent dj, DjTrack track)
    {
        var speed = (track.BPM) / 60 / 6;
        DjComponent.CalculatedAnimationSpeed = speed;
        DjComponent.Time = (Audio.GetTime(PersistentSoundHandles.DjMusic!) * speed) + track.StartOffset;

        foreach (var character in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (character.Entity % 2 == 0) // only some of them
                continue;
            if (Scene.HasTag(character.Entity, Tags.Player)) //not the player
                continue;
            if (character.Stats.DodgeAbility > 0.1f) //only low skill characters
                continue;
            if (Scene.HasComponent<DancingCharacterComponent>(character.Entity)) //dont add it if already added
                continue;

            MadnessUtils.Delay(0, () =>
            {
                Scene.AttachComponent(character.Entity, new DancingCharacterComponent(Utilities.PickRandom(Animations.Dancing)));
            });
        }

        if (Scene.TryGetEntityWithTag(new(123), out var pipin) && Scene.TryGetComponentFrom<FlipbookComponent>(pipin, out var pipinAnim))
        {
            pipinAnim.Duration = 120 / track.BPM;
            while (pipinAnim.Duration > 2)
                pipinAnim.Duration *= 0.5f;
        }
    }
}
