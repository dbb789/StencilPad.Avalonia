using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Export;

public class PngExporter
{
    private const double Dpi = 960.0;
    private const double BaseDpi = 96.0;

    private readonly SheetResolver.Factory _sheetResolverFactory;
    private readonly SheetRenderer.Factory _sheetRendererFactory;
    
    public PngExporter(SheetResolver.Factory sheetResolverFactory,
                       SheetRenderer.Factory sheetRendererFactory)
    {
        _sheetResolverFactory = sheetResolverFactory;
        _sheetRendererFactory = sheetRendererFactory;
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
        double width  = size.X.Millimeters;
        double height = size.Y.Millimeters;

        using var renderer = _sheetRendererFactory.Create(resolver);

        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(1, -1));
        transform.Children.Add(new TranslateTransform(-bounds.Min.X.Millimeters,
                                                      -bounds.Min.Y.Millimeters));

        double scale = Dpi / BaseDpi;
        int widthPx  = (int)Math.Round(width * scale);
        int heightPx = (int)Math.Round(height * scale);

        // NOTE: Avalonia has no DrawingVisual/PngBitmapEncoder/BitmapFrame - a RenderTargetBitmap
        // provides its own DrawingContext to draw into directly, and Bitmap.Save writes PNG.
        using var bitmap = new RenderTargetBitmap(new PixelSize(widthPx, heightPx),
                                                  new Vector(BaseDpi * scale, BaseDpi * scale));

        using (var dc = bitmap.CreateDrawingContext())
        {
            using var state = dc.PushTransform(transform.Value);
            renderer.Render(dc);
        }

        using var stream = File.OpenWrite(path);
        bitmap.Save(stream);
    }
}
