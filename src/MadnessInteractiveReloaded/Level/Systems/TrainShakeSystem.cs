using Walgelijk;

namespace MIR;

/// <summary>
/// Train level specific.
/// </summary>
public class TrainShakeSystem : Walgelijk.System
{
    public override void Update()
    {
        MadnessUtils.Shake(Time.DeltaTime * 200 * Noise.GetPerlin(Time.SecondsSinceLoad, 0, 2318.232f));
    }
}
