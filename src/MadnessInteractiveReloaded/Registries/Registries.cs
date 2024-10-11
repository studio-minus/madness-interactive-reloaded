using MIR.Cutscenes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
namespace MIR;

/// <summary>
/// A static class that aggregates many <see cref="Registry{T}"/>s 
/// </summary>
public static class Registries
{
    public static class Armour
    {
        public static Registry<ArmourPiece> Body = new();
        public static Registry<ArmourPiece> Head = new();
        public static Registry<ArmourPiece> BodyAccessory = new();
        public static Registry<ArmourPiece> HeadAccessory = new();
        public static Registry<HandArmourPiece> HandArmour = new();
    }

    public static readonly Registry<CharacterLook> Looks = new();
    public static readonly Registry<CharacterStats> Stats = new();
    public static readonly Registry<MeleeSequence> MeleeSequences = new();
    public static readonly Registry<LevelEntry> Levels = new();
    public static readonly Registry<WeaponInstructions> Weapons = new();
    public static readonly Registry<CharacterAnimation> Animations = new();

    public static class Experiment
    {
        public static Registry<ExperimentCharacterPreset> CharacterPresets = new();
    }

    public static readonly List<DjTrack> Dj = new();
    public static readonly Registry<Cutscene> Cutscenes = new();
    public static readonly Registry<Campaign> Campaigns = new();
    public static readonly Registry<Faction> Factions = new();
    public static readonly Registry<CampaignStats> CampaignStats = new();
    public static readonly Registry<Language> Languages = new();
    public static readonly List<AssetRef<StreamAudioData>> IncidentMusicSet = new();

    public static void ClearAll()
    {
        Armour.Body.Clear();
        Armour.Head.Clear();
        Armour.BodyAccessory.Clear();
        Armour.HeadAccessory.Clear();
        Armour.HandArmour.Clear();

        Looks.Clear();
        Looks.Clear();
        MeleeSequences.Clear();
        Levels.Clear();
        Weapons.Clear();
        Animations.Clear();

        Experiment.CharacterPresets.Clear();

        Dj.Clear();
        Cutscenes.Clear();
        Campaigns.Clear();
        Factions.Clear();
        CampaignStats.Clear();
        IncidentMusicSet.Clear();
    }

    public static void LoadCharacterPresets()
    {
        const string ext = ".preset";
        Experiment.CharacterPresets.Clear();

        // add player
        Experiment.CharacterPresets.Register("player", new ExperimentCharacterPreset(false, "Player", UserData.Instances.PlayerLook, Stats.Get("player")));

        // add local files
        foreach (var file in Directory.EnumerateFiles(UserData.Paths.ExperimentCharacterPresets, "*" + ext))
            try
            {
                var b = Resources.Load<ExperimentCharacterPreset>(file, true); // we resort to the Resources system for simple local files
                b.Mutable = true;
                Experiment.CharacterPresets.Register(Path.GetFileNameWithoutExtension(file), b);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load character preset \"{file}\": {e.Message}. Skipping...");
            }

