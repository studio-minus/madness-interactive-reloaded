using System.Diagnostics.Contracts;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class PassingTrainSystem : Walgelijk.System
{
    public override void Initialise()
    {
        if (!Scene.FindAnyComponent<PassingTrainComponent>(out var comp))
        {
            var entity = Scene.CreateEntity();
            comp = Scene.AttachComponent(entity, new PassingTrainComponent());

            var whistle = SoundCache.Instance.LoadSoundEffect(comp.TrainWhistleSound, out var whistleProfile);
            var passingLoop = SoundCache.Instance.LoadSoundEffect(comp.TrainLoopSound, out var passingLoopProfile);

            MadnessUtils.DelayPausable(0.5f, () =>
            {
                Audio.Play(whistle);
                passingLoop.Looping = true;
                Audio.Play(passingLoop);
            });

            Scene.OnInactive += () =>
            {
                Audio.Stop(whistle);
                Audio.Stop(passingLoop);

                SoundCache.Instance.Unload(whistleProfile);
                SoundCache.Instance.Unload(passingLoopProfile);
            };

            Assets.AssignLifetime(comp.TrainWhistleSound.Id, new SceneLifetimeOperator());
            Assets.AssignLifetime(comp.TrainLoopSound.Id, new SceneLifetimeOperator());
            Assets.AssignLifetime(comp.LocomotiveTexture.Id, new SceneLifetimeOperator());
            Assets.AssignLifetime(comp.CarTexture.Id, new SceneLifetimeOperator());
        }
    }

    public override void Update()
    {
        const float ratio = MadnessConstants.BackgroundSizeRatio;

        var lvl = Level.CurrentLevel;
        if (lvl == null)
            return;

        if (!Scene.FindAnyComponent<PassingTrainComponent>(out var comp))
            return;

        var locomotive = comp.LocomotiveTexture.Value;
        var car = comp.CarTexture.Value;
        comp.Time += Time.DeltaTime * comp.Speed;

        var area = new Rect(lvl.LevelBounds.MinX, lvl.LevelBounds.MinY, lvl.LevelBounds.MaxX, lvl.LevelBounds.MinY + (car.Height - 236) * ratio);
        area.MinX -= car.Width * ratio;
        area.MaxX += car.Width * ratio;
        var left = new Vector2(area.MinX, (area.MaxY + area.MinY) / 2);
        var offset = comp.Time * area.Width - area.Width;

        Draw.Reset();

        int carCount = 5;
        float carWidth = area.Width / carCount;

        Draw.Texture = car;
        for (int i = 0; i < carCount; i++)
        {
            var mOffset = offset - i * carWidth;
            var center = left;
            center.X = left.X + mOffset % area.Width;
            Draw.Quad(new Rect(center, car.Size * ratio));
        }

        Draw.Texture = locomotive;
        if (offset < area.Width)
        {
            var mOffset = offset - -1 * carWidth;
            var center = left;
            center.X = left.X + mOffset;
            Draw.Quad(new Rect(center, car.Size * ratio));
        }
    }
}
