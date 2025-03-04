using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.OpenTK.MotionTK;
using Walgelijk.SimpleDrawing;

namespace MIR.Cutscenes;

/// <summary>
/// Story related cutscene.
/// </summary>
public class Cutscene : IDisposable
{
    public readonly ISlide[] Slides;

    public Cutscene()
    {
        Slides = [];
    }

    public Cutscene(ISlide[] slides)
    {
        Slides = slides;
    }

    public void Dispose()
    {
        foreach (var item in Slides)
            if (item is IDisposable disp)
                disp.Dispose();
    }
}

public interface ISlide : IDisposable, IRenderTask
{
    public float Duration { get; set; }
    public bool IsReady { get; }

    public void OnStart();
    public void OnUpdate(Window window, Time time);
    public void OnStop();
}

public class TextureSlide : ISlide
{
    public float Duration { get; set; }
    public AssetRef<Texture> Texture;
    public bool IsReady => true;

    private float t = 0;

    public void OnStart()
    {
        t = 0;
    }

    public void OnUpdate(Window window, Time time)
    {
        t += time.DeltaTimeUnscaled;
        var progress = t / Duration;
        float scale = float.Lerp(1, 1.05f, progress);

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.UserInterface;
        Draw.TransformMatrix = Matrix3x2.CreateScale(scale, window.Size / 2);
        Draw.Image(Texture.Value, new Rect(0, 0, window.Width, window.Height), ImageContainmentMode.Contain);
    }

    void IRenderTask.Execute(IGraphics g) { }

    public void OnStop()
    {
    }

    public void Dispose()
    {
    }
}

public class PlayerPosterSlide : ISlide
{
    public float Duration { get; set; }
    public bool IsReady => true;

    private float t = 0;

    public void OnStart()
    {
        t = 0;
    }

    public void OnUpdate(Window window, Time time)
    {
        t += time.DeltaTimeUnscaled;
        var progress = t / Duration;
        float scale = float.Lerp(1, 1.05f, progress);

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.UserInterface.WithOrder(10);
        Draw.TransformMatrix = Matrix3x2.CreateScale(scale, window.Size / 2);

        var a = Draw.Image(Assets.Load<Texture>("textures/cutscene_player_poster.png").Value,
             new Rect(0, 0, window.Width, window.Height), ImageContainmentMode.Contain);

        a = a.Scale(0.25f, 1).Translate(0, a.Height * -0.1f); // we gotta stay proportional

        Draw.Order = Draw.Order.OffsetOrder(-1);
        Draw.Image(ThumbnailRenderer.GetOrCreatePlayerThumbnail(), a, ImageContainmentMode.Contain);
    }

    void IRenderTask.Execute(IGraphics g) { }

    public void OnStop()
    {
    }

    public void Dispose()
    {
    }
}

public class VideoSlide : ISlide
{
    public float Duration { get; set; }
    public AssetRef<Video> Video;

    public bool IsReady => vid?.IsReady ?? false;

    private Video? vid;

    public VideoSlide(AssetRef<Video> video, float duration)
    {
        Video = video;
        Duration = duration;

    }

    public VideoSlide()
    {
    }

    public void OnStart()
    {
        try
        {
            vid = Video.Value;
            vid.Restart();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error initializing video: {ex.Message}");
        }
    }

    public void OnStop()
    {
        try
        {
            vid?.Stop();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping video: {ex.Message}");
        }
    }

    public void OnUpdate(Window window, Time time)
    {
        if (vid == null)
            return;

        try
        {
            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrders.UserInterface;
            Draw.Image(vid.Texture, new Rect(0, 0, window.Width, window.Height), ImageContainmentMode.Contain);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error updating video: {ex.Message}");
        }
    }

    void IRenderTask.Execute(IGraphics g)
    {
        if (vid == null)
            return;

        try
        {
            var ot = g.CurrentTarget;
            vid.UpdateAndRender(g);
            g.CurrentTarget = ot;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing render task: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            vid?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error disposing video: {ex.Message}");
        }
    }
}
