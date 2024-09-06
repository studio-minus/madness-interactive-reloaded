namespace MIR;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Interface for implementing a Registry.<br></br>
/// See: <see cref="BasicRegistry{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IRegistry<TKey, TValue> where TValue : class where TKey : notnull
{
    public void Register(TKey key, TValue val);
    public void Unregister(TKey key);

    public bool TryGetKeyFor(TValue value, [NotNullWhen(true)] out TKey? key);
    public TValue Get(TKey key);
    public bool TryGet(TKey key, out TValue? v);
    public IEnumerable<TKey> GetAllKeys();
    public IEnumerable<TValue> GetAllValues();
    public TValue GetRandomValue();
    public TKey GetRandomKey();
    public bool Has(TKey key);
    public void Clear();
    public int Count { get; }

    public TValue this[TKey key] => Get(key);
}
