using System.Threading.Tasks;
using System;
using Walgelijk;
using System.Collections.Generic;

namespace MIR.LevelEditor;

public class LevelScriptSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene))
            return;

        foreach (var comp in Scene.GetAllComponentsOfType<LevelScriptComponent>())
        {
            var built = LevelScriptCache.Instance.Load(comp.Code);

            if (!built.IsOccupied && built.Script == null)
            {
                RoutineScheduler.Start(BuildRoutine(built));
                continue;
            }

            if (!built.IsReady || !comp.Enabled)
                continue;

            if (!comp.Started)
            {
                comp.Started = true;
                built.OnStart?.Invoke();
            }
            
            built.OnUpdate?.Invoke();
        }
    }

    private static IEnumerator<IRoutineCommand> BuildRoutine(BuiltLevelScript built)
    {
        var t = Task.WhenAll(Task.Run(built.Build), Task.Delay(16));
        yield return new RoutineWaitUntil(() => t.IsCompleted);
    }

    public override void FixedUpdate()
    {
        foreach (var comp in Scene.GetAllComponentsOfType<LevelScriptComponent>())
        {
            var built = LevelScriptCache.Instance.Load(comp.Code);

            if (!built.IsReady || !comp.Enabled)
                continue;

            built.OnFixedUpdate?.Invoke();
        }
    }

    public override void Render()
    {
        foreach (var comp in Scene.GetAllComponentsOfType<LevelScriptComponent>())
        {
            var built = LevelScriptCache.Instance.Load(comp.Code);

            if (!built.IsReady || !comp.Enabled)
                continue;

            built.OnRender?.Invoke();
        }
    }
}
