using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class RichCharacterComponent : Component
{
    public Vector2 TargetPosition;
    public Phase CurrentPhase = Phase.Sword;
    public Phase VisiblePhase = Phase.Sword;
    public float PhaseTimer = 0;

    public SubSprite Sword = new()
    {
        Texture = new("textures/weapons/mag_tacticalkatana/base.png")
    };

    public SubSprite Gun = new()
    {
        Texture = new("textures/weapons/mag_automag/base.png")
    };

    public SubSprite GunSlide = new()
    {
        Texture = new("textures/weapons/mag_automag/slide.png")
    };

    public AssetRef<StreamAudioData> MusicOnWin = new("sounds/music/Lothyde/strenuous_end.ogg");

    public Vector2 BarrelPosition;
    public float GunRecoil;
    public float GunRecoilVel;
    public float GunSlideTimer = float.MaxValue;
    public bool ShootGunSequenceActive = false;

    public float LastHealthRatio = 1;
    public float HealthBarFlashTimer = 0;

    public List<Routine> ActiveRoutines = [];

    public FixedIntervalDistributor AttackClock = new(4);

    public enum Phase
    {
        Sword,
        Gun,
        Dying
    } 

    public struct SubSprite
    {
        public Rect Rectangle;
        public Matrix3x2 Transform;
        public AssetRef<Texture> Texture;
        public RenderOrder Order;

        //public readonly SpriteComponent Materialise(Scene scene)
        //{
        //    var r = float.Atan2(Transform.M12, Transform.M11);

        //    var e = scene.CreateEntity();
        //    var sprite = scene.AttachComponent(e, new SpriteComponent(Texture.Value)
        //    {
        //        RenderOrder = RenderOrders.BackgroundDecals.OffsetOrder(5)
        //    });
        //    scene.AttachComponent(e, new TransformComponent
        //    {
        //        Position = Vector2.Transform(Rectangle.GetCenter(), Transform),
        //        Rotation = float.RadiansToDegrees(r),
        //        Scale = Rectangle.GetSize()
        //    });
        //    scene.AttachComponent(e, new FallenWeaponComponent(Rectangle));
        //    return sprite;
        //}
    }
}