        foreach (var asset in Assets.EnumerateFolder("data/character_presets"))
        {
            var meta = Assets.GetMetadata(asset);
            try
            {
                var b = Assets.Load<ExperimentCharacterPreset>(asset).Value;
                b.Mutable = false;
                Experiment.CharacterPresets.Register(Path.GetFileNameWithoutExtension(meta.Path), b);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load character preset \"{meta.Id}\": {e.Message}. Skipping...");
            }
        }
    }

    public static void LoadCampaigns()
    {
        Campaigns.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/campaigns", SearchOption.AllDirectories))
            try
            {
                var metadata = Assets.GetMetadata(asset);
                if (metadata.MimeType.Equals("application/json"))
                {
                    var c = Assets.Load<Campaign>(asset).Value;
                    Campaigns.Register(c.Id, c);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load campaign \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadCampaignsStats()
    {
        CampaignStats.Clear();

        if (Directory.Exists(UserData.Paths.CampaignStatsDir))
            foreach (var file in Directory.EnumerateFiles(UserData.Paths.CampaignStatsDir, "*.json", SearchOption.AllDirectories))
                try
                {
                    var c = JsonConvert.DeserializeObject<CampaignStats>(File.ReadAllText(file)) ?? throw new Exception("Null campaign stats");
                    CampaignStats.Register(c.CampaignId, c);
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to load campaign stats \"{file}\": {e.Message}. Skipping...");
                }
    }

    public static void LoadCutscenes()
    {
        Cutscenes.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/cutscenes", SearchOption.AllDirectories))
            try
            {
                var metadata = Assets.GetMetadata(asset);
                var cutscene = Assets.Load<Cutscene>(asset);

                Cutscenes.Register(Path.GetFileNameWithoutExtension(metadata.Path), cutscene);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load cutscene \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadMeleeSequences()
    {
        MeleeSequences.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/melee_sequences", SearchOption.AllDirectories))
            try
            {
                MeleeSequences.Register(Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path), Assets.Load<MeleeSequence>(asset));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load melee sequence \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadStats()
    {
        Stats.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/stats", SearchOption.AllDirectories))
            try
            {
                Stats.Register(Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path), Assets.Load<CharacterStats>(asset));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load character stats \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadWeapons()
    {
        Weapons.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/weapons", SearchOption.AllDirectories))
            try
            {
                var weapon = Assets.Load<WeaponInstructions>(asset).Value;
                Weapons.Register(weapon.Id, weapon);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load weapon \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadLooks()
    {
        Looks.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/looks", SearchOption.AllDirectories))
            try
            {
                var look = Assets.Load<CharacterLook>(asset);
                Looks.Register(Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path), look);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load character look \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadArmour()
    {
        Armour.Head.Clear();
        Armour.HeadAccessory.Clear();
        Armour.Body.Clear();
        Armour.BodyAccessory.Clear();
        Armour.HandArmour.Clear();

        foreach (var asset in Assets.EnumerateFolder("data/armour/glove_armour", SearchOption.AllDirectories))
            try
            {
                var piece = Assets.Load<HandArmourPiece>(asset);
                Armour.HandArmour.Register(Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path), piece);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load hand armour \"{asset}\": {e.Message}. Skipping...");
            }

        var otherArmour = Assets.EnumerateFolder("data/armour/body_armour", SearchOption.AllDirectories);
        otherArmour = otherArmour.Concat(Assets.EnumerateFolder("data/armour/head_armour", SearchOption.AllDirectories));
        foreach (var asset in otherArmour)
            try
            {
                var piece = Assets.Load<ArmourPiece>(asset).Value;
                IRegistry<string, ArmourPiece> reg = piece.Type switch
                {
                    ArmourPieceType.Head => Armour.Head,
                    ArmourPieceType.HeadAccessory => Armour.HeadAccessory,
                    ArmourPieceType.Body => Armour.Body,
                    ArmourPieceType.BodyAccessory => Armour.BodyAccessory,
                    ArmourPieceType.Hand => throw new Exception("Hand armour should use *.hand files"),
                    _ => throw new Exception($"unknown armour type"),
                };
                reg.Register(Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path), piece);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load armour \"{asset}\": {e.Message}. Skipping...");
            }
    }

    public static void LoadLevels()
    {
        Levels.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/levels", SearchOption.AllDirectories))
            try
            {
                var entry = LevelDeserialiser.LoadLevelEntry(asset);
                Levels.Register(entry.Id, entry);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load level \"{asset}\": {e.Message}. Skipping...");
                //TODO how to handle level load failure? 
            }
    }

    public static void LoadAnimations()
    {
        Animations.Clear();
        foreach (var asset in Assets.EnumerateFolder("data/animations", SearchOption.AllDirectories))
        {
            try
            {
                var key = Path.GetFileNameWithoutExtension(Assets.GetMetadata(asset).Path);
                var anim = Assets.Load<CharacterAnimation>(asset);
                Animations.Register(key, anim);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load animation \"{asset}\": {e.Message}. Skipping...");
            }
        }
    }

    public static void LoadDjTracks()
    {
        Dj.Clear();
        var asset = new AssetId("data/dj.json");

        foreach (var id in Assets.AssetPackages)
        {
            if (!Assets.TryGetPackage(id, out var package))
                continue;

            if (!package.HasAsset(asset))
                continue;

            var json = package.LoadNoCache<string>(asset);
            var list = JsonConvert.DeserializeObject<DjTrack[]>(json);
            if (list != null)
                for (int i = 0; i < list.Length; i++)
                {
                    var track = list[i];
                    Dj.Add(track);
                }
        }
    }

    public static void LoadIncidentMusicSet()
    {
        IncidentMusicSet.Clear();
        var asset = new AssetId("data/incident_music.json");

        foreach (var id in Assets.AssetPackages)
        {
            if (!Assets.TryGetPackage(id, out var package))
                continue;

            if (!package.HasAsset(asset))
                continue;

            var json = package.LoadNoCache<string>(asset);
            var list = JsonConvert.DeserializeObject<AssetRef<StreamAudioData>[]>(json);
            if (list != null)
                for (int i = 0; i < list.Length; i++)
                {
                    var track = list[i];
                    if (track.IsValid && track.Id.Exists && Assets.TryGetMetadata(track.Id, out var md))
                        if (md.Path.EndsWith("ogg", StringComparison.InvariantCultureIgnoreCase))
                            IncidentMusicSet.Add(track);
                }
        }
    }

    public static void LoadFactions()
    {
        FactionRelationships.Clear();
        Factions.Clear();

        foreach (var asset in Assets.EnumerateFolder("data/factions", SearchOption.AllDirectories))
        {
            try
            {
                var faction = Assets.Load<Faction>(asset).Value;
                Factions.Register(faction.Id, faction);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load faction \"{asset}\": {e.Message}. Skipping...");
            }
        }
    }

    public static void LoadLanguages()
    {
        Languages.Clear();

        foreach (var asset in Assets.EnumerateFolder("locale", SearchOption.AllDirectories))
        {
            try
            {
                var meta = Assets.GetMetadata(asset);
                var lang = Assets.Load<Language>(asset).Value;
                var id = Path.GetFileNameWithoutExtension(meta.Path);

                if (Languages.TryGet(id, out var existing))
                {
                    // languages are additive

                    foreach (var item in lang.Table)
                        existing.Table.AddOrSet(item.Key, item.Value);
                }
                else
                    Languages.Register(id, lang);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load language \"{asset}\": {e.Message}. Skipping...");
            }
        }

        Localisation.FallbackLanguage = Localisation.CurrentLanguage = Languages.GetAllValues().First();
    }
}
