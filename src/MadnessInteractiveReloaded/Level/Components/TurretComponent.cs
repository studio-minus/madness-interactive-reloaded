using System.Numerics;
using Walgelijk;

namespace MIR;

public class TurretComponent : Component
{
    public Vector2 Position;
    public Vector2 AngleRangeRads;
    public RenderOrder RenderOrder;
    public string Faction = "aahw";
    public float AngleRads;

    // ------------

    public ComponentRef<CharacterComponent> Target;
    public float Lifespan = Utilities.RandomFloat(0, 1);
    public float FindTargetClock;
    public float ShootClock;
    public float AimAngleVelocity;
    public float AimAngleRads;
    public float AimAnglePredictedRads;
    public float RenderedAimAngleRads;
}
