using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace StencilPad.UI.Dialogs;

// NOTE: Avalonia has no built-in MessageBox equivalent (unlike WPF's
// System.Windows.MessageBox). Rather than add a third-party package
// dependency, this is a small self-contained replacement covering just the
// OK and Yes/No cases WpfDialogService actually needs.
public enum SimpleMessageBoxButtons
{
    Ok,
    YesNo
}

public enum SimpleMessageBoxResult
{
    Ok,
    Yes,
    No
}

public class MessageBoxWindow : Window
{
    private SimpleMessageBoxResult _result;

    public MessageBoxWindow(string message, string title, SimpleMessageBoxButtons buttons,
                            SimpleMessageBoxResult defaultResult)
    {
        Title = title;
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        MinWidth = 300;
        _result = defaultResult;

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        if (buttons == SimpleMessageBoxButtons.Ok)
        {
            buttonsPanel.Children.Add(CreateButton("OK", SimpleMessageBoxResult.Ok, isDefault: true));
        }
        else
        {
            buttonsPanel.Children.Add(CreateButton("Yes", SimpleMessageBoxResult.Yes,
                isDefault: defaultResult == SimpleMessageBoxResult.Yes));
            buttonsPanel.Children.Add(CreateButton("No", SimpleMessageBoxResult.No,
                isDefault: defaultResult == SimpleMessageBoxResult.No));
        }

        Content = new Grid
        {
            Margin = new Avalonia.Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,16,Auto"),
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 400 },
                buttonsPanel
            }
        };

        Grid.SetRow((Control)((Grid)Content).Children[0], 0);
        Grid.SetRow(buttonsPanel, 2);
    }

    private Button CreateButton(string text, SimpleMessageBoxResult result, bool isDefault)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 75,
            Margin = new Avalonia.Thickness(8, 0, 0, 0),
            IsDefault = isDefault
        };

        button.Click += (_, _) =>
        {
            _result = result;
            Close();
        };

        return button;
    }

    public static async Task<SimpleMessageBoxResult> ShowAsync(Window owner, string message, string title,
                                                                SimpleMessageBoxButtons buttons,
                                                                SimpleMessageBoxResult defaultResult = SimpleMessageBoxResult.Ok)
    {
        var box = new MessageBoxWindow(message, title, buttons, defaultResult);

        await box.ShowDialog(owner);

        return box._result;
    }
}
