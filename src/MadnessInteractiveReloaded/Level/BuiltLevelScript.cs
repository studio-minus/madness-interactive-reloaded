using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Walgelijk;
using Walgelijk.Onion;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class BuiltLevelScript : IDisposable
{
    public readonly string Code;
    public readonly Guid Identifier;

    public BuiltLevelScript(string code)
    {
        Code = code;
        Identifier = Guid.NewGuid();
        Logger.Log("Script assigned ID " + Identifier + ":\n" + Code + "\n --- \n");
    }

    /// <summary>
    /// The script object that will be non-null if <see cref="Build"/> succeeds
    /// </summary>
    public Script<object>? Script;

    /// <summary>
    /// Is this instance busy with an async script building task?
    /// </summary>
    public bool IsOccupied { get; private set; }

    public Start? OnStart;
    public Update? OnUpdate;
    public FixedUpdate? OnFixedUpdate;
    public Render? OnRender;
    public End? OnEnd;

    public event Action<Exception>? OnException = null;

    public delegate void Start();
    public delegate void Update();
    public delegate void FixedUpdate();
    public delegate void Render();
    public delegate void End();

    private readonly Mutex scriptRunLock = new();

    public bool IsReady => !IsOccupied && Script != null;

    private const string CodeWrapper =
@"class Script : LevelScriptBase
{
    %insert%
}

return new Script();";

    private static readonly Assembly[] scriptReferences =
    {
        typeof(object).Assembly,
        typeof(Game).Assembly,
        typeof(Draw).Assembly,
        typeof(Onion).Assembly,
        typeof(MadnessConstants).Assembly
    };

    private static readonly string[] scriptUsings =
    {
        "System",
        "Walgelijk",
        "Walgelijk.SimpleDrawing",
        "Walgelijk.Onion",
        "Walgelijk.AssetManager",
        "Walgelijk.ParticleSystem",
        "Walgelijk.Physics",
        "MIR"
    };
    
    public async Task Build()
    {
        scriptRunLock.WaitOne();
        Logger.Log("Started building script " + Identifier);

        try
        {
            IsOccupied = true;

            var scriptOptions = ScriptOptions.Default.AddReferences(scriptReferences).AddImports(scriptUsings);
            var transformedCode = CodeWrapper.Replace("%insert%", Code);

            Script = CSharpScript.Create<object>(transformedCode, scriptOptions);
            var scriptState = await Script.RunAsync(catchException: CatchScriptException);

            if (scriptState != null)
            {
                var instance = scriptState.ReturnValue;
                var t = instance.GetType();
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                TryGetScriptEventMethod(nameof(Start), out OnStart, instance, methods);
                TryGetScriptEventMethod(nameof(Update), out OnUpdate, instance, methods);
                TryGetScriptEventMethod(nameof(FixedUpdate), out OnFixedUpdate, instance, methods);
                TryGetScriptEventMethod(nameof(Render), out OnRender, instance, methods);
                TryGetScriptEventMethod(nameof(End), out OnEnd, instance, methods);
            }

            Logger.Log("Built script " + Identifier);
        }
        catch (Exception e)
        {
            Logger.Error($"Script {Identifier} building error: " + e);
            Script = null;
        }
        finally
        {
            IsOccupied = false;
            scriptRunLock.ReleaseMutex();
        }
    }

    private bool CatchScriptException(Exception e)
    {
        Logger.Error($"Script {Identifier} threw error: " + e);
        OnException?.Invoke(e);
        return true;
    }

    private static bool TryGetScriptEventMethod<T>(string name, [NotNullWhen(true)] out T? @delegate, object? instance, MethodInfo[]? methods) where T : Delegate
    {
        var vv = methods!.FirstOrDefault(m => IsScriptEventMethod(m, name));
        if (vv != null)
        {
            @delegate = vv.CreateDelegate<T>(instance);
            return true;
        }
        @delegate = default;
        return false;
    }

    /// <summary>
    /// Returns true if the given method is a valid script event method
    /// </summary>
    private static bool IsScriptEventMethod(MethodInfo m, string name)
    {
        if (m.IsAbstract || m.IsStatic || m.ContainsGenericParameters || m.IsGenericMethod || m.ReturnType != typeof(void) || m.Name != name)
            return false;

        var p = m.GetParameters();
        return (p.Length == 0);
    }

    public void Dispose()
    {
        scriptRunLock?.Dispose();
    }
}
