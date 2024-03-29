﻿using DialogHostAvalonia;
using Meadow.Workbench.Dialogs;
using Meadow.Workbench.Services;
using ReactiveUI;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IMeadowCloudClient = Meadow.Cloud.Client.IMeadowCloudClient;

namespace Meadow.Workbench.ViewModels;

public class FirmwareViewModel : FeatureViewModel
{
    private FirmwarePackageViewModel? _selectedFirmware;
    private string? _latestAvailable;
    private bool _makeDownloadDefault = true;
    private bool _flashCoprocessor;
    private bool _flashAll;
    private bool _flashOS;
    private bool _flashRuntime;
    private readonly DeviceService _deviceService;
    private string? _selectedRoute;
    private bool _useDfu = true;
    private bool _defuDeviceAvailable;
    private readonly FirmwareService _firmwareService;
    private readonly IMeadowCloudClient? _meadowCloudClient;

    public ObservableCollection<FirmwarePackageViewModel> FirmwareVersions { get; } = new();
    public ObservableCollection<string> ConnectedRoutes { get; } = new();

    public IReactiveCommand DownloadLatestCommand { get; }
    public IReactiveCommand MakeDefaultCommand { get; }
    public IReactiveCommand DeleteFirmwareCommand { get; }
    public IReactiveCommand FlashCommand { get; }
    public IReactiveCommand RevealFirmwareFolderCommand { get; }
    public IReactiveCommand RefreshLocalStoreCommand { get; }
    public IReactiveCommand DownloadSpecificCommand { get; }

    public FirmwareViewModel()
    {
        _deviceService = Locator.Current.GetService<DeviceService>();
        _firmwareService = Locator.Current.GetService<FirmwareService>();
        _meadowCloudClient = Locator.Current.GetService<IMeadowCloudClient>();

        foreach (var d in _deviceService.KnownDevices)
        {
            if (d.IsConnected && d.LastRoute != null)
            {
                ConnectedRoutes.Add(d.LastRoute);
            }
        }

        _deviceService!.DeviceConnected += OnDeviceConnected;
        _deviceService!.DeviceDisconnected += OnDeviceDisconnected;
        _deviceService!.DeviceRemoved += OnDeviceRemoved;

        _ = RefreshCurrentStore();

        DownloadLatestCommand = ReactiveCommand.CreateFromTask(DownloadLatest);
        DownloadSpecificCommand = ReactiveCommand.CreateFromTask(DownloadSpecific);
        MakeDefaultCommand = ReactiveCommand.CreateFromTask(MakeSelectedTheDefault);
        DeleteFirmwareCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFirmware);
        FlashCommand = ReactiveCommand.CreateFromTask(FlashSelectedFirmware);
        RevealFirmwareFolderCommand = ReactiveCommand.Create(RevealFirmwareRootFolder);
        RefreshLocalStoreCommand = ReactiveCommand.CreateFromTask(RefreshCurrentStore);

