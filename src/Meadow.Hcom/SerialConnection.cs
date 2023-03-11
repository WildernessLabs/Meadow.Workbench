using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Ports;

namespace Meadow.Hcom
{
    public delegate void ConnectionStateChangedHandler(SerialConnection connection, ConnectionState oldState, ConnectionState newState);

    public class SerialConnection : IDisposable
    {
        public const int DefaultBaudRate = 115200;
        public const int ReadBufferSizeBytes = 0x2000;

        public event ConnectionStateChangedHandler ConnectionStateChanged = delegate { };

        private SerialPort _port;
        private ILogger? _logger;
        private bool _isDisposed;
        private ConnectionState _state;

        public IMeadowDevice? Device { get; private set; }

        public SerialConnection(string port, ILogger? logger = default)
        {
            if (!SerialPort.GetPortNames().Contains(port, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"Serial Port '{port}' not found.");
            }

            State = ConnectionState.Disconnected;
            _logger = logger;
            _port = new SerialPort(port);
            _port.ReadTimeout = _port.WriteTimeout = 5000;

            new Thread(ListenerProc)
            {
                IsBackground = true,
                Name = "HCOM Listener"
            }
            .Start();
        }

        public ConnectionState State
        {
            get => _state;
            private set
            {
                if (value == State) return;

                var old = _state;
                _state = value;
                ConnectionStateChanged?.Invoke(this, old, State);
            }
        }

        private void Open()
        {
            if (!_port.IsOpen)
            {
                _port.Open();
            }
            State = ConnectionState.Connected;
        }

        private void Close()
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }

            State = ConnectionState.Disconnected;
        }

        public async Task<bool> TryAttach(TimeSpan timeout = default)
        {
            try
            {
                // ensure the port is open
                Open();

                // search for the device via HCOM
                var command = CommandBuilder.Build<GetDeviceInfoCommand>();
                SendCommand(command);

                // if HCOM fails, check for DFU/bootloader mode

                // create the device instance

                // start a keep-alive/heartbeat
                return true;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to connect");
                return false;
            }
        }

        private void SendCommand(Command command)
        {
            // TODO: verify we're connected

            var payload = command.Serialize();
            EncodeAndSendPacket(payload);
        }

        private void EncodeAndSendPacket(byte[] messageBytes)
        {
            try
            {
                int encodedToSend;
                byte[] encodedBytes;

                // For file download this is a LOT of messages
                // _uiSupport.WriteDebugLine($"Sending packet with {messageSize} bytes");

                // For testing calculate the crc including the sequence number
                //_packetCrc32 = NuttxCrc.Crc32part(messageBytes, messageSize, 0, _packetCrc32);
                try
                {
                    // The encoded size using COBS is just a bit more than the original size adding 1 byte
                    // every 254 bytes plus 1 and need room for beginning and ending delimiters.
                    encodedBytes = new byte[Protocol.HCOM_PROTOCOL_ENCODED_MAX_SIZE];

                    // Skip over first byte so it can be a start delimiter
                    encodedToSend = CobsTools.CobsEncoding(messageBytes, 0, messageBytes.Length, ref encodedBytes, 1);

                    // DEBUG TESTING
                    if (encodedToSend == -1)
                    {
                        _logger?.LogError($"Error - encodedToSend == -1");
                        return;
                    }

                    if (_port == null)
                    {
                        _logger?.LogError($"Error - SerialPort == null");
                        throw new Exception("Port is null");
                    }
                }
                catch (Exception except)
                {
                    string msg = string.Format("Send setup Exception: {0}", except);
                    _logger?.LogError(msg);
                    throw;
                }

                // Add delimiters to packet boundaries
                try
                {
                    encodedBytes[0] = 0;                // Start delimiter
                    encodedToSend++;
                    encodedBytes[encodedToSend] = 0;    // End delimiter
                    encodedToSend++;
                }
                catch (Exception encodedBytesEx)
                {
                    // This should drop the connection and retry
                    Debug.WriteLine($"Adding encodeBytes delimiter threw: {encodedBytesEx}");
                    Thread.Sleep(500);    // Place for break point
                    throw;
                }

                try
                {
                    // Send the data to Meadow
                    _port.Write(encodedBytes, 0, encodedToSend);
                }
                catch (InvalidOperationException ioe)  // Port not opened
                {
                    string msg = string.Format("Write but port not opened. Exception: {0}", ioe);
                    _logger?.LogError(msg);
                    throw;
                }
                catch (ArgumentOutOfRangeException aore)  // offset or count don't match buffer
                {
                    string msg = string.Format("Write buffer, offset and count don't line up. Exception: {0}", aore);
                    _logger?.LogError(msg);
                    throw;
                }
                catch (ArgumentException ae)  // offset plus count > buffer length
                {
                    string msg = string.Format($"Write offset plus count > buffer length. Exception: {0}", ae);
                    _logger?.LogError(msg);
                    throw;
                }
                catch (TimeoutException te) // Took too long to send
                {
                    string msg = string.Format("Write took too long to send. Exception: {0}", te);
                    _logger?.LogError(msg);
                    throw;
                }
            }
            catch (Exception except)
            {
                // DID YOU RESTART MEADOW?
                // This should drop the connection and retry
                _logger?.LogError($"EncodeAndSendPacket threw: {except}");
                throw;
            }
        }


        private void ListenerProc()
        {
            var readBuffer = new byte[ReadBufferSizeBytes];

            while (!_isDisposed)
            {
                if (_port.IsOpen)
                {
                    var length = _port.BytesToRead;

                    if (length > 0)
                    {
                        // TODO: packetize if > buffer size

                        var read = _port.Read(readBuffer, 0, length);

                        if (read > 0)
                        {
                            // process the data

                            // append to queue

                            // look for response packet

                            // extract response packet

                            // forward response packet
                        }
                    }
                    Thread.Sleep(100);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Close();
                    _port.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}