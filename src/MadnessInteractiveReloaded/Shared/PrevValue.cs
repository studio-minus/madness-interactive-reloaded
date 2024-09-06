namespace MIR;

/// <summary>
/// A data type for keeping track of a value and its previous value.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PrevValue<T> where T : notnull
{
    /// <summary>
    /// The current value.
    /// </summary>
    public T Value;

    /// <summary>
    /// The value's previous value.
    /// </summary>
    public T PreviousValue;

    /// <summary>
    /// Has the thing changed?
    /// </summary>
    public bool HasChanged => !Value.Equals(PreviousValue);

    public PrevValue(T v)
    {
        Value = v;
        PreviousValue = v;
    }

    public void Update()
    {
        PreviousValue = Value;
    }

    public void SetBoth(T val)
    {
        Value = val;
        PreviousValue = val;
    }
}
