using System.Windows.Input;
using Avalonia.Input;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Models;

namespace StencilPad.Canvases.Tools.Widgets;

public class InputBindingsBuilder
{
    private class BoundActionCommand : ICommand
    {
        private readonly Sheet _sheet;
        private readonly Action<ISheetElementAction> _actionInvoked;
        private readonly ISheetElementAction[] _actionSet;

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        
        public BoundActionCommand(Sheet sheet,
                                  Action<ISheetElementAction> actionInvoked,
                                  ISheetElementAction[] actionSet)
        {
            _sheet = sheet;
            _actionInvoked = actionInvoked;
            _actionSet = actionSet;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            foreach (var action in _actionSet)
            {
                if (action.IsVisible(_sheet, _sheet.Selection)
                    && action.IsEnabled(_sheet, _sheet.Selection))
                {
                    _actionInvoked.Invoke(action);
                    return;
                }
            }
        }
    }
    
    private readonly Sheet _sheet;
    private readonly Action<ISheetElementAction> _actionInvoked;
    private readonly IList<KeyBinding> _inputBindings;
    
    public InputBindingsBuilder(Sheet sheet,
                                Action<ISheetElementAction> actionInvoked,
                                IList<KeyBinding> inputBindings)
    {
        _sheet = sheet;
        _actionInvoked = actionInvoked;
        _inputBindings = inputBindings;
    }

    public void Add(Key key, KeyModifiers modifiers, params ISheetElementAction[] actionSet)
    {
        _inputBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(key, modifiers),
            Command = BindCommand(actionSet)
        });
    }

    private ICommand BindCommand(ISheetElementAction[] actionSet)
    {
        return new BoundActionCommand(_sheet, _actionInvoked, actionSet);
    }
}
