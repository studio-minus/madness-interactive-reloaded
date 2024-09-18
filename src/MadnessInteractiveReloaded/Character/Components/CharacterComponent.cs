using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Walgelijk;

namespace MIR;

public class CharacterComponent : Component
{
    public const float DodgeRegenerationCooldown = 0.8f;

    /// <summary>
    /// Generic name.
    /// See <see cref="CharacterStats.Name"/> for
    /// actual character names.
    /// </summary>
    public string Name;

    /// <summary>
    /// Is the character alive?
    /// </summary>
    public bool IsAlive { get; private set; } = true;

    /// <summary>
    /// Has the character been ragdolled?
    /// </summary>
    public bool HasBeenRagdolled = false;

    /// <summary>
    /// Should the character be allowed to move?
    /// </summary>
    public bool AllowWalking = true;

    /// <summary>
    /// Flags to enable/disable specific behaviour
    /// </summary>
    public CharacterFlags Flags = CharacterFlags.Default;

    /// <summary>
    /// Returns true if the given flags are present in <see cref="Flags"/>
    /// </summary>
    public bool HasFlag(CharacterFlags flag) => (flag & Flags) == flag;

    /// <summary>
    /// Where the thing we want to shoot at is.
    /// </summary>
    public Vector2 AimTargetPosition;

    /// <summary>
    /// When the aim target position is determined, this should be set to that position relative to the character. This will be used for when an animation uses the <see cref="AnimationConstraint.PreventAiming"/> constraint.
    /// </summary>
    public Vector2 RelativeAimTargetPosition;

    /// <summary>
    /// Our current walk acceleration.
    /// </summary>
    public Vector2 WalkAcceleration;

    /// <summary>
    /// A reference to this Character's <see cref="CharacterPositioning"/> component.
    /// </summary>
    public readonly CharacterPositioning Positioning;

    /// <summary>
    /// A <see cref="Hook"/> event for when the Character dies.
    /// </summary>
    public readonly Hook<CharacterComponent> OnDeath = new();

    public Vector2 AimDirection => Vector2.Normalize(AimTargetPosition - new Vector2(Positioning.GlobalCenter.X, Positioning.Head.GlobalPosition.Y));

    // combat

    /// <summary>
    /// Cooldown for the dodge.
    /// </summary>
    public float DodgeMeter = 1;

    /// <summary>
    /// The current dodge cooldown.
    /// </summary>
    public float DodgeRegenCooldownTimer = 0;

    /// <summary>
    /// Determines if the character is ready to block attacks with their melee weapon. Blocking with a melee weapon may also mean the deflection of bullets.
    /// </summary>
    public bool IsMeleeBlocking;

    /// <summary>
    /// Returns true if the character <see cref="IsMeleeBlocking"/> and holding a weapon that is capable of deflecting bullets
    /// </summary>
    /// <param name="scene"></param>
    /// <returns></returns>
    public bool IsDeflectingBullets(Scene scene) => IsMeleeBlocking && Stats.CanDeflect && EquippedWeapon.TryGet(scene, out var eq) && eq.Data.CanDeflectBullets;

    /// <summary>
    /// Is the character aiming down sights?
    /// </summary>
    public bool IsIronSighting;

    /// <summary>
    /// How many things are attacking this character?
    /// </summary>
    public int CurrentAttackerCount;

    /// <summary>
    /// The <see cref="CharacterStats"/> for this character.
    /// </summary>
    public CharacterStats Stats = new();

    /// <summary>
    /// What team is this character on?
    /// </summary>
    public Faction Faction;

    /// <summary>
    /// The collision layer friendly units are on for this character.
    /// </summary>
    public uint CollisionLayer;

    /// <summary>
    /// The collision layer enemy units are on for this character.
    /// </summary>
    public uint EnemyCollisionLayer;

    /// <summary>
    /// Set that determines which entities to ignore when doing collision queries for attacks
    /// </summary>
    public HashSet<Entity> AttackIgnoreCollision = [];

    /// <summary>
    /// If this character's attacks cannot be autododged.
    /// Such as when they are performing an AccurateShot: <see cref="AccurateShotSystem"/>.
    /// </summary>
    public bool AttacksCannotBeAutoDodged;

    /// <summary>
    /// A <see cref="ComponentRef{T}"/> to the character's currently equipped weapon.
    /// </summary>
    public ComponentRef<WeaponComponent> EquippedWeapon;

    /// <summary>
    /// The range this character can pick up things in
    /// </summary>
    public float HandPickupRange = 400;

    /// <summary>
    /// The <see cref="CharacterLook"/> preset for this character. 
    /// What do they look like? Grunt, clown, conductor, agent, etc.
    /// </summary>
    public CharacterLook Look = Registries.Looks.Get("grunt"); // TODO game crash if "grunt" does not exist

