using SkiaSharp;

public interface IRenderer
{
    void PreRender();
    void Render(SKCanvas canvas, GRContext? grContext);
}
