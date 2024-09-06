namespace MIR.Test.GameTests;

using System.Numerics;
using Walgelijk;
using Walgelijk.Mock;

public class FakeWindow : Window
{
    public override string Title { get; set; } = string.Empty;
    public override Vector2 Position { get; set; }
    public override Vector2 Size { get; set; }
    public override bool VSync { get; set; }

    public override bool IsOpen => true;
    public override bool HasFocus => true;

    public override bool IsVisible { get; set; }
    public override bool Resizable { get; set; }

    private FakeRenderTarget rt = new();
    private MockGraphics internalGraphics = new();
    private Time time = new Time
    {
        SecondsSinceLoad = 0,
        SecondsSinceSceneChange = 0,
        TimeScale = 1,
        DeltaTime = 1 / 60f,
        DeltaTimeUnscaled = 1 / 60f,
    };

    public override IGraphics Graphics => internalGraphics;

    public override RenderTarget RenderTarget => rt;

    public override WindowType WindowType { get; set; }
    public override bool IsCursorLocked { get; set; }
    public override DefaultCursor CursorAppearance { get; set; }
    public override IReadableTexture? CustomCursor { get; set; }

    public override float DPI => 96;

    public override void Close()
    {
    }

    public override void ResetInputState()
    {
    }

    public override Vector2 ScreenToWindowPoint(Vector2 screen)
    {
        return screen;
    }

    public override void SetIcon(IReadableTexture texture, bool flipY = true)
    {
    }

    public override Vector2 WindowToScreenPoint(Vector2 window)
    {
        return window;
    }

    public override Vector2 WindowToWorldPoint(Vector2 window)
    {
        return window;
    }

    public override Vector2 WorldToWindowPoint(Vector2 world)
    {
        return world;
    }

    //public void StepForDuration(TimeSpan duration, TimeSpan dt)
    //{
    //    double t = 0;
    //    while (t < duration.TotalSeconds)
    //    {
    //        Game.Scene?.RenderSystems();
    //        if (Game.DevelopmentMode)
    //            Game.DebugDraw.Render();
    //        Graphics.CurrentTarget = RenderTarget;
    //        Game.Console.Render();
    //        RenderQueue.RenderAndReset(internalGraphics);

    //        Game.Console.Update();
    //        if (!Game.Console.IsActive)
    //        {
    //            Game.Scene?.UpdateSystems();
    //            Game.Scene?.FixedUpdateSystems();
    //        }
    //        Game.Profiling.Tick();
    //        Game.AudioRenderer.Process(Game);

    //        t += dt.TotalSeconds;

    //        time.DeltaTime = (float)dt.TotalSeconds;

    //        time.SecondsSinceSceneChange += (float)dt.TotalSeconds * time.TimeScale;
    //        time.SecondsSinceLoad += (float)dt.TotalSeconds * time.TimeScale;
    //        time.SecondsSinceSceneChangeUnscaled += (float)dt.TotalSeconds;
    //        time.SecondsSinceLoadUnscaled += (float)dt.TotalSeconds;
    //    }
    //}

    public override void Initialise()
    {
    }

    public override void Deinitialise()
    {
    }

    public override void LoopCycle()
    {
    }
}
