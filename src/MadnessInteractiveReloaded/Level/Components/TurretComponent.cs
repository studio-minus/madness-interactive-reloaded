using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class TurretComponent : Component
{
    public Vector2 Position;
    public Vector2 AngleRangeRads;
    public RenderOrder RenderOrder;
    public string Faction = "aahw";
    public float AngleRads;

    public Sound[] ShootSounds = [
        SoundCache.Instance.LoadSoundEffect("sounds/firearms/cqb_1.wav"),
        SoundCache.Instance.LoadSoundEffect("sounds/firearms/cqb_2.wav"),
        SoundCache.Instance.LoadSoundEffect("sounds/firearms/cqb_3.wav"),
    ];

    // ------------

    public float Health;
    public bool Exploded;
    public ComponentRef<CharacterComponent> Target;
    public float Lifespan = Utilities.RandomFloat(0, 1);
    public float FindTargetClock;
    public float ShootClock;

    public Vector2 BarrelNode;
    public Vector2 BarrelNodeNext;
    public Vector2 BarrelNodeVelocity;
    public Vector2 BarrelNodeForceAcc;

    public float AimAngleRads;
    public float RenderedAimAngleRads;
}
