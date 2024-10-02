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

    private float changeFlash = 0;
    private Vector2 dialDir;
    private float dialDragOffset;
    private bool isDraggingDial = false;
    private Vector2 displayDialDir;

    private float lastSnappedDialDir;

    private float t = 0;

    public override void Update()
    {
        changeFlash -= Time.DeltaTime * 2;
        changeFlash = float.Max(0, changeFlash);
        float flash = Easings.Cubic.In(Utilities.Clamp(changeFlash));

        t += Time.DeltaTimeUnscaled;

        if (t > 2 && false)
        {
            t = 0;
            IncidentConfig.Seed = Random.Shared.Next();
            IncidentConfig.KillTarget = Random.Shared.Next(50, 250);
            changeFlash = 1;
        }

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Font = Fonts.Impact;

        // incident title
        {
            Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, flash);
            Draw.FontSize = 72;
            Draw.Text(IncidentConfig.Name, new Vector2(Window.Width / 2, 50), new(flash * 0.05f + 1, 1), HorizontalTextAlign.Center, VerticalTextAlign.Top);
        }

        // big body count thing
        {
            Draw.TransformMatrix = Matrix3x2.CreateSkew(-0.1f - (flash * 0.1f), 0, Window.Size / 2);
            Draw.FontSize = 250;
            var str = IncidentConfig.KillTarget.ToString();

            if (flash > float.Epsilon)
            {
                Draw.Colour = Colors.Red;
                Draw.Text(str, Window.Size / 2, new(flash * flash * 0.05f + 1), HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            }

            Draw.Colour = Colors.White;
            Draw.Text(str, Window.Size / 2, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
            Draw.ResetTransformation();
        }

        // dial
        {
            var center = Window.Size / 2 + new Vector2(0, 3);
            float radius = 300;
            float width = 40;

            if (dialDir.LengthSquared() < 0.1f)
                dialDir = Vector2.UnitX;
            dialDir = Vector2.Normalize(dialDir);

            if (isDraggingDial)
            {
                if (Input.IsButtonReleased(MouseButton.Left))
                    isDraggingDial = false;

                var a = Input.WindowMousePosition - center;
                a = Vector2.Transform(a, Matrix3x2.CreateRotation(-dialDragOffset));
                dialDir = Vector2.Normalize(a);
                Window.CursorStack.SetCursor(DefaultCursor.Pointer);
            }
            else
            {
                if (float.Abs(SDF.Circle(Input.WindowMousePosition, center, radius - width * 0.5f)) < width)
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
            {
                float th = Utilities.Snap(float.Atan2(dialDir.Y, dialDir.X), float.Tau / 30f); // because we have 30 needles or whatever the fuck
                snappedDialDir = new Vector2(float.Cos(th), float.Sin(th));

                if (float.Abs(float.Abs(th) - float.Abs(lastSnappedDialDir)) > 0.001f) // we changed!
                {
                    int l = IncidentConfig.KillTarget;
                    var dir = float.Sign(th - lastSnappedDialDir); // <-- me when i pretend to be a rotary encoder
                    IncidentConfig.KillTarget = int.Clamp(IncidentConfig.KillTarget + dir, IncidentConfig.MinKillTarget, IncidentConfig.MaxKillTarget);
                    IncidentConfig.Seed = Utilities.RandomInt(0, 1000000);
                    if (l != IncidentConfig.KillTarget)
                    {
                        Audio.Play(Sounds.UiPress);
                        changeFlash = 1;
                    }
                    else
                    {
                        Audio.Play(Sounds.UiHover);
                    }
                }

                lastSnappedDialDir = th;
            }
            displayDialDir = Utilities.SmoothApproach(displayDialDir, snappedDialDir, 25, Time.DeltaTime);
            if (isDraggingDial)
                displayDialDir = Utilities.SmoothApproach(displayDialDir, dialDir, 15, Time.DeltaTime);


            //Draw.Line(center, center + dialDir * radius, 5);
            Draw.TransformMatrix = Matrix3x2.CreateRotation(Utilities.NanFallback(float.Atan2(displayDialDir.Y, displayDialDir.X)), center);
            Draw.Colour = Colors.White;
            Draw.Texture = Assets.Load<Texture>("textures/ui/dial.png").Value;
            Draw.Quad(new Rect(center, new(radius * 2)));
        }

        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);

        Ui.Theme.OutlineWidth(2).Once();
        Ui.Layout.FitWidth().MaxWidth(150).Height(40).StickRight().StickBottom().Move(10, -10);
        if (Ui.Button(Localisation.Get("start")))
        {
            var c = IncidentConfig.CreateCampaign();
            CampaignProgress.SetCampaign(c);

            Game.Scene = LevelLoadingScene.Create(Game, Registries.Levels.Get(c.Levels[0]).Level, SceneCacheSettings.NoCache);
            MadnessUtils.Flash(Colors.Black, 0.2f);
        }
    }
}
//😀😀

public class IncidentConfig
{
    public const int MinKillTarget = 20;
    public const int MaxKillTarget = 200;
    public const int LevelCount = 5;

    public int Seed;
    public int KillTarget = 100;

    public string Name => $"INCIDENT:0x{Hashed:X}";
    public ushort Hashed => (ushort)(HashCode.Combine(Seed, KillTarget));

    public Campaign CreateCampaign()
    {
        var rand = new Random(Seed);

        string[] beginLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_begin"))];
        string[] endLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident_end"))];
        string[] midLevels = [.. Registries.Levels.GetAllKeys().Where(k => k.StartsWith("lvl_incident")).Except(beginLevels).Except(endLevels)];

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
            Temporary =  true,
            Levels = [
                begin[0],
                ..midLevels.Take(LevelCount),
                end[0],
            ]
        };

        foreach (var k in c.Levels)
        {
            if (Registries.Levels.TryGet(k, out var lvl))
            {
                lvl.Level.Value.BackgroundMusic = new(selectedTrack);
            }
        }

        return c;
    }
}