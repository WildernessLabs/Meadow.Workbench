using ReactiveUI;

namespace Meadow.Workbench.ViewModels;

public class UserMessageViewModel : ViewModelBase
{
    private string _messageText;

    public UserMessageViewModel(string message)
    {
        UserMessage = message;
    }

    public string UserMessage
    {
        get => _messageText;
        set => this.RaiseAndSetIfChanged(ref _messageText, value);
    }
}
