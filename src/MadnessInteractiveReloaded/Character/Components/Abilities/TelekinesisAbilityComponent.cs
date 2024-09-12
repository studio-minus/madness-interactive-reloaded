using System;
using System.Linq;
using System.Numerics;
using Walgelijk;

namespace MIR;

public class TelekinesisAbilityComponent : CharacterAbilityComponent
{
    public override string DisplayName => "Telekinesis";

    private ComponentRef<VelocityComponent>[] affectedVels = [];
    private float[] weaponTimers = [];
    private bool[] weaponIgnore = [];

    private float time = 0;
    private bool wasUsing = false;

    public TelekinesisAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Hold)
    {
    }

    public override AnimationConstraint Constraints => IsUsing ? AnimationConstraint.PreventAllAttacking : default;

    public override void StartAbility(AbilityParams a)
    {
        // TODO use square distance comparison (does it really matter? its not 1998) 👩
        const float radius = 400;

        affectedVels = [.. a.Scene.GetAllComponentsOfType<VelocityComponent>().Where(s => Vector2.Distance(s.Position, a.Character.AimTargetPosition) < radius).Where(IsValidWeapon)];

        // expelliarmus
        foreach (var weapon in a.Scene.GetAllComponentsOfType<WeaponComponent>())
        {
            if (weapon.Wielder.Entity == a.Character.Entity)
                continue;

            var transform = a.Scene.GetComponentFrom<TransformComponent>(weapon.Entity);

            if (Vector2.Distance(transform.Position, a.Character.AimTargetPosition) < radius)
            {
                if (weapon.Wielder.TryGet(a.Scene, out var wielder) && a.Scene.TryGetComponentFrom<VelocityComponent>(weapon.Entity, out var vel))
                {
                    wielder.DropWeapon(a.Scene);
                    wielder.PlayAnimation(Registries.Animations.Get("stun_gun_stolen"));
                    affectedVels = [vel, .. affectedVels]; // TODO okay this might actually be slow
                }
            }
        }

        // set weapon timers
        weaponTimers = new float[affectedVels.Length];
        for (int i = 0; i < weaponTimers.Length; i++)
            weaponTimers[i] = Utilities.RandomFloat(0, 1);
        weaponIgnore = new bool[affectedVels.Length];

        time = 0;
        wasUsing = true;

        a.Character.Positioning.HandPoseFunctionOverride.Add(PoseHand);
    }

    private bool IsValidWeapon(VelocityComponent n)
    {
        if (Game.Main.Scene.TryGetComponentFrom<WeaponComponent>(n.Entity, out var weapon))
            return !weapon.IsBeingWielded && weapon.HasRoundsLeft;
        return false;
    }

    private void PoseHand(HandPoseParams p)
    {
        float animTime = Easings.Cubic.InOut(float.Clamp(time * 4, 0, 1));

        HandPosingFunctions.FistFight(p);

        var charPos = p.Character.Positioning;
        var secondHand = charPos.Hands.Second;
        var firstHand = charPos.Hands.First;
        bool isHoldingTwoHanded = false;

        if (p.Character.HasWeaponEquipped && p.Character.EquippedWeapon.TryGet(p.Scene, out var equipped))
        {
            isHoldingTwoHanded = equipped.HoldPoints.Length > 1;
        }

        secondHand.PosePosition = Vector2.Lerp(secondHand.PosePosition, charPos.HandMousePosition + p.Character.AimDirection * 40, animTime);
        secondHand.PoseRotation = Utilities.LerpAngle(secondHand.PoseRotation, Utilities.VectorToAngle(p.Character.AimDirection), animTime);
        secondHand.Look = HandLook.Point;

        firstHand.PosePosition = Vector2.Lerp(firstHand.PosePosition, new Vector2((charPos.IsFlipped ? 50 : -50), -80), animTime);
        firstHand.PoseRotation = Utilities.LerpAngle(firstHand.PoseRotation, charPos.IsFlipped ? 180 + 45 : -45, animTime);
        firstHand.Look = HandLook.Fist;
    }

    public override void UpdateAbility(AbilityParams a)
    {
        if (!IsUsing && wasUsing)
        {
            time -= a.Time.DeltaTime;

            if (time < 0)
            {
                a.Character.Positioning.HandPoseFunctionOverride.Remove(PoseHand);
                time = 0;
                wasUsing = false;
            }

            return;
        }

        if (IsUsing && a.Character.DodgeMeter < a.Character.Stats.DodgeAbility)
            a.Character.DodgeMeter += a.Time.DeltaTime;
        time += a.Time.DeltaTime;
    }

    public override void FixedUpdateAbility(AbilityParams a, float dt)
    {
        base.FixedUpdateAbility(a, dt);

        var aimPos = a.Character.AimTargetPosition;
        var floorLevel = Level.CurrentLevel?.GetFloorLevelAt(aimPos.X) ?? 0;
        var distanceFromFloor = Utilities.MapRange(0, 1000, 0, 1, float.Abs(floorLevel - a.Character.AimTargetPosition.Y));
        distanceFromFloor = float.Clamp(distanceFromFloor, 0, 1);

        aimPos.X = float.Lerp(float.Sign(a.Character.AimDirection.X) * 10000, a.Character.AimTargetPosition.X, distanceFromFloor);
        aimPos.Y = float.Lerp(a.Character.AimTargetPosition.Y, floorLevel, distanceFromFloor);

        for (int i = 0; i < affectedVels.Length; i++)
        {
            var item = affectedVels[i];
            if (weaponIgnore[i])
                continue;

            if (!item.TryGet(a.Scene, out var velocity))
                continue;

            if (!a.Scene.TryGetComponentFrom<WeaponComponent>(item.Entity, out var weapon))
                continue;

            if (weapon.IsAttachedToWall && !velocity.Enabled)
            {
                weapon.IsAttachedToWall = false;
                velocity.Enabled = true;
            }

            if (!velocity.Enabled || a.Scene.HasComponent<ThrowableProjectileComponent>(velocity.Entity))
                continue;

            var targetPos = a.Character.AimTargetPosition + MadnessUtils.Noise2D(a.Time * 0.004f, i * 552.5234f) * Utilities.MapRange(0, 5, 0, 500, affectedVels.Length);
            float influence = float.Lerp(0.4f, 1f, Utilities.Hash(i * 323.5689237f));

            var delta = targetPos - velocity.Position;
            var magn = delta.Length();
            var f = 1 - float.Clamp(ForceKernel(magn * 0.02f), 0, 1);

            velocity.Velocity = Utilities.SmoothApproach(velocity.Velocity, default, 12 * influence, dt);
            velocity.RotationalVelocity = Utilities.SmoothApproach(velocity.RotationalVelocity, default, 9 * influence, dt);
            velocity.Acceleration += f * (delta / magn) * 35000 * dt * influence;
            velocity.Acceleration.Y += 120 * dt; // counteract gravity

            if (weapon.Wielder.IsValid(a.Scene))
                continue;

            var dir = Vector2.Normalize(aimPos - velocity.Position);

            if (weapon.Data.MeleeDamageType != MeleeDamageType.Axe)
            {
                var targetAngle = float.RadiansToDegrees(float.Atan2(dir.Y, dir.X)) + velocity.FloorAngleOffset;
                velocity.RotationalAcceleration += Utilities.DeltaAngle(velocity.Rotation, targetAngle) * dt * 15;
            }
            else
                velocity.RotationalAcceleration += 2500 * dt;

            if (weapon.Data.WeaponType == WeaponType.Firearm)
            {
                if (distanceFromFloor < 0.8f)
                    weapon.IsFlipped = (Utilities.AngleToVector(velocity.Rotation + velocity.FloorAngleOffset).X < 0);
            }
            else
                weapon.IsFlipped = true;

            if ((a.Ai.TryGet(a.Scene, out var ai) && ai.HasKillTarget) || (a.Player.IsValid(a.Scene) && a.Input.ActionHeld(GameAction.Attack)))
            {
                ref var timer = ref weaponTimers[i];
                timer += dt;
                weapon.Timer += dt;

                if (weapon.Data.WeaponType == WeaponType.Firearm)
                {
                    var delay = 0.3f;
                    if (timer > delay)
                    {
                        if (weapon.HasRoundsLeft)
                        {
                            velocity.Acceleration += dir * -weapon.Data.Recoil * 400;
                            velocity.RotationalAcceleration += -weapon.Data.RotationalRecoilIntensity * 25;
                            a.Scene.GetSystem<WeaponSystem>().ShootWeapon(weapon, a.Character, recoilMultiplier: 0);
                        }
                        timer = Utilities.RandomFloat(-0.1f, 0.2f);
                        weapon.Timer = 0;
                    }
                }
                else if (weapon.Data.WeaponType == WeaponType.Melee)
                {
                    float m = EaseInBack(float.Clamp((timer - 0.7f) * 3, 0, 1));
                    velocity.Acceleration += dir * m * 120000 * dt;
                    if (m >= 0.9f)
                    {
                        weaponIgnore[i] = true;
                        a.Scene.AttachComponent(velocity.Entity, new ThrowableProjectileComponent
                        (
                            weapon.Data.ThrowableDamage,
                            weapon.Data.ThrowableHeavy,
                                        true,
                            CollisionLayers.BlockPhysics | a.Character.EnemyCollisionLayer,
                            weapon.Data.ThrowableSharpBoxes, weapon.BaseSpriteEntity,
                            new ComponentRef<CharacterComponent>(a.Character.Entity)
                        ));
                    }
                }
            }
        }

        static float EaseInBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return c3 * x * x * x - c1 * x * x;
        }
    }

    public override void EndAbility(AbilityParams a)
    {
        time = float.Min(time, .25f);
        affectedVels = [];
        weaponTimers = [];
    }

    private static float ForceKernel(float x)
    {
        const float sigma = 02f;
        const float denominator = 2.506628274631f /*sqrt(tau)*/ * sigma;
        const float maxValue = 1 / denominator;

        var exponent = -(x * x) / (2 * sigma * sigma);
        var gaussian_value = float.Exp(exponent) / denominator;
        return (float)(gaussian_value / maxValue);
    }
}
