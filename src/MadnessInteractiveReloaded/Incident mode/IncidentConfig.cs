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

        WeightedGrabBag<EnemySpawnInstructions> spawnInstr = [];

        spawnInstr.Add(new("grunt", "grunt", "aahw"), 4);
        spawnInstr.Add(new("agent", "agent", "aahw"), 5);
        spawnInstr.Add(new("engineer", "engineer", "aahw"), 4);
        spawnInstr.Add(new("machinist", "machinist", "aahw"), 2);
        spawnInstr.Add(new("soldat", "soldat", "aahw"), 3.5f);
        //spawnInstr.Add(new("experiment", "experiment", "aahw"), 1);
        //spawnInstr.Add(new("end_soldat", "end_soldat", "aahw"), 0.2f);

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
            int maxCycles = 5000;
            var levels = c.Levels.Select(Registries.Levels.Get).Select(l => l.Level.Value).ToArray(); // TODO this is slow lol
            var mid = levels.Skip(1).SkipLast(1).ToArray();
            while (true)
            {
                KillTarget = levels.Sum(l => l.BodyCountToWin);
                if (KillTarget == targetKillCount || maxCycles-- <= 0)
                    break;
                var sign = int.Sign(targetKillCount - KillTarget);

                var lvl = rand.GetItems(mid, 1)[0];
                var existingNpcs = lvl.Objects.Count(b => b is NPC);

                lvl.BodyCountToWin += sign;
                lvl.BodyCountToWin = int.Max(1, int.Max(existingNpcs, lvl.BodyCountToWin));
            }
        }

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
                        loaded.Objects.Add(new GameSystem(null!) { SystemTypeName = sys }); /// add incident mode system if not present

                    if (i > 0 && i < (c.Levels.Length - 1))
                    {
                        if (loaded.EnemySpawnInstructions.Count != 0 && loaded.MaxEnemyCount != 0)
                        {
                            loaded.MaxEnemyCount = rand.Next(2, 4);
                            loaded.MaxSimultaneousAttackingEnemies = rand.Next(2, 4);
                            loaded.Weapons = [.. rand.GetItems(wpns, rand.Next(2, 30))];
                            loaded.EnemySpawnInterval = float.Lerp(0.01f, 0.2f, rand.NextSingle());

                            loaded.EnemySpawnInstructions.Clear();
                            for (int n = 0; n < rand.Next(2, 10); n++)
                            {
                                var instr = spawnInstr.Grab();
                                loaded.EnemySpawnInstructions.Add(instr);
                            }
                        }
                    }
                }
            }
        }

        return c;
    }
}