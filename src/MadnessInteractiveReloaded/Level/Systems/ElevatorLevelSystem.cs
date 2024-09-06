using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// For the elevator level.
/// </summary>
public class ElevatorLevelSystem : Walgelijk.System
{
    // TODO all of this bullshit should be somewhere else but.. that's for later lol
    public readonly DetailPrefab[] Details =
    {
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_1.png"), 0, HorizontalTextAlign.Left),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_2.png"), 18, HorizontalTextAlign.Left),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_3.png"), 22, HorizontalTextAlign.Left),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_4.png"), 0, HorizontalTextAlign.Right),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_5.png"), 435, HorizontalTextAlign.Left),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_6.png"), 11, HorizontalTextAlign.Left),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_7.png"), 12, HorizontalTextAlign.Right),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_8.png"), 15, HorizontalTextAlign.Right),
        new(Assets.Load<Texture>("textures/elevator_details/background_elevtator_detail_9.png"), 320, HorizontalTextAlign.Right),
    };

    public readonly struct DetailPrefab
    {
        public readonly AssetRef<Texture> Texture;
        public readonly float XOffset;
        public readonly HorizontalTextAlign Alignment;

        public DetailPrefab(AssetRef<Texture> texture, float x, HorizontalTextAlign alignment)
        {
            Texture = texture;
            XOffset = x;
            Alignment = alignment;
        }
    }

    public struct ActiveElevatorDetail
    {
        public DetailPrefab Prefab;
        public float CurrentPostion;
        public bool Visible;
    }

    private readonly ActiveElevatorDetail[] activeDetails = new ActiveElevatorDetail[1];

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene))
            return;

        if (!Scene.FindAnyComponent<ElevatorLevelComponent>(out var comp))
        {
            comp = new ElevatorLevelComponent();
            Scene.AttachComponent(Scene.CreateEntity(), comp);
            return;
        }

        Scene.FindAnyComponent<EnemySpawningComponent>(out var enemySpawner);
        comp.DtMultiplier += Time.DeltaTime / 60;
        comp.DtMultiplier = float.Clamp(comp.DtMultiplier, 0, 6);
        comp.Time += Time.DeltaTime * (1 + comp.DtMultiplier);
        float speed = 
            Easings.Cubic.In(Utilities.Clamp((comp.Time - comp.StartDelay) / 5)) 
            * 1500 
            * (1 + comp.DtMultiplier);

        if (comp.Time > comp.StartDelay)
        {
            if (comp.VerticalOffset <= float.Epsilon)
            {
                MadnessUtils.Shake(5);
                Audio.Play(SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>("sounds/elevator_start.ogg").Value));
            }
            comp.VerticalOffset += Time.DeltaTime * speed;

            if (comp.Time > comp.StartDelay + 3 && enemySpawner != null && !enemySpawner.Enabled)
                enemySpawner.Enabled = true;
        }
        else if (enemySpawner != null)
        {
            if (Level.CurrentLevel != null && CampaignProgress.TryGetCurrentStats(out var stats))
                enemySpawner.Enabled = stats.ByLevel[Level.CurrentLevel.Id].Attempts > 1; // just spawn the enemies if this isn't your first time playing
            else
                enemySpawner.Enabled = false;
        }

        if (Scene.TryGetEntityWithTag(new Tag(9020), out var lampFlipbookEnt) && Scene.TryGetComponentFrom<TransformComponent>(lampFlipbookEnt, out var lampTransform))
        {
            lampTransform.Position -= Vector2.UnitY * Time.DeltaTime * speed;
        }

        for (int i = 0; i < activeDetails.Length; i++)
        {
            ref var d = ref activeDetails[i];
            if (d.Visible)
                d.CurrentPostion += Time.DeltaTime * speed;
        }
    }

    public override void FixedUpdate()
    {
        if (MadnessUtils.IsPaused(Scene))
            return;
        if (!Scene.FindAnyComponent<ElevatorLevelComponent>(out var comp))
            return;
        if (comp.Time > comp.StartDelay)
        {
            MadnessUtils.Shake(0.1f);

            if (comp.Time > comp.StartDelay * 2)
                for (int i = 0; i < activeDetails.Length; i++)
                {
                    ref var d = ref activeDetails[i];
                    if (!d.Visible && Utilities.RandomFloat() > 0.99f)
                    {
                        d.Visible = true;
                        d.Prefab = Utilities.PickRandom(Details);
                        d.CurrentPostion = 0;
                    }
                }
        }
    }

    public override void Render()
    {
        if (Level.CurrentLevel == null)
            return;

        if (!Scene.FindAnyComponent<ElevatorLevelComponent>(out var comp))
            return;

        const float scale = MadnessConstants.BackgroundSizeRatio;
        const float magicalValueThatFixesEverything = 107;

        Draw.Reset();

        float verticalOffset = Math.Max(comp.VerticalOffset, 0);

        var groundFloor = Assets.Load<Texture>("textures/backgrounds/background_elevator.qoi").Value ;
        var platform = Assets.Load<Texture>("textures/backgrounds/background_elevator_platform.qoi").Value;
        var loopingBackground = Assets.Load<Texture>("textures/backgrounds/background_elevator_looping.qoi").Value;

        var groundFloorMat = BackgroundMaterialCache.Instance.Load(groundFloor);
        var platformMat = BackgroundMaterialCache.Instance.Load(platform);

        // draw ground floor
        {
            var rect = new Rect(new Vector2(magicalValueThatFixesEverything, 2332 - verticalOffset), groundFloor.Size * scale);

            Draw.Order = RenderOrders.BackgroundBehind.OffsetOrder(1);
            Draw.Material = groundFloorMat.bg;
            Draw.Image(groundFloor, rect, ImageContainmentMode.Stretch);

            Draw.Order = RenderOrders.BackgroundInFront.OffsetOrder(1);
            Draw.Material = groundFloorMat.fg;
            Draw.Image(groundFloor, rect, ImageContainmentMode.Stretch);
        }

        // draw looping bg
        {
            var bounds = Level.CurrentLevel.LevelBounds.Height;

            Draw.Order = RenderOrders.BackgroundBehind.OffsetOrder(1);
            Draw.ResetMaterial();

            var a = new Rect(new Vector2(magicalValueThatFixesEverything, 2332 - ((verticalOffset + groundFloor.Height * scale) % bounds)), loopingBackground.Size * scale);
            a.MaxY += 4; // adjust for small alpha gap
            if (verticalOffset > groundFloor.Height * scale)
                Draw.Image(loopingBackground, a, ImageContainmentMode.Stretch);
            Draw.Image(loopingBackground, a.Translate(0, loopingBackground.Height * scale), ImageContainmentMode.Stretch);
        }

        // draw details
        {
            var bounds = Level.CurrentLevel.LevelBounds.Height;
            for (int i = 0; i < activeDetails.Length; i++)
            {
                ref var item = ref activeDetails[i];

                if (!item.Visible)
                    continue;

                var detail = item.Prefab;
                var detailTex = detail.Texture.Value;
                var s = detailTex.Size * scale;
                var x = detail.Alignment switch
                {
                    HorizontalTextAlign.Right => (loopingBackground.Width / 2 - detail.XOffset) * scale - s.X,
                    _ => (-loopingBackground.Width / 2 + detail.XOffset) * scale,
                };
                var v = item.CurrentPostion - groundFloor.Height * scale - s.Y;
                var y = -v;

                Draw.Order = RenderOrders.BackgroundBehind.OffsetOrder(1);
                Draw.ResetMaterial();

                var a = new Rect(0, 0, s.X, s.Y);
                a = a.Translate(x, loopingBackground.Height / 2 * scale + s.Y);
                a = a.Translate(magicalValueThatFixesEverything, y);

                if (v > bounds + s.Y) // TODO dit is helemaal kapot
                    item.Visible = false;

                Draw.Image(detailTex, a, ImageContainmentMode.Stretch);
            }
        }

        // draw platform
        {
            var rect = new Rect(new Vector2(107, 1358), platform.Size * scale);

            Draw.Order = RenderOrders.BackgroundBehind.OffsetOrder(2);
            Draw.Material = platformMat.bg;
            Draw.Image(platform, rect, ImageContainmentMode.Stretch);

            Draw.Order = RenderOrders.BackgroundInFront.OffsetOrder(2);
            Draw.Material = platformMat.fg;
            Draw.Image(platform, rect, ImageContainmentMode.Stretch);
        }
    }
}
