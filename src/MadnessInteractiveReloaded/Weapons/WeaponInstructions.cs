using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Information for spawning a weapon. This is also what gets serialised to disk for each weapon file.
/// </summary>
public class WeaponInstructions
{
    /// <summary>
    /// The value by which the weapon is registered.
    /// </summary>
    public string Id;

    /// <summary>
    /// The weapon info. 
    /// Damage, recoil, if swords can deflect this weapons bullets, accuracy, range, etc.
    /// </summary>
    public WeaponData WeaponData;
    
    /// <summary>
    /// The core texture of the weapon, minus moving parts.
    /// </summary>
    public AssetRef<Texture> BaseTexture;

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
    /// Where the weapon can be held.
    /// For instance, one hand on the pistol grip and the other on the foregrip.
    /// </summary>
    public IList<Vector2> HoldPoints;

    /// <summary>
    /// Parts that move like slides.
    /// </summary>
    public IList<AnimatedWeaponPart>? AnimatedParts;

    public float OnFloorAngle = 0;

    public WeaponInstructions(string codeName, WeaponData weaponData, AssetRef<Texture> baseTexture, bool holdForGrip, Vector2 barrelEndPoint, Vector2 casingEjectionPoint, IList<Vector2> holdPoints, IList<AnimatedWeaponPart>? animatedParts = null, bool holdStockHandPose = false)
    {
        Id = codeName;
        WeaponData = weaponData;
        BaseTexture = baseTexture;
        HoldForGrip = holdForGrip;
        BarrelEndPoint = barrelEndPoint;
        CasingEjectionPoint = casingEjectionPoint;
        HoldPoints = holdPoints;
        AnimatedParts = animatedParts;
        HoldStockHandPose = holdStockHandPose;
    }
}
