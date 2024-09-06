namespace MIR.Exceptions;

using System;

/// <summary>
/// Thrown when we fail to deserialize or serialize some game data.
/// See <see cref="Serialisation.BaseDeserialiser"/>.
/// </summary>
public class SerialisationException : Exception
{
    public SerialisationException(string? message) : base(message)
    {
    }
}
