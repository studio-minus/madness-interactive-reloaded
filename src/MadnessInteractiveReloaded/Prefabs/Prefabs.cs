using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.ParticleSystem;
using Walgelijk.ParticleSystem.Modules;
using Walgelijk.ParticleSystem.ParticleInitialisers;
using Walgelijk.Physics;

namespace MIR;


/// <summary>
/// The static prefab class where prefabs are created programmatically.
/// <br></br>
/// Use for commonly spawned things like weapons, enemies, blood, bullet holes, etc.
/// </summary>
public static class Prefabs
{
    private static QueryResult[] buffer = new QueryResult[8];

    /// <summary>
    /// Spawn a piece of text in the world.
    /// Used for things like the "Out of ammo" indicator text.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="worldPos"></param>
    /// <param name="text"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static Entity CreateNotification(Scene scene, Vector2 worldPos, string text, float scale = 12)
    {
        //foreach (var e in scene.GetEntitiesWithTag(Tags.NotificationText))
        //    scene.RemoveEntity(e);

        const float duration = 2f; //seconds
        var ent = scene.CreateEntity();
        scene.SetTag(ent, Tags.NotificationText);

        scene.AttachComponent(ent, new DespawnComponent(duration));
        var transform = scene.AttachComponent(ent,
            new TransformComponent { Position = worldPos, Scale = new Vector2(scale) });
        var textShape = scene.AttachComponent(ent, new TextComponent(text, Fonts.Oxanium)
        {
            Color = Colors.Red.WithAlpha(0.7f),
            RenderOrder = RenderOrders.UserInterface.WithOrder(-10)
        }
        );

        textShape.HorizontalAlignment = HorizontalTextAlign.Center;
        scene.AttachComponent(ent, new AnimationComponent()
        {
            Duration = duration,
            IsPlaying = true,
            InterpolationMode = AnimationInterpolationMode.EaseOut,
            Translational = new Vec2Curve(
                new Curve<Vector2>.Key(worldPos + new Vector2(0, 0), 0),
                new Curve<Vector2>.Key(worldPos + new Vector2(0, 70), 1)
            ),
            Scaling = new Vec2Curve(
                new Curve<Vector2>.Key(new Vector2(scale, scale), 0.8f),
                new Curve<Vector2>.Key(new Vector2(scale, 0), 1)
            )
        });

        return ent;
    }

    /// <summary>
    /// Specific to train level.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="pos"></param>
    /// <param name="normal"></param>
    /// <returns></returns>
    public static Entity CreateTrainEngineBulletHole(Scene scene, Vector2 pos, Vector2 normal)
    {
        var ent = scene.CreateEntity();
        scene.SetTag(ent, Tags.TrainEngineBulletHole);
        scene.AttachComponent(ent, new TrainEngineBulletHoleComponent()
        {
            Position = pos + new Vector2(Utilities.RandomFloat(-5, 24), 0),
            Normal = normal
        });

        scene.Game.AudioRenderer.PlayOnce(Utilities.PickRandom(Sounds.MetalBulletImpact),
            Utilities.RandomFloat(0.8f, 2), Utilities.RandomFloat(0.85f, 1.1f));

        return ent;
    }

    /// <summary>
    /// Spawn a bullet hole decal in the world.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="angleDegrees"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static Entity CreateBulletHole(Scene scene, Vector2 position, float angleDegrees, float scale = 32)
    {
        var p = position + Utilities.RandomPointInCircle(0, 50); //TODO eigenlijk moet hij kiezen in een punt in de zone of iets :(

        var isInFront = false;

        if (scene.FindAnyComponent<BackgroundBufferStorageComponent>(out var bufferComponent))
        {
            // TODO this should be an engine feature
            if (scene.Game.Window.Graphics.TryGetId(bufferComponent.Buffer, out var framebuffer, out _))
            {
                var pos = scene.Game.Window.WorldToWindowPoint(p) / scene.Game.Window.Size;
                pos.Y = 1 - pos.Y;
                pos *= bufferComponent.Buffer.Size;
                if (pos.X >= 0 && pos.X < bufferComponent.Buffer.Width && pos.Y >= 0 && pos.Y < bufferComponent.Buffer.Height)
                {
                    byte[] data = new byte[4];
                    var old = GL.GetInteger(GetPName.FramebufferBinding);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                    GL.ReadPixels((int)pos.X, (int)pos.Y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    isInFront = data[2] >= 250; // get the blue component of the pixel at the impact point. if it is fully blue it means we hit a foreground area
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, old);
                    /* This method works only if the impact area is actually on screen. If it isn't on screen then it will be considered a background bullet hole. 
                     * This isn't a huge issue but it is slightly unforunate and I did no profiling at the time of writing so this ReadPixels thing might actually be really taxing.
                     * An alternative is to build some kind of spatially indexed structure for every level based on its background textures. */
                }
            }
        }

        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new DecalComponent()
        {
            DecalType = DecalType.BulletHole,
            Position = p,
            Color = Colors.Black,
            RotationDegrees = angleDegrees,
            Scale = new Vector2(scale * 0.5f, scale),
            FlipbookIndex = Utilities.RandomInt(0, DecalComponent.GetTextureForType(DecalType.BulletHole).columns),
            RenderOrder = isInFront ? RenderOrders.ForegroundDecals : RenderOrders.BackgroundDecals
        });

