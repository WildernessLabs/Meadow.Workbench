using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Meadow.Workbench.ViewModels;

public class LogRecord
{
    public LogRecord()
    {
    }

    public LogRecord(string recordText)
    {
        RecordText = recordText;
        Timestamp = DateTime.Now;
    }

    public LogRecord(DateTime timestamp, string recordText)
    {
        RecordText = recordText;
        Timestamp = timestamp;
    }

    public DateTime Timestamp { get; init; }
    public string RecordText { get; init; }
}

public class LoggerViewModel : FeatureViewModel
{
    private const int PORT = 5100;
    private const char DELIMITER = '\t';

    private bool _isConnected;
    private UdpClient? _udpClient;

    public ObservableCollection<LogRecord> LogRecords { get; } = new();
    public IReactiveCommand ClearLogCommand { get; }
    public IReactiveCommand ConnectCommand { get; }
    public IReactiveCommand DisconnectCommand { get; }

    public LoggerViewModel()
    {
        ClearLogCommand = ReactiveCommand.Create(ClearLog);
        ConnectCommand = ReactiveCommand.Create(Connect);
        DisconnectCommand = ReactiveCommand.Create(Disconnect);
        IsConnected = false;
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    private void ClearLog()
    {
        LogRecords.Clear();
    }

    private void Connect()
    {
        if (_udpClient == null)
        {
            _udpClient = new UdpClient();
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
        }
        _udpClient.BeginReceive(UdpCallback, null);
        IsConnected = true;
    }

    private void UdpCallback(IAsyncResult ar)
    {
        var from = new IPEndPoint(0, 0);

        var recvBuffer = _udpClient?.EndReceive(ar, ref from);

        if (recvBuffer != null)
        {
            var payload = Encoding.UTF8.GetString(recvBuffer);
            var parts = payload.Split([DELIMITER]);

            LogRecords.Add(new LogRecord(parts[1].Trim()));
        }
        _udpClient?.BeginReceive(UdpCallback, null);
    }

    private void Disconnect()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;
        IsConnected = false;
    }
}
