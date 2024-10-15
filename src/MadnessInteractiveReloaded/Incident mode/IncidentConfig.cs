using MIR.LevelEditor.Objects;
using System;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class IncidentConfig
{
    public const int MinKillTarget = 20;
    public const int MaxKillTarget = 200;
    public const int LevelCount = 5;

    public int Seed = Random.Shared.Next();
    public int KillTarget = 100;

    public string Name => $"INCIDENT:0x{Hashed:X}";
    public ushort Hashed => (ushort)(HashCode.Combine(Seed, KillTarget));

    public Campaign CreateCampaign(out AssetRef<StreamAudioData> selectedMusic)
    {
        var rand = new Random(Seed);

        string[] beginLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_begin"))];
        string[] endLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_end"))];
        string[] midLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident")).Except(beginLevels).Except(endLevels)];
        string[] wpns = [.. Registries.Weapons.GetAllKeys()];

        selectedMusic = Registries.IncidentMusicSet[rand.Next(0, Registries.IncidentMusicSet.Count)];

        var begin = rand.GetItems(beginLevels, 1);
        var end = rand.GetItems(endLevels, 1);
        rand.Shuffle(midLevels);

        var c = new Campaign
        {
            Name = Name,
            Author = "INCIDENT DIRECTOR",
            Description = $"Seed={Seed}; Victims={KillTarget};",
            Id = $"incident_{Hashed:X}",
            Thumbnail = Assets.Load<Texture>("error.png"),
            Temporary = true,
            Levels = [
                begin[0],
                ..midLevels.Take(LevelCount),
                end[0],
            ]
        };

        var targetKillCount = KillTarget;
        {
            var levels = c.Levels.Select(Registries.Levels.Get).Select(l => l.Level.Value).ToArray(); // TODO this is slow lol
            while (true)
            {
                KillTarget = levels.Sum(l => l.BodyCountToWin);
                if (KillTarget == targetKillCount)
                    break;
                var sign = int.Sign(targetKillCount - KillTarget);

                var lvl = Utilities.PickRandom(levels.Skip(1).SkipLast(1));
                var existingNpcs = lvl.Objects.Count(b => b is NPC);

                lvl.BodyCountToWin += sign;
                lvl.BodyCountToWin = int.Max(1, int.Max(existingNpcs, lvl.BodyCountToWin));
            }
        }

        /*{
            int bcOpening = Registries.Levels[c.Levels[0]].Level.Value.BodyCountToWin;
            int bcEnd = Registries.Levels[c.Levels[^1]].Level.Value.BodyCountToWin;

            int remaining = KillTarget - bcOpening - bcEnd;
            int[] bodyCounts = new int[c.Levels.Length];

            while (remaining > 0)
            {
                int i = rand.Next(1, c.Levels.Length - 1); // skip opening and ending 🎈🎈
                string? k = c.Levels[i];
                if (Registries.Levels.TryGet(k, out var lvl))
                {
                    var loaded = lvl.Level.Value;
                    if (i > 0 && i < (c.Levels.Length - 1))
                    {
                        if (loaded.EnemySpawnInstructions.Count != 0 && loaded.MaxEnemyCount != 0)
                        {
                            var existingNpcs = loaded.Objects.Count(b => b is NPC);
                            var o = 1 + existingNpcs;

                            bodyCounts[i] += o;
                            remaining -= o;
                        }
                    }
                }
            }

            KillTarget = bodyCounts.Sum() + bcOpening + bcEnd;
        }*/

        // make shit happen
        {
            var sys = typeof(IncidentSystem).FullName;

            for (int i = 0; i < c.Levels.Length; i++)
            {
                string? k = c.Levels[i];
                if (Registries.Levels.TryGet(k, out var lvl))
                {
                    var loaded = lvl.Level.Value;

                    loaded.BackgroundMusic = selectedMusic; // we set the music

                    if (!loaded.Objects.Any(d => d is GameSystem ss && ss.SystemTypeName != sys))
                    {
                        loaded.Objects.Add(new GameSystem(null!) { SystemTypeName = sys });
                    }

                    if (i > 0 && i < (c.Levels.Length - 1))
                    {
                        if (loaded.EnemySpawnInstructions.Count != 0 && loaded.MaxEnemyCount != 0)
                        {
                            loaded.MaxEnemyCount = rand.Next(2, 4);
                            loaded.MaxSimultaneousAttackingEnemies = rand.Next(2, 4);
                            loaded.Weapons = [.. rand.GetItems(wpns, rand.Next(2, 30))];
                            loaded.EnemySpawnInterval = float.Lerp(0.01f, 0.2f, rand.NextSingle());
                        }
                    }
                }
            }
        }

        return c;
    }
}