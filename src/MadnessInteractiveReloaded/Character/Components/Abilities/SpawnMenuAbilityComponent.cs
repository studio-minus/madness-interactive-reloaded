using MIR.Disks;
using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class SpawnMenuAbilityComponent : CharacterAbilityComponent
{
    public SpawnMenuAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Hold)
    {
    }

    public override string DisplayName => "Spawn menu";
    private float progress = 0;
    private float animTime = 0;
    private float seed = 0;
    private float allyPressTime = 0;
    private WeaponInstructions? chosen = null;

    public override AnimationConstraint Constraints => IsUsing ? AnimationConstraint.PreventWorldInteraction | AnimationConstraint.PreventAllAttacking | AnimationConstraint.FaceForwards : default;

    public override void StartAbility(AbilityParams a)
    {
        a.Time.TimeScale = 0.1f;
        seed = Utilities.RandomFloat(0, 10000);
        animTime = 0;

        Game.AudioRenderer.PlayOnce(Sounds.TimeFreezeStart, track: AudioTracks.UserInterface);
        Game.AudioRenderer.Play(Sounds.TimeFreezeLoop);

        MadnessUtils.Shake(90);
        MadnessUtils.Flash(Colors.Red.WithAlpha(0.8f), 0.25f);
    }

    public override void UpdateAbility(AbilityParams a)
    {
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.Imgui.OffsetLayer(-1);
        float time = a.Time.SecondsSinceLoadUnscaled * 0.2f;
        float shake = IsUsing ? float.Sin(time * 500) * 20 * float.Clamp(1 - animTime, 0, 1) : 0;
        float wobbleIntensity = 40;
        
        const float invDuration = 1 / 0.3f;

        if (progress > float.Epsilon)
        {
            var b = Easings.Cubic.Out(progress) * 0.8f;

            // draw background
            {
                Draw.BlendMode = BlendMode.Multiply;
                Draw.Colour = Colors.Red * b;
                Draw.Quad(new Rect(0, 0, Window.Width, Window.Height));
            }

            // draw lines and shit
            {
                const int streepCount = 5;
                Draw.BlendMode = BlendMode.Addition;
                Draw.Order = RenderOrders.CharacterLower.OffsetLayer(-1);
                Draw.ResetTransformation();
                Draw.Colour = Colors.Red * b * 0.3f;
                for (int i = 0; i < streepCount; i++)
                {
                    float s = seed + i;
                    float lineP = float.Clamp((animTime - Utilities.Hash(s * .591723f)) * 1.5f, 0, 1);

                    var pp = float.Pow(lineP, float.Lerp(0.2f, 4f, Utilities.Hash((s + 83.4123f) * -0.742534f)));
                    var pa = new Vector2(Utilities.Snap(Utilities.Hash((s + 123.41243f) * 0.742534f), 0.1f) * Window.Width, 0);
                    var pb = new Vector2(pa.X, Window.Height * pp);

                    Draw.Line(pa, pb, float.Lerp(2, 4, Utilities.Hash(s * .123f)));
                }
                for (int i = 0; i < streepCount; i++)
                {
                    float s = seed + i;
                    float lineP = float.Clamp((animTime - Utilities.Hash(s * .591723f)) * 1.5f, 0, 1);

                    var pp = float.Pow(lineP, float.Lerp(0.2f, 4f, Utilities.Hash((s + 6451.123f) * -0.145344f)));
                    var pa = new Vector2(0, Utilities.Snap(Utilities.Hash((s + 123.41243f) * 0.742534f), 0.1f) * Window.Height);
                    var pb = new Vector2(Window.Width * lineP, pa.Y);

                    Draw.Line(pa, pb, float.Lerp(2, 4, Utilities.Hash(s * .123f)));
                }

                Draw.FontSize = 24;
                Draw.Font = Fonts.CascadiaMono;
                Draw.Colour = Colors.Red * b * 0.4f;

                for (int i = 0; i < 2; i++)
                {
                    var codePos = new Vector2(
                        (Utilities.Hash((seed + i) * -.356f) % 0.2f) * Window.Width,
                        (Utilities.Hash((seed + i) * -.0356f) % 0.5f + 0.5f * i) * Window.Height);

                    Draw.TextDrawRatio = float.Clamp(animTime * 0.1f, 0, 1);
                    Draw.Text(FloatingStrings[(int)(Utilities.Hash((seed + i) * -.356f) * FloatingStrings.Length)], codePos, Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top);
                }

                Draw.TextDrawRatio = 1;
            }

            Draw.Order = RenderOrders.Imgui.OffsetLayer(-1);

            // draw and process weapon buttons
            {
                Draw.BlendMode = BlendMode.AlphaBlend;
                Draw.Colour = Utilities.Lerp(Colors.Black, new Color(1, 0.8f, 0.8f), progress);
                Draw.Colour.A = Easings.Quad.In(progress);
                var col = Draw.Colour;

                float scaling = 0.5f;
                float stretch = 1;
                var containerRect = new Rect(-Window.Width * stretch, 100, Window.Width * 2 * stretch, Window.Height * 0.5f);
                int index = 0;

                var allyButtonPos = new Vector2(Window.Width * 0.5f, 50);
                allyButtonPos.X += Noise.GetSimplex(time * 0.1f, 74.6234f, 0) * wobbleIntensity * 0.1f;
                allyButtonPos.Y += Noise.GetSimplex(-512.123f, 6.123f, time * 0.1f) * wobbleIntensity * 0.1f + shake;

                int columns = int.Clamp(Window.Width / 100, 1, 40);
                int rows = int.Max(1, Registries.Weapons.Count / columns);

                bool hoverAlly = Vector2.Distance(allyButtonPos, a.Input.WindowMousePosition) < 100;
                float allyBtnScale = Utilities.MapRange(0, 1, 0.8f, 1, Easings.Cubic.Out(Utilities.Clamp(allyPressTime)));
                Draw.Colour = (hoverAlly ? Colors.White : Colors.Red).WithAlpha(0.5f) * progress;
                Draw.FontSize = 48;
                Draw.Font = Fonts.Toxigenesis;
                Draw.TransformMatrix = Matrix3x2.CreateRotation(Noise.GetSimplex(0, 0, -time * 0.02f) * 0.1f, allyButtonPos) * Matrix3x2.CreateScale(allyBtnScale, allyButtonPos);
                Draw.Text("ALLY++", allyButtonPos, Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle);

                Draw.Colour = col;

                WeaponInstructions? hover = null;
                foreach (var wpn in Registries.Weapons.GetAllValues())
                {
                    index++;

                    var y = (index / columns) / (float)rows;

                    float t = time * float.Lerp(0.8f, 2f, Utilities.Hash((y + 412.4123f) * 0.19534f));
                    var x = Utilities.Mod((index % columns) / (float)columns - t * 0.1f, 1);

                    Draw.Texture = wpn.BaseTexture.Value;
                    var r = new Rect(default, Draw.Texture.Size * scaling);
                    r = r.Translate(
                        float.Lerp(containerRect.MinX, containerRect.MaxX, x),
                        float.Lerp(containerRect.MinY, containerRect.MaxY, y) + shake
                    );

                    r = r.Translate(
                        Noise.GetSimplex(time * 0.1f, index * 0.5123f, 0) * wobbleIntensity,
                        Noise.GetSimplex(time * 0.1f, 0, index * 0.5123f) * wobbleIntensity
                    );

                    var deltaToAllyBtn = allyButtonPos - r.GetCenter();
                    deltaToAllyBtn.Y *= 5;
                    var distToAllyBtn = deltaToAllyBtn.Length() * 5;
                    r = r.Translate(deltaToAllyBtn / -distToAllyBtn * 500);

                    const float padding = 200;
                    if (r.MaxX < -padding || r.MinX > Window.Width + padding)
                        continue;

                    Draw.TransformMatrix = Matrix3x2.CreateRotation(
                        Noise.GetSimplex(index, 0, time * 0.05f) * 0.2f + float.DegreesToRadians(wpn.OnFloorAngle),
                        r.GetCenter());

                    if (!hoverAlly)
                        if (r.Expand(10).ContainsPoint(a.Input.WindowMousePosition))
                            hover = wpn;

                    if (chosen == wpn)
                        Draw.Colour = Colors.Red.WithAlpha(col.A);
                    else
                        Draw.Colour = col;

                    Draw.Quad(r);

                    if (wpn.AnimatedParts != null)
                    {
                        var m = Draw.TransformMatrix;
                        foreach (var anim in wpn.AnimatedParts)
                        {
                            if (anim.VisibilityCurve != null)
                                continue;

                            Draw.Texture = anim.Texture.Value;
                            Draw.TransformMatrix = m;
                            var animRect = new Rect(r.GetCenter(), Draw.Texture.Size * scaling);

                            if (anim.TranslationCurve != null && anim.TranslationCurve.Keys.Length > 0)
                            {
                                var n = Noise.GetValue(t * 0.2f, 512.51234f, index * 0.51234f);
                                var offset = anim.TranslationCurve.Evaluate(n) * scaling;
                                offset.Y *= -1; // we're in screenspace
                                animRect = animRect.Translate(offset);
                            }

                            Draw.Quad(animRect);
                        }
                    }
                }

                chosen = hover;

                allyPressTime += a.Time.DeltaTimeUnscaled;

                if (IsUsing && a.Input.IsButtonReleased(MouseButton.Left))
                {
                    if (hoverAlly)
                    {
                        MadnessUtils.Flash(Colors.Red.WithAlpha(0.2f), 1);
                        MadnessUtils.Flash(Colors.White.WithAlpha(0.3f), 0.2f);
                        allyPressTime = 0;
                        var p = a.Character.Positioning.GlobalCenter;
                        p.X += Utilities.RandomFloat(-300, 300);
                        p.Y = 99999;
                        var scene = a.Scene;
                        var ally = Prefabs.CreateCharacter(scene, new CharacterPrefabParams
                        {
                            Faction = a.Character.Faction,
                            Bottom = p,
                            Name = "Ally :)",
                            Stats = Registries.Stats["engineer"],
                            Look = Registries.Looks["agent3"],
                        });
                        scene.AttachComponent(ally.Entity, new AiComponent()
                        {

                        });
                        ally.PlayAnimation(Animations.SpawnFromSky);
                        AuditorDisk.AddAuditorFire(scene, ally);
                    }
                    else if (chosen != null)
                    {
                        a.Character.DeleteHeldWeapon(a.Scene);
                        MadnessCommands.Give(chosen.Id);
                    }
                }
            }
        }

        if (IsUsing)
            animTime += a.Time.DeltaTimeUnscaled * invDuration;
        else
            animTime -= a.Time.DeltaTimeUnscaled * invDuration;

        progress = float.Clamp(animTime, 0, 1);
    }

    public override void EndAbility(AbilityParams a)
    {
        Game.AudioRenderer.Stop(Sounds.TimeFreezeLoop);
        a.Time.TimeScale = 1;
        animTime = 1;
    }

    
    public static readonly string[] FloatingStrings = [
        @"progress = float.Clamp(progress, 0, 1);
float anim = Easings.Cubic.Out(progress);
float s = float.Sin(a.Time * 4) * float.Abs(a.Character.Positioning.FlyingVelocity * 0.002f);
a.Character.Positioning.FlyingOffset = float.Lerp(0, Utilities.MapRange(-1, 1, 500, 600, s), anim);",

@"switch (ability.Behaviour)
{
    case AbilityBehaviour.Hold:
        ability.IsUsing = Input.ActionHeld(action);
        break;
    case AbilityBehaviour.Toggle:
        if (Input.ActionPressed(action))
            ability.IsUsing = !ability.IsUsing;
        break;
}",

@"vec2 newUv = vec2(uv.x + currentColumn, uv.y + currentRow);
newUv.x /= width;
newUv.y /= height;
    
color = vertexColor * texture(mainTex,  newUv);
color.a = mix(color.a, color.a > 0.5? 1 : 0, alphaClip);
color = color * tint;",

@"const float transitionFactorDuration = 0.2f;
var charPos = character.Positioning;
var wasPlayingAnimation = character.IsPlayingAnimation;
var mixed = CharacterUtilities.CalculateMixedAnimation(character);

var speedMultiplier = character.MainAnimation?.Speed ?? 1;
character.AnimationMixProgress = Utilities.Clamp(character.AnimationMixProgress + Time.DeltaTime / character.AnimationMixDuration * speedMultiplier);

// should the animation blend back into the non animated state instantly?
if (character.AnimationConstrainsAny(AnimationConstraint.PreventMixTransition))
    character.AnimationTransitionFactor = 1;
else
{
    if (character.MainAnimation == null || character.MainAnimation.UnscaledTimer > character.MainAnimation.Animation.TotalDuration - transitionFactorDuration)
        character.AnimationTransitionFactor -= Time.DeltaTime / transitionFactorDuration;
    else
        character.AnimationTransitionFactor += Time.DeltaTime / transitionFactorDuration;
}

character.AnimationTransitionFactor = Utilities.Clamp(character.AnimationTransitionFactor);
character.AnimationTransitionFactorEased = Easings.Quad.InOut(character.AnimationTransitionFactor);

if (!wasPlayingAnimation)
{
    character.ResetAnimation();
    return;
}
else
{
    // remove animations that are finished
    for (int i = character.Animations.Count - 1; i >= 0; i--)
    {
        var a = character.Animations[i];
        if (a.IsOver)
        {
            a.InvokeOnEnd();
            character.Animations.RemoveAt(i);
        }
        if (character.EnableAnimationClock)
            a.UnscaledTimer += Time.DeltaTime * a.Speed;
    }
}"
    ];

}
