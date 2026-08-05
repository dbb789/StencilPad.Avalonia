using Avalonia.Media;
using StencilPad.Canvases.Tools.Overlays;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;

namespace StencilPad.Canvases.Tools.Controllers;

public class EllipseTool : PolygonTool<EllipseToolOverlay<Shape>, Shape>
{
    public class Factory(Sheet Sheet,
                         OverlayContainer OverlayContainer,
                         ISettings Settings,
                         IUnitSnapOverlay UnitSnapOverlay,
                         IOperationService OperationService,
                         Factory<EllipseToolOverlay<Shape>> OverlayFactory) : IToolFactory
    {
        public string IconResource => "CircleTool";
        public string Tooltip => "Ellipse";

        public ITool Create(IToolButton button)
        {
            return new EllipseTool(Sheet,
                                  OverlayContainer,
                                  Settings,
                                  UnitSnapOverlay,
                                  OperationService,
                                  OverlayFactory);
        }
    }

    private EllipseTool(Sheet sheet,
                       OverlayContainer overlayContainer,
                       ISettings settings,
                       IUnitSnapOverlay unitSnapOverlay,
                       IOperationService operationService,
                       Factory<EllipseToolOverlay<Shape>> overlayFactory)
        : base(sheet,
               overlayContainer,
               settings,
               unitSnapOverlay,
               operationService,
               overlayFactory)
    {
        // ...
    }

    public override void ToolBegin()
    {
        base.ToolBegin();
        
        if (Overlay is null)
        {
            return;
        }

        Overlay.Element.LineColor = Color.FromArgb(127, 0, 0, 0);
    }
}
