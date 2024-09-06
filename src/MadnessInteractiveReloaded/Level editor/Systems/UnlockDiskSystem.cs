using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR.LevelEditor;

public class UnlockDiskSystem : Walgelijk.System //🎈
{
    public override void Update()
    {
        Draw.Reset();

        var canPickUp = !MadnessUtils.IsPaused(Scene)
            && !MadnessUtils.IsCutscenePlaying(Scene)
            && !Ui.IsBeingUsed
            && !MadnessUtils.EditingInExperimentMode(Scene)
            && MadnessUtils.IsPlayerAlive(Scene);

        var isEditMode = Scene.HasSystem<LevelEditorTestSystem>();

        foreach (var disk in Scene.GetAllComponentsOfType<UnlockDiskComponent>())
        {
            if (!isEditMode && (!ImprobabilityDisks.All.ContainsKey(disk.DiskId) || ImprobabilityDisks.IsUnlocked(disk.DiskId)))
                Scene.RemoveEntity(disk.Entity);

            var tex = disk.Texture.Value;

            Draw.Order = disk.RenderOrder;
            Draw.TransformMatrix = Matrix3x2.CreateRotation(-disk.Angle, disk.Position);
            var r = new Rect(disk.Position, tex.Size);
            Draw.Texture = tex;
            Draw.Quad(r);

            if (canPickUp && r.ContainsPoint(Input.WorldMousePosition) && Input.IsButtonPressed(MouseButton.Left))
            {
                if (isEditMode)
                    Scene.RemoveEntity(disk.Entity);
                else
                    ImprobabilityDisks.Unlock(disk.DiskId);

                RoutineScheduler.Start(UnlockRoutine(disk.DiskId));
            }
        }

#if DEBUG
        if (Input.IsKeyPressed(Key.PageDown))
        {
            RoutineScheduler.Start(UnlockRoutine(Utilities.PickRandom(ImprobabilityDisks.All.Keys)));
        }
#endif
    }


