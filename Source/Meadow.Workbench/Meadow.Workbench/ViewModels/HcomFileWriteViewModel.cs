using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;

namespace Meadow.Workbench.ViewModels;

public class HcomFileWriteViewModel : ViewModelBase
{
    private string? _lastStatus;
    private bool _writeComplete = false;
    private string? _heading1 = string.Empty;
    private string? _heading2 = string.Empty;

    public ILogger Logger { get; }

    public HcomFileWriteViewModel()
    {
        var logger = new LogIntercepter();
        logger.LogMessageReceived += Logger_LogMessageReceived;
        Logger = logger;
    }

    private void Logger_LogMessageReceived(object? sender, string e)
    {
        Heading2Message = e;
    }

    public void FileWriteProgressHandler(object sender, string status, bool isComplete, bool isError)
    {
        Heading1Message = status;
        _writeComplete = isComplete || isError;
    }

    public string? Heading1Message
    {
        get => _heading1;
        set => this.RaiseAndSetIfChanged(ref _heading1, value);
    }

    public string? Heading2Message
    {
        get => _heading2;
        set => this.RaiseAndSetIfChanged(ref _heading2, value);
    }

    public bool WriteComplete
    {
        get => _writeComplete;
        set => this.RaiseAndSetIfChanged(ref _writeComplete, value);
    }

    private class LogIntercepter : ILogger
    {
        public event EventHandler<string>? LogMessageReceived;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogMessageReceived?.Invoke(this, $"{formatter(state, exception)}");
        }
    }

}
