using Newtonsoft.Json;
using System.Linq;

namespace MIR;

public class WaveSequence
{
    public Wave[] Waves = [];

    [JsonIgnore]
    public int TotalTargetCount => Waves.Sum(static w => w.TargetCount);

    public record Wave
    {
        public int TargetCount = 10;
        public EnemySpawnInstructions[] Instructions = [];
    }
}
