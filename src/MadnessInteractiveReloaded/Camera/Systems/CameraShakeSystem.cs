using System;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// See <see cref="CameraShakeComponent"/>
/// </summary>
public class CameraShakeSystem : Walgelijk.System
{
    public override void Update()
    {
        if (Scene.FindAnyComponent<CameraShakeComponent>(out var shake))
        {
            shake.CalculatedShakeOffset = shake.ShakeIntensity * float.Sin(Time.SecondsSinceLoadUnscaled * shake.ShakeFrequency) * 0.3f * UserData.Instances.Settings.General.Screenshake;
            shake.ShakeIntensity = Utilities.SmoothApproach(shake.ShakeIntensity, 0, 4, Time.DeltaTimeUnscaled);

            //if (Scene.FindAnyComponent<CameraMovementComponent>(out var cm))
            //    cm.Offset += new Vector2(0, shake.CalculatedShakeOffset);
        }
    }
}