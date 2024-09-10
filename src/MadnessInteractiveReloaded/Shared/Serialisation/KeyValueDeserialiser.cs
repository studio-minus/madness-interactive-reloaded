using MIR.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MIR.Serialisation;

/// <summary>
/// Generic serialiser for key-value pairs.
/// </summary>
/// <typeparam name="T"></typeparam>
public class KeyValueDeserialiser<T> where T : class, new()
{
    public readonly string Name;
    public bool WhitespaceDelimiter = true;
    public char[] Delimiters = Array.Empty<char>();
    public string[] ArrayEntryPrefices = new[] { "\t", "   " };

    public KeyValueDeserialiser(string name)
    {
        Name = name;
    }

    /* Expanding the list of handlers is, for the sake of simplicity, not straightfoward.
     * Things to consider:
     * - IsArrayHandler 
     * - All the Register____ functions
     * - The switch statement in Deserialise
     * - ProcessArrayEntry
     * - ProcessKeyValuePair
     * - FinaliseArray
     *
     * However, you could always do what KeyValueDeserialiserExtensions is doing:
     * Create a wrapper function around RegisterString and parse the string there instead of here
     */

    public delegate void FloatHandler(T target, float value);
    public delegate void IntHandler(T target, int value);
    public delegate void StringHandler(T target, string value);
    public delegate void BoolHandler(T target, bool value);

    public delegate void FloatArrHandler(T target, IList<float> value);
    public delegate void IntArrHandler(T target, IList<int> value);
    public delegate void StringArrHandler(T target, IList<string> value);
    public delegate void BoolArrHandler(T target, IList<bool> value);

    private static bool IsArrayHandler<T2>(T2 obj) => obj is FloatArrHandler or IntArrHandler or StringArrHandler or BoolArrHandler;

    private readonly Dictionary<string, object> handlers = new();

    public void RegisterFloat(in string key, FloatHandler handler)
        => handlers.Add(key, handler);

    public void RegisterInt(in string key, IntHandler handler)
        => handlers.Add(key, handler);

    public void RegisterString(in string key, StringHandler handler)
        => handlers.Add(key, handler);

    public void RegisterBool(in string key, BoolHandler handler)
        => handlers.Add(key, handler);

    public void RegisterFloatArray(in string key, FloatArrHandler handler)
        => handlers.Add(key, handler);

    public void RegisterIntArray(in string key, IntArrHandler handler)
        => handlers.Add(key, handler);

    public void RegisterStringArray(in string key, StringArrHandler handler)
        => handlers.Add(key, handler);

    public void RegisterBoolArray(in string key, BoolArrHandler handler)
        => handlers.Add(key, handler);

    private bool IsDelimiter(char c)
    {
        if (WhitespaceDelimiter && char.IsWhiteSpace(c))
            return true;

        foreach (var d in Delimiters)
            if (c == d)
                return true;

        return false;
    }

    private void SeparateKey(in string str, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        key = [];
        value = [];
        for (int i = 0; i <= str.Length; i++)
            if (i == str.Length || IsDelimiter(str[i]))
            {
                key = str.AsSpan(0, i);
                value = str.AsSpan(i).Trim();
                return;
            }
    }

    public T Deserialise(string path)
    {
        using var file = File.OpenRead(path);
        return Deserialise(file, path);
    }