    private IEnumerator<IRoutineCommand> UnlockRoutine(string id)
    {
        const float duration = 5;

        float t = 0;
        Time.TimeScale = 0.1f;

        Audio.Play(SoundCache.Instance.LoadUISoundEffect(Assets.Load<FixedAudioData>("sounds/unlock_disk.wav")));

        while (t < 1)
        {
            t += Time.DeltaTimeUnscaled / duration;
            yield return new GameSafeRoutineDelay();

            {
                Draw.Reset();
                Draw.ScreenSpace = true;

                float progress = t;

                var disk = ImprobabilityDisks.All[id];
                var center = Window.Size / 2;
                float r = float.Max(Window.Width, Window.Height) * 2;

                var phase1 = Easings.Expo.Out(Utilities.Clamp(progress * 4));
                phase1 = float.Min(phase1, Easings.Expo.Out(Utilities.Clamp((1 - progress) * 4)));

                float s = float.Lerp(200, 500, phase1) * (Window.Width / 1920f);

                // draw circular noise mask
                {
                    Draw.ClearMask();
                    Draw.WriteMask();
                    Draw.Circle(center, new Vector2(r * phase1 * 0.5f));
                    Draw.DisableMask();
                }

                // draw noise in mask
                {
                    Draw.Order = RenderOrder.UI.WithOrder(-100);
                    Draw.BlendMode = BlendMode.Negate;
                    Draw.Colour = Colors.Red.Brightness(0.8f);
                    Draw.InsideMask();
                    Draw.ImageMode = ImageMode.Tiled;
                    Draw.TransformMatrix = Matrix3x2.CreateScale(2);
                    Draw.Image(PlayerDeathSequenceSystem.TrickyStatic,
                        new Rect(0, 0, r, r).Translate(-Window.Width * Utilities.RandomFloat(), -Window.Height * Utilities.RandomFloat()), ImageContainmentMode.Stretch);
                    Draw.DisableMask();
                    Draw.ResetTransformation();
                    Draw.ImageMode = ImageMode.Stretch;
                    Draw.BlendMode = BlendMode.AlphaBlend;
                }

                // draw disk image
                {
                    var diskRect = (float intensity) => new Rect(center + Utilities.RandomPointInCircle(0, 2 * intensity),
                        new Vector2(s + Utilities.RandomFloat(-20, 20) * intensity, s + Utilities.RandomFloat(-10, 10) * intensity));

                    Draw.BlendMode = BlendMode.AlphaBlend;
                    Draw.Colour = Colors.Green.WithAlpha(phase1);
                    Draw.Image(disk.Texture, diskRect(0), ImageContainmentMode.Stretch);

                    Draw.BlendMode = BlendMode.Addition;
                    Draw.Colour = Colors.Blue.WithAlpha(phase1);
                    Draw.Image(disk.Texture, diskRect(1), ImageContainmentMode.Stretch);

                    Draw.Colour = Colors.Red.WithAlpha(phase1);
                    Draw.Image(disk.Texture, diskRect(2), ImageContainmentMode.Stretch);
                }

                // draw marquee text on top and bottom
                {
                    Draw.BlendMode = BlendMode.AlphaBlend;
                    Draw.Font = Fonts.Toxigenesis;
                    Draw.FontSize = 48;
                    Draw.Colour = Colors.Red;
                    Draw.Colour.A = phase1 * Utilities.MapRange(-1, 1, 0.5f, 1, float.Sin(Time.SecondsSinceLoadUnscaled * 8));

                    var c = Localisation.Get("disk-unlocked");
                    var textWidth = Draw.CalculateTextWidth(c);
                    var hTextWidth = textWidth * 0.5f;
                    var mqC = float.Floor(Window.Width / (textWidth + 100));
                    for (int i = 0; i <= mqC; i++)
                    {
                        float mt = ((Time.SecondsSinceLoadUnscaled * 0.08f + i / (mqC + 1)) % 1);
                        var marquee = new Vector2(float.Lerp(-hTextWidth, Window.Width + hTextWidth, mt), 40);
                        Draw.Text(c, marquee, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);

                        marquee.X = Window.Width - marquee.X;
                        marquee.Y = Window.Height - 40;
                        Draw.Text(c, marquee, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Bottom);
                    }
                }

                // draw disk name
                if (true)
                {
                    Draw.Font = Fonts.CascadiaMono;
                    Draw.FontSize = 72;
                    Draw.Colour = Colors.White;

                    char[] f = disk.DisplayName.ToCharArray();
                    const string substitutes = "qazwsxedcrfvtgbyhnujmikolp!@#$%^&*()+";

                    for (int i = 0; i < f.Length; i++)
                        if (Utilities.RandomFloat() > progress * float.Lerp(4, 10, Utilities.Hash(i * 0.15324f)))
                            f[i] = Utilities.PickRandom(substitutes);

                    var str = new string(f);
                    var strP = center - new Vector2(0, s / 2);

                    Draw.Colour = Colors.Red.WithAlpha(phase1).WithAlpha(0.1f);
                    Draw.Text(str,
                        strP + Utilities.RandomPointInCircle(0, 20), Vector2.One + 0.1f * new Vector2(Utilities.RandomFloat(0, 25), Utilities.RandomFloat(0, 0.5f)),
                        HorizontalTextAlign.Center, VerticalTextAlign.Middle);

                    Draw.Colour = Colors.White.WithAlpha(phase1);
                    Draw.Text(str, strP, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);
                }

                // draw disk description
                if (true)
                {
                    Draw.InsideMask();
                    Draw.Font = Fonts.Oxanium;
                    Draw.FontSize = 32;
                    var p = new Vector2(center.X, Window.Height - 150);
                    Draw.Colour = Colors.Red.WithAlpha(0.5f * phase1);

                    string expl = "Disks can be toggled in the main menu.";

                    Draw.Text(expl, p + Utilities.RandomPointInCircle(0, 5),
                        Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top, textBoxWidth: Window.Width / 2);
                    Draw.Colour = Colors.White * phase1;
                    Draw.Text(expl, p, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top, textBoxWidth: Window.Width / 2);
                }

                // draw flash vignette
                {
                    Draw.DisableMask();
                    Draw.ResetTexture();
                    Draw.Colour = Colors.Red * Easings.Cubic.In(Utilities.Clamp(1 - progress * 2)) * 0.8f;
                    Draw.BlendMode = BlendMode.Addition;
                    Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));
                }
            }
        }

        Time.TimeScale = 1;
    }
}