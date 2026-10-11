using System.Text.Json;
using System.Text.Json.Serialization;
using StencilPad.Spatial;

namespace StencilPad.Schemas;

public static class SchemaJsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateOptions();

    // A source-generation context bound to the options above. Using the typed
    // JsonTypeInfo members this exposes (e.g. Context.ProjectSchema) keeps
    // serialization reflection-free and AOT/trim safe, while still honouring
    // the custom converters registered on the options.
    public static readonly SourceGenerationContext Context = new(Default);

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter<UnitSystem>(),
                new JsonStringEnumConverter<SheetSizeTypeSchema>(),
                new JsonStringEnumConverter<SheetOrientationSchema>(),
                new UnitConverter(),
                new Unit2DConverter(),
                new FractionConverter(),
                new ColorConverter()
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
