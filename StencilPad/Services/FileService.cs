using Avalonia.Platform.Storage;
using StencilPad.Models;
using StencilPad.Schemas;
using StencilPad.UI;

namespace StencilPad.Services;

public class FileService : IFileService
{
    private const int FileVersion = 1;

    private static readonly FilePickerFileType ProjectFileType = new("StencilPad Project")
    {
        Patterns = ["*.spad"]
    };

    private readonly Avalonia.Controls.Window _owner;

    public FileService(IAvaloniaDialogParent parent)
    {
        _owner = parent.Window;
    }

    public async Task<string?> OpenAsync(Project target)
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Project",
            AllowMultiple = false,
            FileTypeFilter = [ProjectFileType]
        });

        var file = files.FirstOrDefault();
        var path = file?.TryGetLocalPath();

        if (path is null)
        {
            return null;
        }

        await OpenAsync(path, target);

        return path;
    }
    
    public async Task OpenAsync(string filename, Project target)
    {
        ProjectSchema schema;

        try
        {
            schema = await SchemaUtil.LoadProjectAsync(filename);
        }
        catch (Exception e)
        {
            throw new FileServiceException($"Failed to load file: {e.Message}", e);
        }

        if (schema.Version != FileVersion)
        {
            throw new FileServiceException(
                $"Unsupported file version {schema.Version}. This version of StencilPad supports version {FileVersion}.");
        }

        target.Clear(); // Safety
        ProjectSchema.Unpack(schema, target);
    }

    public async Task SaveAsync(Project project, string filePath)
    {
        try
        {
            // Write to a temporary file first to avoid data loss in case of an error during the write process.
            var tempFilePath = filePath + ".tmp." + Guid.NewGuid().ToString("N");
            
            await SchemaUtil.SaveProjectAsync(ProjectSchema.Pack(project, FileVersion), tempFilePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempFilePath, filePath);
        }
        catch (Exception e)
        {
            throw new FileServiceException($"Failed to write file: {e.Message}", e);
        }
    }

    public async Task<string?> SaveAsAsync(Project project, string? filePath = null)
    {
        var suggestedFileName = filePath is not null
            ? Path.GetFileNameWithoutExtension(filePath)
            : "Untitled";

        IStorageFolder? suggestedStartLocation = null;

        if (filePath is not null)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (directory is not null)
            {
                suggestedStartLocation = await _owner.StorageProvider.TryGetFolderFromPathAsync(directory);
            }
        }

        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project As",
            SuggestedFileName = suggestedFileName,
            SuggestedStartLocation = suggestedStartLocation,
            DefaultExtension = "spad",
            FileTypeChoices = [ProjectFileType]
        });

        var path = file?.TryGetLocalPath();

        if (path is null)
        {
            return null;
        }

        await SaveAsync(project, path);

        return path;
    }
}

