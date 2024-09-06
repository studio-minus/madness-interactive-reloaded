namespace MIR;
using Walgelijk;

/// <summary>
/// Cache for <see cref="CharacterLook"/> thumbnail textures.
/// </summary>
public class CharacterThumbnailCache : Cache<CharacterLook, IReadableTexture>
{
    public static readonly CharacterThumbnailCache Instance = new();

    protected override IReadableTexture CreateNew(CharacterLook raw)
    {
        return ThumbnailRenderer.CreateThumbnailFor(raw, Colors.Transparent, Textures.UserInterface.ApparelButtonBackground.Value);
    }

    protected override void DisposeOf(IReadableTexture loaded)
    {
        loaded.Dispose();
    }
}
