using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Meadow.Workbench.Services;

internal class SerialPortMonitor
{
    public event EventHandler<string> PortConnected;
    public event EventHandler<string> PortDisconnected;

    private List<string> _knownSerialPorts = new();

    public SerialPortMonitor()
    {
        foreach (var p in System.IO.Ports.SerialPort.GetPortNames().Distinct())
        {
            lock (_knownSerialPorts)
            {
                _knownSerialPorts.Add(p);
            }
        }

        _ = Task.Run(MonitorProc);
    }

    public IEnumerable<string> KnownPorts
    {
        get
        {
            lock (_knownSerialPorts)
            {
                return _knownSerialPorts.ToArray();
            }
        }
    }

    private async void MonitorProc()
    {
        while (true)
        {
            lock (_knownSerialPorts)
            {
                var currentPorts = GetPortNames().Distinct();
                foreach (var p in currentPorts.Except(_knownSerialPorts))
                {
                    _knownSerialPorts.Add(p);
                    PortConnected?.Invoke(this, p);
                }
                foreach (var p in _knownSerialPorts.Except(currentPorts).ToArray())
                {
                    _knownSerialPorts.Remove(p);
                    PortDisconnected?.Invoke(this, p);
                }
            }

            await Task.Delay(2000);
        }
    }

    private IEnumerable<string> GetPortNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var list = new List<string>();

            // Get all serial (COM)-ports you can see in the devicemanager
            var searcher = new System.Management.ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");


            // Add all available (COM)-ports to the combobox
            foreach (var queryObj in searcher.Get())
            {
                var caption = queryObj["Caption"]?.ToString();

                if (caption != null)
                {
                    // extract the port name
                    var m = Regex.Match(caption, "\\(([^\\)]+)\\)");
                    list.Add(m.Value.Trim('(', ')'));
                }
            }

            /*
            using var serialKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DEVICEMAP\\SERIALCOMM");
            if (serialKey != null)
            {
                foreach (var name in serialKey.GetValueNames())
                {
                    var sk = serialKey.GetValue(name)?.ToString();

                    if (sk != null)
                    {
                        list.Add(sk);
                    }
                }
            }
            */
            return list;
        }
        else
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }
    }
}