    /// <summary>
    /// What layer to render this character on.
    /// </summary>
    public RenderOrder BaseRenderOrder;

    /// <summary>
    /// For redrawing the character and their clothes.
    /// For example, when they change direction and flip from left to right or vice versa.
    /// </summary>
    public bool NeedsLookUpdate = true;

    /// <summary>
    /// The list of animations to play.
    /// </summary>
    public readonly List<ActiveCharacterAnimation> Animations = new();

    public int AnimationFlipFlop = 0;

    /// <summary>
    /// The duration of blending between animations.
    /// </summary>
    public float AnimationMixDuration = 0.3125f;

    /// <summary>
    /// The factor of mix between previous animation and current animation (for smooth transitions).
    /// </summary>
    public float AnimationMixProgress = 0;

    /// <summary>
    /// If we have an animation playing.
    /// </summary>
    public bool IsPlayingAnimation => Animations.Count != 0;

    /// <summary>
    /// The factor of mix between the non-animated state and the animated state
    /// </summary>
    public float AnimationTransitionFactor = 0;

    /// <summary>
    /// Easing between animations.
    /// </summary>
    public float AnimationTransitionFactorEased = 0;

    /// <summary>
    /// The currently playing animation if it is playing one, otherwise null.
    /// </summary>
    public ActiveCharacterAnimation? MainAnimation => IsPlayingAnimation ? Animations[^1] : null;

    //public bool IsVerticallyExitingDoor;

    /// <summary>
    /// Overall tint (multiply). Applied when look is updated with <see cref="NeedsLookUpdate"/>
    /// </summary>
    public Color Tint = Colors.White;

    /// <summary>
    /// Should the animation clock be incremented? Animations will pause if this is false, which might cause issues in places where routines are used
    /// </summary>
    public bool EnableAnimationClock = true;

    /// <summary>
    /// Does this character have a weapon equipped?
    /// </summary>
    public bool HasWeaponEquipped => EquippedWeapon.IsValid(Game.Main.Scene); // TODO Ideally, "HasWeaponEquipped" should not be used, but instead EquippedWeapon.IsValid(Scene). The inconvenience is the Scene parameter.

    /// <summary>
    /// The line segment that is used to position deflected bullets and effects. This usually follows the sword blade.
    /// </summary>
    public (Vector2 A, Vector2 B) DeflectionLine;

    /// <summary>
    /// Ability animation constraints set by attached <see cref="IAnimationConstraintComponent"/>s
    /// </summary>
    public AnimationConstraint AdditionalConstraints;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="positioning"></param>
    public CharacterComponent(string name, CharacterPositioning positioning)
    {
        Name = name;
        Positioning = positioning;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scene"></param>
    /// <returns>The width of the weapon texture for approximately getting the offset of the barrel. 
    /// Returns 0 if the weapon is a melee type weapon or if no weapon is equipped.</returns>
    public float GetWeaponBarrelDistance(Scene scene)
    {
        if (EquippedWeapon.TryGet(scene, out var weapon))
        {
            if (weapon.Data.WeaponType == WeaponType.Melee)
                return 0; // if the weapon is a melee weapon, it has no barrel
            return weapon.Texture?.Value.Width ?? 0; // get weapon base texture width and return it, or 0 if it's null
        }
        return 0; // no weapon, return 0
    }

    /// <summary>
    /// Can we dodge at the moment?
    /// </summary>
    /// <returns>A bool denoting if the dodgemeter is full enough to dodge or not.</returns>
    public bool HasDodge() => (int)(DodgeMeter * 100) >= 1;

    /// <summary>
    /// Returns true if this character's dodge, with respect to the stats' dodge ability, is lower than the given threshold (ratio from 0 to 1)
    /// </summary>
    public bool IsLowOnDodge(float threshold = 0.5f) => Stats.DodgeAbility <= float.Epsilon ? false : DodgeMeter / Stats.DodgeAbility < threshold;

    /// <summary>
    /// Remove some "energy" from the <see cref="DodgeMeter"/>.
    /// </summary>
    /// <param name="drain">How much to drain.</param>
    public void DrainDodge(float drain)
    {
        DodgeMeter -= drain;
        if (DodgeMeter < 0)
            DodgeMeter = 0;
        DodgeRegenCooldownTimer = DodgeRegenerationCooldown;
    }

    /// <summary>
    /// Kill the character. Dispatches <see cref="OnDeath"/> and clears it's listeners.
    /// Sets <see cref="IsAlive"/> to false.
    /// </summary>
    public void Kill()
    {
        if (IsAlive)
        {
            OnDeath.Dispatch(this);
            OnDeath.ClearListeners();
        }

        IsAlive = false;
    }

    /// <summary>
    /// Delete this entity.
    /// </summary>
    public void Delete(Scene scene)
    {
        scene.RemoveEntity(Entity);

        foreach (var ent in Positioning.BodyDecorations)
            scene.RemoveEntity(ent);
        foreach (var ent in Positioning.HeadDecorations)
            scene.RemoveEntity(ent);
        foreach (var ent in Positioning.Hands)
            scene.RemoveEntity(ent.Entity);
        foreach (var ent in Positioning.Feet)
            scene.RemoveEntity(ent.Entity);

        scene.RemoveEntity(Positioning.Body.Entity);
        scene.RemoveEntity(Positioning.Head.Entity);

        if (scene.HasTag(Entity, Tags.Player) && scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out var entity))
            scene.RemoveEntity(entity);
    }

