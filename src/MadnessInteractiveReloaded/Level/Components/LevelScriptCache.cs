using Walgelijk;

namespace MIR;

public class LevelScriptCache : Cache<string, BuiltLevelScript>
{
    public readonly static LevelScriptCache Instance = new();

    protected override BuiltLevelScript CreateNew(string raw) => new BuiltLevelScript(raw);

    protected override void DisposeOf(BuiltLevelScript loaded) => loaded.Dispose();
}
