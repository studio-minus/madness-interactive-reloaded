using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

// TODO make this class NOT shit
public class DebarkationLevelSystem : Walgelijk.System
{
    private float time = 0;
    private Sound trainEmergencyHalt => SoundCache.Instance.LoadSoundEffect(Assets.Load<StreamAudioData>("sounds/train_emergency_halt.ogg"));

    public override void Initialise()
    {
        var trainAsset = Assets.Load<Texture>("textures/transportation_locomotive.qoi");
        Graphics.Upload(trainAsset.Value);
        Assets.AssignLifetime(trainAsset.Id, new SceneLifetimeOperator()); 
        
        trainAsset = Assets.Load<Texture>("textures/transportation_wagon.qoi");
        Graphics.Upload(trainAsset.Value);
        Assets.AssignLifetime(trainAsset.Id, new SceneLifetimeOperator());

        time = 0;
    }

    public override void OnActivate()
    {
        time = 0;
        Audio.Play(trainEmergencyHalt);
    }

    public override void OnDeactivate()
    {
        Audio.Stop(trainEmergencyHalt);
    }

    public override void Update()
    {
        if (Level.CurrentLevel == null)
            return;

        const float scale = 1.5f * MadnessConstants.BackgroundSizeRatio;

        var lvl = Level.CurrentLevel;
        var bounds = lvl.LevelBounds;
        var progress = Easings.Cubic.Out(Utilities.Clamp(time));
        var p = Utilities.MapRange(0, 1, 0, bounds.Width * 3f, progress);

        Draw.Reset();
        Draw.Order = RenderOrders.BackgroundInFront.OffsetOrder(100);

        var locomotiveAsset = Assets.Load<Texture>("textures/transportation_locomotive.qoi").Value;
        var wagonAsset = Assets.Load<Texture>("textures/transportation_wagon.qoi").Value;

        var locomotiveRect = new Rect(default, locomotiveAsset.Size * scale);
        var wagonRect = new Rect(default, wagonAsset.Size * scale);

        const float yOffset = 500;
        var trainOffset = p + bounds.MinX - locomotiveRect.Width * 0.5f;
        locomotiveRect = locomotiveRect.Translate(trainOffset, bounds.MinY + locomotiveRect.Height * 0.5f - yOffset);
        wagonRect = wagonRect.Translate(trainOffset - locomotiveRect.Width + 185, bounds.MinY + wagonRect.Height * 0.5f - yOffset);

        Draw.Texture = locomotiveAsset;
        Draw.Quad(locomotiveRect);
        // TODO one day, you should fix this
        // this is vile
        Draw.Texture = wagonAsset;
        Draw.Quad(wagonRect);
        Draw.Quad(wagonRect.Translate(-wagonRect.Width, 0));
        Draw.Quad(wagonRect.Translate(-(wagonRect.Width) * 2, 0));

        if (Scene.TryGetEntityWithTag(new(5002), out var fire) && Scene.TryGetComponentFrom<TransformComponent>(fire, out var fireTransform))
        {
            fireTransform.Position = locomotiveRect.GetCenter() + new Vector2(-650, 1015);
        }

        if (!MadnessUtils.IsPaused(Scene) && Time.DeltaTimeUnscaled < 0.1f)
            time += Time.DeltaTime * 0.1f;

        MadnessUtils.Shake(Time.DeltaTime * 100 * float.Sqrt(1 - progress));

#if DEBUG
        if (Input.IsKeyPressed(Key.F5))
            time = 0;
#endif
    }
}
