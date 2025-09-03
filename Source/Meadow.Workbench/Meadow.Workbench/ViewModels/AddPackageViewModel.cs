using DialogHostAvalonia;
using Meadow.Workbench.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Workbench.ViewModels;

internal class AddPackageViewModel : ViewModelBase
{
    private string _packageName = string.Empty;
    private string _packageVersion = string.Empty;
    private string _description = string.Empty;
    private PackageTarget _selectedTarget = PackageTarget.MeadowF7;

    public List<PackageTarget> AvailableTargets { get; } = Enum.GetValues<PackageTarget>().ToList();

    public IReactiveCommand OkCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public bool IsCancelled { get; private set; }

    public string PackageName
    {
        get => _packageName;
        set => this.RaiseAndSetIfChanged(ref _packageName, value);
    }

    public string PackageVersion
    {
        get => _packageVersion;
        set => this.RaiseAndSetIfChanged(ref _packageVersion, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public PackageTarget SelectedTarget
    {
        get => _selectedTarget;
        set => this.RaiseAndSetIfChanged(ref _selectedTarget, value);
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(PackageName) &&
                          !string.IsNullOrWhiteSpace(PackageVersion);

    public AddPackageViewModel()
    {
        var canExecute = this.WhenAnyValue(
            x => x.PackageName,
            x => x.PackageVersion,
            (name, version) => !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version));

        OkCommand = ReactiveCommand.Create(OnOk, canExecute);
        CancelCommand = ReactiveCommand.Create(OnCancel);

        // Set default version
        PackageVersion = "1.0.0";
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

    public Package CreatePackage()
    {
        var packageId = Guid.NewGuid().ToString();
        var fileName = $"{PackageName}_{PackageVersion}_{SelectedTarget}.pkg";

        return new Package
        {
            PackageID = packageId,
            AppName = PackageName,
            AppVersion = PackageVersion,
            Description = Description,
            Target = SelectedTarget,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow
        };
    }
}