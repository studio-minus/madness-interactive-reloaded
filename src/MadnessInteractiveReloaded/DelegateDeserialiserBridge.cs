using System;
using Walgelijk.AssetManager;

namespace MIR; //👩

[Obsolete]
public class DelegateDeserialiserBridge<T> : TemporaryDeserialiserBridge<T> where T : notnull
{
    private readonly Func<string, T> deserialiser;
    private readonly string extension;

    public override string Extension => extension;

    public DelegateDeserialiserBridge(Func<string, T> deserialiser, string extension)
    {
        this.deserialiser = deserialiser;
        this.extension = extension;
    }

    public override T Deserialise(in string path, in AssetMetadata assetMetadata)
    {
        return deserialiser(path)!;
    }

    public override bool IsCandidate(in AssetMetadata assetMetadata)
    {
        return assetMetadata.Path.EndsWith(extension, StringComparison.CurrentCultureIgnoreCase);
    }
}
