using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StencilPad.Canvases.Common;
using StencilPad.Common;
using StencilPad.Controllers;
using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Services;
using StencilPad.UI;
using StencilPad.UI.Dialogs;
using StencilPad.ViewModels;

#if SP_WINDOWS
using StencilPad.Windows.Services;
#elif SP_LINUX
using StencilPad.Linux.Services;
#elif SP_OSX
using StencilPad.OSX.Services;
#endif

namespace StencilPad.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            AppServices.Provider = services.BuildServiceProvider();

            var appController = AppServices.Provider.GetRequiredService<AppController>();

            appController.Initialize();

            if (desktop.Args is { Length: > 0 } args)
            {
                appController.OpenFile(args[0]);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Project>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IAvaloniaDialogParent>(x => x.GetService<MainWindow>()!);
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IModelPropertiesService, AvaloniaModelPropertiesService>();
        services.AddSingleton<IAppConfigService, AppConfigService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<PngExporter>();
        services.AddSingleton<SvgExporter>();
        services.AddSingleton<PdfExporter>();
        services.AddSingleton<IImportExportService, ImportExportService>();
        services.AddSingleton<IResourceService, ResourceService>();
        services.AddSingleton<IResourceSet>(x => x.GetService<IResourceService>()!);
        services.AddSingleton<ISettings, SettingsService>();
        services.AddSingleton<IOperationService, OperationService>();
        services.AddSingleton<HandleMap.Factory>();
        services.AddSingleton<SheetResolver.Factory>();
        services.AddSingleton<SheetRenderer.Factory>();
        services.AddSingleton<SheetTabController.Factory>();
        services.AddSingleton<MainWindowController>();
        services.AddSingleton<AppController>();

#if SP_WINDOWS
        services.AddSingleton<IPrintService, WindowsPrintService>();
#elif SP_LINUX
        services.AddSingleton<IPrintService, LinuxPrintService>();
#elif SP_OSX
        services.AddSingleton<IPrintService, OSXPrintService>();
#else
	services.AddSingleton<IPrintService, NullPrintService>();
#endif

        services.AddLogging(builder =>
        {
            builder.AddDebug();
        });
    }
}
