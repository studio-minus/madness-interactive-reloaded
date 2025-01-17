using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Data for a weapon.
/// </summary>
[RequiresComponents(typeof(TransformComponent))]
public class WeaponComponent : Component
{
    /// <summary>
    /// The registry key this weapon was created from. Can be null if it wasn't created from a registry key.
    /// </summary>
    public string? RegistryKey;

    /// <summary>
    /// The information about this weapon. 
    /// Includes things like: recoil, bullets per shot, range, if it can deflect bullets (swords).
    /// </summary>
    public WeaponData Data;

    /// <summary>
    /// If it is being held by a character.
    /// </summary>
    [System.Obsolete("Use Wielder.IsValid instead")]
    public bool IsBeingWielded => Wielder.IsValid(Game.Main.Scene);

    /// <summary>
    /// The character holding this weapon.
    /// </summary>
    public ComponentRef<CharacterComponent> Wielder;

    public RenderOrder RenderOrder;

    public Entity BaseSpriteEntity;

    /// <summary>
    /// Where the weapon can be held.
    /// For instance, one hand on the pistol grip and the other on the foregrip.
    /// </summary>
    public Vector2[] HoldPoints;

    /// <summary>
    /// If the secondary hand holds the grip like a vertical foregrip or not.
    /// </summary>
    public bool HoldForGrip;

    /// <summary>
    /// If the secondary hand holds the stock with <see cref="HandLook.HoldStock"/>.
    /// </summary>
    public bool HoldStockHandPose;

    /// <summary>
    /// Where the bullets come out of.
    /// </summary>
    public Vector2 BarrelEndPoint;

    /// <summary>
    /// Where the hot brass comes out of.
    /// </summary>
    public Vector2 CasingEjectionPoint;

    /// <summary>       
    /// Parts that move like slides.
    /// </summary>
    public Entity[]? AnimatedParts;

    /// <summary>
    /// The texture of the weapon.
    /// </summary>
    public AssetRef<Texture>? Texture { get; init; }

    /// <summary>
    /// Is this weapon in the world hung up on a wall all nice and pretty?
    /// </summary>
    public bool IsAttachedToWall;

    /// <summary>
    /// How many bullets left.
    /// </summary>
    public int RemainingRounds = 12;

    public bool HasRoundsLeft => InfiniteAmmo || RemainingRounds > 0;

    /// <summary>
    /// Is it shooting?
    /// </summary>
    public bool IsFiring = false;
    public bool IsFlipped = false;

    /// <summary>
    /// If a gun is burst fire it will need more time before we can shoot again.
    /// </summary>
    public bool IsBusyBurstFiring = false;

    /// <summary>
    /// Timer used for automatic firing and other fire delay
    /// </summary>
    public float Timer = float.MaxValue;

    /// <summary>
    /// If this weapon is pump action, this flag determines if the weapon has been pumped.
    /// </summary>
    public bool HasBeenPumped = true;

    /// <summary>
    /// <b>Resets every frame!</b><br></br>
    /// If you want it to be highlighted, you need to set it to true every frame.
    /// </summary>
    public bool ShouldBeHighlighted = false;

    /// <summary>
    /// If true, <see cref="RemainingRounds"/> is never decremented and has no effect
    /// </summary>
    public bool InfiniteAmmo;

    /// <summary>
    /// Is this weapon stuck in something?<br></br>
    /// Stuck as in a sword lodged inside a character via being stabbed into them.
    /// </summary>
    public StuckInsideParameters? StuckInsideParams;

    public WeaponComponent(WeaponData stats, Entity baseSprite, Vector2[] holdPoints, Vector2 barrelEndPoint, Vector2 casingEjectionPoint)
    {
        Data = stats;
        BaseSpriteEntity = baseSprite;
        HoldPoints = holdPoints;
        BarrelEndPoint = barrelEndPoint;
        CasingEjectionPoint = casingEjectionPoint;
    }

