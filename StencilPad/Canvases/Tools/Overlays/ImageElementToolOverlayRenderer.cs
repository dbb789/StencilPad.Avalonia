using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class ImageElementToolOverlayRenderer : ElementOutlineOverlayRenderer<ImageElement>
{
    public static readonly IToolOverlayRendererFactory Factory = new FactoryImpl();
    
    private class FactoryImpl : IToolOverlayRendererFactory
    {
        public IToolOverlayRenderer? CreateOverlay(ISheetElement element)
        {
            if (element is ImageElement imageElement)
            {
                return new ImageElementToolOverlayRenderer(imageElement);
            }

            return null;
        }
    }

    private ImageElementToolOverlayRenderer(ImageElement element)
        : base(element)
    {
        // ...
    }
    
    protected override UnitBounds GetBounds(ImageElement element)
    {
        return UnitBounds.FromMinMax(element.Min, element.Max);
    }
}
