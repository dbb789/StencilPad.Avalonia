using SkiaSharp;

public interface IViewportRenderer
{
    void Render(SKCanvas canvas, GRContext? grContext, SKMatrix viewportMatrix);
}
