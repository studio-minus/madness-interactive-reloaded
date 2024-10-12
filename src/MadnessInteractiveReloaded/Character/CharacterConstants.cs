using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR;

/// <summary>
/// Constant values used for common character related numbers.
/// </summary>
public static class CharacterConstants
{
    public const float HalfHeight = 200;
    public const float HandHeight = 183;
    public static float GetFloorOffset(float scale) => HalfHeight * scale + 14;
    public static readonly Vector2 HeadOffsetRelativeToBody = new(0.3f - 0.35f, 0.6f - 0.22f);
    public static readonly Vector2 HeadOffsetAbsolute = new(HeadOffsetRelativeToBody.X * 192, HeadOffsetRelativeToBody.Y * 356);
    public static readonly Vector2 HeadRotationPivot = new(-0.2f, -0.3f);
    public static readonly Vector2 BodyRotationPivot = new(0, -0.5f);

    public static readonly Vector2 HandOffset1 = new(-105.09f, 26.76f);
    public static readonly Vector2 HandOffset2 = new(160.63f, 39.61f);
    public static readonly Vector2 IronsightOffset = new Vector2(-25, 50);

    public const float AccurateShotWarningDuration = 2;
    public const float AccurateShotCooldown = 5;

    public const float MaxHandRange = 280;

    public static class RenderOrders
    {
        /// <summary>
        /// The base order
        /// </summary>
        public const int BaseOrder = 0;

        /// <summary>
        /// Foot base order
        /// </summary>
        public const int FootBaseOrder = 1000;

        /// <summary>
        /// Head base order
        /// </summary>
        public const int HeadBaseOrder = 3000;

        /// <summary>
        /// Head decoration base order
        /// </summary>
        public const int HeadDecorOrder = HeadBaseOrder + 100;

        /// <summary>
        /// Body base order
        /// </summary>
        public const int BodyBaseOrder = 2000;

        /// <summary>
        /// Body decoration base order
        /// </summary>
        public const int BodyDecorOrder = BodyBaseOrder + 100;

        /// <summary>
        /// Base main hand order when equipped
        /// </summary>
        public const int MainHandOrder = 4000;

        /// <summary>
        /// Base main hand order when equipped
        /// </summary>
        public const int OtherHandOrder = -100;
    }

    public static readonly Shader BodyPartShader = new(
            Assets.Load<string>("shaders/worldspace-vertex-object-pos.vert").Value,
            Assets.Load<string>("shaders/bodypart.frag").Value);

    public static readonly Shader ApparelShader = new(
            Assets.Load<string>("shaders/worldspace-vertex-object-pos.vert").Value,
            Assets.Load<string>("shaders/apparel.frag").Value);

}
