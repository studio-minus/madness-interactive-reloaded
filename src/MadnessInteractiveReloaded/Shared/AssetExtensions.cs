using System;
using System.Diagnostics.CodeAnalysis;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;
// 👩👩👩 ENCODING JESUS FUCKING CHRIST VISUAL STUDIO
public static class AssetExtensions
{
    //public static string ToFormattedString(this GlobalAssetId id)
    //{
    //    // TODO this function is probably obsolete and can be replaced by id.ToString()
    //    if (Assets.TryGetPackage(id.External, out var p))
    //        return $"{p.Metadata.Name}:{p.GetMetadata(id.Internal).Path}";
    //    throw new Exception("Could not find asset package with id " + id.External);
    //}

    public static bool TryGetValue<T>(this AssetRef<T>? asset, out AssetRef<T> value)
    {
        if (asset.HasValue && Assets.HasAsset(asset.Value.Id))
        {
            value = asset.Value;
            return true;
        }
        value = default;
        return false;
    }
}
