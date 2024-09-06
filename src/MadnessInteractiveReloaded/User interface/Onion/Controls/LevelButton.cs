using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;
using Walgelijk;
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;

namespace MIR;

public readonly struct LevelButton : IControl
{
    private static string[] levelIndexStrings = [];

    static LevelButton()
    {
        levelIndexStrings = new string[128];
        for (int i = 0; i < levelIndexStrings.Length; i++)
            levelIndexStrings[i] = (i + 1).ToString("D3");
    }

    private readonly string levelKey;
    private readonly int levelIndex;

    public LevelButton(string levelKey, int levelIndex)
    {
        this.levelKey = levelKey;
        this.levelIndex = levelIndex;
    }

    public static InteractionReport Start(string levelKey, int levelIndex, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(LevelButton).GetHashCode(), identity, site), new LevelButton(levelKey, levelIndex));
        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.RenderFocusBox = false;
        ControlUtils.ProcessButtonLike(p);
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        instance.Rects.Rendered = instance.Rects.ComputedGlobal;
        var t = node.GetAnimationTime();

        if (t <= float.Epsilon)
            return;

        var anim = instance.Animations;
        bool locked = true;

        if (CampaignProgress.TryGetCurrentStats(out var stats))
            locked = stats.IsLevelLocked(levelKey);

        anim.AnimateRect(ref instance.Rects.Rendered, t);

        Draw.OutlineWidth = 0;

        if (locked)
        {
            var ti = float.Sin(p.GameState.Time * 4 - p.Node.ChronologicalPosition * 0.2f);
            ti = Utilities.MapRange(-1, 1, 0.5f, 0.9f, ti);
            Draw.Colour = Colors.Red.Brightness(ti);
            Draw.Image(Assets.Load<Texture>("textures/ui/locked_level.png").Value, instance.Rects.Rendered, ImageContainmentMode.Contain, 0, p.Theme.Rounding);
        }
        else
        {
            Draw.Colour = Colors.White;
            var lvl = Registries.Levels.Get(levelKey);
            if (Assets.HasAsset(lvl.Thumbnail.Id))
                Draw.Image(lvl.Thumbnail.Value, instance.Rects.Rendered, ImageContainmentMode.Contain, 0, p.Theme.Rounding);
            else
                Draw.Image(Textures.Error, instance.Rects.Rendered, ImageContainmentMode.Contain, 0, p.Theme.Rounding);

            // draw labels and such
            {
                Draw.Font = Fonts.CascadiaMono;
                Draw.FontSize = p.Theme.FontSize.Default - 3;

                Draw.Colour = Colors.Black.WithAlpha(0.7f);
                Draw.ResetTexture();
                Draw.Quad(instance.Rects.Rendered with { MinY = instance.Rects.Rendered.MaxY - Draw.FontSize * 2 });

                Draw.Colour = p.Theme.Text.Default;

                var tw = Draw.CalculateTextWidth(levelKey);
                var tp = instance.Rects.Rendered.TopLeft + new Vector2(10, -10);
                float occludedWidth = float.Max(0, tw - (instance.Rects.Rendered.Width - 20));
                if (occludedWidth > 5)
                {
                    var time = (p.GameState.Time * 0.1f + p.Node.ChronologicalPosition * 1.59384f) % 1;
                    var tt = float.Abs(time - 0.5f) * 2;
                    tp.X -= Easings.Quad.InOut(Easings.Expo.InOut(tt)) * occludedWidth;
                }

                Draw.Text(levelKey, tp, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);

                Draw.Text(levelIndexStrings[levelIndex], instance.Rects.Rendered.BottomRight + new Vector2(-10, 10), Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
            }
        }

        Draw.OutlineWidth = p.Theme.OutlineWidth[p.Instance.State];
        Draw.OutlineColour = p.Theme.OutlineColour[p.Instance.State];
        Draw.Colour = Colors.Transparent;
        //if (Draw.OutlineWidth > 0)
        //    Draw.Quad(instance.Rects.Rendered);

        //Ui.Layout.FitWidth(false).Height(32).Move(5);
        //Ui.TextRect(key, HorizontalTextAlign.Left, VerticalTextAlign.Top);

        //Ui.Layout.FitWidth(false).Height(32).Move(-5, 5);
        //Ui.TextRect(levelIndexStrings[i], HorizontalTextAlign.Right, VerticalTextAlign.Top);
        //Ui.Theme.Pop();
    }

    public void OnEnd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }
}


