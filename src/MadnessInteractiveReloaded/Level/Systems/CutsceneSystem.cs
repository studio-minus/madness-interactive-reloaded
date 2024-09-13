using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Numerics;
using Walgelijk;
using Walgelijk.Localisation;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Plays cutscenes, advances their slides.
/// </summary>
public class CutsceneSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene))
            return;

        if (!Scene.FindAnyComponent<CutsceneComponent>(out var component))
            return;

        if (component.IsComplete && component.DestroyEntityOnCompletion)
        {
            Scene.RemoveEntity(component.Entity);
            return;
        }

        var slide = component.Cutscene.Slides[component.Index];

        if (component.CurrentSlideIsUninitialised)
        {
            component.CurrentSlideIsUninitialised = false;
            slide.OnStart();
        }

        if (component.Time >= slide.Duration)
        {
            component.Time = 0;
            slide.OnStop();
            component.Index++;
            if (component.Index >= component.Cutscene.Slides.Length)
                component.IsComplete = true;
            else
            {
                slide = component.Cutscene.Slides[component.Index];
                component.CurrentSlideIsUninitialised = true;
            }
        }
        else if (slide.IsReady)
        {
            component.Time += Time.DeltaTimeUnscaled;
        }

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Colour = Colors.Black;
        Draw.Order = component.RenderOrder;
        Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));

        slide.OnUpdate(Window, Time);
        RenderQueue.Add(slide, RenderOrders.Default);

        if (Input.IsKeyHeld(Key.Escape))
        {
            component.SkipTimer += Time.DeltaTimeUnscaled;
            if (component.SkipTimer > 1)
                component.IsComplete = true;
        }
        else
            component.SkipTimer = 0;

        if (component.SkipTimer > 0)
        {
            Draw.Reset();
            Draw.FontSize = 48;
            Draw.Font = Fonts.Impact;
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrders.UserInterfaceTop;
            var s = Localisation.Get("skip");
            Draw.Text(s, Window.Size - new Vector2(10),
                Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Bottom);

            var w = Draw.CalculateTextWidth(s);
            var h = Draw.CalculateTextHeight(s);

            Draw.ResetTexture();
            Draw.BlendMode = BlendMode.Negate;
            Draw.Quad(new Rect(0, 0, w * component.SkipTimer, h).Translate(-10 - w, -3 - h).Translate(Window.Size));
        }
    }
    public override void Render()
    {
            return;
        if (!Scene.FindAnyComponent<CutsceneComponent>(out var component) || component.IsComplete)
            return;

        var slide = component.Cutscene.Slides[component.Index];
        
        //slide.Execute(Graphics);
    }
}