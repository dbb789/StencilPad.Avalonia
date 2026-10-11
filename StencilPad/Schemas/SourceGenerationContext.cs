using System.Text.Json.Serialization;

namespace StencilPad.Schemas;

[JsonSerializable(typeof(ProjectSchema))]
[JsonSerializable(typeof(SheetElementSchema[]))]
[JsonSerializable(typeof(GeometryResourceLibrarySchema))]
public partial class SourceGenerationContext : JsonSerializerContext { }
