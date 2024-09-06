using System;
using System.Numerics;
using Walgelijk;
using Walgelijk.Onion;

namespace MIR;

/// <summary>
/// <see cref="Walgelijk.System"/> for moving the Camera.
/// </summary>
public class CameraMovementSystem : Walgelijk.System
{
    public const float OrthographicSizeMultiplier = 3;

    public void ResetCameraPosition()
    {
        if (Scene.FindAnyComponent<CameraMovementComponent>(out var movement))
        {
            //movement.DeltaPosition = default;
            //movement.Position = default;
            //movement.DeltaZoom = 1;
            //movement.Zoom = 1;
        }
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<CameraMovementComponent>(out var movement))
            return;

        var p = Vector2.Zero;
        var o = 0f;

        float totalWeight = 0;

        foreach (var item in movement.Targets)
        {
            if (!movement.States.TryGetValue(item, out var state))
            {
                state = new CameraMovementComponent.TargetState
                {
                    OrthographicSize = movement.ComputedOrthographicSize,
                    Position = movement.ComputedPosition,
                    Weight = 1
                };

                movement.States.Add(item, state);
            }

            if (state.Weight > float.Epsilon)
                item.Apply(Scene, Time, ref state.Position, ref state.OrthographicSize);

            totalWeight += state.Weight;

            state.Position = Utilities.NanFallback(state.Position);
            state.OrthographicSize = float.Max(1, Utilities.NanFallback(state.OrthographicSize, 1));

            p += state.Position * state.Weight;
            o += state.OrthographicSize * state.Weight;
        }

        if (o <= float.Epsilon)
            o = 1;

        if (totalWeight <= float.Epsilon)
            totalWeight = 1;

        float inv = 1 / totalWeight;
        p *= inv;
        o *= inv;

        if (Scene.FindAnyComponent<PlayerDeathSequenceComponent>(out var deathSequence) && MadnessUtils.FindPlayer(Scene, out _, out var character))
        {
            var t = Easings.Cubic.InOut(Utilities.Clamp((deathSequence.Time / 6f)));
            var b = Scene.GetComponentFrom<TransformComponent>(character.Positioning.Body.Entity).Position 
                + Scene.GetComponentFrom<TransformComponent>(character.Positioning.Head.Entity).Position;

            b *= 0.5f;
            p = Utilities.Lerp(p, b, t);
            o = Eerp(o, 1, t);
        }

        movement.ComputedPosition = p;
        movement.ComputedOrthographicSize = o;
    }

    // @FreyaHolmer
    private static float Eerp(float a, float b, float t) => a * float.Exp(t * float.Log(b / a));

    public override void PreRender()
    {
        if (!Scene.FindAnyComponent<CameraComponent>(out var camera))
            return;

        if (!Scene.FindAnyComponent<CameraMovementComponent>(out var movement))
            return;

        var transform = Scene.GetComponentFrom<TransformComponent>(camera.Entity);

        var additionalOffset = Vector2.Zero;
        foreach (var item in Scene.GetAllComponentsOfType<CameraOffsetComponent>())
            additionalOffset += item.GetOffset();
        additionalOffset += movement.Offset;
        movement.Offset = default;

        transform.Position = Utilities.NanFallback(movement.ComputedPosition + additionalOffset, default);
        camera.OrthographicSize = float.Max(1, Utilities.NanFallback(movement.ComputedOrthographicSize, 1));

        Audio.ListenerPosition = default; //new Vector3(transform.Position.X, transform.Position.Y, 100);
        Audio.SpatialMultiplier = 0;
    }
}