    /// <summary>
    /// Equips the character with a weapon.
    /// Drops their currently <see cref="EquippedWeapon"/> if they have one equipped already.
    /// </summary>
    public bool EquipWeapon(Scene scene, WeaponComponent weapon)
    {
        if (weapon.Wielder.TryGet(scene, out _))
            return false;

        if (!IsAlive)
            return false;

        if (EquippedWeapon.IsValid(scene))
            DropWeapon(scene);

        if (scene.HasComponent<ThrowableProjectileComponent>(weapon.Entity))
            scene.DetachComponent<ThrowableProjectileComponent>(weapon.Entity);

        weapon.IsAttachedToWall = false;
        weapon.StuckInsideParams = null;
        weapon.Wielder = new ComponentRef<CharacterComponent>(Entity);
        weapon.Timer = float.MaxValue;
        EquippedWeapon = new ComponentRef<WeaponComponent>(weapon.Entity);
        scene.SyncBuffers(); // TODO is this necessary

        return true;
    }

    /// <summary>
    /// Drops their weapon if they're holding one.
    /// </summary>
    /// <param name="scene"></param>
    public void DropWeapon(Scene scene)
    {
        if (!EquippedWeapon.IsValid(scene))
            return;

        var weapon = EquippedWeapon.Get(scene);
        var transform = scene.GetComponentFrom<TransformComponent>(EquippedWeapon.Entity);

        weapon.Wielder = default;
        transform.Position = new Vector2(transform.Position.X, transform.Position.Y) +
                             Vector2.TransformNormal(weapon.HoldPoints[0], transform.LocalToWorldMatrix);

        if (!weapon.HasRoundsLeft && !weapon.IsAttachedToWall)
        {
            if (scene.TryGetComponentFrom<DespawnComponent>(EquippedWeapon.Entity, out var despawn))
                despawn.DespawnTime = 5;
            else
                scene.AttachComponent(EquippedWeapon.Entity, new DespawnComponent(5));
        }

        EquippedWeapon = default;
    }

    /// <summary>
    /// Get a <see cref="Rect"/> for this characters bounding box.
    /// </summary>
    /// <param name="scene"></param>
    /// <returns></returns>
    public Rect GetBoundingBox(Scene scene)
    {
        if (!scene.HasEntity(Entity) || !scene.HasEntity(Positioning.Head.Entity) || !scene.HasEntity(Positioning.Head.Entity))
            return default;

        var r = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        var headTransform = scene.GetComponentFrom<TransformComponent>(Positioning.Head.Entity);
        var bodyTransform = scene.GetComponentFrom<TransformComponent>(Positioning.Body.Entity);

        r = r.StretchToContain(headTransform.GetBounds());
        r = r.StretchToContain(bodyTransform.GetBounds());

        return r;
    }

    /// <summary>
    /// Delete the weapon the player is holding.
    /// </summary>
    /// <param name="scene"></param>
    public void DeleteHeldWeapon(Scene scene)
    {
        if (!EquippedWeapon.IsValid(scene))
            return;
        foreach (var item in scene.GetComponentFrom<DespawnComponent>(EquippedWeapon.Entity).AlsoDelete!) // this cant be null unless the weapon was created without Prefabs.CreateWeaponw which should never happen
            scene.RemoveEntity(item);
        scene.RemoveEntity(EquippedWeapon.Entity);
    }

    /// <summary>
    /// Get the next available ability slot. Returns true if there is space left.
    /// This method is quite expensive so please use it sparingly or cache the results.
    /// </summary>
    public bool TryGetNextAbilitySlot(Scene scene, out AbilitySlot slot)
    {
        slot = default;
        List<AbilitySlot> remaining = [.. Enum.GetValues<AbilitySlot>()]; // list because we want the results to be ordered
        remaining.Remove(AbilitySlot.None);

        foreach (var item in scene.GetAllComponentsFrom(Entity))
            if (item is CharacterAbilityComponent ability)
                remaining.Remove(ability.Slot);

        if (remaining.Count == 0)
            return false;

        slot = remaining.First();
        return true;
    }

    internal bool HasWeaponOfType(Scene scene, WeaponType type)
    {
        return EquippedWeapon.TryGet(scene, out var wpn) && wpn.Data.WeaponType == type;
    }
}