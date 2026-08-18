using StencilPad.Canvases.Common;
using StencilPad.Models;

namespace StencilPad.Canvases.UI.Properties;

// Deliberately separate from IModelPropertiesService: that interface lives in
// Services and is keyed purely on Sheet + a single ISheetElement subtype, but
// this dialog also needs the canvas-scoped IHandleMap, so it's registered and
// resolved alongside the rest of the per-canvas tool machinery instead.
public interface IHandlePropertiesService
{
    void ShowHandleProperties(Sheet sheet, IHandleMap handleMap);
    void CloseAll();
}
