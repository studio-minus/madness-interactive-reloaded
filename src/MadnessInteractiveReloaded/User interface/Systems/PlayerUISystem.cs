using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Localisation;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Draws the player UI.<br></br>
/// Weapon name, ammo, dodge meter and dodge exhaustion vignette.
/// </summary>
public class PlayerUISystem : Walgelijk.System
{
    private int lastAmmoCounter = 0;
    private float lastAmmoFlashCounter = 0;

    private float dodgeSmooth = 0;
    private int lastProgressIndex;
    private float lastProgressIndexFlashCounter = 0;

    public override void Render()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var character) || !character.IsAlive)
            return;
        if (MadnessUtils.IsCutscenePlaying(Scene))
            return;
        if (!Scene.FindAnyComponent<GameModeComponent>(out var gm))
            return;
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        lastAmmoFlashCounter += Time.DeltaTimeUnscaled;
        lastProgressIndexFlashCounter += Time.DeltaTimeUnscaled;
        var gameMode = gm.Mode;

        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.OutlineWidth = 0;
        Draw.Order = RenderOrders.UserInterface;

        float p = Utilities.Clamp(character.DodgeMeter / character.Stats.DodgeAbility);
        dodgeSmooth = Utilities.SmoothApproach(dodgeSmooth, p, 10, Time.DeltaTime);
        if (dodgeSmooth < 1)
        {
            Draw.OutlineWidth = 0;
            Draw.Texture = Assets.Load<Texture>("textures/red_vignette.png").Value;
            Draw.Colour = Utilities.Lerp(Colors.White, Colors.Transparent, dodgeSmooth);
            Draw.Quad(Vector2.Zero, Window.Size, 0, 0);
        }

        Draw.Colour = Colors.White;
        Draw.Font = Fonts.Toxigenesis;

        const float padding = 25;
        const float iconSize = 40;
        const float wpnHeight = 60;
        const float maxWpnWidth = 370;

        var c = new Vector2(padding, 0);
        bool hasWeapon = false;
        Draw.FontSize = iconSize;

        if (character.EquippedWeapon.TryGet(Scene, out var eq))
        {

            Draw.Colour = Colors.White;
            // draw weapon silhouette
            if (eq.RegistryKey != null && Registries.Weapons.TryGet(eq.RegistryKey, out var wpn))
            {
                hasWeapon = true;

                var baseTex = wpn.BaseTexture.Value;
                var aspectRatio = baseTex.Height / (float)baseTex.Width;

                bool flipped = wpn.WeaponData.WeaponType is WeaponType.Melee;
                if (flipped)
                    aspectRatio = 1 / aspectRatio;

                var wpnRect = flipped ?
                    new Rect(0, 0, wpnHeight, wpnHeight / aspectRatio).Translate(padding, padding) :
                    new Rect(0, 0, wpnHeight / aspectRatio, wpnHeight).Translate(padding, padding);

                if ((flipped ? wpnRect.Height : wpnRect.Width) > maxWpnWidth)
                {
                    // TODO this is so ugly please make it more elegant and nice
                    if (flipped)
                    {
                        wpnRect.Height = maxWpnWidth;
                        wpnRect.Width = maxWpnWidth * aspectRatio;
                    }
                    else
                    {
                        wpnRect.Width = maxWpnWidth;
                        wpnRect.Height = maxWpnWidth * aspectRatio;
                    }
                }

                float recoilEffect = 1 - float.Clamp(lastAmmoFlashCounter * 4f, 0, 1);
                float rot = recoilEffect * -0.05f * (Noise.GetSimplex(Time * 2, 452.123f, 0)) * wpn.WeaponData.Recoil;

                wpnRect = wpnRect.Translate(0, c.Y);
                wpnRect = wpnRect.Translate((MadnessUtils.Noise2D(Time * 2, 452.123f) + new Vector2(-0.5f, 0)) * 15 * recoilEffect);

                Draw.Material = Materials.BlackToWhiteOutline;
                if (flipped)
                {
                    Draw.TransformMatrix = Matrix3x2.CreateRotation(float.Pi / 2, wpnRect.BottomLeft);
                    wpnRect = wpnRect.Translate(0, -wpnRect.Height);
                }

                Draw.TransformMatrix *= Matrix3x2.CreateRotation(rot, wpnRect.GetCenter() with { X = wpnRect.MinX });

                Draw.Image(baseTex, wpnRect, ImageContainmentMode.Stretch);
                int i = 0;
                if (wpn.AnimatedParts != null)
                {
                    float wS = wpnRect.Width / baseTex.Width;
                    float hS = wpnRect.Height / baseTex.Height;
                    foreach (var part in wpn.AnimatedParts)
                    {
                        var rect = new Rect(wpnRect.GetCenter(), part.Texture.Value.Size).Scale(wS, hS);
                        if (part.TranslationCurve != null)
                        {
                            float s = 0;

                            if (eq.AnimatedParts != null
                                && eq.AnimatedParts.Length > i
                                && Scene.TryGetComponentFrom<WeaponPartAnimationComponent>(eq.AnimatedParts[i++], out var comp))
                            {
                                s = comp.CurrentPlaybackTime / part.Duration;
                            }

                            var n = part.TranslationCurve.Evaluate(s);
                            n.X *= wS;
                            n.Y *= -hS;
                            rect = rect.Translate(n);
                        }
                        Draw.Image(part.Texture.Value, rect, ImageContainmentMode.Stretch);
                    }
                }
                Draw.ResetMaterial();
                Draw.ResetTransformation();
            }

            c.Y += wpnHeight + padding * 2;


            Draw.FontSize = 55;
            Draw.Text(eq.Data.Name, c, new Vector2(0.6f), HorizontalTextAlign.Left, VerticalTextAlign.Top);
            c.Y += 40;

            Draw.FontSize = 24;

            switch (eq.Data.WeaponType)
            {
                case WeaponType.Firearm:

                    if (lastAmmoCounter != eq.RemainingRounds)
                    {
                        lastAmmoFlashCounter = 0;
                        lastAmmoCounter = eq.RemainingRounds;
                    }

                    if (eq.InfiniteAmmo)
                    {
                        Draw.Colour = Color.FromHsv(Time, 0.2f, 1);
                        Draw.Text(Localisation.Get("infinite-ammo"), c, new Vector2(1), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                    }
                    else
                    {
                        if (!eq.HasRoundsLeft)
                        {
                            Draw.Colour = float.Sin(Time.SecondsSinceLoadUnscaled * 24f) > 0 ? Colors.Red : Colors.White;
                            Draw.Text(Localisation.Get("empty"), c, new Vector2(1), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                        }
                        else
                        {
                            Draw.Colour = Vector4.Lerp(Colors.Red, Colors.White, float.Clamp(lastAmmoFlashCounter * 8f, 0, 1));
                            DrawCounter(c, eq.RemainingRounds, eq.Data.RoundsPerMagazine);
                        }
                    }
                    c.Y += 40;
                    break;
                default:
                    break;
            }
        }

        Draw.Colour = Colors.White;

        if (Level.CurrentLevel != null && Scene.FindAnyComponent<LevelProgressComponent>(out var progress))
        {
            switch (Level.CurrentLevel.ProgressionType)
            {
                case ProgressionType.Always:
                    break;
                case ProgressionType.BodyCount:
                    if (lastProgressIndex != progress.BodyCount.Current)
                    {
                        lastProgressIndexFlashCounter = 0;
                        lastProgressIndex = progress.BodyCount.Current;
                    }

                    Draw.FontSize = 48;

                    if (progress.BodyCount.Current == progress.BodyCount.Target)
                        Draw.Colour = Utilities.Lerp(Colors.White, Colors.White.WithAlpha(0.5f), Utilities.MapRange(-1, 1, 0, 0.5f, float.Sin(Time.SecondsSinceLoadUnscaled * 12)));
                    else
                        Draw.Colour = Utilities.Lerp(Colors.Red, Colors.White, Easings.Cubic.Out(float.Clamp(lastProgressIndexFlashCounter * 2f, 0, 1)));

                    DrawCounter(new Vector2(Window.Width - padding, padding), progress.BodyCount.Current, progress.BodyCount.Target, HorizontalTextAlign.Right);
                    var ir = new Rect(default, new Vector2(60, 60)).Translate(Window.Width - 235, 46);
                    Draw.Image(Assets.Load<Texture>("textures/ui/kills_icon.png").Value, ir, ImageContainmentMode.Contain);
                    break;
                case ProgressionType.Time:
                    break;
                case ProgressionType.Explicit:
                    break;
                default:
                    break;
            }

        }

        Draw.Font = Fonts.CascadiaMono;
        Draw.FontSize = 18;
        int abilityCursor = 0;
        var abilityColour = Colors.White.WithAlpha(0.7f);
        foreach (var item in Scene.GetAllComponentsFrom(character.Entity))
        {
            if (item is CharacterAbilityComponent characterAbility && characterAbility.Behaviour is not AbilityBehaviour.Always)
            {
                Draw.FontSize = 18;

                var col = abilityColour;
                var action = characterAbility.Slot.AsAction();
                var input = ControlScheme.ActiveControlScheme.InputMap[action].ToString();
                var inputWidth = Draw.CalculateTextWidth(input);
                var inputHeight = Draw.FontSize;

                const float horizontalExpansion = 5;
                var pos = new Vector2(padding, (abilityCursor++) * 40 + padding + (hasWeapon ? (c.Y) : 0));
                if (characterAbility.IsUsing)
                    col.A = 1;

                var rect = new Rect(pos.X, pos.Y, inputWidth + pos.X + horizontalExpansion, inputHeight + pos.Y).Expand(5).Translate(0, -2);
                pos.X += horizontalExpansion * 0.5f;

                Draw.Colour = col with { A = abilityColour.A * 0.1f };
                Draw.OutlineColour = col;
                Draw.OutlineWidth = 4;
                Draw.Quad(rect, roundness: 5);

                Draw.Colour = col;
                Draw.Text(input, pos, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);

                Draw.FontSize = 16;
                Draw.Colour = col;
                Draw.Text(characterAbility.DisplayName, pos + new Vector2(inputWidth + 20, 0), Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);
            }
        }
    }

    private static void DrawCounter(Vector2 p, int c, int max, HorizontalTextAlign alignment = HorizontalTextAlign.Left)
    {
        const float smallScale = 0.7f;
        const float padding = 5;
        var s = c > (int.MaxValue - 10000) ? "Inf" : $"{c:00}";

        float charWidth = Draw.CalculateTextWidth("5");

        switch (alignment)
        {
            case HorizontalTextAlign.Right:
                {
                    var s2 = $"/ {max:00}";
                    Draw.Text(s2, p, new Vector2(smallScale), HorizontalTextAlign.Right, VerticalTextAlign.Top);
                    p.X -= Draw.CalculateTextWidth(s2) * smallScale + padding * 2;
                    Draw.Text(s, p, Vector2.One, HorizontalTextAlign.Right, VerticalTextAlign.Top);
                }
                break;
            default:
                {
                    Draw.Text(s, p, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                    p.X += s.Length * charWidth + padding;
                    Draw.Text($"/ {max:00}", p, new Vector2(smallScale), HorizontalTextAlign.Left, VerticalTextAlign.Top);
                }
                break;
        }

        static float snapCeil(float x, float s) => float.Ceiling(x / s) * s;
    }

    private void DrawCrosshair(CharacterComponent character)
    {
        // calculate distance to nearest enemy
        // TODO this might be far too slow... we'll see
        float distToNearestEnemy = ConVars.Instance.InaccuracyMaxDistance * ConVars.Instance.InaccuracyMaxDistance;
        foreach (var other in Scene.GetAllComponentsOfType<CharacterComponent>())
            if (other.IsAlive && !other.Faction.IsAlliedTo(character.Faction))
            {
                var dist = Vector2.DistanceSquared(other.Positioning.Head.GlobalPosition, character.AimTargetPosition);
                distToNearestEnemy = MathF.Min(dist, distToNearestEnemy);
            }

        Draw.ScreenSpace = false;
        var accuracy = MadnessUtils.GetAimAccuracy(MathF.Sqrt(distToNearestEnemy));
        const float maxSize = 200;
        const float minSize = 30;
        var s = Utilities.Lerp(maxSize * 2 + 8, minSize * 2 - 5, accuracy);
        var r = new Rect(character.AimTargetPosition, new Vector2(s));
        Draw.Colour = Colors.Transparent;
        Draw.OutlineColour = Colors.Green;
        Draw.OutlineWidth = 16 * Math.Max(accuracy, 0.5f);
        Draw.Quad(r, 0, s);
        float o = 8 * Math.Max(accuracy, 0.5f);
        Draw.Line(character.AimTargetPosition + new Vector2(0, minSize), character.AimTargetPosition + new Vector2(0, maxSize), o, 0);
        Draw.Line(character.AimTargetPosition + new Vector2(minSize, 0), character.AimTargetPosition + new Vector2(maxSize, 0), o, 0);
        Draw.Line(character.AimTargetPosition - new Vector2(0, minSize), character.AimTargetPosition - new Vector2(0, maxSize), o, 0);
        Draw.Line(character.AimTargetPosition - new Vector2(minSize, 0), character.AimTargetPosition - new Vector2(maxSize, 0), o, 0);
        Draw.ScreenSpace = true;
    }
}