public readonly struct ModifierButton : IControl
{
    private readonly string modifierKey;

    public ModifierButton(string modifierKey)
    {
        this.modifierKey = modifierKey;
    }

    public static InteractionReport Start(string modifierKey, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(ModifierButton).GetHashCode(), identity, site), new ModifierButton(modifierKey));
        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.RenderFocusBox = false;
        ControlUtils.ProcessButtonLike(p);
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        instance.Rects.Rendered = instance.Rects.ComputedGlobal;
        var t = node.GetAnimationTime();

        if (t <= float.Epsilon)
            return;

        var anim = instance.Animations;
        bool unlocked = ImprobabilityDisks.IsUnlocked(modifierKey);

        anim.AnimateRect(ref instance.Rects.Rendered, t);

        Draw.OutlineWidth = 0;

        if (unlocked)
        {
            bool incompatible = ImprobabilityDisks.IsIncompatibleWithEnabled(modifierKey); // TODO slow
            var modifier = ImprobabilityDisks.All[modifierKey];

            if (modifier.Enabled)
            {
                Draw.ResetTexture();
                Draw.Colour = Colors.Red.WithAlpha(0.05f);
                Draw.OutlineWidth = 4;
                Draw.OutlineColour = Colors.Red;
                Draw.Quad(instance.Rects.Rendered);
            }

            Draw.Colour = Colors.White;
            if (incompatible)
                Draw.Colour.A = 0.25f;
            Draw.OutlineWidth = 0;
            Draw.Image(modifier.Texture, instance.Rects.Rendered.Expand(-10), ImageContainmentMode.Contain, 0, p.Theme.Rounding);
            // draw labels and such
            {
                Draw.Font = Fonts.CascadiaMono;
                Draw.FontSize = p.Theme.FontSize.Default - 3;

                var tRect = instance.Rects.Rendered with { MinY = instance.Rects.Rendered.MaxY - Draw.FontSize * 2 };

                if (modifier.Enabled)
                {
                    tRect.MinX += 4;
                    tRect.MaxX -= 4;
                    tRect.MaxY -= 4;
                }

                Draw.Colour = Colors.Black.WithAlpha(0.7f);
                Draw.ResetTexture();
                if (incompatible)
                    Draw.Colour.A = 0.25f;
                Draw.Quad(tRect);

                Draw.Colour = p.Theme.Text.Default;

                var displayName = modifier.DisplayName;
                if (incompatible)
                {
                    Draw.Colour = new Color(1, 0, 0, 0.25f);
                    displayName = Localisation.Get("incompatible").ToUpper();
                }

                var tw = Draw.CalculateTextWidth(displayName);
                var tp = instance.Rects.Rendered.TopLeft + new Vector2(10, -10);
                float occludedWidth = float.Max(0, tw - (instance.Rects.Rendered.Width - 20));
                if (occludedWidth > 5)
                {
                    var time = (p.GameState.Time * 0.1f + p.Node.ChronologicalPosition * 1.59384f) % 1;
                    var tt = float.Abs(time - 0.5f) * 2;
                    tp.X -= Easings.Quad.InOut(Easings.Expo.InOut(tt)) * occludedWidth;
                }

                Draw.Text(displayName, tp, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
            }
        }
        else
        {
            var ti = float.Sin(p.GameState.Time * 4 - p.Node.ChronologicalPosition * 0.2f);
            ti = Utilities.MapRange(-1, 1, 0.5f, 0.9f, ti);
            Draw.Colour = Colors.Red.Brightness(ti);
            Draw.Image(Assets.Load<Texture>("textures/ui/locked_level_sqr.png").Value, instance.Rects.Rendered, ImageContainmentMode.Contain, 0, p.Theme.Rounding);
        }

        Draw.OutlineWidth = p.Theme.OutlineWidth[p.Instance.State];
        Draw.OutlineColour = p.Theme.OutlineColour[p.Instance.State];
        Draw.Colour = Colors.Transparent;
    }

    public void OnEnd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }
}

