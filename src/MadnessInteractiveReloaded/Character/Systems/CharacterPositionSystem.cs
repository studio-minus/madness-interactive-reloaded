using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Calculates and applies limb positioning.
/// </summary>
public class CharacterPositionSystem : Walgelijk.System
{
    private static QueryResult[] resultBuffer = new QueryResult[1];

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        bool exp = MadnessUtils.EditingInExperimentMode(Scene);

        foreach (var c in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (c.HasBeenRagdolled)
                continue;

            c.EquippedWeapon.TryGet(Scene, out var equipped);

            // iron sight and deflection animation stuff
            c.Positioning.IronSightProgress = Utilities.Clamp(c.Positioning.IronSightProgress + (c.IsIronSighting ? 1 : -1) * Time.DeltaTime / 0.2f);
            c.Positioning.MeleeBlockProgress = Utilities.Clamp(c.Positioning.MeleeBlockProgress + (c.IsMeleeBlocking ? 1 : -1) * Time.DeltaTime / 0.25f);

            ProcessRecoil(c, equipped);

            ProcessRenderers(c, equipped);
            SetLimbScale(c);
            ProcessArmour(c);

            c.NeedsLookUpdate = false;

            PositionCenter(exp, c);

            if (!exp)
                PrepareAnimation(c);
            PositionBody(c); // body goes first because it can rotate abruptly, and the head has to follow immediately
            PositionHead(c, exp); // note: the head direction determines the body rotation, but it is smoothed so it does not cause jitter. be careful with it though.
            PositionHands(c, equipped);
            PositionFeet(c);

            if (equipped != null && Scene.HasEntity(equipped.Entity))
                ApplyWeaponPosition(c, equipped);

            if (c.IsPlayingAnimationGroup("deaths") && !c.AnimationConstrainsAny(AnimationConstraint.PreventRagdoll))
            {
                var headRect = Scene.GetComponentFrom<PhysicsBodyComponent>(c.Positioning.Head.Entity).Collider.Bounds;
                var bodyRect = Scene.GetComponentFrom<PhysicsBodyComponent>(c.Positioning.Body.Entity).Collider.Bounds;
                if (Scene.GetSystem<PhysicsSystem>().QueryCircle(headRect.GetCenter(), 10, resultBuffer, CollisionLayers.BlockPhysics) > 0 ||
                    Scene.GetSystem<PhysicsSystem>().QueryCircle(bodyRect.GetCenter(), 10, resultBuffer, CollisionLayers.BlockPhysics) > 0)
                    MadnessUtils.TurnIntoRagdoll(Scene, c);
            }

            //Draw.Reset();
            //Draw.Order = RenderOrders.UserInterface;
            //Draw.Line(c.AimTargetPosition, c.AimOrigin, 5);
        }
    }

    private void ProcessRenderers(CharacterComponent character, WeaponComponent? equipped)
    {
        var charPos = character.Positioning;
        bool lookUpdate = character.NeedsLookUpdate;

        // head renderer
        var headRenderer = Scene.GetComponentFrom<QuadShapeComponent>(charPos.Head.Entity);
        headRenderer.HorizontalFlip = charPos.IsFlipped;
        headRenderer.RenderOrder = character.BaseRenderOrder.WithOrder(CharacterConstants.RenderOrders.HeadBaseOrder);
        if (lookUpdate)
        {
            var tex = charPos.IsFlipped ? character.Look.Head.Left : character.Look.Head.Right;
            if (tex.IsValid && Assets.HasAsset(tex.Id))
            {
                var transform = Scene.GetComponentFrom<TransformComponent>(headRenderer.Entity);

                var v = tex.Value;
                headRenderer.Material.SetUniform(ShaderDefaults.MainTextureUniform, v);
                headRenderer.Color = character.Tint;
                transform.Scale = v.Size * charPos.Scale * character.Look.Head.TextureScale;
            }
        }
        if (character.IsHeadAnimated())
        {
            if (charPos.Head.Unscaled)
                headRenderer.AdditionalTransform = null;
            else
            {
                var th = float.DegreesToRadians(charPos.Head.GlobalRotation);
                headRenderer.AdditionalTransform =
                    Matrix3x2.CreateRotation(-th, charPos.Head.GlobalPosition)
                    * Matrix3x2.CreateScale(charPos.Head.AnimationScale, charPos.Head.GlobalPosition)
                    * Matrix3x2.CreateRotation(th, charPos.Head.GlobalPosition);
            }
        }
        else headRenderer.AdditionalTransform = null;

        // body renderer
        var bodyRenderer = Scene.GetComponentFrom<QuadShapeComponent>(charPos.Body.Entity);
        bodyRenderer.HorizontalFlip = charPos.IsFlipped;
        bodyRenderer.RenderOrder = character.BaseRenderOrder.WithOrder(CharacterConstants.RenderOrders.BodyBaseOrder);
        if (lookUpdate)
        {
            var tex = charPos.IsFlipped ? character.Look.Body.Left : character.Look.Body.Right;
            if (tex.IsValid && Assets.HasAsset(tex.Id))
            {
                var v = tex.Value;
                bodyRenderer.Material.SetUniform(ShaderDefaults.MainTextureUniform, v);
                bodyRenderer.Color = character.Tint;
                Scene.GetComponentFrom<TransformComponent>(bodyRenderer.Entity).Scale = v.Size * charPos.Scale * character.Look.Body.TextureScale;
            }
        }
        if (character.IsBodyAnimated())
        {
            if (charPos.Body.Unscaled)
                bodyRenderer.AdditionalTransform = null;
            else
            {
                var th = float.DegreesToRadians(charPos.Body.GlobalRotation);
                bodyRenderer.AdditionalTransform =
                    Matrix3x2.CreateRotation(-th, charPos.Body.GlobalPosition)
                    * Matrix3x2.CreateScale(charPos.Body.AnimationScale, charPos.Body.GlobalPosition)
                    * Matrix3x2.CreateRotation(th, charPos.Body.GlobalPosition);
            }
        }
        else bodyRenderer.AdditionalTransform = null;

        // hand renderers
        int i = 0;
        foreach (var hand in charPos.Hands)
        {
            var hr = Scene.GetComponentFrom<QuadShapeComponent>(hand.Entity);
            hr.VerticalFlip = charPos.IsFlipped;
            var isAnimated = character.IsHandAnimated(hand);

            var handLook = hand.Look;
            if (isAnimated && hand.AnimatedHandLook.HasValue)
                handLook = hand.AnimatedHandLook.Value;

            var shouldRenderBackHand = charPos.IsFlipped ^ hand.IsLeftHand;
            var targetHandMaterial = Textures.Character.GetMaterialForHandLook(character.Look.Hands, handLook, shouldRenderBackHand, equipped?.Data.WeaponType ?? WeaponType.Firearm);
            var isTwoHandedWeapon = equipped != null && equipped.HoldPoints.Length >= 2;
            var targetHandRenderOrder = character.BaseRenderOrder;

            // if the character is holding a melee weapon and ready to block, we should render in front of the body
            if (!hand.IsLeftHand && character.Positioning.MeleeBlockProgress > 0.5f)
                targetHandRenderOrder.OrderInLayer = CharacterConstants.RenderOrders.MainHandOrder + 101;
            else if (isTwoHandedWeapon)
                targetHandRenderOrder.OrderInLayer = (charPos.IsFlipped ? CharacterConstants.RenderOrders.OtherHandOrder : CharacterConstants.RenderOrders.MainHandOrder);
            else
                targetHandRenderOrder.OrderInLayer = (shouldRenderBackHand ? CharacterConstants.RenderOrders.OtherHandOrder : CharacterConstants.RenderOrders.MainHandOrder);

            if (character.IsHandAnimated(hand))
            {
                if (hand.Unscaled)
                    hr.AdditionalTransform = null;
                else
                {
                    var th = float.DegreesToRadians(hand.GlobalRotation);
                    hr.AdditionalTransform =
                        Matrix3x2.CreateRotation(-th, hand.GlobalPosition)
                        * Matrix3x2.CreateScale(hand.AnimationScale, hand.GlobalPosition)
                        * Matrix3x2.CreateRotation(th, hand.GlobalPosition);
                }
            }
            else hr.AdditionalTransform = null;

            hr.RenderOrder = targetHandRenderOrder;
            hr.Material = targetHandMaterial;
            hr.Color = character.Tint;
            hand.ApparentRenderOrder = targetHandRenderOrder;
            i++;
        }

        // foot renderers
        i = 0;
        foreach (var foot in charPos.Feet)
        {
            var fr = Scene.GetComponentFrom<QuadShapeComponent>(foot.Entity);

            if (lookUpdate)
            {
                var tex = character.Look.Feet?.Value ?? Textures.Character.DefaultFoot;
                fr.Material = SpriteMaterialCreator.Instance.Load(tex);
                if (Scene.TryGetComponentFrom<TransformComponent>(fr.Entity, out var footTransform))
                    footTransform.Scale = charPos.Scale * tex.Size;
            }

            fr.HorizontalFlip = charPos.IsFlipped;
            fr.RenderOrder = character.BaseRenderOrder.WithOrder(CharacterConstants.RenderOrders.FootBaseOrder - charPos.Feet.Length + i);
            fr.Color = character.Tint;
            i++;
        }
    }

    private void ProcessArmour(CharacterComponent character)
    {
        var charPos = character.Positioning;

        var headRenderer = Scene.GetComponentFrom<QuadShapeComponent>(charPos.Head.Entity);
        var bodyRenderer = Scene.GetComponentFrom<QuadShapeComponent>(charPos.Body.Entity);

        // body decorations
        for (int i = 0; i < charPos.BodyDecorations.Length; i++)
        {
            var bodyDecoration = charPos.BodyDecorations[i];
            var decorationRenderer = Scene.GetComponentFrom<ApparelSpriteComponent>(bodyDecoration);
            var piece = character.Look.GetBodyLayer(i);

            decorationRenderer.Visible = piece != null;
            decorationRenderer.RenderOrder = character.BaseRenderOrder.WithOrder(CharacterConstants.RenderOrders.BodyDecorOrder);

            if (piece != null)
            {
                decorationRenderer.AdditionalTransform = bodyRenderer.AdditionalTransform;
                decorationRenderer.HorizontalFlip = charPos.IsFlipped;
                decorationRenderer.Color = character.Tint;
                if (character.NeedsLookUpdate)
                {
                    var texture = charPos.IsFlipped ? piece.Left.Value : piece.Right.Value;
                    decorationRenderer.SetPiece(texture);
                    Scene.GetComponentFrom<TransformComponent>(bodyDecoration).Scale = texture.Size * charPos.Scale * piece.TextureScale;

                    var offset = charPos.IsFlipped ? piece.OffsetLeft : piece.OffsetRight;
                    offset.X /= charPos.Head.Scale.X * charPos.FlipScaling;
                    offset.Y /= -charPos.Head.Scale.Y;
                    Scene.GetComponentFrom<TransformConstraintComponent>(bodyDecoration).PositionOffset = offset * charPos.Scale;
                }
            }
        }

        // head decorations
        for (int i = 0; i < charPos.HeadDecorations.Length; i++)
        {
            var headDecoration = charPos.HeadDecorations[i];
            var decorationRenderer = Scene.GetComponentFrom<ApparelSpriteComponent>(headDecoration);
            var piece = character.Look.GetHeadLayer(i);
            decorationRenderer.Color = character.Tint;
            decorationRenderer.Visible = piece != null;
            decorationRenderer.RenderOrder = character.BaseRenderOrder.WithOrder(i + CharacterConstants.RenderOrders.HeadDecorOrder);
            if (piece != null)
            {
                decorationRenderer.AdditionalTransform = headRenderer.AdditionalTransform;
                decorationRenderer.HorizontalFlip = charPos.IsFlipped;
                if (character.NeedsLookUpdate)
                {
                    var t = Scene.GetComponentFrom<TransformComponent>(headDecoration);
                    var texture = charPos.IsFlipped ? piece.Left.Value : piece.Right.Value;
                    decorationRenderer.SetPiece(texture);
                    t.Scale = texture.Size * charPos.Scale * piece.TextureScale;

                    var offset = charPos.IsFlipped ? piece.OffsetLeft : piece.OffsetRight;
                    offset.X /= charPos.Head.Scale.X * charPos.FlipScaling;
                    offset.Y /= -charPos.Head.Scale.Y;
                    Scene.GetComponentFrom<TransformConstraintComponent>(headDecoration).PositionOffset = offset * charPos.Scale;
                }
            }
        }
    }

    private void ApplyWeaponPosition(CharacterComponent character, WeaponComponent weapon)
    {
        if (!Scene.TryGetComponentFrom<TransformComponent>(weapon.Entity, out var wpnTransform))
            return;

        var hand = character.Positioning.Hands.First;
        var weaponTransform = Scene.GetComponentFrom<TransformComponent>(weapon.Entity);
        var velocityComponent = Scene.GetComponentFrom<VelocityComponent>(weapon.Entity);
        var measured = Scene.GetComponentFrom<MeasuredVelocityComponent>(weapon.Entity);

        weaponTransform.LocalPivot = -weapon.HoldPoints[0];
        weaponTransform.Rotation = hand.GlobalRotation;
        weaponTransform.Position = hand.GlobalPosition;
        weapon.IsFlipped = character.Positioning.IsFlipped;
        weaponTransform.Scale = new Vector2(1, weapon.IsFlipped ? -1 : 1);

        velocityComponent.Velocity = measured.DeltaTranslation;
        velocityComponent.RotationalVelocity = measured.DeltaRotation;

        weapon.RenderOrder = hand.ApparentRenderOrder.OffsetOrder(-100);
    }

    private void ProcessRecoil(CharacterComponent character, WeaponComponent? equipped)
    {
        character.Positioning.SmoothRecoil
            = Utilities.SmoothApproach(character.Positioning.SmoothRecoil, character.Positioning.CurrentRecoil, 25, Time.DeltaTime);

        if (character.HasWeaponEquipped && equipped != null)
        {
            float recoilHandlingMultiplier = (character.IsIronSighting ? 2 : 1) * character.Stats.RecoilHandlingAbility;
            character.Positioning.CurrentRecoil =
                Utilities.SmoothApproach(character.Positioning.CurrentRecoil, 0,
                equipped.Data.RecoilHandling * recoilHandlingMultiplier * 5 + 2, Time.DeltaTime);
        }
        else
            character.Positioning.CurrentRecoil = Utilities.SmoothApproach(character.Positioning.CurrentRecoil, 0, 19, Time.DeltaTime);
    }

    private void SetLimbScale(CharacterComponent character)
    {
        var charPos = character.Positioning;

        charPos.Head.Scale = Scene.GetComponentFrom<TransformComponent>(charPos.Head.Entity).Scale;
        charPos.Body.Scale = Scene.GetComponentFrom<TransformComponent>(charPos.Body.Entity).Scale;

        charPos.Hands.First.Scale = Scene.GetComponentFrom<TransformComponent>(charPos.Hands.First.Entity).Scale;
        charPos.Hands.Second.Scale = Scene.GetComponentFrom<TransformComponent>(charPos.Hands.Second.Entity).Scale;
    }

    private void PrepareAnimation(CharacterComponent character)
    {
        const float transitionFactorDuration = 0.2f;
        var charPos = character.Positioning;
        var wasPlayingAnimation = character.IsPlayingAnimation;
        var mixed = CharacterUtilities.CalculateMixedAnimation(character);

        var speedMultiplier = character.MainAnimation?.Speed ?? 1;
        character.AnimationMixProgress = Utilities.Clamp(character.AnimationMixProgress + Time.DeltaTime / character.AnimationMixDuration * speedMultiplier);

        // should the animation blend back into the non animated state instantly?
        if (character.AnimationConstrainsAny(AnimationConstraint.PreventMixTransition))
            character.AnimationTransitionFactor = 1;
        else
        {
            if (character.MainAnimation == null || character.MainAnimation.UnscaledTimer > character.MainAnimation.Animation.TotalDuration - transitionFactorDuration)
                character.AnimationTransitionFactor -= Time.DeltaTime / transitionFactorDuration;
            else
                character.AnimationTransitionFactor += Time.DeltaTime / transitionFactorDuration;
        }

        character.AnimationTransitionFactor = Utilities.Clamp(character.AnimationTransitionFactor);
        character.AnimationTransitionFactorEased = Easings.Quad.InOut(character.AnimationTransitionFactor);

        if (!wasPlayingAnimation)
        {
            character.ResetAnimation();
            return;
        }
        else
        {
            // remove animations that are finished
            for (int i = character.Animations.Count - 1; i >= 0; i--)
            {
                var a = character.Animations[i];
                if (a.IsOver)
                {
                    a.InvokeOnEnd();
                    character.Animations.RemoveAt(i);
                }
                if (character.EnableAnimationClock)
                    a.UnscaledTimer += Time.DeltaTime * a.Speed;
            }
        }

        charPos.Head.AnimationPosition = mixed.HeadPosition * charPos.Scale;
        charPos.Head.AnimationAngle = mixed.HeadRotation;
        charPos.Head.AnimationScale = mixed.HeadScale;

        charPos.Body.AnimationPosition = mixed.BodyPosition * charPos.Scale;
        charPos.Body.AnimationAngle = mixed.BodyRotation;
        charPos.Body.AnimationScale = mixed.BodyScale;

        for (int i = 0; i < charPos.Hands.Length; i++)
        {
            var transformedIndex = charPos.IsFlipped ? charPos.Hands.Length - 1 - i : i;
            var hand = charPos.Hands[i];

            //if (character.MainAnimation?.Animation.DoSmoothing ?? false)
            //    hand.AnimationPosition = Utilities.SmoothApproach(hand.AnimationPosition, transformedIndex == 0 ? mixed.Hand1Position : mixed.Hand2Position, 15, Time.DeltaTime);
            //else
            hand.AnimationPosition = (transformedIndex == 0 ? mixed.Hand1Position : mixed.Hand2Position);
            hand.AnimationScale = transformedIndex == 0 ? mixed.Hand1Scale : mixed.Hand2Scale;
            hand.AnimationAngle = transformedIndex == 0 ? mixed.Hand1Rotation : mixed.Hand2Rotation;
            hand.AnimatedHandLook = transformedIndex == 0 ? mixed.Hand1Look : mixed.Hand2Look;
        }
    }

    private void PositionCenter(bool experimentMode, CharacterComponent character)
    {
        var transform = Scene.GetComponentFrom<TransformComponent>(character.Entity);
        var charPos = character.Positioning;

        if (!Scene.HasComponent<ExitDoorComponent>(character.Entity))
        {
            var target = charPos.GlobalTarget;
            var floorLevel = (Level.CurrentLevel?.GetFloorLevelAt(target.X) ?? 0) + CharacterConstants.GetFloorOffset(charPos.Scale);
            //target.Y -= Utilities.MapRange(RenderOrders.CharacterLower.Layer, RenderOrders.CharacterUpper.Layer, 0, 80, character.BaseRenderOrder.Layer);

            if (experimentMode && character.IsAlive)
                charPos.GlobalCenter = target;
            else
            {
                // the target is (normally) set by the CharacterMovementSystem, so we offset it by the floor height to make sure it's on the ground at all times.
                target.Y += floorLevel;

                if (float.Max(charPos.FlyingAnimationOffset, charPos.FlyingOffset) > 0)
                {
                    var o = float.Abs(charPos.FlyingVelocity);
                    charPos.FlyingAnimationOffset = Utilities.SmoothApproach(charPos.FlyingAnimationOffset, charPos.FlyingOffset - o * 0.2f, 10, Time.DeltaTime);
                    target.Y += charPos.FlyingAnimationOffset;
                    target += float.Clamp(Utilities.MapRange(0, 1000, 0, 1, charPos.FlyingOffset), 0, 1) * 15 * MadnessUtils.Noise2D(Time * 0.1f, (int.Abs(character.Entity) % 1000) * 0.534f) * 10;
                }
                else
                    charPos.FlyingAnimationOffset = 0;

                if (Vector2.DistanceSquared(target, charPos.GlobalCenter) > (400 * 400))
                    charPos.GlobalCenter = target;
                else
                    charPos.GlobalCenter = Utilities.SmoothApproach(charPos.GlobalCenter, target, 20, Time.DeltaTime);
            }
        }
        transform.Position = charPos.GlobalCenter;
    }

    private void PositionHead(CharacterComponent character, bool experimentMode)
    {
        var charPos = character.Positioning;
        var head = charPos.Head;
        var impactOffset = Scene.GetComponentFrom<ImpactOffsetComponent>(head.Entity);

        // head look offset
        var targetLookOffset = Vector2.Normalize(character.AimTargetPosition - head.GlobalPosition) * 10;
        head.LookOffset = Utilities.SmoothApproach(head.LookOffset, targetLookOffset, charPos.LookOffsetSpeed, Time.DeltaTime);

        // head direction
        var headTargetDir = Utilities.Lerp(new Vector2(targetLookOffset.X * (charPos.IsFlipped ? -1 : 1), targetLookOffset.Y), new Vector2(1, 0), 0.95f);
        head.Direction = Utilities.SmoothApproach(head.Direction, headTargetDir, charPos.LookDirectionSpeed, Time.DeltaTime);

        // final position
        var headTransform = Scene.GetComponentFrom<TransformComponent>(charPos.Head.Entity);
        headTransform.LocalPivot = new Vector2(
            CharacterConstants.HeadRotationPivot.X * charPos.FlipScaling,
            CharacterConstants.HeadRotationPivot.Y);

        Vector2 pos, animatedPos, basePos;

        // head bob stuff
        if (charPos.IsBusyHopping)
        {
            if (experimentMode)
                head.BobOffset = Utilities.SmoothApproach(head.BobOffset, default, 7, Time.DeltaTime);
            else
            {
                const float intensity = 20;
                var l = charPos.HopAnimationTimer;
                var hopCurve = -4 * (l * l) + 4 * l;
                var o = new Vector2();
                o.X = intensity * Noise.GetFractal(Time.SecondsSinceLoad * 0.2f, -5.28f, charPos.NoiseOffset, 3);
                o.Y = intensity * Noise.GetFractal(64.45f, charPos.NoiseOffset, Time.SecondsSinceLoad * -0.2f, 3) / 2;
                o += character.WalkAcceleration * 0.05f * (1 - hopCurve);
                head.BobOffset = Utilities.SmoothApproach(head.BobOffset, o, 7, Time.DeltaTime);
            }
        }
        else
            head.BobOffset = Utilities.SmoothApproach(head.BobOffset, default, 15, Time.DeltaTime);

        // base position calculation
        basePos = charPos.Body.GlobalPosition;
        basePos += head.BobOffset * charPos.Scale * Utilities.Clamp(character.Positioning.HopAcceleration);
        //basePos += head.LookOffset; // disabled look offset because it looks dumb

        // animated position calculation
        animatedPos = charPos.Body.GlobalPosition;
        animatedPos += head.AnimationPosition;

        // lerp from base pos to animated pos based on the animation transition
        pos = Utilities.Lerp(basePos, animatedPos, character.AnimationTransitionFactorEased);
        // move up half body height to get to the body center (ignoring body rotation)
        pos.Y += charPos.Body.Scale.Y / 2;
        // move the relative offset (as is decided as a constant to make animating easier) multiplied by the body scale to get to the neck
        pos += charPos.Body.Scale * new Vector2(CharacterConstants.HeadOffsetRelativeToBody.X * charPos.FlipScaling, CharacterConstants.HeadOffsetRelativeToBody.Y);
        // not all characters are exactly the same height
        if (!Scene.HasComponent<PlayerComponent>(character.Entity)) // except the player lmao
            pos.Y += charPos.RandomHeightOffset * charPos.Scale;
        // rotate the position to account for body rotation
        pos = Utilities.RotatePoint(pos, charPos.Body.GlobalRotation, charPos.Body.GlobalPosition);
        // apply impact offset )
        pos += impactOffset.TranslationOffset / charPos.Scale;

        if (character.Look.Jitter)
            pos += MadnessUtils.Noise2D(Time * 2, (character.Entity.Identity % 3000)) * 12;

        //  pos.X += Easings.Cubic.InOut(charPos.IronSightProgress) * 20 * charPos.FlipScaling;

        headTransform.Position = head.GlobalPosition = pos;

        // final rotation
        float rot, animatedRot;
        animatedRot = charPos.Body.AnimationAngle;
        animatedRot += head.AnimationAngle;
        rot = character.AnimationConstrainsAny(AnimationConstraint.FaceForwards) ? 0 : Utilities.VectorToAngle(head.Direction) * charPos.FlipScaling;

        // instead of lerping, we add the animation angle because (for the head only) it is an offset
        rot += Utilities.DeltaAngle(rot, rot + animatedRot) * character.AnimationTransitionFactorEased;

        if (character.Look.Jitter)
        {
            rot += Noise.GetValue(Time * 2, character.Entity % 3000, 0.3123f) * 4;
            if ((int)(Time.SecondsSinceLoad * 100) % 400 == 3)
                rot += Utilities.RandomFloat(-180, 180);
        }

        rot += impactOffset.RotationOffset / charPos.Scale;
        headTransform.Rotation = head.GlobalRotation = rot;
    }

    private void PositionBody(CharacterComponent character)
    {
        var charPos = character.Positioning;
        var body = charPos.Body;
        var impactOffset = Scene.GetComponentFrom<ImpactOffsetComponent>(body.Entity);

        // final position
        var bodyTransform = Scene.GetComponentFrom<TransformComponent>(body.Entity);
        bodyTransform.LocalPivot = new Vector2(
            CharacterConstants.BodyRotationPivot.X * charPos.FlipScaling,
            CharacterConstants.BodyRotationPivot.Y);

        var pos = charPos.GlobalCenter;
        pos += body.InitialOffset;
        pos += body.AnimationPosition;
        pos += impactOffset.TranslationOffset / charPos.Scale;

        if (character.Look.Jitter)
            pos += MadnessUtils.Noise2D(Time * 2, (character.Entity.Identity % 3000)) * 8;

        bodyTransform.Position = body.GlobalPosition = pos;

        // final rotation
        var rot = body.AnimationAngle;
        if (character.IsPlayingAnimationGroup("deaths")) // ensuring the death animation offset is equal to whatever Koof had set in his blender file, this is a fucky patch but it is what it is
            rot += charPos.FlipScaling * 7.5f; // 7.5 picked out of thin air
        else
        {
            rot += charPos.FlipScaling * (8 + charPos.Head.Direction.Y * 15);
            rot += impactOffset.RotationOffset / charPos.Scale;
            rot += charPos.SmoothedMeleeBlockImpactIntensity;
        }

        if (character.Look.Jitter)
            rot += Noise.GetValue(Time * 2, character.Entity % 3000, 0.3123f) * 3;

        // tilt the body depending on walking speed and direction
        var walkTiltIntensity = Noise.GetValue(0, Time.SecondsSinceLoad * 0.2f, 0) * 0.5f + 0.5f;
        if (charPos.IsFlying)
            walkTiltIntensity *= 3;
        var walkTiltTarget = character.WalkAcceleration.X * walkTiltIntensity * -0.01f;

        // tilt the body based on recoil
        var recoilTilt = character.Positioning.SmoothRecoil * character.Positioning.SmoothRecoil * 3 * charPos.FlipScaling;
        if (character.IsIronSighting)
            recoilTilt *= 2;

        float flyingTilt = charPos.IsFlying ? (charPos.FlyingVelocity * -0.012f) : 0;

        charPos.TiltIntensity = Utilities.SmoothApproach(charPos.TiltIntensity, walkTiltTarget + recoilTilt + flyingTilt, 15, Time.DeltaTime);

        rot += charPos.TiltIntensity * (1 - character.AnimationTransitionFactorEased);

        bodyTransform.Rotation = body.GlobalRotation = rot;
        body.ComputedVisualCenter = Utilities.RotatePoint(body.GlobalPosition + new Vector2(0, body.Scale.Y / 2), body.GlobalRotation, body.GlobalPosition);
    }

    private void PositionHands(CharacterComponent character, WeaponComponent? equipped)
    {
        var charPos = character.Positioning;

        // calculate recoil before anything else
        {
            float recoilRotation = charPos.CurrentRecoil * 25 * charPos.FlipScaling;
            if (equipped != null)
                recoilRotation *= equipped.Data.RotationalRecoilIntensity;

            var recoilOffset = charPos.SmoothRecoil * Utilities.RotatePoint(new Vector2(-100, charPos.FlipScaling * 40), Utilities.VectorToAngle(character.AimDirection));

            charPos.RecoilPositionOffset = recoilOffset;
            charPos.RecoilAngleOffset = recoilRotation;

            charPos.SmoothedMeleeBlockImpactIntensity = Utilities.SmoothApproach(charPos.SmoothedMeleeBlockImpactIntensity, charPos.MeleeBlockImpactIntensity, 25, Time.DeltaTime);
            charPos.MeleeBlockImpactIntensity = Utilities.SmoothApproach(charPos.MeleeBlockImpactIntensity, 0, 8, Time.DeltaTime);
        }

        var ironSightOffset = CharacterConstants.IronsightOffset * Easings.Quad.InOut(charPos.IronSightProgress) * charPos.Scale;
        ironSightOffset.X *= charPos.FlipScaling;

        CharacterUtilities.PositionHandsForWeapon(Scene, character, equipped);

        int index = 0;
        foreach (var hand in charPos.Hands)
        {
            // skip the other hand when it is 'Fixed'
            if (charPos.SecondaryHandFollowsPrimary && hand != charPos.Hands.First)
                continue; // its okay not to increment index here

            var isAnimated = character.IsHandAnimated(hand);

            Vector2 pos, animatedPos;
            float rot, animatedRot;

            rot = hand.PoseRotation;

            // base position
            pos = charPos.GlobalCenter + hand.PosePosition;
            pos.X += charPos.TiltIntensity * -5;
            animatedPos = hand.AnimationPosition;

            // base rotation
            if (character.IsPlayingAnimation)
            {
                // TODO what the fuck
                if ((character.IsPlayingAnimationGroup("dodge") || character.IsPlayingAnimationGroup("pickup")) && !character.IsPlayingAnimationGroup("melee"))
                    animatedRot = rot;
                else
                    animatedRot = hand.AnimationAngle + (charPos.IsFlipped ? 180 : 0);
            }
            else
                animatedRot = hand.AnimationAngle;

            if (!isAnimated) // fallback to PreviousAnimatedPosition if not animated
            {
                animatedRot = hand.PreviousAnimatedAngle;
                animatedPos = hand.PreviousAnimatedPosition;
            }
            else
            {
                hand.PreviousAnimatedAngle = animatedRot;
                hand.PreviousAnimatedPosition = animatedPos;
            }

            // apply recoil
            if (character.HasWeaponEquipped && hand.ShouldFollowRecoil)
            {
                rot += charPos.RecoilAngleOffset;
                // TODO this may no longer be necessary? since SecondaryHandFollowsPrimary exists now, you can probably move the hand wherever you like
                // there is no need for a common transform origin point
                pos = Utilities.RotatePoint(
                    pos + charPos.RecoilPositionOffset,
                    charPos.RecoilAngleOffset * 0.5f,
                    charPos.Body.ComputedVisualCenter + charPos.HandMousePosition * 0.5f);
            }

            pos += ironSightOffset;
            if (character.Stats.DodgeAbility > 0.4f && character.DodgeMeter < 0.2f)
            {
                pos.X += Noise.GetValue(Time.SecondsSinceLoad, index, 0) * 8;
                pos.Y += Noise.GetValue(index * 23, 0, Time.SecondsSinceLoad) * 8;
            }

            pos = Utilities.Lerp(pos, animatedPos, character.AnimationTransitionFactorEased);
            rot = Utilities.LerpAngle(rot, animatedRot, character.AnimationTransitionFactorEased);

            if (character.Look.Jitter)
                pos += MadnessUtils.Noise2D(Time * 2, (character.Entity.Identity % 1000) * index) * 12;

            hand.GlobalPosition = pos;
            hand.GlobalRotation = rot;

            index++;
        }

        if (charPos.SecondaryHandFollowsPrimary)
        {
            var mainHand = charPos.Hands.First;
            var secondHand = charPos.Hands.Second;

            // we only need to keep the relative position constant, so we take the poseposition and rotation as a value relative from the mainhand instead of the character itself

            secondHand.GlobalPosition = mainHand.GlobalPosition + Utilities.RotatePoint(secondHand.PosePosition, mainHand.GlobalRotation);
            secondHand.GlobalRotation = mainHand.GlobalRotation + secondHand.PoseRotation;
        }

        foreach (var hand in charPos.Hands)
        {
            // after all of these calculations, we apply the final position and rotation to the transform

            var handTransform = Scene.GetComponentFrom<TransformComponent>(hand.Entity);
            handTransform.Position = hand.GlobalPosition;
            handTransform.Rotation = hand.GlobalRotation;
        }
    }

    private void PositionFeet(CharacterComponent character)
    {
        var charPos = character.Positioning;

        foreach (var foot in charPos.Feet)
        {
            var footTransform = Scene.GetComponentFrom<TransformComponent>(foot.Entity);

            var pos = charPos.GlobalCenter;

            var transformedOffset = foot.InitialOffset;
            transformedOffset.X *= charPos.FlipScaling;

            pos += transformedOffset;

            if (!character.AnimationConstrainsAny(AnimationConstraint.PreventWalking) && character.IsAlive && !charPos.IsFlying)
                pos += foot.Offset * charPos.Scale;

            pos += charPos.ShouldFeetFollowBody ? charPos.Body.AnimationPosition : charPos.Body.AnimationPosition with { Y = 0 };
            pos = Vector2.Transform(pos, Matrix3x2.CreateRotation(charPos.Body.GlobalRotation * Utilities.DegToRad, charPos.Body.GlobalPosition));

            // magic numbers once decided by Koof for no particular reason
            pos.X -= 5 * charPos.FlipScaling;
            pos.Y -= 7;

            var flyingOffset = 0f;
            if (charPos.IsFlying)
                flyingOffset = -15f * charPos.FlipScaling;

            footTransform.Position = foot.GlobalPosition = pos;
            footTransform.Rotation = charPos.Body.AnimationAngle + (charPos.IsFlipped ? 8 : -8) + flyingOffset;
        }
    }
}