    public T Deserialise(Stream input, in string debugLocation)
    {
        IList? currentWriteList = null;
        object? currentArrayHandler = null;
        var stage = Stage.ReadLine;
        var target = new T();
        using var file = new StreamReader(input);

        foreach (var line in BaseDeserialiser.Read(file))
            try
            {
            ProcessLine:

                switch (stage)
                {
                    case Stage.ReadLine:
                        SeparateKey(line.String, out var key, out var value);
                        if (!key.IsEmpty)
                        {
                            if (!handlers.TryGetValue(key.ToString(), out var handler))
                                throw new SerialisationException("Invalid key: " + key.ToString());

                            if (IsArrayHandler(handler))
                            {
                                currentArrayHandler = handler;
                                stage = Stage.ReadArrayEntry;
                                currentWriteList = handler switch
                                {
                                    FloatArrHandler => new List<float>(),
                                    IntArrHandler => new List<int>(),
                                    StringArrHandler => new List<string>(),
                                    BoolArrHandler => new List<bool>(),
                                    _ => throw new SerialisationException("Invalid array type handler. You must've added a new key value array type handler, but forgot to include it in the switch expression."),
                                };
                            }
                            else
                            {
                                currentArrayHandler = null;
                                ProcessKeyValuePair(target, value, handler);
                            }
                        }
                        break;
                    case Stage.ReadArrayEntry:
                        if (currentArrayHandler is null)
                            throw new SerialisationException("Entered ReadArrayEntry stage without setting a valid currentArrayHandler");

                        if (!ArrayEntryPrefices.Any(a => line.Untrimmed.StartsWith(a))) // We have left the array entry section. Go back and invoke the array handler
                        {
                            FinaliseArray(currentWriteList, currentArrayHandler, target, ref stage);
                            goto ProcessLine;
                        }
                        else
                            ProcessArrayEntry(currentWriteList, line);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new SerialisationException($"({Name}) failed to process line#{line.LineNumber} in \"{debugLocation}\": " + e);
            }

        if (stage is Stage.ReadArrayEntry) // Hey!!! We were still reading array entries and the file ended before a newline was read. Finalise final array.
            FinaliseArray(currentWriteList, currentArrayHandler, target, ref stage);

        file.Close();
        return target;
    }

    public static bool Serialise(IEnumerable<KeyValuePair<string, string>> kvps, string path)
    {
        var lines = new List<string>(kvps.Count());
        foreach (var kvp in kvps)
            lines.Add(string.Format("{0} {1}", kvp.Key, kvp.Value));
        
        BaseDeserialiser.Write(path, lines);
        return true;
    }

    private static void FinaliseArray(IList? currentWriteList, object? currentArrayHandler, T target, ref KeyValueDeserialiser<T>.Stage stage)
    {
        switch (currentArrayHandler)
        {
            case FloatArrHandler f:
                f(target, (currentWriteList as List<float>)!);
                break;
            case IntArrHandler f:
                f(target, (currentWriteList as List<int>)!);
                break;
            case StringArrHandler f:
                f(target, (currentWriteList as List<string>)!);
                break;
            case BoolArrHandler f:
                f(target, (currentWriteList as List<bool>)!);
                break;
            default: throw new SerialisationException("Invalid array type handler. You must've added a new key value array type handler, but forgot to include it in the switch expression.");
        }
        stage = Stage.ReadLine;
    }

    private void ProcessArrayEntry(IList? currentWriteList, in BaseDeserialiser.Line line)
    {
        if (currentWriteList is null)
            throw new SerialisationException("Entered ReadArrayEntry stage without setting a valid currentWriteList");

        switch (currentWriteList)
        {
            case List<float> l:
                l.Add(float.Parse(line.String, CultureInfo.InvariantCulture));
                break;
            case List<int> l:
                l.Add(int.Parse(line.String, CultureInfo.InvariantCulture));
                break;
            case List<string> l:
                l.Add(line.String);
                break;
            case List<bool> l:
                l.Add(bool.Parse(line.String));
                break;
            default:
                throw new SerialisationException("Invalid currentWriteList type. You must've added a new array type handler, but forgot to include it in the switch expression.");
        }
    }

    private static void ProcessKeyValuePair(T target, in ReadOnlySpan<char> value, object handler)
    {
        switch (handler)
        {
            case FloatHandler f:
                f(target, float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
                break;
            case IntHandler f:
                f(target, int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture));
                break;
            case StringHandler f:
                f(target, value.ToString());
                break;
            case BoolHandler f:
                f(target, bool.Parse(value));
                break;
            default: throw new SerialisationException("Invalid key-value type handler. You must've added a new handler but forgot to include it in ProcessKeyValuePair.");
        }
    }

    private enum Stage
    {
        None,
        ReadLine,
        ReadArrayEntry
    }
}