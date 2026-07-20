using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Canvases.UI;

public class SheetRenderPanel : ContentControl
{
    private SheetRenderer _sheetRenderer;
    private IViewport _viewport;

    public SheetRenderPanel(SheetRenderer sheetRenderer,
                            IViewport viewport)
    {
        _sheetRenderer = sheetRenderer;
        _viewport = viewport;
        
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _sheetRenderer.RendererDirty += ForceRedraw;
    }

    public void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _sheetRenderer.RendererDirty -= ForceRedraw;
    }
    
    private void ForceRedraw()
    {
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext dc)
    {
        using var state = dc.PushTransform(_viewport.MillimetersToPixelsTransform.Value);

        _sheetRenderer.Render(dc);
    }
}
