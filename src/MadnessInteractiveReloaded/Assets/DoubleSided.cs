using System;

namespace MIR;

/// <summary>
/// A structure for holding two 2D facing things.
/// Very commonly used for containing left and right facing versions of a <see cref="Walgelijk.Texture"/>.
/// <example>
/// <code>
/// // Example:
/// public DoubleSided<![CDATA[<Texture>]]> ErrorTexture = new(Texture.ErrorTexture, Texture.ErrorTexture);
/// </code>
/// </example>
/// </summary>
/// <typeparam name="T"></typeparam>
public struct DoubleSided<T>
{
    public T FacingRight;
    public T FacingLeft;

    public DoubleSided(T front, T behind)
    {
        FacingLeft = behind;
        FacingRight = front;
    }

    public T Select(bool flipped) => flipped ? FacingLeft : FacingRight;
}

