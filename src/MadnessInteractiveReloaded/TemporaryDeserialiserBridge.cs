using System;
using System.IO;
using Walgelijk.AssetManager;
using Walgelijk.AssetManager.Deserialisers;

namespace MIR;//👩

public abstract class TemporaryDeserialiserBridge<T> : IAssetDeserialiser<T> where T : notnull
{
    public Type ReturningType => typeof(T);

    public virtual string Extension => string.Empty;

    public T Deserialise(Func<Stream> stream, in AssetMetadata assetMetadata)
    {
        using var temp = File.OpenWrite(Path.GetTempFileName() + "." + Extension);
        using var s = stream();
        s.CopyTo(temp);
        temp.Dispose();
        return Deserialise(temp.Name, assetMetadata);
    }

    public abstract T Deserialise(in string path, in AssetMetadata assetMetadata);
    public abstract bool IsCandidate(in AssetMetadata assetMetadata);
}
