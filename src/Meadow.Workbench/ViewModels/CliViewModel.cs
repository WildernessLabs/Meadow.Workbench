using ReactiveUI;

namespace Meadow.Workbench;

public class CliViewModel : ViewModelBase
{
    private string? _stuff;

    public CliViewModel()
    {
        Stuff = "Put CLI content here";
    }

    public string? Stuff
    {
        get => _stuff;
        set => this.RaiseAndSetIfChanged(ref _stuff, value);
    }

}
