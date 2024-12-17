using System;
using System.Collections.Generic;
using Walgelijk;
using Walgelijk.AssetManager;
using static MIR.Textures;

namespace MIR;

/// <summary>
/// Singleton component for the character creation menu.
/// </summary>
public class CharacterCreationComponent : Component, IDisposable
{
    private const int PlayerTargetTextureSize = 1024 * 2;

    public AssetRef<Texture> Background;
    public RenderTexture PlayerDrawTarget = new(PlayerTargetTextureSize, PlayerTargetTextureSize, flags: RenderTargetFlags.None);

    public CharacterCreationTab CurrentTab = default;

    public int SelectedHeadLayer = 0;
    public int SelectedBodyLayer = 0;

    public List<Color> Swatches = [Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.Black];

    public CharacterCreationComponent()
    {
        Background = Assets.Load<Texture>("textures/backgrounds/character_creation_1.qoi");
    }

    public void Dispose()
    {
        PlayerDrawTarget.Dispose();
    }

    public ref ArmourPiece? GetBodyPieceRef(CharacterComponent character, int layer = -1)
    {
        if (layer < 0)
            layer = SelectedBodyLayer;

        switch (layer)
        {
            case 0: return ref character.Look.Body!;
            case 1: return ref character.Look.BodyLayer1;
            case 2: return ref character.Look.BodyLayer2;
        }
        return ref character.Look.BodyLayer2;
    }

    public ref ArmourPiece? GetHeadPieceRef(CharacterComponent character, int layer = -1)
    {
        if (layer < 0)
            layer = SelectedHeadLayer;

        switch (layer)
        {
            case 0: return ref character.Look.Head!;
            case 1: return ref character.Look.HeadLayer1;
            case 2: return ref character.Look.HeadLayer2;
            case 3: return ref character.Look.HeadLayer3;
        }
        return ref character.Look.HeadLayer3;
    }
}
