using DialogHostAvalonia;
using ReactiveUI;
using System;

namespace Meadow.Workbench.ViewModels;

public class UserMessageViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private bool _isQuestion = false;

    public IReactiveCommand OkCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public bool IsCancelled { get; private set; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public bool IsQuestion
    {
        get => _isQuestion;
        set => this.RaiseAndSetIfChanged(ref _isQuestion, value);
    }

    [Obsolete("Use Message property instead")]
    public string UserMessage
    {
        get => Message;
        set => Message = value;
    }

    public UserMessageViewModel()
    {
        OkCommand = ReactiveCommand.Create(OnOk);
        CancelCommand = ReactiveCommand.Create(OnCancel);
    }

    public UserMessageViewModel(string message) : this()
    {
        Message = message;
        Title = "Message";
        IsQuestion = false;
    }

    private void OnOk()
    {
        IsCancelled = false;
        DialogHost.Close(null);
    }

    private void OnCancel()
    {
        IsCancelled = true;
        DialogHost.Close(null);
    }
}
