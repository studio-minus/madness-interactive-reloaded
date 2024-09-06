namespace MIR;

using System.Numerics;
using Walgelijk;
using Walgelijk.SimpleDrawing;

public static class MadnessDraw
{
    public static Rect Image(IReadableTexture texture, Rect rect, ImageContainmentMode containmentMode, float degrees = 0, float roundness = 0)
    {
        var textureSize = texture.Size;
        var size = rect.GetSize();
        var topLeft = rect.BottomLeft;

        Vector2 imageSize;
        Vector2 imagePos = default;

        switch (containmentMode)
        {
            case ImageContainmentMode.Stretch:
                imageSize = size;
                break;
            case ImageContainmentMode.Contain:
            case ImageContainmentMode.Cover:
                var aspectRatio = textureSize.X / textureSize.Y;

                imageSize = size;
                bool a = size.X / aspectRatio > size.Y;

                if (containmentMode == ImageContainmentMode.Contain)
                    a = !a;

                if (a)
                    imageSize.Y = size.X / aspectRatio;
                else
                    imageSize.X = size.Y * aspectRatio;

                imagePos = size / 2 - imageSize / 2;
                break;
            case ImageContainmentMode.Center:
                imageSize = textureSize;
                imagePos = size / 2 - imageSize / 2;
                break;
            default:
            case ImageContainmentMode.OriginalSize:
                imageSize = textureSize;
                break;
        }

        Draw.Texture = texture;
        Draw.Quad(topLeft + imagePos, imageSize, degrees, roundness);

        return new Rect(topLeft.X + imagePos.X, topLeft.Y + imagePos.Y, topLeft.X + imagePos.X + imageSize.X, topLeft.Y + imagePos.Y + imageSize.Y);
    }
}
