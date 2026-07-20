using Avalonia.Controls;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Models;

namespace StencilPad.Canvases.Tools.Widgets;

public class ContextMenuBuilder
{
    private readonly Sheet _sheet;
    private readonly Action<ISheetElementAction>? _actionInvoked;
    
    public ContextMenuBuilder(Sheet sheet,
                              Action<ISheetElementAction>? actionInvoked)
    {
        _sheet = sheet;
        _actionInvoked = actionInvoked;
    }
    
    public bool AddContextMenuItemSet(ItemCollection items,
                                      params (ISheetElementAction Action, string Title, string InputGestureText)[] actions)
    {
        bool addedAny = false;

        foreach (var action in actions)
        {
            if (AddContextMenuItem(items,
                                   action.Action,
                                   action.Title,
                                   action.InputGestureText))
            {
                addedAny = true;
            }
        }

        return addedAny;
    }

    public bool AddContextMenuItem(ItemCollection items,
                                   ISheetElementAction action,
                                   string title,
                                   string inputGestureText = "")
    {
        if (action is null || !action.IsVisible(_sheet, _sheet.Selection))
        {
            return false;
        }

        var header = string.IsNullOrEmpty(inputGestureText) ? title : $"{title} ({inputGestureText})";

        // NOTE: Avalonia's MenuItem has no InputGestureText/HotKey property like WPF's,
        // so the gesture hint is appended directly to the header text instead.
        var menuItem = new MenuItem
        {
            Header = header,
            IsEnabled = action.IsEnabled(_sheet, _sheet.Selection)
        };

        menuItem.Click += (_, _) => _actionInvoked?.Invoke(action);

        items.Add(menuItem);

        return true;
    }
}
