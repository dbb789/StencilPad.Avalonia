namespace StencilPad.Tests.Schemas;

using System.Text.Json;
using Avalonia.Media;
using StencilPad.Schemas;
using StencilPad.Spatial;

// These tests exercise the source-generated JSON path (SchemaJsonOptions.Context)
// that the AOT/trim-safe serialization relies on. They guard against two kinds of
// regression: (1) the custom converters no longer being honoured through the
// source-generation context, and (2) the on-disk format silently changing.
public class SchemaRoundTripTests
{
    private static T RoundTrip<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        var result = JsonSerializer.Deserialize(json, typeInfo);

        Assert.That(result, Is.Not.Null);

        return result!;
    }

    private static ProjectSchema BuildPopulatedProject()
    {
        var shape = new ShapeSchema
        {
            Trns = new UnitTransformSchema { Pos = Unit2D.FromMillimeters(12.5, -7.25), Ang = "45.5" },
            FlC = Color.FromArgb(200, 10, 20, 30),
            LnC = Color.FromArgb(255, 40, 50, 60),
            LnW = Unit.FromMillimeters(0.35),
            LSt = [Unit.FromMillimeters(2), Unit.FromMillimeters(1)],
            StC = 3,
            EdC = 7,
            Plys =
            [
                new PolygonSchema
                {
                    C = true,
                    V =
                    [
                        new VertexSchema { Pos = Unit2D.FromMillimeters(0, 0) },
                        new VertexSchema { Pos = Unit2D.FromMillimeters(10, 0) },
                        new VertexSchema { Pos = Unit2D.FromMillimeters(10, 10) }
                    ]
                }
            ]
        };

        var text = new TextElementSchema
        {
            Trns = new UnitTransformSchema { Pos = Unit2D.FromMillimeters(1, 2), Ang = "0" },
            Min = Unit2D.FromMillimeters(1, 2),
            Max = Unit2D.FromMillimeters(50, 20),
            Text = "Hello \"world\"",
            Font = "Courier New",
            FSz = 7.5,
            Col = Color.FromArgb(255, 1, 2, 3)
        };

        return new ProjectSchema
        {
            Version = 3,
            UnitSystem = UnitSystem.Imperial,
            UnitRatio = new Fraction(3, 8),
            GridSpacingMetric = Unit.FromMillimeters(5),
            GridSubdivisionsMetric = 2,
            GridSpacingImperial = Unit.FromInches(0.5),
            GridSubdivisionsImperial = 8,
            Defaults = [shape],
            Sheets =
            [
                new SheetSchema
                {
                    Name = "Sheet A",
                    Format = new SheetFormatSchema
                    {
                        SizeType = SheetSizeTypeSchema.A3,
                        Orientation = SheetOrientationSchema.Landscape,
                        CustomSize = Unit2D.FromMillimeters(420, 297)
                    },
                    Elements = [shape, text]
                }
            ]
        };
    }

    [Test]
    public void ProjectSchema_RoundTrips_PreservingAllFields()
    {
        var original = BuildPopulatedProject();

        var result = RoundTrip(original, SchemaJsonOptions.Context.ProjectSchema);

        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo(3));
            Assert.That(result.UnitSystem, Is.EqualTo(UnitSystem.Imperial));
            Assert.That(result.UnitRatio, Is.EqualTo(new Fraction(3, 8)));
            Assert.That(result.GridSpacingMetric, Is.EqualTo(Unit.FromMillimeters(5)));
            Assert.That(result.GridSubdivisionsMetric, Is.EqualTo(2));
            Assert.That(result.GridSpacingImperial, Is.EqualTo(Unit.FromInches(0.5)));
            Assert.That(result.GridSubdivisionsImperial, Is.EqualTo(8));
            Assert.That(result.Defaults, Has.Length.EqualTo(1));
            Assert.That(result.Sheets, Has.Length.EqualTo(1));
            Assert.That(result.Sheets[0].Name, Is.EqualTo("Sheet A"));
            Assert.That(result.Sheets[0].Format.SizeType, Is.EqualTo(SheetSizeTypeSchema.A3));
            Assert.That(result.Sheets[0].Format.Orientation, Is.EqualTo(SheetOrientationSchema.Landscape));
            Assert.That(result.Sheets[0].Format.CustomSize, Is.EqualTo(Unit2D.FromMillimeters(420, 297)));
            Assert.That(result.Sheets[0].Elements, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ProjectSchema_RoundTrips_PreservingShapeDetail()
    {
        var original = BuildPopulatedProject();

        var result = RoundTrip(original, SchemaJsonOptions.Context.ProjectSchema);

        var shape = (ShapeSchema)result.Sheets[0].Elements[0];

        Assert.Multiple(() =>
        {
            Assert.That(shape.Trns.Pos, Is.EqualTo(Unit2D.FromMillimeters(12.5, -7.25)));
            Assert.That(shape.Trns.Ang, Is.EqualTo("45.5"));
            Assert.That(shape.FlC, Is.EqualTo(Color.FromArgb(200, 10, 20, 30)));
            Assert.That(shape.LnC, Is.EqualTo(Color.FromArgb(255, 40, 50, 60)));
            Assert.That(shape.LnW, Is.EqualTo(Unit.FromMillimeters(0.35)));
            Assert.That(shape.LSt, Is.EqualTo(new[] { Unit.FromMillimeters(2), Unit.FromMillimeters(1) }));
            Assert.That(shape.StC, Is.EqualTo(3));
            Assert.That(shape.EdC, Is.EqualTo(7));
            Assert.That(shape.Plys, Has.Length.EqualTo(1));
            Assert.That(shape.Plys[0].C, Is.True);
            Assert.That(shape.Plys[0].V, Has.Length.EqualTo(3));
            Assert.That(shape.Plys[0].V[1].Pos, Is.EqualTo(Unit2D.FromMillimeters(10, 0)));
        });
    }

    [Test]
    public void SheetElementSchemaArray_RoundTrips_PreservingPolymorphicTypes()
    {
        var original = BuildPopulatedProject().Sheets[0].Elements;

        var result = RoundTrip(original, SchemaJsonOptions.Context.SheetElementSchemaArray);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.TypeOf<ShapeSchema>());
            Assert.That(result[1], Is.TypeOf<TextElementSchema>());
        });

        var text = (TextElementSchema)result[1];

        Assert.Multiple(() =>
        {
            Assert.That(text.Text, Is.EqualTo("Hello \"world\""));
            Assert.That(text.Font, Is.EqualTo("Courier New"));
            Assert.That(text.FSz, Is.EqualTo(7.5));
            Assert.That(text.Col, Is.EqualTo(Color.FromArgb(255, 1, 2, 3)));
            Assert.That(text.Min, Is.EqualTo(Unit2D.FromMillimeters(1, 2)));
            Assert.That(text.Max, Is.EqualTo(Unit2D.FromMillimeters(50, 20)));
        });
    }

    [Test]
    public void GeometryResourceLibrarySchema_RoundTrips()
    {
        var original = new GeometryResourceLibrarySchema
        {
            Caps =
            [
                new GeometryResourceSchema { Id = 1, Filename = "cap.spad", Size = Unit2D.FromMillimeters(4, 4) }
            ],
            Markers =
            [
                new GeometryResourceSchema { Id = 2, Filename = "marker.spad", Size = null }
            ]
        };

        var result = RoundTrip(original, SchemaJsonOptions.Context.GeometryResourceLibrarySchema);

        Assert.Multiple(() =>
        {
            Assert.That(result.Caps, Has.Count.EqualTo(1));
            Assert.That(result.Caps[0].Id, Is.EqualTo(1));
            Assert.That(result.Caps[0].Filename, Is.EqualTo("cap.spad"));
            Assert.That(result.Caps[0].Size, Is.EqualTo(Unit2D.FromMillimeters(4, 4)));
            Assert.That(result.Markers, Has.Count.EqualTo(1));
            Assert.That(result.Markers[0].Id, Is.EqualTo(2));
            Assert.That(result.Markers[0].Size, Is.Null);
        });
    }

    // The following tests assert the actual on-disk JSON shape produced through the
    // source-generation context, confirming the custom converters are honoured and
    // the file format was not changed by the AOT fix.

    [Test]
    public void Enums_SerializeAsStringNames()
    {
        var project = new ProjectSchema { UnitSystem = UnitSystem.Imperial };

        var json = JsonSerializer.Serialize(project, SchemaJsonOptions.Context.ProjectSchema);

        Assert.That(json, Does.Contain("\"UnitSystem\":\"Imperial\""));
    }

    [Test]
    public void SheetFormatEnums_SerializeAsStringNames()
    {
        var format = new SheetFormatSchema
        {
            SizeType = SheetSizeTypeSchema.Legal,
            Orientation = SheetOrientationSchema.Landscape
        };
        var sheet = new SheetSchema { Name = "s", Format = format };
        var project = new ProjectSchema { Sheets = [sheet] };

        var json = JsonSerializer.Serialize(project, SchemaJsonOptions.Context.ProjectSchema);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"SizeType\":\"Legal\""));
            Assert.That(json, Does.Contain("\"Orientation\":\"Landscape\""));
        });
    }

    [Test]
    public void CustomConverters_ProduceExpectedJsonShapes()
    {
        var shape = new ShapeSchema
        {
            Trns = new UnitTransformSchema { Pos = Unit2D.Zero, Ang = "0" },
            FlC = Color.FromArgb(255, 10, 20, 30),
            LnW = Unit.FromMillimeters(1.5)
        };
        var project = new ProjectSchema { Defaults = [shape] };

        var json = JsonSerializer.Serialize(project, SchemaJsonOptions.Context.ProjectSchema);

        Assert.Multiple(() =>
        {
            // Unit -> bare numeric string; Color -> [r,g,b,a]; Unit2D -> [x,y];
            // polymorphic discriminator property "T".
            Assert.That(json, Does.Contain("\"LnW\":\"1.5\""));
            Assert.That(json, Does.Contain("\"FlC\":[10,20,30,255]"));
            Assert.That(json, Does.Contain("\"Pos\":[\"0\",\"0\"]"));
            Assert.That(json, Does.Contain("\"T\":\"Shp\""));
        });
    }

    [Test]
    public void Fraction_SerializesAsNumeratorDenominatorArray()
    {
        var project = new ProjectSchema { UnitRatio = new Fraction(3, 8) };

        var json = JsonSerializer.Serialize(project, SchemaJsonOptions.Context.ProjectSchema);

        Assert.That(json, Does.Contain("\"UnitRatio\":[3,8]"));
    }
}
