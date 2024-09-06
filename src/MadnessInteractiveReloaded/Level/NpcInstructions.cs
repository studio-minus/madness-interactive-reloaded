using System.Numerics;

namespace MIR;

/// <summary>
/// Info for spawning an NPC.
/// </summary>
public class NpcInstructions
{
    public string Name = "Unnamed NPC";
    public string Look;
    public string Stats;
    public string Faction;
    public string? Weapon;
    public bool Flipped;
    public Vector2 BottomCenter;
    public bool ScaleOverride = false;
    public float Scale;
    public bool IsProgressionRequirement;

    public NpcInstructions(string look, string stats, string faction, string? weapon = null, bool flipped = false, float scale = 1)
    {
        Look = look;
        Stats = stats;
        Weapon = weapon;
        Faction = faction;
        Flipped = flipped;
        Scale = scale;
    }
}
