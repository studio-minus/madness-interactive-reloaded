using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Walgelijk;
namespace MIR;

public class Registry<T> : IRegistry<string, T> where T : class
{
    private readonly ConcurrentDictionary<string, T> dict = new();

    public int Count => dict.Count;

    public void Clear() => dict.Clear();

    public T Get(string key) => dict[key];

    public IEnumerable<string> GetAllKeys() => dict.Keys;

    public IEnumerable<T> GetAllValues() => dict.Values;

    public string GetRandomKey() => Utilities.PickRandom(dict.Keys);

    public T GetRandomValue() => Utilities.PickRandom(dict.Values);

    public bool Has(string key) => key != null && dict.ContainsKey(key);

    public bool TryGet(string key, [NotNullWhen(true)] out T? v) => dict.TryGetValue(key, out v);

    public bool TryGetKeyFor(T value, [NotNullWhen(true)] out string? key)
    {
        key = null;
        foreach (var item in dict)
            if (item.Value.Equals(value))
            {
                key = item.Key;
                return true;
            }
        return false;
    }

    public void Unregister(string key)
    {
        dict.TryRemove(key, out _); //👍👍
    }

    public void Register(string key, T val)
    {
        if (!dict.TryAdd(key, val))
        {
            dict[key] = val;
            Logger.Log($"Registry {this} replaced {key}");
        }
            //throw new Exception($"Already registered a value at {key}");
    }

    public T this[string key] => Get(key);
}
