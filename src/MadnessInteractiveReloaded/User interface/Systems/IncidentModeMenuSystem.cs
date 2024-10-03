using System;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class IncidentModeMenuSystem : Walgelijk.System
{
    public IncidentConfig IncidentConfig = new();
    public const int NeedleCount = 30;

    private float changeFlash = 0;
    private Vector2 dialDir;
    private float dialDragOffset;
    private bool isDraggingDial = false;
    private Vector2 displayDialDir;

    private float lastSnappedDialDir;
    private float titleFntSize = 72;

    private bool isBusyWithIntroAnimation;
    private float introAnimationTimer;

    public override void OnActivate()
    {
        IncidentConfig.Seed = Random.Shared.Next();
        IncidentConfig.KillTarget = 100;
        isBusyWithIntroAnimation = false;
    }

    public override void Update()
    {
        float animationDuration = 10;
        const float phase2 = 0.25f;
        const float phase3 = 0.5f;
        const float phase4 = 0.6f;

        if (isBusyWithIntroAnimation)
            introAnimationTimer += Time.DeltaTime / animationDuration;
        else
            introAnimationTimer = 0;

        var ph1timer = Utilities.Clamp(Utilities.MapRange(0, phase2, 0, 1, introAnimationTimer));
        var ph2timer = Utilities.Clamp(Utilities.MapRange(phase2, phase3, 0, 1, introAnimationTimer));
        var ph3timer = Utilities.Clamp(Utilities.MapRange(phase3, phase4, 0, 1, introAnimationTimer));
        var ph4timer = Utilities.Clamp(Utilities.MapRange(phase4, 1, 0, 1, introAnimationTimer));

        if (Input.IsKeyPressed(Key.F5))
        {
            if (Input.IsKeyHeld(Key.D4))
                introAnimationTimer = phase4;
            else if (Input.IsKeyHeld(Key.D3))
                introAnimationTimer = phase3;
            else if (Input.IsKeyHeld(Key.D2))
                introAnimationTimer = phase2;
            else
                introAnimationTimer = 0;
        }

        changeFlash -= Time.DeltaTime * 2;
        changeFlash = float.Max(0, changeFlash);
        float flash = Easings.Cubic.In(Utilities.Clamp(changeFlash));

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Font = Fonts.Impact;

        // intro bg
        {
            float b = ph1timer * 1.5f;
            var tex = Assets.Load<Texture>("textures/black-transparent-gradient.png").Value;
            var r = new Rect(0, Window.Height * b, Window.Width, 0);
            Draw.Texture = tex;
            Draw.Quad(r);
            Draw.ResetTexture();
            Draw.Colour = Colors.Black.WithAlpha(b);
            Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));

            // title bar thing
            if (introAnimationTimer > phase2)
            {
                b = Utilities.Clamp(Utilities.MapRange(phase2, phase2 + 0.2f, 1, 0, introAnimationTimer));
                Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, Easings.Cubic.In(b * b));
                Draw.Colour.A *= Utilities.MapRange(-1, 1, 0.8f, 1, float.Sin(Time.SecondsSinceLoadUnscaled * 60));
                Draw.TransformMatrix = Matrix3x2.CreateScale(1, (1 - Easings.Circ.In(ph2timer)), Window.Size * 0.5f);
                Draw.Texture = Assets.Load<Texture>("textures/horizontal-gradient.png").Value;
                Draw.Quad(new Rect(Window.Size * 0.5f, new Vector2(Window.Width, 70)));
                Draw.ResetTransformation();
                ////var snd = SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>("sounds/firearms/automag_1.wav"));
                //var snd = SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>("sounds/proceed.wav"));
                //if (!Audio.IsPlaying(snd) && introAnimationTimer < phase2 + 0.05f)
                //    Audio.Play(snd, 0.25f);
            }

            if (introAnimationTimer > phase3)
            {
                var bg = Assets.Load<Texture>("textures/incident_background.qoi").Value;
                var bgOverlay = Assets.Load<Texture>("textures/incident_background_overlay.qoi").Value;
                Draw.Colour = Vector4.Lerp(Colors.Black, Colors.White, Utilities.Clamp(ph3timer * 3));
                Draw.ClearMask();
                Draw.WriteMask();
                var rect = Draw.Image(bg, new Rect(0, 0, Window.Width, Window.Height), ImageContainmentMode.Contain);
                Draw.InsideMask();
                Draw.TransformMatrix = Matrix3x2.CreateScale(1.1f, Window.Size * 0.5f);
                Draw.TransformMatrix *= Matrix3x2.CreateTranslation(Utilities.MapRange(0, 1, 50, 0, Easings.Quad.Out(ph3timer)), 0);
                Draw.Image(bg, rect, ImageContainmentMode.Stretch);

                var flicker = ph4timer < 0.9f || Time % 0.1f > 0.05f;
                if (introAnimationTimer > phase4 && (flicker))
                {
                    Draw.FontSize = Window.Height / 1080f * 180;
                    // we COULD do the glowing thing, but i think its too complex for this little intro animation
                    var p = Easings.Cubic.InOut(Utilities.Clamp((ph4timer - 0.2f) * 4));
                    var p2 = Utilities.Clamp(Easings.Expo.Out(ph4timer * 4));
                    var p3 = Easings.Cubic.InOut(Utilities.Clamp((ph4timer + 0.17f) * 1.2f));
                    var v = (int)float.Round(IncidentConfig.KillTarget * p);
                    var pos = new Vector2(rect.MaxX - 100, Window.Height * 0.5f);

                    var bnds = rect with { MinX = float.Lerp(rect.MaxX, rect.MinX, p3), MaxX = pos.X - Draw.CalculateTextWidth("###") };
                    Draw.DrawBounds = new DrawBounds(bnds);
                    Draw.Colour = Vector4.Lerp(Colors.White, Colors.Red, p3);
                    Draw.Image(bgOverlay, rect, ImageContainmentMode.Stretch);
                    Draw.ResetDrawBounds();

                    Draw.TransformMatrix = Matrix3x2.CreateScale(1, 1 + 0.5f * (1 - p2), pos);
                    Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, p2);
                    Draw.Text($"{v:0}", pos, Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Middle);
                    Draw.ResetTransformation();
                }

                Draw.DisableMask();
            }
        }

        // incident title
        {
            Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, flash);
            Draw.FontSize = titleFntSize;

            var p = new Vector2(Window.Width * 0.5f, 100);
            p.Y = float.Lerp(p.Y, Window.Height * 0.5f - 3, Easings.Quad.InOut(Utilities.Clamp(ph1timer * 1.1f)));

            if (introAnimationTimer > phase2)
                Draw.Colour = Colors.Black.WithAlpha(1 - Easings.Cubic.In(ph2timer));

            var r = new Rect(p, new Vector2(700, 70));
            if (!isBusyWithIntroAnimation && r.ContainsPoint(Input.WindowMousePosition))
            {
                Window.CursorStack.SetCursor(DefaultCursor.Pointer);
                titleFntSize = Utilities.SmoothApproach(titleFntSize, 75, 25, Time.DeltaTime);
                if (Input.IsButtonPressed(MouseButton.Left))
                {
                    Audio.Play(Sounds.UiConfirm);
                    IncidentConfig.Seed = Random.Shared.Next();
                    changeFlash = 1;
                }
            }
            else
                titleFntSize = Utilities.SmoothApproach(titleFntSize, 72, 15, Time.DeltaTime);

            Draw.Text(IncidentConfig.Name, p, new(flash * 0.05f + 1, 1), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
        }

        // big body count thing
        {
            Draw.TransformMatrix = Matrix3x2.CreateSkew(-0.1f - (flash * 0.1f), 0, Window.Size / 2);
            Draw.FontSize = 250;
            var str = IncidentConfig.KillTarget.ToString();

            if (flash > float.Epsilon && !isBusyWithIntroAnimation)
            {
                Draw.Colour = Colors.Red;
                Draw.Text(str, Window.Size / 2, new(flash * flash * 0.05f + 1), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            }

            Draw.Colour = Colors.White.WithAlpha(1 - ph1timer * 3);
            var size = Draw.CalculateTextHeight(str);
            Draw.Text(str, Window.Size / 2, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            Draw.ResetTransformation();
            Draw.Colour = Colors.Red.WithAlpha(1 - ph1timer * 5);
            Draw.Font = Fonts.Toxigenesis;
            Draw.Text("VICTIMS",
                new(Window.Width * 0.5f, Window.Height * 0.5f + size * 0.5f),
                new(0.15f + (flash * flash * 0.01f)),
                HorizontalTextAlign.Center, VerticalTextAlign.Middle);

            if (!isBusyWithIntroAnimation && Vector2.DistanceSquared(Input.WindowMousePosition, Window.Size / 2) < 300 * 300)
            {
                if (float.Abs(Input.MouseScrollDelta) > float.Epsilon)
                {
                    var s = float.Sign(Input.MouseScrollDelta);
                    dialDir = Vector2.TransformNormal(dialDir, Matrix3x2.CreateRotation(float.Tau / NeedleCount * s));
                }
            }
        }

        // dial
        {
            var center = Window.Size / 2 + new Vector2(0, 3);
            float radius = 300 + Easings.Cubic.In(ph1timer) * 2000;
            float width = 40;

            if (dialDir.LengthSquared() < 0.1f)
                dialDir = Vector2.UnitX;
            dialDir = Vector2.Normalize(dialDir);

            if (!isBusyWithIntroAnimation)
                if (isDraggingDial)
                {
                    if (Input.IsButtonReleased(MouseButton.Left))
                        isDraggingDial = false;

                    var a = Vector2.Normalize(Input.WindowMousePosition - center);
                    a = Vector2.TransformNormal(a, Matrix3x2.CreateRotation(-dialDragOffset));
                    dialDir = a;
                    Window.CursorStack.SetCursor(DefaultCursor.Pointer);
                }
                else
                {
                    if (Vector2.Distance(Input.WindowMousePosition, center) < radius)
                    //if ((SDF.Circle(Input.WindowMousePosition, center, radius - width * 0.5f)) < width)
                    {
                        Window.CursorStack.SetCursor(DefaultCursor.Pointer);
                        if (Input.IsButtonPressed(MouseButton.Left))
                        {
                            isDraggingDial = true;
                            var a = Input.WindowMousePosition - center;
                            dialDragOffset = float.Atan2(a.Y, a.X) - float.Atan2(dialDir.Y, dialDir.X);
                        }
                    }
                }

            var snappedDialDir = dialDir;
            if (!isBusyWithIntroAnimation)
            {
                float th = Utilities.Snap(float.Atan2(dialDir.Y, dialDir.X), float.Tau / NeedleCount); // because we have NeedleCount needles or whatever the fuck
                snappedDialDir = new Vector2(float.Cos(th), float.Sin(th));

                if (float.Abs(float.Abs(th) - float.Abs(lastSnappedDialDir)) > 0.001f) // we changed!
                {
                    var dir = float.Sign(th - lastSnappedDialDir); // <-- me when i pretend to be a rotary encoder
                    ChangeTargetKillCount(dir);
                }

                lastSnappedDialDir = th;
            }

            displayDialDir = Utilities.SmoothApproach(displayDialDir, snappedDialDir, 25, Time.DeltaTime);
            if (isDraggingDial)
                displayDialDir = Utilities.SmoothApproach(displayDialDir, dialDir, 15, Time.DeltaTime);
            // little animation to suggest interactivity
            if (!isDraggingDial && !isBusyWithIntroAnimation)
            {
                float th = float.Sin(Time * 30) * 0.002f;
                float tt = 1 - (Time % 4 * 0.25f);
                th *= Easings.Expo.In(tt * tt * tt);
                displayDialDir = Vector2.Transform(displayDialDir, Matrix3x2.CreateRotation(th));
            }

            {
                float th = Utilities.NanFallback(float.Atan2(displayDialDir.Y, displayDialDir.X)) + Easings.Cubic.In(ph1timer * 3);
                Draw.TransformMatrix = Matrix3x2.CreateRotation(th, center);
                Draw.Colour = Colors.White.WithAlpha(1 - (ph1timer * 4));
                Draw.Texture = Assets.Load<Texture>("textures/ui/dial.png").Value;
                Draw.Quad(new Rect(center, new(radius * 2)));
            }
        }

        float buttonOffset = Easings.Cubic.In(ph1timer * 10) * 60;

        Ui.Theme.OutlineWidth(2).Once();
        Ui.Layout.FitWidth().MaxWidth(150).Height(40).StickLeft().StickBottom().Move(10, -10 + buttonOffset);
        if (Ui.Button(Localisation.Get("back")) && !isBusyWithIntroAnimation)
        {
            MadnessUtils.Flash(Colors.Black, 0.2f);
            Game.Scene = MainMenuScene.Load(Game);
        }

        Ui.Theme.OutlineWidth(2).Once();
        Ui.Layout.FitWidth().MaxWidth(150).Height(40).StickRight().StickBottom().Move(-10, -10 + buttonOffset);
        if (Ui.Button(Localisation.Get("start")))
        {
            if (!isBusyWithIntroAnimation)
            {
                introAnimationTimer = 0;
                isBusyWithIntroAnimation = true;

                var c = IncidentConfig.CreateCampaign(out var mus);
                CampaignProgress.SetCampaign(c);

                // copied this from CampaignScene.cs to start the music sooner
                if (PersistentSoundHandles.MainMenuMusic != null)
                    Audio.Stop(PersistentSoundHandles.MainMenuMusic);
                if (PersistentSoundHandles.PauseMusic != null)
                    Audio.Stop(PersistentSoundHandles.PauseMusic);

                var atmos = SoundCache.Instance.LoadMusicNonLoop(Assets.Load<StreamAudioData>("sounds/atmos2.ogg"));
                Audio.Play(atmos);

                MadnessUtils.Delay(animationDuration * phase2, () =>
                {
                    var l = SoundCache.Instance.LoadMusic(mus.Value);
                    if (PersistentSoundHandles.LevelMusic != l)
                    {
                        if (PersistentSoundHandles.LevelMusic != null)
                            Audio.Stop(PersistentSoundHandles.LevelMusic);
                        PersistentSoundHandles.LevelMusic = l;
                    }
                    Audio.Play(l);
                });

                MadnessUtils.Delay(animationDuration, () =>
                {
                    Game.Scene = LevelLoadingScene.Create(Game, Registries.Levels.Get(c.Levels[0]).Level, SceneCacheSettings.NoCache);
                    MadnessUtils.Flash(Colors.Black, 0.2f);
                });
            }
        }
    }

    private bool ChangeTargetKillCount(int sign)
    {
        var l = IncidentConfig.KillTarget;
        IncidentConfig.KillTarget = int.Clamp(IncidentConfig.KillTarget + sign, IncidentConfig.MinKillTarget, IncidentConfig.MaxKillTarget);
        var b = l != IncidentConfig.KillTarget;

        if (b)
        {
            Audio.Play(Sounds.UiPress);
            changeFlash = 1;
        }
        else
            Audio.Play(Sounds.UiHover);

        return b;
    }
}
//😀😀

