using System.IO;
using StencilPad.Models;
using StencilPad.Schemas;

namespace StencilPad.Services;

// NOTE: The file-open/save-as dialogs here were WPF-only (Microsoft.Win32
// OpenFileDialog/SaveFileDialog) and have been stubbed out rather than
// silently ported wrong. Avalonia needs the async TopLevel.StorageProvider
// API for file pickers, which requires a window/TopLevel reference not
// currently threaded through this service. This needs a proper redesign,
// not a mechanical swap. OpenAsync(string, Project) and SaveAsync(Project,
// string), which do not involve dialogs, are unaffected and still work.
public class FileService : IFileService
{
    private const int FileVersion = 1;

    public Task<string?> OpenAsync(Project target)
    {
        // TODO: Port to Avalonia's TopLevel.StorageProvider.OpenFilePickerAsync.
        throw new NotImplementedException(
            "File open dialog needs porting to Avalonia's TopLevel.StorageProvider.");
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

    public Task<string?> SaveAsAsync(Project project, string? filePath = null)
    {
        // TODO: Port to Avalonia's TopLevel.StorageProvider.SaveFilePickerAsync.
        throw new NotImplementedException(
            "File save-as dialog needs porting to Avalonia's TopLevel.StorageProvider.");
    }
}
