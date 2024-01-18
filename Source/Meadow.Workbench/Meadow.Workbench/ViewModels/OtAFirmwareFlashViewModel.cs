using Meadow.Workbench.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Workbench.ViewModels;

public class SoftwareFlashTimelineEntry
{
    public TimeSpan Time { get; set; }
    public string Activity { get; set; }
    public string LedState { get; set; }
}

public class OtAFirmwareFlashViewModel : ViewModelBase
{
    private int _startTick;
    private Timer? _timer;
    private TimeSpan _timestamp;
    private SoftwareFlashTimelineEntry? _activeTimelineEntry;

    public OtAFirmwareFlashViewModel()
    {
        // TODO: get these based on selected options, platform, etc
        Timeline =
        [
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(0),
                Activity = "Device reset",
                LedState = "Off"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(1),
                Activity = "Transfer OS Image",
                LedState = "Solid Blue"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(40),
                Activity = "Transfer Runtime Image",
                LedState = "Solid Blue"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(100),
                Activity = "Device reset",
                LedState = "Off"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(101),
                Activity = "Validating binaries",
                LedState = "Off"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(130),
                Activity = "Flashing OS",
                LedState = "Solid Blue/Flashing Green"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(165),
                Activity = "Verifying flash",
                LedState = "Solid Green"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(170),
                Activity = "Flashing Runtime",
                LedState = "Solid Blue/Flashing Green"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(230),
                Activity = "Device reset",
                LedState = "Off"
            },
            new SoftwareFlashTimelineEntry
            {
                Time = TimeSpan.FromSeconds(231),
                Activity = "Flash Complete",
                LedState = "Solid Blue"
            },
        ];

        ResetTimer();
    }

    internal OtAFirmwareFlashViewModel(DeviceService deviceService, string route, bool flashOS, bool flashCoprocessor)
        : this()
    {
        Task.Run(() =>
        {
            _ = deviceService.FlashFirmwareWithOtA(
                route,
                flashOS,
                flashCoprocessor);
        });

        this.RaisePropertyChanged(nameof(Timeline));
    }

    public List<SoftwareFlashTimelineEntry> Timeline { get; }

    public SoftwareFlashTimelineEntry? ActiveTimelineEntry
    {
        get => _activeTimelineEntry;
        set => this.RaiseAndSetIfChanged(ref _activeTimelineEntry, value);
    }

    public TimeSpan Timestamp
    {
        get => _timestamp;
        set
        {
            SoftwareFlashTimelineEntry? s = null;

            foreach (var entry in Timeline)
            {
                if (entry.Time > value) break;
                s = entry;
            }
            ActiveTimelineEntry = s;
            this.RaiseAndSetIfChanged(ref _timestamp, value);
        }
    }

    public void StopTimer()
    {
        if (_timer == null) return;

        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void ResetTimer()
    {
        if (_timer != null)
        {
            _timer.Dispose();
        }

        _startTick = Environment.TickCount;
        _timer = new Timer((o) =>
        {
            var et = Environment.TickCount - _startTick;
            Timestamp = TimeSpan.FromMilliseconds(et);
        },
        null,
        1000,
        1000);

    }
}