        Task.Run(CheckForUpdate);
    }

    private async Task RefreshCurrentStore()
    {
        FirmwareVersions.Clear();

        if (_firmwareService.CurrentStore == null)
        {
            await _firmwareService.SelectStore();
        }
        else
        {

            await _firmwareService.CurrentStore.Refresh();
        }

        foreach (var fw in _firmwareService.CurrentStore)
        {
            FirmwareVersions.Add(
                new FirmwarePackageViewModel(
                    fw, fw == _firmwareService.CurrentStore.DefaultPackage));
        }

        this.RaisePropertyChanged(nameof(UpdateIsAvailable));
    }

    public bool UsingDfu
    {
        get => _useDfu;
        set
        {
            this.RaiseAndSetIfChanged(ref _useDfu, value);

            _ = CheckForDfuDevice();
        }
    }

    private Task CheckForDfuDevice()
    {
        return Task.Run(() =>
        {
            DfuDeviceAvailable = _deviceService.IsLibUsbDeviceConnected();
        });
    }

    private void OnDeviceConnected(object? sender, DeviceInformation e)
    {
        if (e.LastRoute != null)
        {
            ConnectedRoutes.Add(e.LastRoute);
        }
    }

    private void OnDeviceDisconnected(object? sender, DeviceInformation e)
    {
        if (e.LastRoute != null)
        {
            ConnectedRoutes.Remove(e.LastRoute);
        }
    }

    private void OnDeviceRemoved(object? sender, string e)
    {
        ConnectedRoutes.Remove(e);
    }

    public bool FlashAll
    {
        get => _flashAll;
        set
        {
            if (value == FlashAll) return;
            this.RaiseAndSetIfChanged(ref _flashAll, value);

            FlashOS = value;
            FlashRuntime = value;
            FlashCoprocessor = value;
        }
    }

    public bool FlashOS
    {
        get => _flashOS;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashOS, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool FlashRuntime
    {
        get => _flashRuntime;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashRuntime, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool FlashCoprocessor
    {
        get => _flashCoprocessor;
        set
        {
            this.RaiseAndSetIfChanged(ref _flashCoprocessor, value);
            if (!value)
            {
                _flashAll = false;
                this.RaisePropertyChanged(nameof(FlashAll));
            }
        }
    }

    public bool DfuDeviceAvailable
    {
        get => _defuDeviceAvailable;
        set
        {
            this.RaiseAndSetIfChanged(ref _defuDeviceAvailable, value);
        }
    }

    private void RevealFirmwareRootFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo("explorer", $"\"{_firmwareService.CurrentStore.PackageFileRoot}\""));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // TODO
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO
        }
    }

    private async Task FlashSelectedFirmware()
    {
        if (SelectedFirmwareVersion == null) return;

        if (UsingDfu)
        {
            await _deviceService.FlashFirmwareWithDfu(SelectedRoute, FlashOS, FlashRuntime, FlashCoprocessor, SelectedFirmwareVersion.Version);
        }
        else
        {
            await _deviceService.FlashFirmwareWithOtA(SelectedRoute, FlashOS, FlashCoprocessor, SelectedFirmwareVersion.Version);
        }
    }

    private async Task DeleteSelectedFirmware()
    {
        if (SelectedFirmwareVersion == null) return;
        try
        {
            await _firmwareService.CurrentStore.DeletePackage(SelectedFirmwareVersion.Version);
            await RefreshCurrentStore();
        }
        catch (Exception ex)
        {
            // TODO: log this?
            Debug.WriteLine(ex.Message);
            await RefreshCurrentStore();
        }
    }

    private async Task MakeSelectedTheDefault()
    {
        if (SelectedFirmwareVersion == null) return;
        try
        {
            await _firmwareService.CurrentStore.SetDefaultPackage(SelectedFirmwareVersion.Version);
            await RefreshCurrentStore();
        }
        catch (Exception ex)
        {
            // TODO: log this?
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task DownloadSpecific()
    {
        string? version = null;

        var inputDialog = new InputBoxDialog(
            "Download Specific Version",
            string.Empty,
            "Version to retrieve:");

        var result = await DialogHost.Show(inputDialog, closingEventHandler: (s, e) =>
        {
            if (!inputDialog.IsCancelled && inputDialog.Text != null)
            {
                version = inputDialog.Text;
            }
        });

        if (version == null) { return; }

        if (!await _meadowCloudClient!.Authenticate())
        {
            var authDialog = new NotAuthenticatedDialog(new NotAuthenticatedViewModel(NotAuthenticatedViewModel.AuthReason.FirmwareDownload));

            // notify user to log in
            await DialogHost.Show(authDialog);
            // get the auth token
            await _meadowCloudClient!.Authenticate();
        }

        var umvm = new UserMessageViewModel(Strings.UserMessageDownloadingFirmware);

        var messageDialog = new UserMessageDialog(umvm);
        _ = DialogHost.Show(messageDialog);

        try
        {
            try
            {
                if (!await _firmwareService.CurrentStore!.RetrievePackage(version, true))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached) Debugger.Break();
                // TODO show a dialog
                // TODO: log?
                return;
            }
        }
        finally
        {
            DialogHost.Close(null);
        }

        await RefreshCurrentStore();
    }

    private async Task DownloadLatest()
    {
        if (LatestAvailableVersion == null) return;

        // TODO: progress indicator
        // _store.DownloadProgress += ....

        if (!await _meadowCloudClient!.Authenticate())
        {
            var dialog = new NotAuthenticatedDialog(new NotAuthenticatedViewModel(NotAuthenticatedViewModel.AuthReason.FirmwareDownload));

            // notify user to log in
            await DialogHost.Show(dialog);
            // get the auth token
            await _meadowCloudClient!.Authenticate();
        }

        var umvm = new UserMessageViewModel(Strings.UserMessageDownloadingFirmware);

        var messageDialog = new UserMessageDialog(umvm);
        _ = DialogHost.Show(messageDialog);

        try
        {
            try
            {
                if (!await _firmwareService.CurrentStore!.RetrievePackage(LatestAvailableVersion, true))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached) Debugger.Break();
                // TODO show a dialog
                // TODO: log?
                return;
            }

            if (MakeDownloadDefault)
            {
                await _firmwareService.CurrentStore.SetDefaultPackage(LatestAvailableVersion);
            }
        }
        finally
        {
            DialogHost.Close(null);
        }

        await RefreshCurrentStore();
    }

    public bool UpdateIsAvailable
    {
        get
        {
            if (LatestAvailableVersion == null || _firmwareService.CurrentStore == null) return false;

            return !_firmwareService.CurrentStore.Any(f => f.Version == LatestAvailableVersion);
        }
    }

    public bool MakeDownloadDefault
    {
        get => _makeDownloadDefault;
        private set => this.RaiseAndSetIfChanged(ref _makeDownloadDefault, value);
    }

    public string? SelectedRoute
    {
        get => _selectedRoute;
        private set
        {
            this.RaiseAndSetIfChanged(ref _selectedRoute, value);

            var device = _deviceService.KnownDevices.FirstOrDefault(d => d.LastRoute == _selectedRoute);
            if (device != null)
            {
                if (Version.TryParse(device.OsVersion, out Version? v))
                {
                    if (v != null)
                    {
                        if (v.Minor < 7)
                        {
                            UsingDfu = true;
                            return;
                        }
                    }
                }
            }
            UsingDfu = false;
        }
    }

    public string? LatestAvailableVersion
    {
        get => _latestAvailable;
        private set => this.RaiseAndSetIfChanged(ref _latestAvailable, value);
    }

    private async Task CheckForUpdate()
    {
        var latest = await _firmwareService.CurrentStore.GetLatestAvailableVersion();

        if (latest != null)
        {
            LatestAvailableVersion = latest;
            this.RaisePropertyChanged(nameof(UpdateIsAvailable));
        }
    }

    public FirmwarePackageViewModel? SelectedFirmwareVersion
    {
        get => _selectedFirmware;
        set => this.RaiseAndSetIfChanged(ref _selectedFirmware, value);
    }
}
