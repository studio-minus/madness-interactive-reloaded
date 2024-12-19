using MIR.LevelEditor.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Walgelijk;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Loads of helpful functions and extension methods.
/// </summary>
public static class MadnessUtils
{
    private static bool IsBusyTransitioning = false; // TODO not the best place for this

    public static void EquipStoredWeapon(Level level, Scene scene, CharacterComponent charComponent)
    {
        if (!TryGetStartWeaponForLevel(level, out var wpn))
            wpn = SharedLevelData.EquippedWeaponPortal;

        if (wpn.HasValue)
        {
            if (!Registries.Weapons.Has(wpn.Value.Key))
                Logger.Warn($"The weapon wielded in the last level does not exist: '{wpn.Value.Key}'");
            else
            {
                var weapon = Prefabs.CreateWeapon(scene, default, Registries.Weapons.Get(wpn.Value.Key));

                charComponent.DeleteHeldWeapon(scene);
                charComponent.EquipWeapon(scene, weapon);

                weapon.RemainingRounds = Utilities.Clamp(wpn.Value.Ammo, 0, weapon.Data.RoundsPerMagazine);
                weapon.InfiniteAmmo = wpn.Value.InfiniteAmmo;
            }
        }

        SharedLevelData.EquippedWeaponPortal = default;
    }

    public static bool TryGetStartWeaponForLevel(Level level, out PersistentEquippedWeapon? weapon)
    {
        weapon = null;

        var sp = level.Objects.OfType<PlayerSpawn>().FirstOrDefault()?.SpawnWeapon;
        if (sp != null && Registries.Weapons.TryGet(sp, out var wpn))
        {
            weapon = new PersistentEquippedWeapon
            {
                Key = sp,
                Ammo = wpn.WeaponData.RoundsPerMagazine
            };
            return true;
        }

        if (CampaignProgress.TryGetCurrentStats(out var s) && s.ByLevel.TryGetValue(level.Id, out var lvs))
        {
            weapon = lvs.EquippedWeapon;
            return true;
        }

        return false;
    }

    public static void OpenExplorer(string path)
    {
        string executable;

        if (OperatingSystem.IsWindows())
            executable = "explorer";
        else if (OperatingSystem.IsMacOS())
            executable = "open";
        else if (OperatingSystem.IsLinux())
            executable = "xdg-open";
        else return;

        System.Diagnostics.Process.Start(executable, path);
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(url);
        }
        catch (Exception)
        {
            if (OperatingSystem.IsWindows())
            {
                url = url.Replace("&", "^&");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (OperatingSystem.IsMacOS())
                System.Diagnostics.Process.Start("open", url);
            else if (OperatingSystem.IsLinux())
                System.Diagnostics.Process.Start("xdg-open", url);
            else
                throw;
        }
    }

    public static Matrix4x4 Invert(this Matrix4x4 matrix)
    {
        if (Matrix4x4.Invert(matrix, out var inverted))
            return inverted;
        return matrix;
    }

    public static Matrix3x2 Invert(this Matrix3x2 matrix)
    {
        if (Matrix3x2.Invert(matrix, out var inverted))
            return inverted;
        return matrix;
    }

    /// <summary>
    /// Returns true if a cutscene is currently playing
    /// </summary>
    public static bool IsCutscenePlaying(Scene scene) => scene.FindAnyComponent<CutsceneComponent>(out var c) && !c.IsComplete;

    public static ReadOnlySpan<char> ToHexCode(this Color color)
    {
        var toFill = new char[9];

        var bytes = color.ToBytes();
        toFill[0] = '#';

        convert(bytes.r, out char a, out char b);
        toFill[1] = char.ToLower(a);
        toFill[2] = char.ToLower(b);

        convert(bytes.g, out a, out b);
        toFill[3] = char.ToLower(a);
        toFill[4] = char.ToLower(b);

        convert(bytes.b, out a, out b);
        toFill[5] = char.ToLower(a);
        toFill[6] = char.ToLower(b);

        convert(bytes.a, out a, out b);
        toFill[7] = char.ToLower(a);
        toFill[8] = char.ToLower(b);

        return toFill;

        static void convert(byte x, out char a, out char b)
        {
            var s = x.ToString("x2");
            a = s.Length == 2 ? s[0] : '0';
            b = s.Length == 2 ? s[1] : s[0];
        }
    }

    /// <summary>
    /// Returns a value from 0 to 1 where 1 is perfectly accurate and 0 is beyond the tolerated radius
    /// </summary>
    [Obsolete]
    public static float GetAimAccuracy(float distanceFromTargetToImpact)
    {
        return 1 - Utilities.Clamp(distanceFromTargetToImpact / ConVars.Instance.InaccuracyMaxDistance);
    }