    /// <summary>
    /// Pump a gun that pumps.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="soundPosition"></param>
    public void PumpAction(Scene scene, Vector2 soundPosition)
    {
        scene.Game.AudioRenderer.PlayOnce(Sounds.ShotgunCock);
        if (AnimatedParts != null)
            foreach (var animatedPart in AnimatedParts)
            {
                var animation = scene.GetComponentFrom<WeaponPartAnimationComponent>(animatedPart);
                animation.IsPlaying = true;
                animation.CurrentPlaybackTime = 0;
            }
        HasBeenPumped = true;
    }

    /// <summary>
    /// Get the <see cref="Rect"/> of a weapon.
    /// </summary>
    /// <param name="scene"></param>
    /// <returns></returns>
    public Rect GetBoundingBox(Scene scene)
    {
        if (!scene.HasEntity(Entity))
            return default;

        var r = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        var mainTransform = scene.GetComponentFrom<TransformComponent>(Entity);
        var baseSpriteTransform = scene.GetComponentFrom<TransformComponent>(BaseSpriteEntity);

        var center = baseSpriteTransform.Position;
        var size = baseSpriteTransform.Scale;
        var hs = size / 2;

        var a = Vector2.Transform(center + new Vector2(hs.X, hs.Y), mainTransform.LocalToWorldMatrix);
        var b = Vector2.Transform(center + new Vector2(-hs.X, hs.Y), mainTransform.LocalToWorldMatrix);
        var c = Vector2.Transform(center + new Vector2(hs.X, -hs.Y), mainTransform.LocalToWorldMatrix);
        var d = Vector2.Transform(center + new Vector2(-hs.X, -hs.Y), mainTransform.LocalToWorldMatrix);

        r.MinX = float.Min(r.MinX, float.Min(a.X, float.Min(b.X, float.Min(c.X, d.X))));
        r.MaxX = float.Max(r.MaxX, float.Max(a.X, float.Max(b.X, float.Max(c.X, d.X))));

        r.MinY = float.Min(r.MinY, float.Min(a.Y, float.Min(b.Y, float.Min(c.Y, d.Y))));
        r.MaxY = float.Max(r.MaxY, float.Max(a.Y, float.Max(b.Y, float.Max(c.Y, d.Y))));

        return r;
    }

    /// <summary>
    /// Get damage while taking into account damage falloff.
    /// </summary>
    /// <param name="bulletTravelDistance"></param>
    /// <returns></returns>
    public float GetDamageAtDistance(float bulletTravelDistance)
    {
        return float.Max((2 * Data.Damage) / float.Pow(bulletTravelDistance, 0.1f), Data.Damage);
    }
}

/// <summary>
/// How a weapon is stuck inside something. <br></br>
/// Not stuck like bugginess stuck, but stuck as in stabbed into and left there.
/// </summary>
public struct StuckInsideParameters
{
    /// <summary>
    /// The thing it is stuck in.
    /// </summary>
    public ComponentRef<TransformComponent> Parent;

    /// <summary>
    /// The offset from the thing.
    /// </summary>
    public Vector2 LocalOffset;

    /// <summary>
    /// The rotation relative to thing.
    /// </summary>
    public float LocalRotation;

    public override bool Equals(object? obj)
    {
        return obj is StuckInsideParameters parameters &&
               Parent.Entity == parameters.Parent.Entity &&
               LocalOffset.Equals(parameters.LocalOffset) &&
               float.Abs(LocalRotation - parameters.LocalRotation) < 0.001f;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Parent, LocalOffset, LocalRotation);
    }

    public static bool operator ==(StuckInsideParameters left, StuckInsideParameters right)
    {
        return left.Parent == right.Parent &&
               left.LocalOffset == right.LocalOffset &&
               float.Abs(left.LocalRotation - right.LocalRotation) < 0.001f;
    }

    public static bool operator !=(StuckInsideParameters left, StuckInsideParameters right)
    {
        return !(left == right);
    }
}