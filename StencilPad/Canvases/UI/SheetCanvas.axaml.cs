using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Microsoft.Extensions.DependencyInjection;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.Tools.Overlays;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Canvases.UI
{
    public partial class SheetCanvas : UserControl
    {
        public static readonly StyledProperty<Sheet?> SheetProperty =
            AvaloniaProperty.Register<SheetCanvas, Sheet?>(nameof(Sheet));

        public static readonly StyledProperty<double> ZoomProperty =
            AvaloniaProperty.Register<SheetCanvas, double>(nameof(Zoom), defaultValue: 1.0,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> ShowGridProperty =
            AvaloniaProperty.Register<SheetCanvas, bool>(nameof(ShowGrid), defaultValue: true,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> SnapToGridProperty =
            AvaloniaProperty.Register<SheetCanvas, bool>(nameof(SnapToGrid), defaultValue: true,
                defaultBindingMode: BindingMode.TwoWay);
        
        public static readonly StyledProperty<bool> SnapToPointProperty =
            AvaloniaProperty.Register<SheetCanvas, bool>(nameof(SnapToPoint), defaultValue: true,
                defaultBindingMode: BindingMode.TwoWay);

        public Sheet? Sheet
        {
            get => GetValue(SheetProperty);
            set => SetValue(SheetProperty, value);
        }

        public double Zoom
        {
            get => GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public bool ShowGrid
        {
            get => GetValue(ShowGridProperty);
            set => SetValue(ShowGridProperty, value);
        }

        public bool SnapToGrid
        {
            get => GetValue(SnapToGridProperty);
            set => SetValue(SnapToGridProperty, value);
        }
        
        public bool SnapToPoint
        {
            get => GetValue(SnapToPointProperty);
            set => SetValue(SnapToPointProperty, value);
        }

        public OverlayContainer OverlayContainer => _overlayContainer;
        public CanvasGrid CanvasGrid => _canvasGrid;
        public SheetRenderer SheetRenderer => _renderer;
        public IViewport Viewport => _viewport;
        public IHandleMap HandleMap => _handleMap;
        public IRubberBand RubberBand => _rubberBandEventPanel;
        public IUnitSnap UnitSnap => _unitSnap;
        public IUnitSnapOverlay UnitSnapOverlay => _unitSnapOverlay;
        
        private readonly VisualViewport _viewport;
        private readonly HandleMap _handleMap;
        private readonly CanvasGrid _canvasGrid;
        private readonly RubberBandEventPanel _rubberBandEventPanel;
        private readonly SheetResolver _resolver;
        private readonly SheetRenderer _renderer;
        private readonly SheetRenderPanel _rendererPanel;
        private readonly OverlayContainer _overlayContainer;
        private readonly RubberBandRenderPanel _rubberBandRenderPanel;
        private readonly UnitSnapOverlay _unitSnapOverlay;
        private readonly CompositeUnitSnap _unitSnap;
        
        public event Action? CanvasReady;

        static SheetCanvas()
        {
            SheetProperty.Changed.AddClassHandler<SheetCanvas>((c, e) => c.OnSheetChanged(e));
            ZoomProperty.Changed.AddClassHandler<SheetCanvas>((c, e) => c.OnZoomChanged(e));
            ShowGridProperty.Changed.AddClassHandler<SheetCanvas>((c, e) => c.OnShowGridChanged(e));
            SnapToGridProperty.Changed.AddClassHandler<SheetCanvas>((c, e) => c.OnSnapChanged(e));
            SnapToPointProperty.Changed.AddClassHandler<SheetCanvas>((c, e) => c.OnSnapChanged(e));
        }

        public SheetCanvas()
            : this(AppServices.Provider.GetRequiredService<ISettings>(),
                   AppServices.Provider.GetRequiredService<HandleMap.Factory>(),
                   AppServices.Provider.GetRequiredService<SheetResolver.Factory>(),
                   AppServices.Provider.GetRequiredService<SheetRenderer.Factory>())
        {
            // Slightly nasty to do things this way but it avoids a ton of
            // component plumbing just to get the SheetRenderer into the
            // SheetCanvas. This component also essentially bridges MVC onto
            // vanilla MVVM, so the last thing we want is to have a load of
            // funny machinery just to instantiate it.
        }
        
        public SheetCanvas(ISettings settings,
                           HandleMap.Factory handleMapFactory,
                           SheetResolver.Factory sheetResolverFactory,
                           SheetRenderer.Factory sheetRendererFactory)
        {   
            _viewport = new VisualViewport();
            _handleMap = handleMapFactory.Create();

            _canvasGrid = new CanvasGrid(settings, _viewport);

            _resolver = sheetResolverFactory.Create();
            _renderer = sheetRendererFactory.Create(_resolver);
            
            _rendererPanel = new SheetRenderPanel(_renderer, _viewport);
            _canvasGrid.Content = _rendererPanel;

            _rubberBandEventPanel = new RubberBandEventPanel(_viewport);
            _rendererPanel.Content = _rubberBandEventPanel;

            _unitSnap = new CompositeUnitSnap();
            _unitSnapOverlay = new UnitSnapOverlay(_viewport, _unitSnap);
            _rubberBandEventPanel.Content = _unitSnapOverlay;

            _overlayContainer = new OverlayContainer();
            _unitSnapOverlay.Content = _overlayContainer;

            _rubberBandRenderPanel = new RubberBandRenderPanel();
            _rubberBandEventPanel.RenderPanel = _rubberBandRenderPanel;
            
            InitializeComponent();

            _viewport.Visual = this;

            CanvasRoot.Children.Add(_canvasGrid);
            CanvasRoot.Children.Add(_rubberBandRenderPanel);
            
            _viewport.ViewportChanged += UpdateCanvasSize;
            
            _unitSnap.Add(_canvasGrid);

            Loaded += (s, e) =>
            {
                UpdateCanvasSize();
                CanvasReady?.Invoke();
            };
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<OverlayContainer>(_overlayContainer);
            services.AddSingleton<CanvasGrid>(_canvasGrid);
            services.AddSingleton<IViewport>(_viewport);
            services.AddSingleton<IHandleMap>(_handleMap);
            services.AddSingleton<IRubberBand>(_rubberBandEventPanel);
            services.AddSingleton<IUnitSnap>(_unitSnap);
            services.AddSingleton<IUnitSnapOverlay>(_unitSnapOverlay);
            services.AddSingleton<SheetResolver>(_resolver);
            services.AddSingleton<IRenderHooks>(_renderer);
        }
        
        private void OnSheetChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is Sheet oldSheet)
            {
                oldSheet.PropertyChanged -= Sheet_PropertyChanged;
            }

            var sheet = e.NewValue as Sheet;

            if (sheet is null)
            {
                return;
            }

            _resolver.Sheet = sheet;
            _handleMap.Sheet = sheet;
            
            sheet.PropertyChanged += Sheet_PropertyChanged;
            UpdateViewportSize(sheet.Format);
        }

        private void Sheet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Sheet.Format) && Sheet is not null)
            {
                UpdateViewportSize(Sheet.Format);
            }
        }

        private void UpdateViewportSize(SheetFormat format)
        {
            var sheetSize = format.Size;
            
            // Calculate 10% of the largest dimension, rounded up to the nearest 10mm
            double maxDim = Math.Max(sheetSize.X.Millimeters, sheetSize.Y.Millimeters);
            double marginMm = Math.Ceiling((maxDim * 0.1) / 10.0) * 10.0;
            var margin = Unit.FromMillimeters(marginMm);
            
            _viewport.SheetSize = sheetSize;
            _viewport.Size = new Unit2D(sheetSize.X + margin * 2,
                                        sheetSize.Y + margin * 2);
        }

        private void OnZoomChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _viewport.Zoom = (double)e.NewValue!;
        }

        private void OnShowGridChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _canvasGrid.ShowGrid = (bool)e.NewValue!;
        }

        private void OnSnapChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _unitSnap.Clear();

            if (SnapToPoint)
            {
                _unitSnap.Add(_handleMap);
            }

            if (SnapToGrid)
            {
                _unitSnap.Add(_canvasGrid);
            }
        }
        
        private void UpdateCanvasSize()
        {
            if (CanvasRoot is null)
            {
                return;
            }
            
            Width = _viewport.ToPixels(_viewport.Size.X);
            Height = _viewport.ToPixels(_viewport.Size.Y);

            _canvasGrid.InvalidateVisual();
            _rendererPanel.InvalidateVisual();
        }
    }
}
