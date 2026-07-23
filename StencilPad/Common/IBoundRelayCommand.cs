namespace CommunityToolkit.Mvvm.Input;

public interface IBoundRelayCommand : IRelayCommand
{
    IRelayCommand? Command { get; set; }
}
