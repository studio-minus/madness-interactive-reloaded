using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;
using static MIR.Textures;

namespace MIR;

public class HolyShieldAbilityComponent : CharacterAbilityComponent, IDisposable
{
    public override string DisplayName => "Holy shield";

    public HolyShieldAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Hold)
    {
    }

    private float visiblity = 0;
    private Entity blocker;

    private Matrix3x2 pTransform;
    private Vector2 pOffset;

    private float flashTimer = 0;

    public override AnimationConstraint Constraints => IsUsing ? AnimationConstraint.PreventAllAttacking : default;

    public override void StartAbility(AbilityParams a)
    {
        if (!a.Character.Positioning.HandPoseFunctionOverride.Contains(PoseHand))
            a.Character.Positioning.HandPoseFunctionOverride.Add(PoseHand);

        pOffset = default;

        blocker = Scene.CreateEntity();
        var transform = Scene.AttachComponent(blocker, new TransformComponent());
        Scene.AttachComponent(blocker, new IgnoreLineOfSightComponent());

        Scene.AttachComponent(blocker, new IsShotTriggerComponent()).Event.AddListener(e =>
        {
            var reflected = Vector2.Normalize(Vector2.Reflect(e.Incoming, e.Normal) + Utilities.RandomPointInCircle(0, 0.2f));
            Scene.GetSystem<BulletTracerSystem>().ShowTracer(e.Point, e.Point + reflected * 5000); // fake deflect!
            flashTimer = 1;
        });

        Scene.AttachComponent(blocker, new PhysicsBodyComponent
        {
            BodyType = BodyType.Dynamic,
            FilterBits = CollisionLayers.BlockBullets,
            Collider = new RectangleCollider(transform, new Vector2(123, 500))
        });
    }

    public override void UpdateAbility(AbilityParams a)
    {
        const float duration = 0.3f;

        float lastVisibility = visiblity;

        if (IsUsing)
            visiblity += a.Time.DeltaTime / duration;
        else
        {
            if (blocker != Entity.None)
                Scene.RemoveEntity(blocker);

            visiblity -= a.Time.DeltaTime / duration;
        }

        visiblity = float.Clamp(visiblity, 0, 1);

        flashTimer -= a.Time.DeltaTime / duration;
        flashTimer = float.Clamp(flashTimer, 0, 1);

        if (visiblity < float.Epsilon)
        {
            if (lastVisibility >= float.Epsilon)
                a.Character.Positioning.HandPoseFunctionOverride.Remove(PoseHand);
            return;
        }

        var p = a.Character.Positioning.Body.ComputedVisualCenter;
        p += pOffset = Utilities.SmoothApproach(pOffset, a.Character.AimDirection * 400 * a.Character.Positioning.Scale, 12, Time.DeltaTime);
        p.Y += 50;

        var texture = Assets.Load<Texture>("textures/holy_shield.qoi").Value;
        var glow = Assets.Load<Texture>("textures/holy_shield_glow.qoi").Value;

        Draw.Reset();
        Draw.Order = RenderOrders.Effects;


        var targetTransform =
            Matrix3x2.CreateScale(1, float.Sign(a.Character.AimDirection.X)) *
            Matrix3x2.CreateRotation(float.Atan2(a.Character.AimDirection.Y * 0.5f, a.Character.AimDirection.X));

        Draw.TransformMatrix = pTransform = decayMatrix(pTransform, targetTransform, 16, Time.DeltaTime);
        Draw.TransformMatrix *= Matrix3x2.CreateTranslation(p);

        Draw.Colour = Colors.Red.WithAlpha(visiblity);
        Draw.BlendMode = BlendMode.Addition;
        Draw.Texture = glow;
        Draw.Quad(new Rect(default, glow.Size));

        Draw.Colour = Colors.White.WithAlpha(visiblity * visiblity);
        Draw.BlendMode = BlendMode.AlphaBlend;
        Draw.Colour = Utilities.Lerp(Draw.Colour, Colors.Red, Easings.Cubic.In(flashTimer));
        Draw.Texture = texture;
        Draw.Quad(new Rect(default, texture.Size));

        if (Scene.TryGetComponentFrom<TransformComponent>(blocker, out var blockerTransform))
        {
            blockerTransform.Position = p;
        }

        // from the mind of Freya Holmér
        static float decay(float a, float b, float decay, float dt) => b + (a - b) * float.Exp(-decay * dt);
        static Matrix3x2 decayMatrix(Matrix3x2 a, Matrix3x2 b, float decay, float dt) => b + (a - b) * float.Exp(-decay * dt);
    }

    public override void EndAbility(AbilityParams a)
    {
        if (blocker != Entity.None)
            Scene.RemoveEntity(blocker);
    }

    private void PoseHand(HandPoseParams p)
    {
        float animTime = Easings.Cubic.InOut(visiblity);

        var charPos = p.Character.Positioning;
        var secondHand = charPos.Hands.Second;
        var firstHand = charPos.Hands.First;
        bool isHoldingTwoHanded = false;

        charPos.SecondaryHandFollowsPrimary = false;

        if (p.Character.HasWeaponEquipped && p.Character.EquippedWeapon.TryGet(p.Scene, out var equipped))
        {
            isHoldingTwoHanded = equipped.HoldPoints.Length > 1;
        }

        secondHand.PosePosition = Vector2.Lerp(secondHand.PosePosition, charPos.HandMousePosition + p.Character.AimDirection * -20, animTime);
        secondHand.PoseRotation = Utilities.LerpAngle(secondHand.PoseRotation, Utilities.VectorToAngle(p.Character.AimDirection) + 45 * charPos.FlipScaling, animTime);
        if (animTime > 0.3f)
            secondHand.Look = HandLook.Open;

        firstHand.PosePosition = Vector2.Lerp(firstHand.PosePosition, new Vector2((charPos.IsFlipped ? 50 : -50), -80), animTime);
        firstHand.PoseRotation = Utilities.LerpAngle(firstHand.PoseRotation, charPos.IsFlipped ? 180 + 45 : -45, animTime);
        firstHand.Look = HandLook.Fist;
    }

    public void Dispose()
    {
        if (blocker != Entity.None)
            Scene.RemoveEntity(blocker);
    }
}
