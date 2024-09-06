using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// Component to describe how the camera moves.
/// </summary>
[RequiresComponents(typeof(CameraComponent))]
public class CameraMovementComponent : Component
{
    public List<ITarget> Targets = [];
    public float TransitionTimer;

    public Vector2 ComputedPosition = default;
    public float ComputedOrthographicSize = 1;

    public Vector2 Offset;

    public Dictionary<ITarget, TargetState> States = [];

    private Routine? transitionRoutine;

    public void TransitionTo(ITarget target, float duration = 1)
    {
        if (!States.ContainsKey(target))
        {
            if (!Targets.Contains(target))
                Targets.Add(target);

            States.Add(target, new TargetState
            {
                OrthographicSize = ComputedOrthographicSize,
                Position = ComputedPosition,
                Weight = 0
            });
        }

        var oldWeights = new Dictionary<ITarget, float>();
        foreach (var item in States)
            oldWeights.Add(item.Key, item.Value.Weight);

        if (transitionRoutine != null)
            RoutineScheduler.Stop(transitionRoutine);
        transitionRoutine = RoutineScheduler.Start(Routine());

        IEnumerator<IRoutineCommand> Routine()
        {
            float progress = 0;
            float time = 0;

            while (true)
            {
                progress = Easings.Quad.InOut(time / duration);

                foreach (var k in States)
                {
                    var old = oldWeights[k.Key];

                    var t = k.Key == target ? 1 : 0;
                    k.Value.Weight = float.Lerp(old, t, progress);
                }

                time += Game.Main.State.Time.DeltaTime;

                if (time > duration)
                    break;

                yield return new GameSafeRoutineDelay();
            }

            foreach (var k in States)
            {
                var t = k.Key == target ? 1 : 0;
                k.Value.Weight = t;
            }
        }
    }

    public interface ITarget
    {
        /// <summary>
        /// Applies a transformation to the camera and returns whether this Target has expired, in which case it will be removed.
        /// </summary>
        bool Apply(Scene scene, Time time, ref Vector2 position, ref float orthographicSize);
    }

    public class TargetState
    {
        public Vector2 Position;
        public float OrthographicSize = 1;
        public float Weight = 1;
    }

    public class FreeMoveTarget : ITarget
    {
        private Vector2 previousMousePos;

        public bool Apply(Scene scene, Time time, ref Vector2 position, ref float orthographicSize)
        {
            ref var input = ref scene.Game.State.Input;

            var delta = input.WindowMousePosition - previousMousePos;
            previousMousePos = input.WindowMousePosition;

            if (Ui.IsBeingUsed)
                return false;

            if (float.Abs(input.MouseScrollDelta) > float.Epsilon)
            {
                var dirToMouse = input.WorldMousePosition - position;
                if (input.MouseScrollDelta > 0)
                {
                    orthographicSize -= orthographicSize * 0.1f;
                    position += dirToMouse * 0.1f;
                }
                else
                {
                    orthographicSize -= orthographicSize * -0.1f;
                    position += dirToMouse * -0.1f;
                }
            }

            if (input.IsButtonHeld(MouseButton.Middle))
                position += new Vector2(-delta.X, delta.Y) * orthographicSize;

            return false;
        }
    }

    public class PlayerTarget : ITarget
    {
        public bool Apply(Scene scene, Time time, ref Vector2 position, ref float orthographicSize)
        {
            var window = scene.Game.Window;

            if (MadnessUtils.FindPlayer(scene, out _, out var character) && character.IsAlive)
            {
                var targetPos = character.Positioning.GlobalCenter;

                if (Level.CurrentLevel != null)
                {
                    var bnds = Level.CurrentLevel.LevelBounds;

                    if (Level.CurrentLevel.FullZoom)
                        orthographicSize = float.Max(bnds.Width / window.Size.X, bnds.Height / window.Size.Y);
                    else
                        orthographicSize = float.Min(bnds.Width / window.Size.X, bnds.Height / window.Size.Y);

                    var min = window.WindowToWorldPoint(Vector2.Zero);
                    var max = window.WindowToWorldPoint(window.Size);
                    var worldViewRect = new Rect(min.X, max.Y, max.X, min.Y);

                    targetPos.X = PositionCoordinate(targetPos.X, bnds.MinX, bnds.MaxX, worldViewRect.Width);
                    targetPos.Y = PositionCoordinate(targetPos.Y, bnds.MinY, bnds.MaxY, worldViewRect.Height);

                    static float PositionCoordinate(float v, float levelMin, float levelMax, float windowSize)
                    {
                        // this function clamps a coordinate within the screen bounds, accounting for the level and window size, 
                        // and ensures that the level is centered when it needs to be

                        float min = levelMin + windowSize * 0.5f;
                        float max = levelMax - windowSize * 0.5f;

                        if (min >= max)
                        {
                            // the screen is too small for the thing, we should center
                            v = (levelMin + levelMax) * 0.5f;
                        }
                        else
                            v = Utilities.Clamp(v, min, max);

                        return v;
                    }

                    position = Utilities.SmoothApproach(position, targetPos, 5, time.DeltaTime);
                }
                else
                {
                    position = Utilities.SmoothApproach(position, targetPos, 5, time.DeltaTime);
                    orthographicSize = 1;
                }
            }

            return false;
        }
    }

    public class CharacterTarget : ITarget
    {
        public readonly ComponentRef<CharacterComponent> Character;

        public CharacterTarget(ComponentRef<CharacterComponent> character)
        {
            Character = character;
        }

        public bool Apply(Scene scene, Time time, ref Vector2 position, ref float orthographicSize)
        {
            if (Character.TryGet(scene, out var character))
            {
                position = Utilities.SmoothApproach(position, character.Positioning.Body.ComputedVisualCenter, 5, time.DeltaTime);
                orthographicSize = 1;
            }

            return false;
        }
    }
}
