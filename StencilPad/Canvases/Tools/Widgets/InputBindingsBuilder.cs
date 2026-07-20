using Avalonia.Input;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Common;
using StencilPad.Models;

namespace StencilPad.Canvases.Tools.Widgets;

// NOTE: WPF's InputBindingCollection/KeyBinding(ICommand, Key, ModifierKeys)/
// RelayCommand model doesn't map mechanically onto Avalonia. RelayCommand was
// removed earlier as a flagged non-mechanical item, and wiring real Avalonia
// KeyBindings needs an actual ICommand implementation (e.g. from
// CommunityToolkit.Mvvm) plus KeyGesture-based bindings. Hotkey registration
// is stubbed out here - Add() compiles but registers nothing - until that
// redesign happens.
public class InputBindingsBuilder
{
    private readonly Sheet _sheet;
    private readonly Action<ISheetElementAction>? _actionInvoked;

    public InputBindingsBuilder(Sheet sheet,
                                Action<ISheetElementAction>? actionInvoked,
                                IList<KeyBinding> inputBindings)
    {
        _sheet = sheet;
        _actionInvoked = actionInvoked;
    }

    public void Add(Key key, KeyModifiers modifiers, params ISheetElementAction[] actionSet)
    {
        // TODO: Wire up real Avalonia KeyBindings once a command implementation
        // replaces WPF's RelayCommand.
    }
}
