using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Onion.Controls;
using Walgelijk.Onion.Layout;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class ModMenuSystem : Walgelijk.System
{
    private string userFilter = string.Empty;
    private Mod? selectedMod;

    public override void Update()
    {
        if (selectedMod != null && !ModLoader.TryGet(selectedMod.Id, out _))
            selectedMod = null;

        if (selectedMod != null)
            ModLoader.TryGet(selectedMod.Id, out selectedMod);

        MenuUiUtils.StartFullMenuPanel(Scene);
        {
            //Ui.Theme.Padding(20).Once();
            // mod list
            Ui.Theme.Foreground(new Appearance(Colors.White.WithAlpha(0.9f), Assets.Load<Texture>("textures/right-border.png").Value, ImageMode.Slice)).Once();
            Ui.Layout.FitContainer(1 / 2.5f, 1).StickLeft().StickTop();
            Ui.StartGroup(true);
            {
                Ui.Theme.FontSize(35).Once();
                Ui.Layout.FitContainer(0.2f, null).Height(40).Move(13, 13);
                Ui.TextRect("Mods", HorizontalTextAlign.Left, VerticalTextAlign.Top);

                userFilter ??= string.Empty;
                Ui.Theme.OutlineWidth(2).Padding(7).Once();
                Ui.Layout.FitContainer(0.5f, null).Height(35).Move(0, 13).StickRight().Move(-9, 0);
                Ui.StringInputBox(ref userFilter, new(placeholder: "Filter"));

                //Ui.Theme.Padding(20).Once();
                Ui.Layout.FitContainer().Scale(0, -60).Scale(-10, -60).StickTop().Move(0, 60).StickLeft().VerticalLayout();
                Ui.Theme.ScrollbarWidth(16).Accent(Colors.Red.Brightness(0.5f)).Once();
                Ui.StartScrollView();
                {
                    int i = 0;
                    foreach (var m in ModLoader.Mods)
                    {
                        if (!m.Name.Contains(userFilter, System.StringComparison.InvariantCultureIgnoreCase)
                            && !m.Authors.Contains(userFilter, System.StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        //ModView(m, i);
                        Ui.Theme.OutlineWidth(3).Once();
                        if (m == selectedMod)
                        {
                            Ui.Decorate(new CrosshairDecorator());
                            Ui.Theme.ForegroundColor(Colors.Red.WithAlpha(0.2f)).Once();
                        }
                        Ui.Layout.FitWidth().Scale(-20, 0).Height(150).CenterHorizontal();
                        if (ModViewControl.Click(m, i))
                        {
                            selectedMod = m;
                        }
                        Ui.Spacer(10, identity: i);
                        i++;
                    }
                }
                Ui.End();
            }
            Ui.End();

            // readme
            Ui.Layout.FitContainer(1 - 1 / 2.5f, 1).StickRight().StickTop();
            Ui.StartGroup(false);
            {
                Ui.Layout.FitContainer().Scale(-24, -24).Center();
                Ui.StartGroup(false);
                {
                    if (selectedMod != null)
                    {
                        // little info panel up top
                        Ui.Layout.FitContainer(0.5f, 1, false).Height(110).StickLeft(false).StickTop(false);
                        Ui.StartGroup(false);
                        {
                            Ui.Layout.Size(100, 100).StickTop().StickLeft();
                            Ui.Image(selectedMod.Thumbnail, ImageContainmentMode.Stretch);

                            Ui.Layout.Height(30).FitWidth().Scale(-110, 0).StickTop().StickRight();
                            Ui.Theme.FontSize(24).Once();
                            Ui.TextRect(selectedMod.Name, HorizontalTextAlign.Left, VerticalTextAlign.Top);

                            Ui.Layout.Height(30).FitWidth().Scale(-110, 0).StickTop().Move(0, 25).StickRight();
                            Ui.Theme.FontSize(18).Text(Colors.White.WithAlpha(0.5f)).Once();
                            Ui.TextRect(selectedMod.Authors, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                        }
                        Ui.End();

                        // error panel
                        if (!selectedMod.Errors.IsEmpty)
                        {
                            Ui.Layout.FitContainer(0.5f, 1, false).Height(110).StickRight(false).StickTop(false).VerticalLayout();
                            Ui.StartScrollView(false);
                            {
                                Ui.Layout.Height(30).PreferredSize().FitWidth(false).StickTop(false).StickLeft(false);
                                Ui.Theme.FontSize(18).Text(Color.Red).Once();
                                Ui.TextRect("ERRORS", HorizontalTextAlign.Left, VerticalTextAlign.Top);

                                foreach (var item in selectedMod.Errors)
                                {
                                    Ui.Layout.Height(30).PreferredSize().FitWidth().StickTop().StickLeft();
                                    Ui.Theme.FontSize(16).Once();
                                    Ui.TextRect(item.Message, HorizontalTextAlign.Left, VerticalTextAlign.Top, identity: item.GetHashCode());
                                }
                            }
                            Ui.End();
                        }

                        // wall of text

                        Ui.Layout.Height(5).FitWidth(false).Scale(32, 0).CenterHorizontal().Move(-2, 120);
                        Ui.Theme.Image(Colors.Red).Once();
                        Block.Start();

                        Ui.Layout.FitContainer().Scale(0, -135).StickBottom().StickLeft().Overflow(false, true).VerticalLayout();
                        Ui.StartScrollView();
                        {
                            Ui.Theme.FontSize(24).Once();
                            Ui.Label("Dependencies");

                            Ui.Spacer(8);
                            Ui.Layout.Height(2).FitWidth(false).CenterHorizontal();
                            Ui.Theme.Image(Colors.White.WithAlpha(0.5f)).Once();
                            Block.Start();
                            Ui.Spacer(8);

                            if (selectedMod.Dependencies.IsEmpty)
                            {
                                Ui.Layout.Height(40).FitWidth().CenterHorizontal();
                                Ui.TextRect("No dependencies", HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                            }
                            else
                            {
                                int b = 0;
                                foreach (var dependency in selectedMod.Dependencies)
                                {
                                    if (ModLoader.TryGet(dependency, out var mod))
                                    {
                                        Ui.Theme.Text(Color.White).Once();
                                        Ui.Label(mod.Name, identity: b);
                                    }
                                    else
                                    {
                                        Ui.Theme.Text(Color.Red).Once();
                                        Ui.Label(dependency.Value, identity: b);
                                    }
                                    b++;
                                }
                            }

                            Ui.Spacer(8);
                            Ui.Layout.Height(2).FitWidth(false).CenterHorizontal();
                            Ui.Theme.Image(Colors.White.WithAlpha(0.5f)).Once();
                            Block.Start();
                            Ui.Spacer(8);

                            Ui.Theme.FontSize(24).Once();
                            Ui.Label("Description");
                            Ui.Spacer(8);

                            Ui.Theme.FontSize(16).Once();
                            Ui.Layout.FitWidth().PreferredSize();
                            Ui.TextRect(selectedMod.Description, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                        }
                        Ui.End();
                    }
                }
                Ui.End();
            }
            Ui.End();
        }
        Ui.End();

        // back button
        if (MenuUiUtils.BackButton())
            Game.Scene = MainMenuScene.Load(Game);

        // folder button
        Ui.Layout.FitWidth().MaxWidth(160).Height(40).StickRight().StickBottom().Move(-10);
        Ui.Theme.OutlineWidth(2).Once();
        if (Ui.Button("Mods folder"))
            MadnessUtils.OpenExplorer(ModLoader.Sources.OfType<LocalModCollectionSource>().First().Directory.FullName);
    }
}

public readonly struct ModViewControl(Mod mod) : IControl
{
    private readonly Mod mod = mod;

    public static bool Click(Mod mod, int identity = 0, [CallerLineNumber] int site = 0)
    {
        return Start(mod, identity, site).Up;
    }

    public static InteractionReport Start(Mod mod, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Create(nameof(ModViewControl).GetHashCode(), identity, site), new ModViewControl(mod));
        instance.RenderFocusBox = false;

        Texture alert = Assets.Load<Texture>("textures/ui/icon/alert.png");
        Texture trash = Assets.Load<Texture>("textures/ui/icon/trash.png");
        Texture cog = Assets.Load<Texture>("textures/ui/icon/cog.png");
        Texture check = Assets.Load<Texture>("textures/ui/icon/check.png");
        Texture folder = Assets.Load<Texture>("textures/ui/icon/folder.png");
        Texture refresh = Assets.Load<Texture>("textures/ui/icon/refresh.png");

        Ui.Theme.Padding(10).Once();
        Ui.Layout.FitWidth().Height(32).Scale(-instance.Rects.Intermediate.Height, 0).StickRight(false).StickBottom().HorizontalLayout();
        Ui.StartGroup(false);
        {
            var source = ModLoader.GetSourceFor(mod.Id);
            if (source is LocalModCollectionSource local)
            {
                Ui.Layout.FitHeight().EnqueueConstraint(new AspectRatio(1)).CenterVertical();
                Ui.Theme.OutlineWidth(0).ForegroundColor(Colors.Transparent).Image(new(Colors.Red, Colors.White)).Once();
                if (Ui.ImageButton(folder, ImageContainmentMode.Stretch))
                    MadnessUtils.OpenExplorer(local.GetModDirectory(mod.Id)?.FullName ?? local.Directory.FullName);
            }
        }
        Ui.End();

        if (!mod.Errors.IsEmpty)
        {
            Ui.Theme.Padding(10).Once();
            Ui.Layout.Height(32).EnqueueConstraint(new AspectRatio(1)).StickBottom().StickRight();
            Ui.Theme.OutlineWidth(0).ForegroundColor(Colors.Transparent).Image((Color)Utilities.Lerp(Colors.White, Colors.Red, node.SecondsAlive % 1)).Once();
            Ui.ImageButton(alert, ImageContainmentMode.Stretch);
        }

        Onion.Tree.End();
        return new InteractionReport(instance, node, InteractionReport.CastingBehaviour.Up);
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        ControlUtils.ProcessButtonLike(p);
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        var fg = p.Theme.Foreground[instance.State];
        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;
        Draw.ImageMode = fg.ImageMode;
        Draw.OutlineColour = p.Theme.OutlineColour.Default;
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);

        var thumbnailRect = instance.Rects.Rendered with { MaxX = instance.Rects.Rendered.MinX + instance.Rects.Rendered.Height };
        var infoRect = instance.Rects.Rendered with { MinX = instance.Rects.Rendered.MinX + instance.Rects.Rendered.Height + p.Theme.Padding * 5 };

        // draw info and stuff
        Draw.OutlineWidth = 0;
        Draw.Quad(infoRect, 0, p.Theme.Rounding);
        {
            //text business
            Draw.Colour = p.Theme.Text.Default;

            // title
            Draw.FontSize = p.Theme.FontSize.Default * 2;
            Draw.Text(mod.Name, infoRect.BottomLeft + new Vector2(15), Vector2.One);

            // authors
            Draw.Colour = p.Theme.Text.Default.WithAlpha(0.5f);
            Draw.FontSize = p.Theme.FontSize.Default;
            Draw.Text(mod.Authors, infoRect.BottomLeft + new Vector2(15, Draw.FontSize * 2 + 15), Vector2.One);

            // description
            Draw.Colour = p.Theme.Text.Default;
            Draw.FontSize = p.Theme.FontSize.Default * 1.1f;
            Draw.Text(mod.ShortDescription, infoRect.BottomLeft + new Vector2(15, Draw.FontSize * 2 + 40), Vector2.One);

            // size & version
            Draw.Colour = p.Theme.Text.Default.WithAlpha(0.5f);
            Draw.FontSize = p.Theme.FontSize.Default;
            Draw.Text(mod.Version, infoRect.BottomRight + new Vector2(-15, 15), Vector2.One, HorizontalTextAlign.Right);
            Draw.Text(mod.CompressedSizeString, infoRect.BottomRight + new Vector2(-15, 35), Vector2.One, HorizontalTextAlign.Right);
        }

        // draw thumbnail
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];
        Draw.Colour = Colors.White;
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.Image(mod.Thumbnail, thumbnailRect, ImageContainmentMode.Stretch);
        if (!mod.Errors.IsEmpty)
        {
            Draw.OutlineWidth = 0;
            Draw.Colour = Colors.White.WithAlpha(Draw.Colour.A * (float.Sin(node.SecondsAlive * 9) * 0.5f + 0.5f));
            Draw.Image(Assets.Load<Texture>("textures/ui/angry.png").Value, thumbnailRect.Scale(0.8f), ImageContainmentMode.Stretch);
        }

        bool active = ModLoader.IsActive(mod.Id);
        if (!active)
        {
            var b = thumbnailRect.TopLeft + new Vector2(10, -10);
            var a = Utilities.MapRange(-1, 1, 0.8f, 1, float.Sin((p.GameState.Time.SecondsSinceLoad + node.SiblingIndex * 0.2f) * 5));
            Draw.FontSize = 18;
            Draw.Colour = Colors.Black.WithAlpha(a);
            Draw.Text("Inactive", b, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
            Draw.Colour = Colors.Red.WithAlpha(a);
            Draw.Text("Inactive", b + new Vector2(-1), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Bottom);
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }
}

public readonly struct AspectRatio : IConstraint
{
    private readonly float heightOverWidth;
    private readonly Behaviour behaviour;

    public AspectRatio(float heightOverWidth, Behaviour behaviour = Behaviour.Grow)
    {
        this.heightOverWidth = heightOverWidth;
        this.behaviour = behaviour;
    }

    public enum Behaviour
    {
        Shrink,
        Grow
    }

    public void Apply(in ControlParams p)
    {
        if (p.Node.Parent == null)
            return;

        var w = p.Instance.Rects.Intermediate.Width;
        var h = p.Instance.Rects.Intermediate.Height;

        switch (behaviour)
        {
            case Behaviour.Shrink:
                if (w > h)
                    p.Instance.Rects.Intermediate.Width = h / heightOverWidth;
                else
                    p.Instance.Rects.Intermediate.Height = w * heightOverWidth;
                break;
            case Behaviour.Grow:
                if (w < h)
                    p.Instance.Rects.Intermediate.Width = h * heightOverWidth;
                else
                    p.Instance.Rects.Intermediate.Height = w / heightOverWidth;
                break;
        }
    }
}

public readonly struct Block : IControl
{
    public static void Start(int identity = 0, [CallerLineNumber] int site = 0)
    {
        Onion.Tree.Start(IdGen.Create(nameof(Block).GetHashCode(), identity, site), new Block());
        Onion.Tree.End();
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        p.Instance.Rects.Rendered = p.Instance.Rects.ComputedGlobal;
        p.Instance.CaptureFlags = CaptureFlags.Hover;
        p.Instance.Rects.Raycast = p.Instance.Rects.Rendered;
        p.Instance.Rects.DrawBounds = p.Instance.Rects.Rendered;
    }

    public void OnRender(in ControlParams p)
    {
        ControlParams controlParams = p;
        controlParams.Deconstruct(out ControlTree _, out LayoutQueue _, out Input _, out GameState _, out Node node, out ControlInstance instance);
        Node n = node;
        ControlInstance controlInstance = instance;
        controlInstance.Rects.Rendered = controlInstance.Rects.ComputedGlobal;
        float animationTime = n.GetAnimationTime();
        if (animationTime > float.Epsilon)
        {
            var animations = controlInstance.Animations;
            Draw.Colour = p.Theme.Image[instance.State];
            Draw.Texture = Texture.White;
            Draw.OutlineWidth = 0f;
            animations.AnimateRect(ref controlInstance.Rects.Rendered, animationTime);
            animations.AnimateColour(ref Draw.Colour, animationTime);
            Draw.Quad(controlInstance.Rects.Rendered, 0, p.Theme.Rounding);
        }
    }

    public void OnEnd(in ControlParams p)
    {
    }

    public void OnRemove(in ControlParams p)
    {
    }
}
