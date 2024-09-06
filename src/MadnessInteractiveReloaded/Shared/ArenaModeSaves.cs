using Newtonsoft.Json;
using System.IO;

namespace MIR;

public class ArenaModeSaves
{
    public static ArenaModeSaves Load()
    {
        if (!File.Exists(UserData.Paths.ArenaModeSaves))
            return new();

        var json = File.ReadAllText(UserData.Paths.ArenaModeSaves);
        return JsonConvert.DeserializeObject<ArenaModeSaves>(json) ?? new();
    }

    public static void Save(ArenaModeSaves instance)
    {
        File.WriteAllText(UserData.Paths.ArenaModeSaves, JsonConvert.SerializeObject(instance));
    }

    public ArenaModeSave[] Saves = [];

    public class ArenaModeSave
    {
        public string PlayerName = "Hero";
        public int Money = 0;

        public CharacterState Player = new()
        {
            Experience = 0,
            Look = new CharacterLook(Registries.Looks.Get("grunt")),
            Stats = new CharacterStats(Registries.Stats.Get("agent")),
            
        };
        public CharacterState[] Allies = [];
    }

    public class CharacterState
    {
        public CharacterLook Look = new();
        public CharacterStats Stats = new();

        public (string, int)? EquippedWeapon;

        public int Experience;

        public int Level => (int)float.Ceiling(float.Max(Experience, 1) / 100f);
    }

    /* THINGS TO CONSIDER FOR ARENA MODE
     *
     * This is not Project Nexus, it is Madness Interactive
     * 
     * | DIFFERENCE                           | IMPLEMENT TIME ESTIMATE |
     * | -------------------------------------|-------------------------|
     * | A: Firearms cannot be reloaded       | Mid: animations, AI...  |
     * | B: Melee weapons have no durability  | Low                     |
     * | C: Can't move up or down             | Unfeasible              |
     * | Ç: Can't take cover                  | Mid                     |
     * | D: Melee is not especially fun       | HIgh                    |
     * 
     * COMPENSATION
     * A: Guns are cheap
     * B: Melee weapons straight up have to have durability
     * C: Multiple arena levels for variety
     * Ç: Powerups / pickuppables / abilities (disks?) to replenish dodge / cause more damage
     * D: More melee animation types
     * 
     * NOTES
     * I never liked the idea of having allies
     */
}
