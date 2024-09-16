using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

public class ArenaModeComponent : Component
{
    public AssetRef<ArenaModeWave[]> Waves = Assets.Load<ArenaModeWave[]>("data/arena/waves.json");
}
//🎈