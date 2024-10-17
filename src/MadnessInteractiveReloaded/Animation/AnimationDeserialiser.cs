using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// Deserializes animation files into <see cref="CharacterAnimation"/> objects.
/// </summary>
public static class AnimationDeserialiser
{
    public const string BodyIdentifier = "BODY";
    public const string HeadIdentifier = "HEAD";
    public const string HandIdentifier = "HAND";

    public const string FpsSpecifier = "fps ";
    public const string GlobalSpeedSpecifier = "speed ";
    public const string GlobalScaleSpecifier = "scale ";
    public const string AimFlag = "aim";
    public const string SmoothFlag = "smooth";
    public const string RelativeFlag = "relative";

    public const string FrameSuffix = "";
    public const string SecondSuffix = "s";
    public const string DegreeSuffix = "deg";
    public const string PixelSuffix = "px";

    private static readonly Regex TimePattern = new(@"(\d|\.)+" + SecondSuffix, RegexOptions.Compiled);
    private static readonly Regex FramePattern = new(@"\d+" + FrameSuffix, RegexOptions.Compiled);
    private static readonly Regex DegreePattern = new(@"-?(\d|\.)+" + DegreeSuffix, RegexOptions.Compiled);
    private static readonly Regex PixelPattern = new(@"-?(\d|\.)+" + PixelSuffix, RegexOptions.Compiled);
    private static readonly Regex HandPattern = new(@"([a-zA-Z]+)", RegexOptions.Compiled);
    private static readonly Regex IntegerPattern = new(@"-?\d+", RegexOptions.Compiled);
    private static readonly Regex FloatPattern = new(@"-?(\d|\.)+", RegexOptions.Compiled);
    private static readonly Regex ConstraintPattern = new(@"(constraints)\((\w|\ )*\)", RegexOptions.Compiled);
    private static readonly Regex LimbScalePattern = new(@"(scale)\((\d+\.?\d*),\s*(\d+\.?\d*)\)", RegexOptions.Compiled);
    private static readonly string[] AnimationConstraints;
    private static readonly string[] HandLooks;

    static AnimationDeserialiser()
    {
        AnimationConstraints = Enum.GetNames<AnimationConstraint>();
        HandLooks = Enum.GetNames<HandLook>();
    }

