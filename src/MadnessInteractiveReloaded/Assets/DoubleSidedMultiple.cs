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

    public DoubleSidedMultiple(T[] right, T[] left)
    {
        FromBehind = left;
        FromFront = right;
    }

    //TODO wtf is dit??? waarom is flipped niet correct waarom moet ik het inverten wtf wat doe jij sukkel
    public T[] Select(bool front) => front ? FromFront : FromBehind;
    public T PickRandom(bool front) => front ? Utilities.PickRandom(FromFront) : Utilities.PickRandom(FromBehind);
}

