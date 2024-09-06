using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Prefabs that are pooled (re-used).
/// <br></br>
/// See:
/// <see cref="PrefabPoolSystem"/>
/// <br></br>
/// <see cref="PrefabPoolSystem.EntityPool"/>
/// </summary>
public readonly struct PoolablePrefabs
{
    /// <summary>
    /// Poolable muzzleflash.
    /// </summary>
    public static readonly PoolablePrefab MuzzleFlash = new(
            //create new
            static (scene) =>
            {
                var ent = scene.CreateEntity();

                scene.AttachComponent(ent, new TransformComponent());

                var tex = Utilities.PickRandom(Textures.Muzzleflashes).Value;
                var mat = FlipbookMaterialCreator.LoadMaterialFor(tex, 5, 1, 0, Colors.White, true, ent);

                scene.AttachComponent(ent, new QuadShapeComponent(true)
                {
                    Material = mat,
                    RenderOrder = RenderOrders.Effects
                });

                float duration = Utilities.RandomFloat(0.05f, 0.12f);
                scene.AttachComponent(ent, new FlipbookComponent(mat)
                {
                    DeleteWhenDone = false,
                    Loop = false,
                    Duration = duration
                });

                var poolable = scene.AttachComponent(ent, new PoolablePrefabComponent());
                scene.AttachComponent(ent, new ReturnToPrefabPoolAfterTimeComponent { TimeInSeconds = duration });
                scene.SyncBuffers();

                return poolable;
            },

            //on return
            static (scene, entity) =>
            {
                scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = false;
            }
        );

    /// <summary>
    /// Poolable blood spurt.
    /// </summary>
    public static readonly PoolablePrefab BloodSpurt = new(
            //create new
            static (scene) =>
            {
                var ent = scene.CreateEntity();
                var anim = Utilities.PickRandom(Textures.BloodSpurts);
                scene.AttachComponent(ent, new TransformComponent());
                var tex = anim.Asset;
                var mat = FlipbookMaterialCreator.LoadMaterialFor(tex.Value, anim.Columns, anim.Rows, 0, Colors.Black, true, ent);
                scene.AttachComponent(ent, new BloodSpurtComponent(anim, mat));
                scene.AttachComponent(ent, new QuadShapeComponent(true)
                {
                    Material = mat,
                    RenderOrder = RenderOrders.Effects
                });
                float duration = Utilities.RandomFloat(0.4f, 0.5f);
                scene.AttachComponent(ent, new FlipbookComponent(mat)
                {
                    DeleteWhenDone = false,
                    Loop = false,
                    Duration = duration
                });
                var poolable = scene.AttachComponent(ent, new PoolablePrefabComponent());
                scene.AttachComponent(ent, new ReturnToPrefabPoolAfterTimeComponent { TimeInSeconds = duration });
                scene.SyncBuffers();

                return poolable;
            },

            //on return
            static (scene, entity) =>
            {
                //   scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = false;
            }
        );

    /// <summary>
    /// Poolable bullet impact.
    /// </summary>
    public static readonly PoolablePrefab BulletImpact = new(
            //create new
            static (scene) =>
            {
                var ent = scene.CreateEntity();
                var anim = Utilities.PickRandom(Textures.BulletImpacts);
                scene.AttachComponent(ent, new TransformComponent());
                var tex = anim.texture.Value;
                var mat = FlipbookMaterialCreator.LoadMaterialFor(tex, anim.columns, anim.rows, 0, Colors.White, false, ent);
                scene.AttachComponent(ent, new QuadShapeComponent(true)
                {
                    Material = mat,
                    RenderOrder = RenderOrders.Effects
                });
                float duration = Utilities.RandomFloat(0.1f, 0.3f);
                scene.AttachComponent(ent, new FlipbookComponent(mat)
                {
                    DeleteWhenDone = false,
                    Loop = false,
                    Duration = duration
                });
                var poolable = scene.AttachComponent(ent, new PoolablePrefabComponent());
                scene.AttachComponent(ent, new ReturnToPrefabPoolAfterTimeComponent { TimeInSeconds = duration });
                scene.SyncBuffers();

                return poolable;
            },

            //on return
            static (scene, entity) =>
            {
                //   scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = false;
            }
        );

    /// <summary>
    /// Poolable deflection effect.
    /// </summary>
    public static readonly PoolablePrefab DeflectionSpark = new(
            //create new
            static (scene) =>
            {
                var ent = scene.CreateEntity();
                var anim = Utilities.PickRandom(Textures.DeflectionSpark);
                scene.AttachComponent(ent, new TransformComponent());
                var tex = anim.texture.Value;
                var mat = FlipbookMaterialCreator.LoadMaterialFor(tex, anim.columns, anim.rows, 0, Colors.White, false, ent);
                scene.AttachComponent(ent, new QuadShapeComponent(true)
                {
                    Material = mat,
                    RenderOrder = RenderOrders.Effects
                });
                float duration = Utilities.RandomFloat(0.1f, 0.2f);
                scene.AttachComponent(ent, new FlipbookComponent(mat)
                {
                    DeleteWhenDone = false,
                    Loop = false,
                    Duration = duration
                });
                var poolable = scene.AttachComponent(ent, new PoolablePrefabComponent());
                scene.AttachComponent(ent, new ReturnToPrefabPoolAfterTimeComponent { TimeInSeconds = duration });
                scene.SyncBuffers();

                return poolable;
            },

            //on return
            static (scene, entity) =>
            {
                //   scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = false;
            }
        );
}