    public static float LerpRadians(float startAngle, float endAngle, float t)
    {
        float delta = endAngle - startAngle;
        if (delta > float.Pi)
            delta -= float.Tau;
        else if (delta < -float.Pi)
            delta += float.Tau;
        return startAngle + delta * t;
    }

    public static float ClampRadians(float angle, float min, float max)
    {
        angle = NormaliseRadians(angle, float.Tau);
        min = NormaliseRadians(min, float.Tau);
        max = NormaliseRadians(max, float.Tau);

        if (max < min)
            max += float.Tau;

        if (angle < min)
            angle += float.Tau;
        else if (angle > max)
            angle -= float.Tau;

        return float.Clamp(angle, min, max);
    }

    public static float NormaliseRadians(float angle)
    {
        return float.DegreesToRadians(float.RadiansToDegrees(angle));
    }

    private static float NormaliseRadians(float angle, float modulus)
    {
        angle %= modulus;
        if (angle < 0)
            angle += modulus;
        return angle;
    }

    /// <summary>
    /// Get floor height or a fallback
    /// </summary>
    /// <returns></returns>
    public static float GetFloorLevelAt(float x, float fallback = 0)
    {
        if (Level.CurrentLevel != null)
            return Level.CurrentLevel.GetFloorLevelAt(x);
        return fallback;
    }

    public static bool IsTargetedByAccurateShot(Scene scene, Entity character)
    {
        if (character != Entity.None && scene.FindAnyComponent<AccurateShotComponent>(out var a))
            return a.TargetCharacter.Entity == character;
        return false;
    }

    public static void EmitBulletImpactParticles(Scene scene, Vector2 point, Vector2 direction)
    {
        if (!scene.GetSystem<PrefabPoolSystem>().TryRequest(PoolablePrefabs.BulletImpact, out var entity))
            return;

        var s = Utilities.RandomFloat(150, 256);
        var transform = scene.GetComponentFrom<TransformComponent>(entity);
        transform.Position = point + direction * (s / 2);
        transform.Rotation = Utilities.VectorToAngle(direction) - 90;
        transform.Scale = new Vector2(s);
        transform.RecalculateModelMatrix(Matrix3x2.Identity);
        var flipbook = scene.GetComponentFrom<FlipbookComponent>(entity);
        flipbook.CurrentTime = 0;
        scene.GetComponentFrom<QuadShapeComponent>(entity).Visible = true;
    }

    public static string NormalisePath(in string s)
    {
        var c = s.ToCharArray();
        NormalisePath(c);
        return new string(c);
    }

    public static string NormalisePath(in ReadOnlySpan<char> s)
    {
        var c = s.ToArray();
        NormalisePath(c);
        return new string(c);
    }