    /// <summary>
    /// TODO
    /// </summary>
    public static CharacterAnimation Load(Stream input, string name, string group)
    {
        var anim = new CharacterAnimation(name, group);
        List<KeyframeLine> currentKeyframes = new();
        LimbAnimation? animationToWriteTo = null;
        bool writingToHandAnimation = false;
        int handAnimationIndex = -1;
        int framerate = 24;
        float globalSpeed = 1;
        float globalScale = 1;
        int lineNumber = 0;

        using var file = new StreamReader(input);

        while (true)
        {
            lineNumber++;
            var line = file.ReadLine();
            if (line == null)
            {
                markEndOfAnimationData();
                break;
            }
            line = line.Trim();

            if (line.StartsWith('#'))//comments
                continue;

            switch (line)
            {
                case BodyIdentifier:
                    if (anim.BodyAnimation != null)
                        throw new Exceptions.SerialisationException($"double {BodyIdentifier} identifier found");
                    markEndOfAnimationData();
                    anim.BodyAnimation = new();
                    animationToWriteTo = anim.BodyAnimation;
                    writingToHandAnimation = false;
                    break;
                case HeadIdentifier:
                    markEndOfAnimationData();
                    if (anim.HeadAnimation != null)
                        throw new Exceptions.SerialisationException($"double {HeadIdentifier} identifier found");
                    anim.HeadAnimation = new();
                    animationToWriteTo = anim.HeadAnimation;
                    writingToHandAnimation = false;
                    break;
                case HandIdentifier:
                    handAnimationIndex++;
                    if (anim.HandAnimations == null)
                        anim.HandAnimations = new HandLimbAnimation[1];
                    else
                        Array.Resize(ref anim.HandAnimations, handAnimationIndex + 1);

                    markEndOfAnimationData();
                    animationToWriteTo = anim.HandAnimations[handAnimationIndex] = new HandLimbAnimation();
                    writingToHandAnimation = true;
                    break;
                case AimFlag:
                    if (animationToWriteTo == null)
                        throw new Exceptions.SerialisationException($"Aim flag found in an invalid place at line #{lineNumber}. An aim flag is only valid in an animation data block");
                    else
                        animationToWriteTo.AdjustForAim = true;
                    break;
                case SmoothFlag:
                    if (animationToWriteTo != null)
                        throw new Exceptions.SerialisationException($"Smooth flag found in an invalid place at line #{lineNumber}. A smooth flag is only valid outside an animation data block");
                    else
                        anim.DoSmoothing = true;
                    break;
                case RelativeFlag:
                    if (animationToWriteTo != null)
                        throw new Exceptions.SerialisationException($"Relative flag found in an invalid place at line #{lineNumber}. A relative flag is only valid outside an animation data block");
                    else
                        anim.RelativeHandPosition = true;
                    break;
                case "":
                    break;
                default:
                    if (line.StartsWith(FpsSpecifier)) //its an fps specifier
                    {
                        var fpsMatch = IntegerPattern.Match(line);
                        if (fpsMatch != null && fpsMatch.Success)
                            framerate = int.Parse(fpsMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                        else
                            throw new Exceptions.SerialisationException($"Framerate specifier is invalid at line #{lineNumber}. No positive integer could be found");
                        continue;
                    }

                    if (line.StartsWith(GlobalSpeedSpecifier)) //its a speed specifier
                    {
                        var speedMatch = FloatPattern.Match(line);
                        if (speedMatch != null && speedMatch.Success)
                            globalSpeed = float.Parse(speedMatch.Value, NumberStyles.Number, CultureInfo.InvariantCulture);
                        else
                            Logger.Warn($"Speed specifier is invalid at line #{lineNumber}. No float could be found");
                        continue;
                    }

                    if (line.StartsWith(GlobalScaleSpecifier)) //its a scale specifier
                    {
                        var scaleMatch = FloatPattern.Match(line);
                        if (scaleMatch != null && scaleMatch.Success)
                            globalScale = float.Parse(scaleMatch.Value, NumberStyles.Number, CultureInfo.InvariantCulture);
                        else
                            throw new Exceptions.SerialisationException($"Scale specifier is invalid at line #{lineNumber}. No float could be found");
                        continue;
                    }

                    if (animationToWriteTo != null) //its a keyframe
                    {
                        var keyframe = new KeyframeLine();

                        // 0s 0px 0px 0deg openHand scale(1,1) constraints(...)

                        // find time
                        var time = TimePattern.Match(line);
                        if (time == null || !time.Success)
                        {
                            //no time, check for frame specifier
                            var frame = FramePattern.Match(line);
                            if (frame != null && frame.Success)
                            {
                                int frameNumber = FrameStringToInt(frame.Value);
                                keyframe.Time = frameNumber / (float)framerate / globalSpeed;
                            }
                            else
                                throw new Exceptions.SerialisationException($"Invalid value at line #{lineNumber}: no time or frame specifier found");
                        }
                        else
                        {
                            keyframe.Time = TimeStringToFloat(time.Value) / globalSpeed;
                            if (keyframe.Time < 0)
                                throw new Exceptions.SerialisationException($"Invalid value at line #{lineNumber}: time specifiers need to be positive");
                        }

                        // find position
                        var pixelPatterns = PixelPattern.Matches(line);
                        if (pixelPatterns != null && pixelPatterns.Count > 0)
                        {
                            if (pixelPatterns.Count != 2)
                                throw new Exceptions.SerialisationException($"Invalid value at line #{lineNumber}: a keyframe can only have 0 or 2 position specifiers, one for each axis");

                            keyframe.Position = new Vector2(
                                PixelStringToFloat(pixelPatterns[0].Value),
                                PixelStringToFloat(pixelPatterns[1].Value)
                            );
                        }

                        // find rotation
                        var degreePatterns = DegreePattern.Match(line);
                        if (degreePatterns != null && degreePatterns.Success)
                            keyframe.Angle = DegreeStringToFloat(degreePatterns.Value);

                        // find hand specifier
                        var handPatterns = HandPattern.Matches(line);//TODO dit kan sneller
                        if (writingToHandAnimation)
                            if (handPatterns != null && handPatterns.Any())
                            {
                                if (!writingToHandAnimation)
                                    throw new Exceptions.SerialisationException($"Hand sprite specifiers are only valid inside {HandIdentifier} animations #{lineNumber}.");

                                // TODO dit is helemaal shit en langzaam
                                // Note: maakt dat uit? dit is een deserialiser. het is niet alsof dit elke frame gebeurt
                                for (int i = 0; i < HandLooks.Length; i++)
                                    if (handPatterns.Any(v => v.Value.Equals(HandLooks[i], StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        keyframe.HandSprite = (HandLook)i;
                                        break;
                                    }
                            }

                        // find constraints
                        var constraints = ConstraintPattern.Match(line);
                        if (constraints.Success)
                        {
                            var v = constraints.ValueSpan;
                            var inside = v[(v.IndexOf('(') + 1)..v.IndexOf(')')];
                            var entries = inside.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var f = AnimationConstraint.AllowAll;
                            foreach (var entry in entries)
                            {
                                for (int i = 0; i < AnimationConstraints.Length; i++)
                                    if (entry.Equals(AnimationConstraints[i], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (!Enum.TryParse<AnimationConstraint>(entry, out var flag))
                                            throw new Exceptions.SerialisationException($"Constraint is invalid at line #{lineNumber}. {entry} is not a valid animation constraint. Valid values are {string.Join(", ", AnimationConstraints)}");

                                        f |= flag;
                                        break;
                                    }
                            }
                            anim.Constraints.Add((keyframe.Time, f));
                        }

                        // find scale
                        var localScale = LimbScalePattern.Match(line);
                        if (localScale.Success)
                        {
                            var v = localScale.ValueSpan;
                            var inside = v[(v.IndexOf('(') + 1)..v.IndexOf(')')];
                            var parts = inside.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (parts.Length != 2)
                                throw new Exceptions.SerialisationException($"Limb scale is invalid at line #{lineNumber}. Two float parameters are expected, but {parts.Length} are given.");

                            if (!float.TryParse(parts[0], out var scaleX)) 
                                throw new Exceptions.SerialisationException($"Limb scale X is invalid at line #{lineNumber}. {parts[0]} is not a number.");

                            if (!float.TryParse(parts[1], out var scaleY)) 
                                throw new Exceptions.SerialisationException($"Limb scale Y is invalid at line #{lineNumber}. {parts[1]} is not a number.");

                            keyframe.Scale = new Vector2(scaleX, scaleY);
                        }

                        currentKeyframes.Add(keyframe);
                    }
                    break;
            }
        }

        void markEndOfAnimationData()
        {
            if (animationToWriteTo == null)
            {
                currentKeyframes.Clear();
                return;
            }

            float duration = -1;
            foreach (var keyframe in currentKeyframes)
                duration = float.Max(keyframe.Time, duration);

            animationToWriteTo.Duration = duration;
            int handLookIndex = 0;
            {
                if (writingToHandAnimation && animationToWriteTo is HandLimbAnimation handlimbAnim)
                {
                    var handLookCount = currentKeyframes.Count(static k => k.HandSprite.HasValue);
                    handlimbAnim.HandLooks = new (float timeInSeconds, HandLook? look)[handLookCount];
                }
            }

            foreach (var keyframe in currentKeyframes)
            {
                var scaledTime = keyframe.Time / duration;

                if (keyframe.Position.HasValue)
                {
                    animationToWriteTo.TranslationCurve ??= new Vec2Curve();

                    AppendKey(animationToWriteTo.TranslationCurve, new Curve<Vector2>.Key(keyframe.Position.Value * globalScale, scaledTime));
                }

                if (keyframe.Angle.HasValue)
                {
                    animationToWriteTo.RotationCurve ??= new AngleCurve();

                    AppendKey(animationToWriteTo.RotationCurve, new Curve<float>.Key(keyframe.Angle.Value, scaledTime));
                }

                if (keyframe.Scale.HasValue)
                {
                    animationToWriteTo.ScaleCurve ??= new Vec2Curve();
                    AppendKey(animationToWriteTo.ScaleCurve, new Curve<Vector2>.Key(keyframe.Scale.Value, scaledTime));
                }

                if (writingToHandAnimation && keyframe.HandSprite.HasValue && animationToWriteTo is HandLimbAnimation handlimbAnim && handlimbAnim.HandLooks != null)
                {
                    handlimbAnim.HandLooks[handLookIndex] = (scaledTime, keyframe.HandSprite);
                    handLookIndex++;
                }
            }
            currentKeyframes.Clear();
        }
        anim.Constraints.Sort((a, b) => a.Time == b.Time ? 0 : (a.Time > b.Time ? 1 : -1));
        anim.CalculateTotalDuration();
        return anim;
    }

    public class AssetDeserialiser : IAssetDeserialiser<CharacterAnimation>
    {
        public CharacterAnimation Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
        {
            var name = Path.GetFileNameWithoutExtension(assetMetadata.Path);
            var group = assetMetadata.Path[..^(Path.GetFileName(assetMetadata.Path).Length + 1)];
            var l = group.LastIndexOf('/');
            if (l != -1)
                group = group[(l + 1)..];
            return Load(stream(), name, group);
        }

        public bool IsCandidate(in AssetMetadata assetMetadata)
        {
            return assetMetadata.Path.EndsWith(".anim", StringComparison.InvariantCultureIgnoreCase);
        }
    }

    private static float TimeStringToFloat(string s) => float.Parse(s.AsSpan()[..^(SecondSuffix.Length)], NumberStyles.Number, CultureInfo.InvariantCulture);
    private static float DegreeStringToFloat(string s) => float.Parse(s.AsSpan()[..^(DegreeSuffix.Length)], NumberStyles.Number, CultureInfo.InvariantCulture);
    private static float PixelStringToFloat(string s) => float.Parse(s.AsSpan()[..^(PixelSuffix.Length)], NumberStyles.Number, CultureInfo.InvariantCulture);
    private static int FrameStringToInt(string s) => int.Parse(s.AsSpan()[..^(FrameSuffix.Length)], NumberStyles.Integer, CultureInfo.InvariantCulture);

    private struct KeyframeLine
    {
        public float Time;
        public Vector2? Position;
        public float? Angle;
        public HandLook? HandSprite;
        public Vector2? Scale;
    }

    private static void AppendKey<T>(Curve<T> curve, Curve<T>.Key key) where T : notnull
    {
        var keys = curve.Keys;
        Array.Resize(ref keys, curve.Keys.Length + 1);
        keys[curve.Keys.Length] = key;
        curve.Keys = keys;
    }
}
