using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Level scripting.
/// </summary>
public class LevelScriptComponent : Component, IDisposable
{
    /// <summary>
    /// Is it on or off?
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// The script name
    /// </summary>
    public string Name = "untitled";

    /// <summary>
    /// The actual C# script
    /// </summary>
    public string Code;

    /// <summary>
    /// Keeps track of whether Start was called on the script
    /// </summary>
    public bool Started;

    /// <summary>
    /// Keeps track of whether End was called on the script
    /// </summary>
    public bool Ended;

    public LevelScriptComponent()
    {
        Code = string.Empty;
        Enabled = false;
    }

    public void Dispose()
    {
        if (!Ended && LevelScriptCache.Instance.Has(Code))
        {
            var b = LevelScriptCache.Instance.Load(Code);
            if (b.Script != null)
                b.OnEnd?.Invoke();
        }
    }
}
