using System.IO;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Export;

public class PngExporter
{
    private const double MmPerInch = 25.4;
    private const double Dpi = 254.0;

    private readonly IResourceSet _resourceSet;
    private readonly SheetResolver.Factory _sheetResolverFactory;

    public PngExporter(IResourceSet resourceSet,
                       SheetResolver.Factory sheetResolverFactory)
    {
        _resourceSet = resourceSet;
        _sheetResolverFactory = sheetResolverFactory;
    }

    public void Export(Sheet sheet, string path)
    {
        UnitBounds? sheetBounds = null;

        using var resolver = _sheetResolverFactory.Create(sheet);

        foreach (var elementResolver in resolver.Elements)
        {
            sheetBounds = UnitBounds.Union(sheetBounds, elementResolver.GetOutlineBounds());
        }

        var bounds = sheetBounds ??
            UnitBounds.FromCenterSize(Unit2D.Zero,
                                      new Unit2D(Unit.FromMillimeters(100),
                                                 Unit.FromMillimeters(100)));

        var size = bounds.Size;
        double widthMm  = size.X.Millimeters;
        double heightMm = size.Y.Millimeters;

        double pixelsPerMm = Dpi / MmPerInch;

        // A margin so that geometry sitting exactly on the bounds edge, stroke
        // half-widths and anti-aliased pixels (which become more visible when
        // zoomed in) are never clipped. Kept deliberately generous.
        const int paddingPx = 4;

        double contentWidthPx  = widthMm * pixelsPerMm;
        double contentHeightPx = heightMm * pixelsPerMm;

        int widthPx  = Math.Max(1, (int)Math.Ceiling(contentWidthPx))  + paddingPx * 2;
        int heightPx = Math.Max(1, (int)Math.Ceiling(contentHeightPx)) + paddingPx * 2;

        var info = new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        float scale = (float)pixelsPerMm;

        // Model space has Y pointing up with the origin at bounds.Min; image
        // space has Y pointing down from the top-left. Scale mm -> px, flip Y
        // and translate so that bounds.Max.Y maps to the top padding edge and
        // bounds.Min.X maps to the left padding edge. Using the exact content
        // height (rather than the rounded canvas height) keeps the top edge
        // from being clipped.
        var matrix = SKMatrix.CreateScale(scale, -scale);
        matrix = SKMatrix.Concat(SKMatrix.CreateTranslation(paddingPx, paddingPx + (float)contentHeightPx), matrix);
        matrix = SKMatrix.Concat(matrix, SKMatrix.CreateTranslation((float)-bounds.Min.X.Millimeters,
                                                                    (float)-bounds.Min.Y.Millimeters));

        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, matrix));

        foreach (var elementResolver in resolver.Elements)
        {
            using var renderer = new ModelRenderer(_resourceSet);

            elementResolver.Attach(renderer);
            renderer.PreRender();
            renderer.Render(canvas, null);
        }

        canvas.Restore();
        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);

        data.SaveTo(stream);
    }
}
