namespace MIR;

/// <summary>
/// A type for storing a bool's previous state.
/// </summary>
public class BoolPrev : PrevValue<bool>
{
    public BoolPrev(bool v = false) : base(v)
    {
    }

    /// <summary>
    /// If the bool has become true when previously false.
    /// </summary>
    public bool BecameTrue => Value && !PreviousValue;

    /// <summary>
    /// If the bool has become false when previously true.
    /// </summary>
    public bool BecameFalse => !Value && PreviousValue;
}
