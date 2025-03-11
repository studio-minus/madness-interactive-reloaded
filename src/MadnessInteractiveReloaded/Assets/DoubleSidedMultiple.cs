using Walgelijk;

namespace MIR;

/// <summary>
/// Like <see cref="DoubleSided{T}"/> but holds multiple things per side.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct DoubleSidedMultiple<T>
{
    public T[] FromFront;
    public T[] FromBehind;

    public DoubleSidedMultiple(T[] front, T[] behind)
    {
        FromBehind = behind;
        FromFront = front;
    }

    public T[] Select(bool front) => front ? FromFront : FromBehind;
    public T PickRandom(bool front) => front ? Utilities.PickRandom(FromFront) : Utilities.PickRandom(FromBehind);
}

