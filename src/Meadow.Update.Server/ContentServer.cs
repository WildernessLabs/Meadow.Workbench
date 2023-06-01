using Meadow.Foundation.Web.Maple;
using System;
using System.Diagnostics;
using System.Net;

namespace Meadow.Update
{
    /// <summary>
    /// This server provides a REST endpoint to allow a Meadow device to download update packages
    /// </summary>
    public class ContentServer
    {
        public event EventHandler StateChanged = delegate { };

        private MapleServer _maple;
        private int _port = 5000;

        public ContentServer()
        {
        }

        public int ServerPort
        {
            get => _port;
            set
            {
                if (value == _port) return;

                var wasRunning = IsRunning;

                if (_maple != null)
                {
                    _maple.Stop();
                }

                _port = value;

                if (wasRunning)
                {
                    Start();
                }
            }
        }

        public void Start()
        {
            _maple = new MapleServer(IPAddress.Any, ServerPort);

            if (!_maple.Running)
            {
                _maple.Start();

                Debug.WriteLine("Content Server started");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (_maple.Running)
            {
                _maple.Stop();
                Debug.WriteLine("Content Server stopped");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsRunning
        {
            get => _maple?.Running ?? false;
        }
    }
}
