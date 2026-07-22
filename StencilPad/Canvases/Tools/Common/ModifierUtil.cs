using Avalonia.Input;

namespace StencilPad.Canvases.Tools.Common;

// NOTE: Avalonia has no equivalent of WPF's Keyboard.IsKeyDown global
// keyboard-state polling API. Properly supporting this would mean threading
// live KeyModifiers from pointer/key event args through many call layers
// across several tool overlays/controllers - a real refactor, not a
// mechanical port. Stubbed out for now (always "not held") so the app
// compiles and runs; modifier-key behaviours (multi-select, axis/aspect
// lock, angle snap) are effectively disabled until this is revisited.
public static class ModifierUtil
{
    public static bool IsModifyingSelection(IKeyModifiersEventArgs args)
    {
        return args.KeyModifiers.HasFlag(KeyModifiers.Control);
    }

    public static bool IsLockToAxis(IKeyModifiersEventArgs args)
    {
        return args.KeyModifiers.HasFlag(KeyModifiers.Shift);
    }

    public static bool IsLockAspect(IKeyModifiersEventArgs args)
    {
        return args.KeyModifiers.HasFlag(KeyModifiers.Shift);
    }

    public static bool IsAngleSnap(IKeyModifiersEventArgs args)
    {
        return args.KeyModifiers.HasFlag(KeyModifiers.Shift);
    }
}