        scene.GetSystem<DecalSystem>().IsDirty = true;

        MadnessUtils.EmitBulletImpactParticles(scene, position, Utilities.AngleToVector(angleDegrees));

        return ent;
    }

    /// <summary>
    /// Spawn a blood splat decal in the world.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="angleDegrees"></param>
    /// <param name="color"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public static Entity CreateBloodSplat(Scene scene, Vector2 position, float angleDegrees, Color color,
        float scale = 256)
    {
        var p = position +
                Utilities.RandomPointInCircle(0, 50); //TODO eigenlijk moet hij kiezen in een punt in de zone of iets :(
        //TODO dubbele code
        var isInsideWall = (Level.CurrentLevel?.GetFloorLevelAt(p.X) > p.Y) ||
                           scene.GetSystem<PhysicsSystem>().QueryPoint(p, buffer, CollisionLayers.BlockBullets) > 0;

        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new DecalComponent()
        {
            DecalType = DecalType.Blood,
            Position = p,
            Color = color,
            RotationDegrees = angleDegrees,
            Scale = new Vector2(scale),
            FlipbookIndex = Utilities.RandomInt(0, DecalComponent.GetTextureForType(DecalType.Blood).columns),
            RenderOrder = isInsideWall ? RenderOrders.ForegroundDecals : RenderOrders.BackgroundDecals
        });

        scene.GetSystem<DecalSystem>().IsDirty = true;

        //MadnessUtils.EmitBulletImpactParticles(scene, position, Utilities.AngleToVector(angleDegrees));

        return ent;
    }

    /// <summary>
    /// Spawn a simple entity with a sprite component.
    /// </summary>
    public static Entity CreateSprite(Scene scene, Texture texture, Vector2 position, RenderOrder renderOrder = default,
        ComponentRef<TransformComponent>? parent = null, float xScaleMultiplier = 1, float yScaleMultiplier = 1)
    {
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Position = position,
            Scale = new Vector2(texture.Width * xScaleMultiplier, texture.Height * yScaleMultiplier),
            Parent = parent,
            //InterpolationFlags = InterpolationFlags.Position | InterpolationFlags.Rotation
        });
        scene.AttachComponent(ent, new QuadShapeComponent(true)
        {
            Material = SpriteMaterialCreator.Instance.Load(texture),
            RenderOrder = renderOrder
        });

        return ent;
    }

    /// <summary>
    /// Spawn a sprite entity for a piece of destructible clothes.
    /// </summary>
    public static Entity CreateApparelSprite(Scene scene, ApparelMaterialParams materialParams, Vector2 position, RenderOrder renderOrder = default,
        ComponentRef<TransformComponent>? parent = null)
    {
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Position = position,
            Scale = new Vector2(materialParams.Texture.Width * materialParams.Scale, materialParams.Texture.Height * materialParams.Scale),
            Parent = parent,
        });

        scene.AttachComponent(ent, new ApparelSpriteComponent(true)
        {
            Material = ApparelMaterialPool.Instance.RequestObject(materialParams) ?? Material.DefaultTextured,
            RenderOrder = renderOrder
        });

        return ent;
    }

    /// <summary>
    /// Helper function for creating a character body.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Entity CreateBodypartSprite(Scene scene, BodyPartMaterialParams materialParams,
        RenderOrder renderOrder = default, ComponentRef<TransformComponent>? parent = null)
    {
        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent
        {
            Scale = new Vector2(materialParams.SkinTexture.Width, materialParams.SkinTexture.Height) * materialParams.Scale,
            Parent = parent,
            //InterpolationFlags = InterpolationFlags.Position | InterpolationFlags.Rotation
        });

        var c = scene.AttachComponent(ent, new BodyPartShapeComponent(true)
        {
            Material = BodyPartMaterialPool.Instance.RequestObject(materialParams) ?? throw new Exception("Too many body parts!"),
            RenderOrder = renderOrder,
            BloodColour = materialParams.BloodColour
        });

        c.Material.SetUniform("tint", Colors.White);

        return ent;
    }
    /// <summary>
    /// This creates an entity in the scene 
    /// and attaches a <see cref="Walgelijk.ParticleSystem.ParticlesComponent"/> onto it.
    /// <br></br>
    /// Then, when the <see cref="WeaponSystem"/> wants to create a casing particle (see: <see cref="WeaponSystem.EjectCasingParticle(WeaponComponent, TransformComponent)"/>
    /// it finds the one associated with the ammo type.
    /// <br></br>
    /// (See <see cref="SceneUtils.PrepareGameScene(Game, GameMode, bool, Level?)"/> for
    /// where the particle systems for each casing type are created.)
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="ejectedMaterial"></param>
    /// <param name="columns"></param>
    /// <param name="floorHitSounds"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ParticlesComponent CreateCasingEjectionParticleSystem(Scene scene, Material ejectedMaterial,
        int columns, Sound[] floorHitSounds, EjectionParticle type)
    {
        const float cellSize = 64;

        var ent = scene.CreateEntity();
        scene.AttachComponent(ent, new TransformComponent());
        var particles = scene.AttachComponent(ent, new ParticlesComponent
        {
            WorldSpace = true,
            Material = ejectedMaterial,
            Depth = RenderOrders.BulletEjections,
        });

        if (scene.FindAnyComponent<CasingParticleDictComponent>(out var dict))
            dict.EntityByParticle.Add(type, ent);
        else
            Logger.Warn("Ejection particle system created but a " + nameof(CasingParticleDictComponent) +
                        " could not be found!");

        //particles.Initalisers.Add(new PointShapeInitialiser(new Vector2(0, 500)));
        particles.Initalisers.Add(new RandomFrameSheet(columns, 1, new Vector2(cellSize * columns, cellSize)));
        particles.Initalisers.Add(new RandomLifespan(new(5)));
        particles.Initalisers.Add(new RandomStartSize(new(50)));
        particles.Initalisers.Add(new RandomStartRotVel(new(-1500, 1500)));
        particles.Initalisers.Add(new RandomStartVelocity(new(new Vector2(-500, 2500), new Vector2(500, 6500))));

        var worldCollision = new WorldCollision(0.4f, 0.95f, scene) { CollisionMask = CollisionLayers.BlockPhysics };
        worldCollision.OnCollide.AddListener(collisionHandler);
        particles.Modules.Add(worldCollision);
        particles.Modules.Add(new GravityModule(new Vec2Range(new Vector2(0, -9000))));

        //particles.Emitters.Add(new ContinuousEmitter(50));

        //particles.RenderTask.Material = ejectedMaterial;
        //particles.OnHitFloor.AddListener(collisionHandler);

        void collisionHandler(Particle p)
        {
            if (p.Velocity.LengthSquared() > 7000000) // epic trail-and-error picked number
                Game.Main.AudioRenderer.PlayOnce(Utilities.PickRandom(floorHitSounds));
        }

        return particles;
    }

    /// <summary>
    /// Create fire.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="vuurIndex"></param>
    /// <returns></returns>
    public static Entity CreateFire(Scene scene, Vector2 position, int vuurIndex)
    {
        var flipbook = Textures.Vuurtjes[vuurIndex];
        var mat = FlipbookMaterialCreator.LoadMaterialFor(flipbook.texture.Value, flipbook.columns, flipbook.rows, 0, Colors.White, true, vuurIndex);
        var ent = scene.CreateEntity();

        scene.AttachComponent(ent, new TransformComponent
        {
            Position = position,
            Rotation = 0,
            Scale = new Vector2(flipbook.texture.Value.Width / (float)flipbook.columns,
                flipbook.texture.Value.Height / (float)flipbook.rows) * 2 //het is downscaled vandaar die 2
        });

        scene.AttachComponent(ent, new QuadShapeComponent(true)
        {
            Material = mat,
            RenderOrder = RenderOrders.BackgroundBehind.WithOrder(2)
        });

        scene.AttachComponent(ent, new FlipbookComponent(mat)
        {
            DeleteWhenDone = false,
            Loop = true,
            Duration = 1
        });

        return ent;
    }

    /// <summary>
    /// The train explosion...
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Entity CreateTrainExplosion(Scene scene, Vector2 position)
    {
        MadnessUtils.Shake(100);
        Game.Main.AudioRenderer.Play(Sounds.TrainExplosion, 1);

        var ent = scene.CreateEntity();
        var tex = Textures.TrainLevelExplosion.Value;


        scene.AttachComponent(ent, new TransformComponent
        {
            Position = position,
            Rotation = 0,
            Scale = new Vector2(tex.Width / 15f, tex.Height) * 4 * MadnessConstants.BackgroundSizeRatio //het is downscaled :) vandaar die x4
        });

        var mat = FlipbookMaterialCreator.LoadMaterialFor(tex, 15, 1, 0, Colors.White, true, 0);

        scene.AttachComponent(ent, new QuadShapeComponent(true)
        {
            Material = mat,
            RenderOrder = RenderOrders.Effects
        });

        scene.AttachComponent(ent, new FlipbookComponent(mat)
        {
            DeleteWhenDone = true,
            Duration = 0.625f //24 fps (15 / 24)
        });

        return ent;
    }

    /// <summary>
    /// Spawn a muzzleflash for a gun.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="size"></param>
    public static void CreateMuzzleFlash(Scene scene, Vector2 position, float rotation, float size = 1)
    {
        if (scene.GetSystem<PrefabPoolSystem>().TryRequest(PoolablePrefabs.MuzzleFlash, out var entity))
        {
            var quadshape = scene.GetComponentFrom<QuadShapeComponent>(entity);
            var tex = Utilities.PickRandom(Textures.Muzzleflashes);
            var mat = FlipbookMaterialCreator.LoadMaterialFor(tex.Value, 5, 1, 0, Colors.White, true, entity);
            // TODO: wtf this already happen at poolableprefabs yo vraag van vandaag? Which one is the real one?
            var transform = scene.GetComponentFrom<TransformComponent>(entity);
            transform.Position = position;
            transform.Rotation = rotation;
            transform.Scale = new Vector2(256 * size);
            transform.RecalculateModelMatrix(Matrix3x2.Identity);
            var flipbook = scene.GetComponentFrom<FlipbookComponent>(entity);
            flipbook.Material = mat;
            flipbook.CurrentTime = .2f;
            quadshape.Material = mat;
            quadshape.Visible = true;
        }
    }

    /// <summary>
    /// Spawn the cool effect of a sword deflecting a bullet.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="size"></param>
    public static void CreateDeflectionSpark(Scene scene, Vector2 position, float rotation, float size = 1)
    {
        if (scene.GetSystem<PrefabPoolSystem>().TryRequest(PoolablePrefabs.DeflectionSpark, out var entity))
        {
            var transform = scene.GetComponentFrom<TransformComponent>(entity);
            transform.Position = position;
            transform.Rotation = rotation - 90;
            transform.Scale = new Vector2(256 * size);
            transform.RecalculateModelMatrix(Matrix3x2.Identity);
            var flipbook = scene.GetComponentFrom<FlipbookComponent>(entity);
            flipbook.CurrentTime = 0;
            scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = true;
        }
    }

    /// <summary>
    /// Spawn gross blood spurts from injuries.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="color"></param>
    /// <param name="size"></param>
    public static void CreateBloodSpurt(Scene scene, Vector2 position, float rotation, Color color, float size = 1)
    {
        if (scene.GetSystem<PrefabPoolSystem>().TryRequest(PoolablePrefabs.BloodSpurt, out var entity))
        {
            var bloodSpurtComponent = scene.GetComponentFrom<BloodSpurtComponent>(entity);
            bloodSpurtComponent.Material.SetUniform("tint", color);

            var anim = bloodSpurtComponent.FrameSheet;

            if (!anim.ShouldPointTowardsBulletDirection)
                rotation = 0;

            var transform = scene.GetComponentFrom<TransformComponent>(entity);
            transform.Position = position;
            transform.Rotation = rotation;
            transform.Scale = new Vector2(256 * size) * anim.Size * Utilities.RandomFloat(0.9f, 1.1f);
            transform.RecalculateModelMatrix(Matrix3x2.Identity);

            scene.GetComponentFrom<FlipbookComponent>(entity).CurrentTime = 0;
            scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = true;
        }
    }

    /// <summary>
    /// Spawn a weapon from <see cref="WeaponInstructions"/>.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="instructions"></param>
    /// <returns></returns>
    public static WeaponComponent CreateWeapon(Scene scene, Vector2 position, WeaponInstructions instructions)
    {
        var @base = scene.CreateEntity();
        var transform = scene.AttachComponent(@base, new TransformComponent
        {
            Position = position,
            Scale = new Vector2(1)
        });
        var baseTransformRef = new ComponentRef<TransformComponent>(@base);

        var baseSpriteEntity = CreateSprite(scene, instructions.BaseTexture, default, parent: baseTransformRef,
            renderOrder: RenderOrders.Default);

        Entity[]? animatedParts = null;
        if (instructions.AnimatedParts?.Count > 0)
        {
            animatedParts = new Entity[instructions.AnimatedParts.Count];
            for (int i = 0; i < instructions.AnimatedParts.Count; i++)
            {
                AnimatedWeaponPart animatedPart = instructions.AnimatedParts[i];
                var startPos = animatedPart.TranslationCurve?.Evaluate(0) ?? default;
                var ent = CreateSprite(scene, animatedPart.Texture, startPos, parent: baseTransformRef,
                    xScaleMultiplier: animatedPart.Scale.X, yScaleMultiplier: animatedPart.Scale.Y);
                scene.AttachComponent(ent, new WeaponPartAnimationComponent
                {
                    IsPlaying = false,
                    Loops = false,
                    Duration = animatedPart.Duration,
                    InvisbleWhenOutOfAmmo = animatedPart.InvisbleWhenOutOfAmmo,
                    OutOfAmmoKeyframeTime = animatedPart.OutOfAmmoKeyframeTime,
                    Translational = animatedPart.TranslationCurve,
                    Visibility = animatedPart.VisibilityCurve
                });
                animatedParts[i] = ent;
            }
        }

        var alsoDespawn = new Entity[1 + (animatedParts?.Length ?? 0)];
        alsoDespawn[0] = baseSpriteEntity;
        animatedParts?.CopyTo(alsoDespawn, 1);

        scene.AttachComponent(@base, new DespawnComponent(200) { AlsoDelete = alsoDespawn });
        scene.AttachComponent(@base, new MeasuredVelocityComponent());
        var wpn = scene.AttachComponent(@base,
            new WeaponComponent(instructions.WeaponData, baseSpriteEntity, [.. instructions.HoldPoints],
                instructions.BarrelEndPoint, instructions.CasingEjectionPoint)
            {
                RegistryKey = instructions.Id,
                AnimatedParts = animatedParts ?? global::System.Array.Empty<Entity>(),
                HoldForGrip = instructions.HoldForGrip,
                HoldStockHandPose = instructions.HoldStockHandPose,
                RemainingRounds = instructions.WeaponData.RoundsPerMagazine,
                Texture = instructions.BaseTexture
            });
        scene.AttachComponent(@base, new PhysicsBodyComponent
        {
            BodyType = BodyType.Dynamic,
            Collider = new CircleCollider(transform, 50),
            FilterBits = CollisionLayers.None
        });
        scene.AttachComponent(@base, new VelocityComponent(transform)
        {
            FloorAngleOffset = instructions.OnFloorAngle,
            CollideSounds = instructions.WeaponData.WeaponType is WeaponType.Firearm ? Sounds.FirearmCollision : Sounds.SwordCollision
        });

        if (instructions.WeaponData.AdditionalComponents != null)
            foreach (var type in instructions.WeaponData.AdditionalComponents)
            {
                var t = Type.GetType(type);
                if (t == null)
                    Logger.Error($"{nameof(WeaponData)} specified additional component type \"{type}\" but the type is invalid");
                else
                {
                    var instance = Activator.CreateInstance(t);
                    if (instance is Component c)
                    {
                        dynamic riskyConverstion = Convert.ChangeType(instance, t);
                        scene.AttachComponent(@base, riskyConverstion);
                    }
                    else
                        Logger.Error($"{nameof(WeaponData)} specified additional component type \"{type}\" but the type is not a component");
                }
            }

        if (instructions.WeaponData.EnsureSystems != null)
            foreach (var type in instructions.WeaponData.EnsureSystems)
            {
                var t = Type.GetType(type);
                if (t == null)
                    Logger.Error($"{nameof(WeaponData)} specified that it should ensure system of type \"{type}\" but the type is invalid");
                else if (!scene.TryGetSystem(t, out _))
                {
                    var instance = Activator.CreateInstance(t);
                    if (instance is Walgelijk.System s)
                    {
                        dynamic riskyConverstion = Convert.ChangeType(instance, t);
                        scene.AddSystem(riskyConverstion);
                    }
                    else
                        Logger.Error($"{nameof(WeaponData)} specified that it should ensure system of type \"{type}\" but the type is not a system");
                }
            }

        scene.SyncBuffers();
        return wpn;
    }

    /// <summary>
    /// Spawn a character.
    /// </summary>
    public static CharacterComponent CreateCharacter(Scene scene, CharacterPrefabParams @params)
    {
        float scale = @params.ScaleOverride ?? @params.Stats.Scale;
        var attackIgnoreCollision = new HashSet<Entity>();

        var renderOrder = new RenderOrder(Utilities.RandomInt(RenderOrders.CharacterLower.Layer, RenderOrders.CharacterUpper.Layer) + 1);

        var characterEntity = scene.CreateEntity();
        scene.SetTag(characterEntity, @params.Tag);

        var bodyFlesh = Textures.Character.FleshBody;
        if (@params.BodyFleshTexture.TryGetValue(out var bf))
            bodyFlesh = bf;
        else if (@params.Look.BodyFlesh.TryGetValue(out bf))
            bodyFlesh = bf;

        var headFlesh = Textures.Character.FleshHead;
        if (@params.HeadFleshTexture.TryGetValue(out var hf))
            headFlesh = hf;
        else if (@params.Look.HeadFlesh.TryGetValue(out hf))
            headFlesh = hf;

        var body = CreateBodypartSprite(scene, new BodyPartMaterialParams
        {
            SkinTexture = @params.Look.Body.Right,
            GoreTexture = @params.Look.BodyGore ?? Textures.Character.GoreBody,
            FleshTexture = bodyFlesh,
            BloodColour = @params.Look.BloodColour,
            Scale = scale
        }, renderOrder);

        var head = CreateBodypartSprite(scene, new BodyPartMaterialParams
        {
            SkinTexture = @params.Look.Head.Right,
            GoreTexture = @params.Look.HeadGore ?? Textures.Character.GoreHead,
            FleshTexture = headFlesh,
            BloodColour = @params.Look.BloodColour,
            Scale = scale
        }, renderOrder.WithOrder(1));

        var headTransform = scene.GetComponentFrom<TransformComponent>(head);
        var bodyTransform = scene.GetComponentFrom<TransformComponent>(body);
        scene.SetTag(body, @params.Tag);
        scene.SetTag(head, @params.Tag);
        attackIgnoreCollision.Add(body);
        attackIgnoreCollision.Add(head);

        headTransform.LocalPivot = CharacterConstants.HeadRotationPivot * scale;
        bodyTransform.LocalPivot = CharacterConstants.BodyRotationPivot * scale;

        bodyTransform.Position = @params.Bottom;
        bodyTransform.RecalculateModelMatrix(Matrix3x2.Identity);
        headTransform.Position =
            Vector2.Transform(CharacterConstants.HeadOffsetRelativeToBody * scale, bodyTransform.LocalToWorldMatrix);
        headTransform.RecalculateModelMatrix(Matrix3x2.Identity);
        var bodyCenter = @params.Bottom + new Vector2(0, CharacterConstants.HalfHeight * scale);

        scene.AttachComponent(body, new PhysicsBodyComponent
        {
            Collider = new RectangleCollider(bodyTransform, new Vector2(1)),
            FilterBits = @params.Faction.CollisionLayer
        });
        scene.AttachComponent(body, new MeasuredVelocityComponent());
        scene.AttachComponent(body, new ImpactOffsetComponent());
        scene.AttachComponent(body,
            new BodyPartComponent
            {
                Character = new(characterEntity),
                MaxHealth = @params.Stats.BodyHealth,
                Health = @params.Stats.BodyHealth
            });

        scene.AttachComponent(head, new PhysicsBodyComponent
        {
            Collider = new RectangleCollider(headTransform, new Vector2(0.8f)),
            FilterBits = @params.Faction.CollisionLayer
        });
        scene.AttachComponent(head, new MeasuredVelocityComponent());
        scene.AttachComponent(head, new ImpactOffsetComponent());
        scene.AttachComponent(head,
            new BodyPartComponent
            {
                Character = new(characterEntity),
                MaxHealth = @params.Stats.HeadHealth,
                Health = @params.Stats.HeadHealth
            });

        var hand1pos = bodyCenter + CharacterConstants.HandOffset1 * scale;
        var hand2pos = bodyCenter + CharacterConstants.HandOffset2 * scale;
        var foot1pos = @params.Bottom + new Vector2(40, 10) * scale;
        var foot2pos = @params.Bottom + new Vector2(-30, -6) * scale;

        var footTexture = @params.Look.Feet ?? Textures.Character.DefaultFoot;

        var charPos = new CharacterPositioning(
            scale,
            bodyCenter,
            head: new HeadAnimatedLimb { Entity = head, GlobalPosition = headTransform.Position },
            body: new BodyAnimatedLimb { Entity = body, GlobalPosition = bodyTransform.Position },
            hands: new(
                new HandAnimatedLimb
                {
                    Entity = CreateSprite(scene, Textures.Character.DefaultFist, hand1pos, renderOrder.OffsetOrder(1), xScaleMultiplier: scale, yScaleMultiplier: scale),
                    IsLeftHand = false,
                    GlobalPosition = hand1pos
                },
                new HandAnimatedLimb
                {
                    Entity = CreateSprite(scene, Textures.Character.DefaultFistBack, hand2pos, renderOrder.OffsetOrder(-1), xScaleMultiplier: scale, yScaleMultiplier: scale),
                    IsLeftHand = true,
                    GlobalPosition = hand2pos
                }
            ),
            feet: new(
                new FootLimb
                {
                    Entity = CreateSprite(scene, footTexture, foot1pos, renderOrder.OffsetOrder(-2), xScaleMultiplier: scale, yScaleMultiplier: scale),
                    GlobalPosition = foot1pos
                },
                new FootLimb
                {
                    Entity = CreateSprite(scene, footTexture, foot2pos, renderOrder.OffsetOrder(-1), xScaleMultiplier: scale, yScaleMultiplier: scale),
                    GlobalPosition = foot2pos
                }
            )
        );

        charPos.HopAnimationDuration = @params.Stats.WalkHopDuration;

        var character = scene.AttachComponent(characterEntity, new CharacterComponent(@params.Name, charPos)
        {
            BaseRenderOrder = renderOrder,
            Faction = @params.Faction,
            Look = @params.Look,
            Stats = @params.Stats,
            CollisionLayer = @params.Faction.CollisionLayer,
            EnemyCollisionLayer = @params.Faction.AttackHitLayerComposite,
            AttackIgnoreCollision = attackIgnoreCollision
        });

        createHeadDecoration(@params.Look.HeadLayer1, 0);
        createHeadDecoration(@params.Look.HeadLayer2, 1);
        createHeadDecoration(@params.Look.HeadLayer3, 2);

        createBodyDecoration(@params.Look.BodyLayer1, 0);
        createBodyDecoration(@params.Look.BodyLayer2, 1);

        foreach (var item in charPos.Hands)
        {
            scene.SetTag(item.Entity, @params.Tag);
            scene.AttachComponent(item.Entity, new MeasuredVelocityComponent());
        }

        foreach (var item in charPos.Feet)
            scene.SetTag(item.Entity, @params.Tag);

        scene.AttachComponent(characterEntity, new TransformComponent
        {
            Position = @params.Bottom
        });

        scene.AttachComponent(characterEntity, new LifetimeComponent());

        if (character.Stats.Abilities != null)
            foreach (var abilityType in character.Stats.Abilities)
            {
                if (character.TryGetNextAbilitySlot(scene, out var slot))
                {
                    var occupiesSlot = CharacterAbilityComponent.OccupiesSlot(abilityType);
                    var instance = occupiesSlot ? Activator.CreateInstance(abilityType, slot) : Activator.CreateInstance(abilityType);

                    if (instance is not CharacterAbilityComponent abilityComponent)
                        Logger.Error($"Ability \"{abilityType}\" could not be instantiated because it is not an ability component!");
                    else
                        scene.AttachComponent(characterEntity, abilityComponent);
                }
            }

        return character;

        void createHeadDecoration(ArmourPiece? armour, int index)
        {
            var matParams = new ApparelMaterialParams
            {
                Texture = armour == null ? Textures.Transparent : armour.Right.Value,
                Scale = scale,
                DamageScale = armour?.ProceduralDamageScale ?? 1
            };

            var decor = CreateApparelSprite(scene, matParams, @params.Bottom, renderOrder.OffsetOrder(2));
            scene.AttachComponent(decor, new TransformConstraintComponent
            {
                LockPosition = true,
                LockRotation = true,
                PositionOffset = (armour?.OffsetRight ?? default) / headTransform.Scale,
                Other = new ComponentRef<TransformComponent>(head)
            });

            scene.AttachComponent(decor, new ArmourComponent(armour));
            charPos.HeadDecorations[index] = decor;
        }

        void createBodyDecoration(ArmourPiece? armour, int index)
        {
            var matParams = new ApparelMaterialParams
            {
                Texture = armour == null ? Textures.Transparent : armour.Right.Value,
                Scale = scale,
                DamageScale = armour?.ProceduralDamageScale ?? 1
            };

            var decor = CreateApparelSprite(scene, matParams, @params.Bottom, renderOrder.OffsetOrder(0));
            scene.AttachComponent(decor, new TransformConstraintComponent
            {
                LockPosition = true,
                LockRotation = true,
                PositionOffset = default,
                Other = new ComponentRef<TransformComponent>(body)
            });
            scene.AttachComponent(decor, new ArmourComponent(armour));
            charPos.BodyDecorations[index] = decor;
        }
    }

    /// <summary>
    /// Spawn an enemy with a random weapon equipped.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="position"></param>
    /// <param name="stats"></param>
    /// <param name="look"></param>
    /// <param name="randomAmmo"></param>
    /// <returns></returns>
    public static CharacterComponent CreateEnemyWithRandomWeapon(Scene scene, Vector2 position, CharacterStats stats,
        CharacterLook look, Faction faction, bool randomAmmo = true)
    {
        if (Utilities.RandomFloat() > 0.2f)
            return CreateEnemyWithWeapon(scene, position, Registries.Weapons.GetRandomValue(), stats, look, faction, randomAmmo);
        return CreateEnemy(scene, position, stats, look, faction);
    }

    /// <summary>
    /// Spawn an enemy equipped with a weapon of your choosing.
    /// </summary>
    public static CharacterComponent CreateEnemyWithWeapon(Scene scene, Vector2 position, WeaponInstructions? weapon,
        CharacterStats stats, CharacterLook look, Faction faction, bool randomAmmo = true, float? scaleOverride = null)
    {
        var enemyCharacter = CreateEnemy(scene, position, stats, look, faction, scaleOverride);
        if (weapon != null)
        {
            var weaponComponent = CreateWeapon(scene, position, weapon);
            if (randomAmmo && weaponComponent.Data.RoundsPerMagazine > 0)
                weaponComponent.RemainingRounds =
                    Utilities.RandomInt(Math.Min(weaponComponent.Data.RoundsPerMagazine, 8), weaponComponent.Data.RoundsPerMagazine);

            enemyCharacter.EquipWeapon(scene, weaponComponent);
        }

        return enemyCharacter;
    }

    /// <summary>
    /// Spawn an enemy.
    /// </summary>
    public static CharacterComponent CreateEnemy(Scene scene, Vector2 bottom, CharacterStats stats, CharacterLook look, Faction faction, float? scaleOverride = null)
    {
        var character = CreateCharacter(scene, new CharacterPrefabParams
        {
            Name = "Unnamed NPC",
            Bottom = bottom,
            Faction = faction,
            Look = look,
            Stats = stats,
            Tag = Tags.EnemyAI,
            ScaleOverride = scaleOverride
        });
        scene.AttachComponent(character.Entity, new AiComponent());
        var c = scene.Id;

        character.OnDeath.AddListener(e =>
        {
            if (Level.CurrentLevel != null
                && Game.Main.Scene.Id == c
                && Game.Main.Scene.FindAnyComponent<LevelProgressComponent>(out var progress))
                progress.BodyCount.Current++;
        });

        return character;
    }

    /// <summary>
    /// Spawn the player.
    /// </summary>
    public static PlayerComponent CreatePlayer(Scene scene, Vector2 bottom)
    {
        var look = UserData.Instances.PlayerLook;
        var stats = Registries.Stats["player"];

        if (CampaignProgress.CurrentCampaign != null)
        {
            if (CampaignProgress.CurrentCampaign.Stats != null)
                if (Registries.Stats.TryGet(CampaignProgress.CurrentCampaign.Stats, out var s))
                    stats = s;

            if (CampaignProgress.CurrentCampaign.Look != null)
                if (Registries.Looks.TryGet(CampaignProgress.CurrentCampaign.Look, out var l))
                    look = l;
        }

        var character = CreateCharacter(scene, new CharacterPrefabParams
        {
            Name = "Player",
            Bottom = bottom,
            Faction = Registries.Factions["player"],
            Look = look,
            Stats = stats,
            Tag = Tags.Player
        });

        character.Flags &= ~(CharacterFlags.StunAnimationOnNonFatalAttack | CharacterFlags.DeleteRagdoll);

        CharacterUtilities.ApplyActiveModifiers(scene, character);

        return scene.AttachComponent(character.Entity, new PlayerComponent());
    }

    /// <summary>
    /// Spawn a door.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static DoorComponent CreateDoor(Scene scene, in LevelEditor.DoorProperties properties)
    {
        var vertices = new Vertex[4]
        {
            new Vertex(new Vector3(properties.BottomLeft, 0), new Vector2(0, 0), Colors.White),
            new Vertex(new Vector3(properties.BottomRight, 0), new Vector2(1, 0), Colors.White),
            new Vertex(new Vector3(properties.TopRight, 0), new Vector2(1, 1), Colors.White),
            new Vertex(new Vector3(properties.TopLeft, 0), new Vector2(0, 1), Colors.White),
        };
        var indices = new uint[]
        {
            0u, 1u, 2u,
            0u, 3u, 2u
        };
        var mesh = new VertexBuffer(vertices, indices);
        mesh.PrimitiveType = Primitive.Triangles;

        var mat = DoorMaterialPool.Instance.RequestObject(0) ?? throw new Exception("TOO MANY DOORS!!");
        var entity = scene.CreateEntity();
        scene.AttachComponent(entity, new TransformComponent());
        var door = scene.AttachComponent(entity, new DoorComponent(mat, properties));
        scene.AttachComponent(entity, new CustomShapeComponent(mesh, mat) //wordt automatisch gedisposed :)
        {
            RenderOrder = RenderOrders.BackgroundBehind.WithOrder(1)
        });
        return door;
    }

    /// <summary>
    /// Create the objects that handle the player death animation, stop audio, play death sound, etc.
    /// </summary>
    /// <param name="scene"></param>
    /// <exception cref="Exception"></exception>
    public static void CreatePlayerDeathSequence(Scene scene)
    {
        if (scene.TryGetEntityWithTag(Tags.PlayerDeathSequence, out _))
            throw new Exception("There is already a player death sequence in progress");

        bool isTricky = ImprobabilityDisks.IsEnabled("tricky");

        if (isTricky)
        {
            MadnessUtils.Flash(Colors.Red, 2);
            var snd = SoundCache.Instance.LoadMusicNonLoop(Assets.Load<FixedAudioData>("sounds/tricky_revive.wav"));
            Game.Main.AudioRenderer.Play(snd, 4);
            MadnessUtils.Delay(2f, static () =>
            {
                MadnessCommands.Revive();
            });
        }
        else
        {
            if (!scene.FindAnyComponent<GameModeComponent>(out var gm) || gm.Mode != GameMode.Experiment)
            {
                if (scene.FindAnyComponent<MusicPlaylistComponent>(out var playlist))
                    playlist.Stop();
                if (PersistentSoundHandles.LevelMusic != null)
                    scene.Game.AudioRenderer.Pause(PersistentSoundHandles.LevelMusic);
            }
            scene.Game.AudioRenderer.Play(Sounds.DeathSound);

            MadnessUtils.Delay(1f, static () =>
            {
                if (Game.Main.Scene.GetEntitiesWithTag(Tags.PlayerDeathSequence).Any())
                    Game.Main.AudioRenderer.Play(Sounds.DeathMusic);
            });
        }
        var entity = scene.CreateEntity();
        scene.AttachComponent(entity, new PlayerDeathSequenceComponent());
        scene.SetTag(entity, Tags.PlayerDeathSequence);

        MadnessUtils.Shake(20);
    }

    /// <summary>
    /// Spawn the warning crosshair for an enemy's accurate shot.
    /// <br></br>
    /// Throws an exception if there is already an accurate shot happening.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="target"></param>
    /// <param name="shooter"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static AccurateShotComponent CreateAccurateShotWarning(Scene scene, ComponentRef<CharacterComponent> target, ComponentRef<CharacterComponent> shooter)
    {
        if (scene.TryGetEntityWithTag(Tags.AccurateShotHUD, out _))
            throw new Exception("There is already an accurate shot event in progress");

        scene.Game.AudioRenderer.Play(Sounds.AccurateShotWarning);
        var entity = scene.CreateEntity();
        scene.SetTag(entity, Tags.AccurateShotHUD);
        var s = scene.AttachComponent(entity, new AccurateShotComponent(shooter, target));

        return s;
    }

    /// <summary>
    /// Spawn the level transition effect.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Background.BackgroundComponent CreateSceneTransition(Scene scene, Transition type)
    {
        const float duration = 0.3f;
        var background = Background.CreateBackground(scene, Textures.Black);
        background.RenderOrder = RenderOrders.UserInterfaceTop;
        scene.AttachComponent(background.Entity, new DespawnComponent(duration * 1.5f));
        scene.AttachComponent(background.Entity, new BackgroundOffsetAnimationComponent
        {
            IsPlaying = true,
            Duration = duration,
            AffectedByTimeScale = false,
            OffsetCurve =
                type == Transition.Entry
                    ? new Vec2Curve(
                        new Curve<Vector2>.Key(new Vector2(0, 0), 0),
                        new Curve<Vector2>.Key(new Vector2(1, 0), 1))
                    : new Vec2Curve(
                        new Curve<Vector2>.Key(new Vector2(-1, 0), 0),
                        new Curve<Vector2>.Key(new Vector2(0, 0), 1)),
        });

        return background;
    }

    public static class Editor
    {
        //TODO wat is dit voor bullshit
        public static readonly Material ExampleDoorMaterial = new(new Shader(
            ShaderDefaults.WorldSpaceVertex,
            Assets.Load<string>("shaders/door.frag").Value
        ));

        public static readonly Material ExampleStaticDoorMaterial = new(new Shader(
            ShaderDefaults.WorldSpaceVertex,
            Assets.Load<string>("shaders/door.frag").Value
        ));
    }
}