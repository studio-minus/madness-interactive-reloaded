using Xunit;

namespace MIR.Test.Deserialisation;

[Collection("Registry collection")]
public class AnimationDeserialiserTest : global::System.IDisposable
{
    public AnimationDeserialiserTest()
    {
        Registries.ClearAll();
    }

    public void PositiveCaseTest()
    {
        //TODO
    }

    public void Dispose()
    {
        Registries.ClearAll();
    }
}
