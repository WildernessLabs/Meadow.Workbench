using Microsoft.Extensions.Logging;

namespace Meadow.Workbench.ViewModels;

public delegate void LogInfoHandler(LogLevel level, string info);

public class CaptureLogger : ILogger
{
    public event LogInfoHandler OnLogInfo = delegate { };

    public IDisposable BeginScope<TState>(TState state) => default!;

    public CaptureLogger()
    {
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (state is string s)
        {
            OnLogInfo(logLevel, s);
        }
        else
        {
            OnLogInfo(logLevel, state.ToString());
        }
    }
}