    public static void NormalisePath(Span<char> s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\' || c == '/')
                s[i] = Path.DirectorySeparatorChar;
        }
    }

    public static Rect GetBounds(this TransformComponent transform)
    {
        Rect r = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

        var a = Vector2.Transform(new Vector2(0.5f, 0.5f), transform.LocalToWorldMatrix);
        var b = Vector2.Transform(new Vector2(-0.5f, 0.5f), transform.LocalToWorldMatrix);
        var c = Vector2.Transform(new Vector2(0.5f, -0.5f), transform.LocalToWorldMatrix);
        var d = Vector2.Transform(new Vector2(-0.5f, -0.5f), transform.LocalToWorldMatrix);

        r.MinX = MathF.Min(r.MinX, MathF.Min(a.X, MathF.Min(b.X, MathF.Min(c.X, d.X))));
        r.MinY = MathF.Min(r.MinY, MathF.Min(a.Y, MathF.Min(b.Y, MathF.Min(c.Y, d.Y))));

        r.MaxX = MathF.Max(r.MaxX, MathF.Max(a.X, MathF.Max(b.X, MathF.Max(c.X, d.X))));
        r.MaxY = MathF.Max(r.MaxY, MathF.Max(a.Y, MathF.Max(b.Y, MathF.Max(c.Y, d.Y))));

        return r;
    }

    public static Rect FitImageRect(IReadableTexture texture, Rect rect, ImageContainmentMode containmentMode)
    {
        var textureSize = texture.Size;
        var size = rect.GetSize();
        var topLeft = rect.BottomLeft;

        Vector2 imageSize;
        Vector2 imagePos = default;

        switch (containmentMode)
        {
            case ImageContainmentMode.Stretch:
                imageSize = size;
                break;
            case ImageContainmentMode.Contain:
            case ImageContainmentMode.Cover:
                var aspectRatio = textureSize.X / textureSize.Y;

                imageSize = size;
                bool a = size.X / aspectRatio > size.Y;

                if (containmentMode == ImageContainmentMode.Contain)
                    a = !a;

                if (a)
                    imageSize.Y = size.X / aspectRatio;
                else
                    imageSize.X = size.Y * aspectRatio;

                imagePos = size / 2 - imageSize / 2;
                break;
            case ImageContainmentMode.Center:
                imageSize = textureSize;
                imagePos = size / 2 - imageSize / 2;
                break;
            default:
            case ImageContainmentMode.OriginalSize:
                imageSize = textureSize;
                break;
        }

        return new Rect(topLeft.X + imagePos.X, topLeft.Y + imagePos.Y, topLeft.X + imagePos.X + imageSize.X, topLeft.Y + imagePos.Y + imageSize.Y);
    }

    public static bool IsPaused(Scene scene)
    {
        if (scene.FindAnyComponent<PauseComponent>(out var c))
            return c.Paused;
        return false;
    }

    public static bool EditingInExperimentMode(Scene scene)
    {
        if (scene.FindAnyComponent<ExperimentModeComponent>(out var exp))
            return exp.IsEditMode;
        return false;
    }

    public static void Flash(Color colour, float durationInSeconds)
    {
        //TODO dit kan mooier dus
        Draw.Reset();
        Draw.ScreenSpace = true;
        Draw.Order = RenderOrders.UserInterface.WithOrder(5);
        Draw.Colour = colour;
        Draw.Quad(Vector2.Zero, Game.Main.Window.Size);

        float t = 0;
        RoutineForSeconds(durationInSeconds, dt =>
        {
            Draw.Reset();
            Draw.ScreenSpace = true;
            Draw.Order = RenderOrders.UserInterface.OffsetLayer(1);
            Draw.Colour = Utilities.Lerp(colour, colour.WithAlpha(0), t);
            Draw.Quad(Vector2.Zero, Game.Main.Window.Size);

            t += dt / durationInSeconds / Game.Main.State.Time.TimeScale;
        });
    }

    public static void Shake(float v)
    {
        if (Game.Main.Scene.FindAnyComponent<CameraShakeComponent>(out var shake))
            shake.ShakeIntensity += v;
    }

    public static Vector2 Noise2DNormalised(float time, float seed)
    {
        return new Vector2(
            Noise.GetValue(time, seed, 0) * 0.5f + 0.5f,
            Noise.GetValue(time, -seed, 37.29372f) * 0.5f + 0.5f
        );
    }

    public static Vector2 Noise2D(float time, float seed)
    {
        return new Vector2(
            Noise.GetValue(time, seed, 0),
            Noise.GetValue(time, -seed, 37.29372f)
        );
    }

    public static bool IsPlayerAlive(Scene scene)
    {
        if (scene.FindAnyComponent<PlayerComponent>(out var player) &&
            scene.TryGetComponentFrom<CharacterComponent>(player.Entity, out var character))
            return character.IsAlive;
        return false;
    }

    public static bool FindPlayer(Scene scene,
        [NotNullWhen(true)] out PlayerComponent? player,
        [NotNullWhen(true)] out CharacterComponent? character)
    {
        if (scene.FindAnyComponent(out player) && scene.TryGetComponentFrom(player.Entity, out character))
            return true;

        player = null;
        character = null;
        return false;
    }

    public static bool FindCamera(Scene scene, out CameraComponent? camera, out TransformComponent? transform)
    {
        if (scene.FindAnyComponent(out camera) && scene.TryGetComponentFrom(camera.Entity, out transform))
            return true;

        camera = null;
        transform = null;
        return false;
    }

    public static bool TransitionScene(Func<Game, Scene> creationFunction, bool ignoreBusyState = false)
    {
        if (IsBusyTransitioning && !ignoreBusyState)
            return false;

        IsBusyTransitioning = true;

        if (Level.CurrentLevel == null || Level.CurrentLevel.ExitingTransition)
        {
            var originalScene = Game.Main.Scene;
            Game.Main.AudioRenderer.Play(Sounds.Scenesweep);
            var t = Prefabs.CreateSceneTransition(originalScene, Transition.Exit);
            WaitUntil(() => t.IsComplete, () =>
            {
                IsBusyTransitioning = false;
                originalScene.RemoveEntity(t.Entity);

                var scene = Game.Main.Scene = creationFunction(Game.Main);
                if (scene.FindAnyComponent<BackgroundOffsetAnimationComponent>(out var compo))
                {
                    compo.IsPlaying = true;
                    compo.IsComplete = false;
                    compo.CurrentPlaybackTime = 0;
                }
            });
        }
        else
        {
            Delay(0, () =>
            {
                Game.Main.Scene = creationFunction(Game.Main);
                IsBusyTransitioning = false;
            });
        }
        return true;
    }

    public static Routine Delay(float seconds, Action action)
    {
        return RoutineScheduler.Start(delay());

        IEnumerator<IRoutineCommand> delay()
        {
            yield return new RoutineDelay(seconds);
            action?.Invoke();
        }
    }

    public static Routine WaitUntil(Func<bool> condition, Action action)
    {
        return RoutineScheduler.Start(waitUntil());

        IEnumerator<IRoutineCommand> waitUntil()
        {
            yield return new RoutineWaitUntil(condition);
            action?.Invoke();
        }
    }

    public delegate void DeltaTimeAction(float dt);

    public static Routine RoutineForSeconds(float seconds, DeltaTimeAction action)
    {
        return RoutineScheduler.Start(repeatForSeconds());

        IEnumerator<IRoutineCommand> repeatForSeconds()
        {
            float t = 0;
            while (t < seconds)
            {
                t += Game.Main.State.Time.DeltaTime;
                action?.Invoke(Game.Main.State.Time.DeltaTime);
                yield return new RoutineFrameDelay();
            }
        }
    }

    public static Routine RoutineWhile(Func<bool> condition, DeltaTimeAction action)
    {
        return RoutineScheduler.Start(routineUntil());

        IEnumerator<IRoutineCommand> routineUntil()
        {
            while (condition())
            {
                action?.Invoke(Game.Main.State.Time.DeltaTime);
                yield return new RoutineFrameDelay();
            }
        }
    }

    public static Routine DelayPausable(float seconds, Action action)
    {
        var sceneOnStart = Game.Main.Scene.Id;

        return RoutineScheduler.Start(delay());

        IEnumerator<IRoutineCommand> delay()
        {
            yield return new GameSafeRoutineDelay(seconds);
            if (Game.Main.Scene.Id == sceneOnStart)
                action?.Invoke();
        }
    }

    public static Routine RoutineForSecondsPausable(float seconds, DeltaTimeAction action)
    {
        var sceneOnStart = Game.Main.Scene.Id;
        return RoutineScheduler.Start(repeatForSeconds());

        IEnumerator<IRoutineCommand> repeatForSeconds()
        {
            float t = 0;
            while (t < seconds)
            {
                if (Game.Main.Scene.Id != sceneOnStart)
                    yield break;

                t += Game.Main.State.Time.DeltaTime;
                action?.Invoke(Game.Main.State.Time.DeltaTime);
                yield return new GameSafeRoutineDelay();
            }
        }
    }

    public static Routine RoutineWhilePausable(Func<bool> condition, DeltaTimeAction action)
    {
        var sceneOnStart = Game.Main.Scene.Id;
        return RoutineScheduler.Start(routineUntil());

        IEnumerator<IRoutineCommand> routineUntil()
        {
            while (condition())
            {
                if (Game.Main.Scene.Id != sceneOnStart)
                    yield break;

                action?.Invoke(Game.Main.State.Time.DeltaTime);
                yield return new GameSafeRoutineDelay();
            }
        }
    }

    public static float DtLerp(float factor, float dt)
    {
        return 1 - MathF.Exp(-factor * dt);
    }

    public static float NormalisedSineWave(float x) => MathF.Sin(6.2831853f * x + 4.71238898f) * 0.5f + 0.5f;

    public static float TimeSafeRandom(float seed = 0)
    {
        return Noise.GetValue(Game.Main.State.Time.SecondsSinceLoad * 31.32486f, seed, -seed * 34.459f) * 0.5f + 0.5f;
    }

    public static void SetCollisionLayer(Scene scene, Entity entity, uint layer)
    {
        if (scene.HasEntity(entity))
        {
            var body = scene.GetComponentFrom<PhysicsBodyComponent>(entity);
            body.FilterBits = layer;
        }
    }

    public static ReadOnlySpan<char> EatQuotedString(ReadOnlySpan<char> text, out ReadOnlySpan<char> remaining)
    {
        int startIndex = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (startIndex != -1)
            {
                if (i > startIndex + 1 && text[i] == '"' && text[i - 1] != '\\') //unescaped quote
                {
                    var a = text;
                    remaining = a[(i + 1)..];
                    return a[(startIndex + 1)..i];
                }
            }
            else if (text[i] == '"')
                startIndex = i;
        }

        remaining = text;
        return ReadOnlySpan<char>.Empty;
    }

    public static ReadOnlySpan<char> EatStringUntil(ReadOnlySpan<char> text, char delimiter,
        out ReadOnlySpan<char> remaining)
    {
        if (text.Length > 0)
            for (int i = 1; i < text.Length; i++)
            {
                var r = text[0..i];
                if (text[i] == delimiter)
                {
                    var a = text;
                    remaining = text[(i + 1)..];
                    return r;
                }
            }

        remaining = ReadOnlySpan<char>.Empty;
        return text;
    }

    public static ReadOnlySpan<char> EatStringUntil(ReadOnlySpan<char> text, Func<char, bool> predicate,
        out ReadOnlySpan<char> remaining)
    {
        if (text.Length > 0)
            for (int i = 1; i < text.Length; i++)
            {
                if (predicate(text[i]))
                {
                    remaining = text[(i + 1)..];
                    return text[0..i];
                }
            }

        remaining = ReadOnlySpan<char>.Empty;
        return text;
    }

    public static int EatInt(ReadOnlySpan<char> text, out ReadOnlySpan<char> remaining)
    {
        int startIndex = -1;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (startIndex != -1)
            {
                if (!char.IsDigit(text[i]))
                {
                    var a = text;
                    remaining = a[(i + 1)..];
                    var n = a[startIndex..i];
                    return int.Parse(n, NumberStyles.Integer);
                }
            }
            else if (char.IsDigit(c))
                startIndex = i;
        }

        remaining = text;
        return 0;
    }

    public static float EatFloat(ReadOnlySpan<char> text, out ReadOnlySpan<char> remaining)
    {
        const char decimalPoint = '.';
        int startIndex = -1;
        bool decimalPointFound = false;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (startIndex != -1)
            {
                if (!char.IsDigit(text[i]) && !(text[i] == decimalPoint && !decimalPointFound))
                {
                    var a = text;
                    remaining = a[(i + 1)..];
                    var n = a[startIndex..i];
                    return float.Parse(n, NumberStyles.Number);
                }
            }
            else if (c == decimalPoint || char.IsDigit(c))
            {
                decimalPointFound = c == decimalPoint;
                startIndex = i;
            }
        }

        remaining = text;
        return float.NaN;
    }

    public static void EjectHeadDecorations(Scene scene, CharacterComponent character)
    {
        if (!scene.HasEntity(character.Positioning.Head.Entity))
            return;

        if (!scene.TryGetComponentFrom<MeasuredVelocityComponent>(character.Positioning.Head.Entity, out var headVelocity))
            return;

        foreach (var decor in character.Positioning.HeadDecorations)
        {
            if (!scene.GetComponentFrom<QuadShapeComponent>(decor).Visible)
                continue;

            var armor = scene.GetComponentFrom<ArmourComponent>(decor);
            if (!armor.Ejected && (armor.Piece?.Detachable ?? false))
            {
                armor.Ejected = true;
                var transform = scene.GetComponentFrom<TransformComponent>(decor);
                scene.GetComponentFrom<TransformConstraintComponent>(decor).Enabled = false;
                scene.AttachComponent(decor, new PhysicsBodyComponent()
                {
                    BodyType = BodyType.Dynamic,
                    FilterBits = CollisionLayers.Default,
                    Collider = new CircleCollider(transform)
                });
                scene.AttachComponent(decor, new VelocityComponent(transform)
                {
                    Enabled = true,
                    OverrideVelocity = headVelocity.DeltaTranslation + new Vector2(0, Utilities.RandomFloat(15, 54)),
                    RotationalVelocity = headVelocity.DeltaRotation + Utilities.RandomFloat(-360, 360),
                });
            }
        }
    }

    public static void TurnIntoRagdoll(Scene scene, CharacterComponent character, Vector2 addVelocity = default, float addTorque = default)
    {
        if (character.IsAlive || character.HasBeenRagdolled || character.Flags.HasFlag(CharacterFlags.NoRagdoll))
            return;

        // TODO update feet positions (DO NOT call CharacterSystem.Update(), it is not a fix)
        var center = character.Positioning.Body.ComputedVisualCenter;

        bool isPlayer = scene.HasComponent<PlayerComponent>(character.Entity);
        character.HasBeenRagdolled = true;

        if (character.HasWeaponEquipped)
            character.DropWeapon(scene);

        var targetRenderOrder = isPlayer ? RenderOrders.PlayerRagdoll.Layer : Utilities.RandomInt(RenderOrders.RagdollsLower.Layer, RenderOrders.RagdollsUpper.Layer);
        //var offset = Utilities.MapRange(RenderOrders.CharacterLower.Layer, RenderOrders.CharacterUpper.Layer, 0, 80, character.BaseRenderOrder.Layer);

        character.WalkAcceleration = Vector2.Zero;

        if (scene.TryGetComponentFrom<QuadShapeComponent>(character.Positioning.Body.Entity, out var quad))
        {
            quad.Color = Colors.White;
            quad.RenderOrder = quad.RenderOrder with { Layer = targetRenderOrder };
        }
        if (scene.TryGetComponentFrom<QuadShapeComponent>(character.Positioning.Head.Entity, out quad))
        {
            quad.Color = Colors.White;
            quad.RenderOrder = quad.RenderOrder with { Layer = targetRenderOrder };
        }

        foreach (var item in character.Positioning.BodyDecorations)
        {
            if (scene.TryGetComponentFrom<QuadShapeComponent>(item, out var renderer) && renderer.Visible)
            {
                renderer.RenderOrder = renderer.RenderOrder with { Layer = targetRenderOrder };
                renderer.Color = Colors.White;
            }
        }

        foreach (var item in character.Positioning.HeadDecorations)
        {
            var renderer = scene.GetComponentFrom<QuadShapeComponent>(item);
            if (renderer.Visible)
            {
                renderer.RenderOrder = renderer.RenderOrder with { Layer = targetRenderOrder };
                renderer.Color = Colors.White;
            }
        }

        for (int i = 0; i < character.Positioning.Hands.Length; i++)
        {
            HandAnimatedLimb? hand = character.Positioning.Hands[i];
            hand.Look = HandLook.Open;
            var renderer = scene.GetComponentFrom<QuadShapeComponent>(hand.Entity);
            renderer.Material = Textures.Character.GetMaterialForHandLook(character.Look.Hands, hand.Look,
                character.Positioning.IsFlipped ^ hand.IsLeftHand, WeaponType.Firearm);
            renderer.RenderOrder = renderer.RenderOrder with { Layer = targetRenderOrder };
            renderer.Color = Colors.White;
        }

        foreach (var foot in character.Positioning.Feet)
        {
            var footTransform = scene.GetComponentFrom<TransformComponent>(foot.Entity);
            scene.AttachComponent(foot.Entity, new TransformConstraintComponent
            {
                Other = new ComponentRef<TransformComponent>(character.Positioning.Body.Entity),
                LockRotation = true,
                RotationOffset = 0,

                LockPosition = true,
                PositionOffset = Vector2.Transform(footTransform.Position,
                    scene.GetComponentFrom<TransformComponent>(character.Positioning.Body.Entity).WorldToLocalMatrix)
            });

            var renderer = scene.GetComponentFrom<QuadShapeComponent>(foot.Entity);
            renderer.Color = Colors.White;
            renderer.RenderOrder = renderer.RenderOrder with { Layer = targetRenderOrder };
        }

        if (Utilities.RandomFloat() > 0.8f)
            EjectHeadDecorations(scene, character);

        var ragdoll = scene.AttachComponent(character.Entity, new RagdollComponent());
        ragdoll.Main = character.Entity;
        ragdoll.ShouldDelete = character.Flags.HasFlag(CharacterFlags.DeleteRagdoll);

        //var visualOffset = new Vector2(0, Utilities.RandomFloat(-5,5));
        foreach (var component in Ragdoll.BuildDefaultRagdoll(scene, character))
        {
            switch (component)
            {
                case VerletNodeComponent node:
                    ragdoll.Nodes.Add(new ComponentRef<VerletNodeComponent>(node.Entity));
                    var rotAcc = (node.Position - center);
                    rotAcc = new Vector2(-rotAcc.Y, rotAcc.X);
                    rotAcc = Vector2.Normalize(rotAcc);
                    node.Acceleration += rotAcc * addTorque + addVelocity;
                    break;
                case VerletLinkComponent link:
                    ragdoll.Links.Add(new ComponentRef<VerletLinkComponent>(link.Entity));
                    break;
                case VerletTransformComponent tlink:
                    ragdoll.TransformLinks.Add(new ComponentRef<VerletTransformComponent>(tlink.Entity));
                    //tlink.GlobalOffset += visualOffset;
                    break;
                default:
                    throw new Exception("Unknown object returned by ragdoll constructor");
            }
        }
    }

    public static (Entity bg, Entity fg) CreateBackground(Scene scene, Texture tex, Vector2 center)
    {
        var mat = BackgroundMaterialCache.Instance.Load(tex);

        var bg = create(scene, tex, mat.bg, RenderOrders.BackgroundBehind.WithOrder(-1), center);
        var fg = create(scene, tex, mat.fg, RenderOrders.BackgroundInFront.WithOrder(1), center);

        static Entity create(Scene scene, Texture tex, Material mat, RenderOrder order, Vector2 center)
        {
            var ent = scene.CreateEntity();
            var transform = scene.AttachComponent(ent, new TransformComponent());
            var renderer = scene.AttachComponent(ent, new QuadShapeComponent(true));
            transform.Scale = tex.Size;
            transform.Position = center;
            renderer.Material = mat;
            renderer.RenderOrder = order;
            return ent;
        }

        return (bg, fg);
    }

    //Gejat van IQ
    public static float DistanceToPolygon(in Vector2[] vertices, in Vector2 point)
    {
        int N = vertices.Length;
        float d = Vector2.Dot(point - vertices[0], point - vertices[0]);
        float s = 1.0f;
        for (int i = 0, j = N - 1; i < N; j = i, i++)
        {
            Vector2 e = vertices[j] - vertices[i];
            Vector2 w = point - vertices[i];
            Vector2 b = w - e * Utilities.Clamp(Vector2.Dot(w, e) / Vector2.Dot(e, e), 0.0f, 1.0f);
            d = MathF.Min(d, Vector2.Dot(b, b));
            var c = (point.Y >= vertices[i].Y, point.Y < vertices[j].Y, e.X * w.Y > e.Y * w.X);
            if (all(c) || all(not(c)))
                s *= -1.0f;
        }

        return s * MathF.Sqrt(d);

        static bool all((bool, bool, bool) a) => a.Item1 && a.Item2 && a.Item3;
        static (bool, bool, bool) not((bool, bool, bool) a) => (!a.Item1, !a.Item2, !a.Item3);
    }

    public static float DistanceToRectAngled(in Vector2 point, in Vector2 min, in Vector2 max, in float theta)
    {
        float l = (max - min).Length();
        var d = (max - min) / l;
        var q = (point - (min + max) * 0.5f);
        //q = mat2(d.x, -d.y, d.y, d.x) * q;

        var mat = new Matrix3x2(d.X, -d.Y, d.Y, d.X, 0, 0);
        q = Vector2.Transform(q, mat);

        q = Vector2.Abs(q) - new Vector2(l, theta) * 0.5f;
        return Vector2.Max(q, Vector2.Zero).Length() + MathF.Min(MathF.Max(q.X, q.Y), 0);
    }

    public static bool TryGetLastSpaceIndex(ReadOnlySpan<char> line, out int index)
    {
        for (int i = line.Length - 1; i >= 0; i--)
            if (char.IsWhiteSpace(line[i]))
            {
                index = i;
                return true;
            }
        index = -1;
        return false;
    }

    public static bool TryGetFirstSpaceIndex(ReadOnlySpan<char> line, out int index)
    {
        for (int i = 0; i < line.Length; i++)
            if (char.IsWhiteSpace(line[i]))
            {
                index = i;
                return true;
            }
        index = -1;
        return false;
    }

    /// <summary>
    /// [name] [s]
    /// </summary>
    public static bool GetValueFromString(ReadOnlySpan<char> input, out ReadOnlySpan<char> value)
    {
        value = ReadOnlySpan<char>.Empty;

        if (!TryGetFirstSpaceIndex(input, out int spaceIndex))
            return false;
        value = (input[(spaceIndex + 1)..]).Trim();
        if (value.Length > 0)
            return true;
        return false;
    }

    /// <summary>
    /// [name] [s]
    /// </summary>
    public static bool GetValueFromString(ReadOnlySpan<char> input, out string value)
    {
        var v = ReadOnlySpan<char>.Empty;
        value = string.Empty;

        if (!TryGetFirstSpaceIndex(input, out int spaceIndex))
            return false;
        v = (input[(spaceIndex + 1)..]).Trim();
        if (v.Length > 0)
        {
            value = v.ToString();
            return true;
        }

        return false;
    }

    /// <summary>
    /// [name] [x]
    /// </summary>
    public static bool GetValueFromString(ReadOnlySpan<char> input, out float value)
    {
        value = float.NaN;

        if (!TryGetLastSpaceIndex(input, out int spaceIndex))
            return false;
        var part = input[spaceIndex..];
        if (part.Length > 0 && float.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;
        return false;
    }

    /// <summary>
    /// [name] [x]
    /// </summary>
    public static bool GetValueFromString(in ReadOnlySpan<char> input, out int value)
    {
        value = 0;

        if (!TryGetLastSpaceIndex(input, out int spaceIndex))
            return false;
        var part = input[spaceIndex..];
        if (part.Length > 0 && int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;
        return false;
    }

    /// <summary>
    /// [name] [true/false]
    /// </summary>
    public static bool GetValueFromString(in ReadOnlySpan<char> input, out bool value)
    {
        value = false;

        if (!TryGetLastSpaceIndex(input, out int spaceIndex))
            return false;
        var part = input[spaceIndex..];
        if (part.Length > 0 && bool.TryParse(part, out value))
            return true;
        return false;
    }

    /// <summary>
    /// [name] [x] [y]
    /// </summary>
    public static bool GetValueFromString(in ReadOnlySpan<char> input, out Vector2 value)
    {
        value = default;

        if (!TryGetFirstSpaceIndex(input, out int firstSpaceIndex))
            return false;
        var part = input[firstSpaceIndex..];
        if (part.Length >= 3 &&
            TryGetLastSpaceIndex(part, out var lastSpaceIndex)) //dit horen twee waarden te zijn: "x x"
        {
            var firstValue = part[..lastSpaceIndex];
            var secondValue = part[lastSpaceIndex..];

            if (float.TryParse(firstValue, out var f1) && float.TryParse(secondValue, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var f2))
            {
                value.X = f1;
                value.Y = f2;
                return true;
            }
        }

        return false;
    }

    public static Vector2 ClosestPointOnLine(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ap = p - a;       // Vector from A to P     
        Vector2 ab = b - a;       // Vector from A to B  

        float magnitudeAB = ab.LengthSquared();     // Magnitude of AB vector (it's length squared)     
        float ABAPproduct = Vector2.Dot(ap, ab);    // The DOT product of a_to_p and a_to_b     
        float distance = ABAPproduct / magnitudeAB; // The normalized "distance" from a to your closest point  

        if (distance < 0)     // If the point falls before the start of the line segment
            return a;

        if (distance > 1) // If the point falls after the end of the line segment
            return b;

        return a + ab * distance;
    }

    public static T PickRandom<T>(IEnumerable<T> enumerable)
    {
        return enumerable.ElementAt(Utilities.RandomInt(0, enumerable.Count()));
    }

    public static string Ellipsis(in string name, int length)
    {
        if (name.Length > length - 3)
            return string.Concat(name.AsSpan(0, length - 3), "...");
        return name;
    }

    // Straight up from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        if (recursive)
            foreach (var subDir in dir.GetDirectories())
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
    }

    //public static void PlayerEquipLastWeapon(Scene scene, Entity playerEntity, Level? level = null)
    //{
    //    if (SharedLevelData.LastPlayerWeapon.HasValue && (level?.EquipWeaponFromLastLevel ?? true))
    //    {
    //        var lastWeapon = SharedLevelData.LastPlayerWeapon.Value;

    //        if (!Registry.Weapons.Has(lastWeapon.Key))
    //            Logger.Warn($"The weapon wielded in the last level does not exist: '{lastWeapon.Key}'");
    //        else
    //        {
    //            var playerCharacter = scene.GetComponentFrom<CharacterComponent>(playerEntity);
    //            var weapon = Prefabs.CreateWeapon(scene, default, Registry.Weapons.Get(lastWeapon.Key));

    //            playerCharacter.DeleteHeldWeapon(scene);
    //            playerCharacter.EquipWeapon(scene, weapon);

    //            weapon.RemainingRounds = Utilities.Clamp(lastWeapon.Ammo, 0, weapon.Stats.RoundsPerMagazine);
    //        }
    //    }
    //    else
    //    {
    //        scene.GetComponentFrom<CharacterComponent>(playerEntity).DeleteHeldWeapon(scene);
    //    }
    //}
}

public struct GameSafeRoutineDelay : IRoutineCommand
{
    public float DelayInSeconds;
    public float CurrentTime;

    public GameSafeRoutineDelay(float delayInSeconds)
    {
        DelayInSeconds = delayInSeconds;
        CurrentTime = 0;
    }

    public bool CanAdvance(float dt)
    {
        var s = Game.Main.Scene;

        if (MadnessUtils.EditingInExperimentMode(s) || MadnessUtils.IsPaused(s) || MadnessUtils.IsCutscenePlaying(s))
            return false;

        CurrentTime += dt;
        return CurrentTime >= DelayInSeconds;
    }
}