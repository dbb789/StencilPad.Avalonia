using StencilPad.Spatial;

namespace StencilPad.Services;

public interface IDialogService
{
    Task<string?> ShowRenameDialogAsync(string currentName);
    Task<(Unit Spacing, int Subdivisions)?> ShowGridSettingsDialogAsync(Unit currentSpacing,
                                                                        int currentSubdivisions,
                                                                        UnitSettings unitSettings);
    Task<Fraction?> ShowUnitScaleDialogAsync(Fraction current);
    Task<bool> ShowConfirmationAsync(string message, string title, bool defaultYes = true);
    Task ShowWarningAsync(string message, string title);
    Task ShowErrorAsync(string message, string title);
}
