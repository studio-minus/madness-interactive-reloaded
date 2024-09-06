using Walgelijk;

namespace MIR;

public interface ICharacterCustomisationItem
{
    string DisplayName { get; }
    bool Hidden { get; }
    IReadableTexture Texture { get; }
    int Order { get; }
}
