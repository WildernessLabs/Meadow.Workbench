using Meadow.TestSuite;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Net;

namespace Meadow.Workbench;

public delegate void StatusChangedHandler(string status);

public class TestSuiteViewModel : ViewModelBase
{
    private string? _assemblyPath;
    private string[]? _assemblies;
    private bool _localVisible;
    private RestTestDirector? _director;
    private string? _connectionStatus = "Not tested.";

    public ObservableCollection<string> AssembliesToSend { get; } = new ObservableCollection<string>();
    public ObservableCollection<string> DeviceAssemblies { get; } = new ObservableCollection<string>();
    public ObservableCollection<string> KnownTests { get; } = new ObservableCollection<string>();
    public ObservableCollection<TestResult> KnownResults { get; } = new ObservableCollection<TestResult>();

    public event StatusChangedHandler StatusChanged = delegate { };

    internal RestTestDirector Director { get; set; }

    public TestSuiteViewModel()
    {
        AssembliesToSend.CollectionChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(SendEnabled));
        };
    }

    public string? ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public async Task<bool> CreateDirector(IPEndPoint meadowAddress)
    {
        _director = new RestTestDirector(meadowAddress);

        try
        {
            ConnectionStatus = $"Testing...";

            var info = await _director.GetInfo();

            ConnectionStatus = $"Device: {info.Name} version {info.Version}";

            this.RaisePropertyChanged(nameof(SendEnabled));


            _ = RefreshRemoteAssemblies();
            _ = RefreshKnownTests();

            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            _director = null;
            return false;
        }
    }

    public async Task ResetDevice()
    {
        if (_director == null) return;

        await _director.ResetDevice();
    }

    public async Task EnableDebugOutput()
    {
        if (_director == null) return;

        await _director.SetDebug(true);
    }

    public async Task<DateTime> GetDeviceTime()
    {
        if (_director == null) return DateTime.MinValue;

        return await _director.GetTime();
    }

    public async Task SetDeviceTime(DateTime time)
    {
        if (_director == null) return;

        await _director.SetTime(time);
    }

    public string? LocalAssemblyPath
    {
        get => _assemblyPath;
        set
        {
            if (value == LocalAssemblyPath) return;
            _assemblyPath = value;
            this.RaisePropertyChanged();

            // get assemblies
            string[]? files = null;
            if (LocalAssemblyPath != null)
            {
                var di = new DirectoryInfo(LocalAssemblyPath);
                if (di.Exists)
                {
                    files = di.GetFiles("*.dll").Select(fi => fi.Name).ToArray();
                }
            }

            Assemblies = files;
        }
    }

    public GridLength Col0Width
    {
        get => LocalIsVisible ? GridLength.Star : new GridLength(0);
    }

    public GridLength Col1Width
    {
        get => LocalIsVisible ? new GridLength(80) : new GridLength(0);
    }

    public GridLength Col4Width
    {
        get => LocalIsVisible ? new GridLength(0) : GridLength.Star;
    }

    public bool LocalIsVisible
    {
        get => _localVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _localVisible, value);
            this.RaisePropertyChanged(nameof(Col0Width));
            this.RaisePropertyChanged(nameof(Col1Width));
            this.RaisePropertyChanged(nameof(Col4Width));
        }
    }

    public string[]? Assemblies
    {
        get => _assemblies;
        set => this.RaiseAndSetIfChanged(ref _assemblies, value);
    }

    public async void SendAssemblies()
    {
        if (_director == null) return;

        foreach (var asm in AssembliesToSend.ToArray()) // copy to an array to prevent error on modification
        {
            StatusChanged?.Invoke($"Sending {asm}...");

            var fi = new FileInfo(Path.Combine(LocalAssemblyPath ?? String.Empty, asm));
            if (fi.Exists)
            {
                try
                {
                    await _director.SendFile(fi);
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke(ex.Message);
                }
            }
        }

        StatusChanged?.Invoke($"Done.");

        await RefreshRemoteAssemblies();
    }

    public async void RefreshTestsCommand()
    {
        await RefreshKnownTests();
    }

    public async void RefreshAssembliesCommand()
    {
        await RefreshRemoteAssemblies();
    }

    public async void RefreshResultsCommand()
    {
        await RefreshKnownResults();
    }

    public void AddAssembliesCommand()
    {
        LocalIsVisible = !LocalIsVisible;
    }

    public string? SelectedTest
    {
        get; set;
    }

    public bool SendEnabled
    {
        get
        {
            if (_director == null) return false;
            if (AssembliesToSend == null) return false;
            return AssembliesToSend.Count > 0;
        }
    }

    public async void RunTestsCommand()
    {
        if (SelectedTest == null) return;
        if (_director == null) return;

        StatusChanged?.Invoke($"Starting test...");

        await _director.ExecuteTest(SelectedTest);

        StatusChanged?.Invoke($"Done.");
    }

    internal async Task RefreshRemoteAssemblies()
    {
        if (_director == null) return;

        DeviceAssemblies.Clear();

        StatusChanged?.Invoke($"Getting assemblies...");

        var remote = await _director.GetAssemblies();

        if (remote != null)
        {
            foreach (var a in remote)
            {
                DeviceAssemblies.Add(a);
            }
        }

        StatusChanged?.Invoke($"Done.");
    }

    internal async Task RefreshKnownTests()
    {
        if (_director == null) return;

        KnownTests.Clear();

        StatusChanged?.Invoke($"Getting tests...");

        var remote = await _director.GetTestNames();

        if (remote != null)
        {
            foreach (var a in remote)
            {
                KnownTests.Add(a);
            }
        }

        StatusChanged?.Invoke($"Done.");
    }

    private async Task RefreshKnownResults()
    {
        if (_director == null) return;

        KnownResults.Clear();

        StatusChanged?.Invoke($"Getting results...");

        var remote = await _director.GetTestResults();

        if (remote != null)
        {
            foreach (var a in remote)
            {
                KnownResults.Add(a);
            }
        }

        StatusChanged?.Invoke($"Done.");
    }
}