public class IncidentConfig
{
    public const int MinKillTarget = 20;
    public const int MaxKillTarget = 200;
    public const int LevelCount = 5;

    public int Seed = Random.Shared.Next();
    public int KillTarget = 100;

    public string Name => $"INCIDENT:0x{Hashed:X}";
    public ushort Hashed => (ushort)(HashCode.Combine(Seed, KillTarget));

    public Campaign CreateCampaign(out AssetRef<StreamAudioData> selectedMusic)
    {
        var rand = new Random(Seed);

        string[] beginLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_begin"))];
        string[] endLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_end"))];
        string[] midLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident")).Except(beginLevels).Except(endLevels)];

        // all music choices
        // TODO make sure only sensible music is available. currently, even the death music might be chosen.
        // how to do this? tags? maybe a list somewhere in a text file asset?
        GlobalAssetId[] music = [..Assets.EnumerateFolder("sounds/music", System.IO.SearchOption.AllDirectories)
            .Where(d => Assets.GetMetadata(d).MimeType.Contains("audio", StringComparison.InvariantCultureIgnoreCase))];

        var selectedTrack = rand.GetItems(music, 1)[0];

        var begin = rand.GetItems(beginLevels, 1);
        var end = rand.GetItems(endLevels, 1);
        rand.Shuffle(midLevels);

        var c = new Campaign
        {
            Name = Name,
            Author = "INCIDENT DIRECTOR",
            Description = $"Seed={Seed}; Victims={KillTarget};",
            Id = $"incident_{Hashed:X}",
            Thumbnail = Assets.Load<Texture>("error.png"),
            Temporary = true,
            Levels = [
                begin[0],
                ..midLevels.Take(LevelCount),
                end[0],
            ]
        };

        selectedMusic = new(selectedTrack);
        foreach (var k in c.Levels)
            if (Registries.Levels.TryGet(k, out var lvl))
                lvl.Level.Value.BackgroundMusic = selectedMusic;

        return c;
    }
}