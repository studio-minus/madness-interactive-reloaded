using Newtonsoft.Json;
using System;

namespace MIR;
#if false
public class ResourcePathConverter : JsonConverter<ResourcePath>
{
    public override ResourcePath ReadJson(JsonReader reader, Type objectType, ResourcePath existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new ResourcePath(reader.Value?.ToString() ?? throw new NullReferenceException());
    }

    public override void WriteJson(JsonWriter writer, ResourcePath value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}
#endif