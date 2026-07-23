namespace CommunityToolkit.Mvvm.Input;

public class BoundRelayCommand : IBoundRelayCommand
{
    public IRelayCommand? Command
    {
        get => _command;
        set
        {
            if (_command != value)
            {
                if (_command != null)
                {
                    _command.CanExecuteChanged -= OnCommandCanExecuteChanged;
                }

                _command = value;

                if (_command != null)
                {
                    _command.CanExecuteChanged += OnCommandCanExecuteChanged;
                }

                NotifyCanExecuteChanged();
            }
        }
    }

    private IRelayCommand? _command;
    
    public event EventHandler? CanExecuteChanged;

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(object? parameter)
    {
        return Command?.CanExecute(parameter) ?? false;
    }

    public void Execute(object? parameter)
    {
        Command?.Execute(parameter);
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        CanExecuteChanged?.Invoke(this, e);
    }
}
