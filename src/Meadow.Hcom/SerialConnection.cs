using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;
using System.IO.Ports;

namespace Meadow.Hcom
{
    public delegate void ConnectionStateChangedHandler(SerialConnection connection, ConnectionState oldState, ConnectionState newState);

    public class SerialConnection : IDisposable, IHcomConnection
    {
        public const int DefaultBaudRate = 115200;
        public const int ReadBufferSizeBytes = 0x2000;

        public event ConnectionStateChangedHandler ConnectionStateChanged = delegate { };

        private SerialPort _port;
        private ILogger? _logger;
        private bool _isDisposed;
        private ConnectionState _state;
        private readonly CancellationTokenSource _cts;
        private List<IConnectionListener> _listeners = new List<IConnectionListener>();
        private Queue<Request> _pendingCommands = new Queue<Request>();
        private bool _maintainConnection;

        public IMeadowDevice? Device { get; private set; }

        public SerialConnection(string port, ILogger? logger = default)
        {
            _cts = new CancellationTokenSource();

            if (!SerialPort.GetPortNames().Contains(port, StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"Serial Port '{port}' not found.");
            }

            State = ConnectionState.Disconnected;
            _logger = logger;
            _port = new SerialPort(port);
            _port.ReadTimeout = _port.WriteTimeout = 5000;

            new Thread(() => _ = ListenerProc())
            {
                IsBackground = true,
                Name = "HCOM Listener"
            }
            .Start();

            new Thread(CommandManager)
            {
                IsBackground = true,
                Name = "HCOM Sender"
            }
            .Start();
        }

        private bool MaintainConnection
        {
            get => _maintainConnection;
            set
            {
                if (value == MaintainConnection) return;

                _maintainConnection = value;

                if (value)
                {
                    if (_connectionManager == null || _connectionManager.ThreadState != System.Threading.ThreadState.Running)
                    {
                        _connectionManager = new Thread(ConnectionManagerProc)
                        {
                            IsBackground = true,
                            Name = "HCOM Connection Manager"
                        };
                        _connectionManager.Start();

                    }
                }
            }
        }

        private Thread? _connectionManager = null;

        private void ConnectionManagerProc()
        {
            while (_maintainConnection)
            {
                Debug.WriteLine("Checking COM port...");
                if (!_port.IsOpen)
                {
                    try
                    {
                        Debug.WriteLine("Opening COM port...");
                        _port.Open();
                        Debug.WriteLine("Opened COM port");
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public void AddListener(IConnectionListener listener)
        {
            lock (_listeners)
            {
                _listeners.Add(listener);
            }

            Open();

            MaintainConnection = true;
        }

        public void RemoveListener(IConnectionListener listener)
        {
            lock (_listeners)
            {
                _listeners.Remove(listener);
            }

            // TODO: stop maintaining connection?
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
                command.SequenceNumber = 42;

                _pendingCommands.Enqueue(command);

                // wait for sequence number

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

        private void CommandManager()
        {
            while (!_isDisposed)
            {
                while (_pendingCommands.Count > 0)
                {
                    var command = _pendingCommands.Dequeue();
                    SendCommand(command);
                }

                Thread.Sleep(1000);
            }
        }

        private void SendCommand(Request command)
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


        private class SerialMessage
        {
            private readonly IList<Memory<byte>> _segments;

            public SerialMessage(Memory<byte> segment)
            {
                _segments = new List<Memory<byte>>();
                _segments.Add(segment);
            }

            public void AddSegment(Memory<byte> segment)
            {
                _segments.Add(segment);
            }

            public byte[] ToArray()
            {
                using var ms = new MemoryStream();
                foreach (var segment in _segments)
                {
                    // We could just call ToArray on the `Memory` but that will result in an uncontrolled allocation.
                    var tmp = ArrayPool<byte>.Shared.Rent(segment.Length);
                    segment.CopyTo(tmp);
                    ms.Write(tmp, 0, segment.Length);
                    ArrayPool<byte>.Shared.Return(tmp);
                }
                return ms.ToArray();
            }
        }

        private async Task ListenerProc()
        {
            var readBuffer = new byte[ReadBufferSizeBytes];
            var decodedBuffer = new byte[8192];
            var messageBytes = new CircularBuffer<byte>(8192 * 2);
            var delimiter = new byte[] { 0x00 };
            var index = 0;

            while (!_isDisposed)
            {
                if (_port.IsOpen)
                {
                    try
                    {
                        var receivedLength = await _port.BaseStream.ReadAsync(readBuffer, 0, readBuffer.Length);

                        if (receivedLength > 0)
                        {
                            messageBytes.Append(readBuffer, 0, receivedLength);

                            while (messageBytes.Count > 0)
                            {
                                index = messageBytes.FirstIndexOf(delimiter);

                                if (index < 0)
                                {
                                    break;
                                }
                                var packetBytes = messageBytes.Remove(index + 1);

                                if (packetBytes.Length == 1)
                                {
                                    // It's possible that we may find a series of 0x00 values in the buffer.
                                    // This is because when the sender is blocked (because this code isn't
                                    // running) it will attempt to send a single 0x00 before the full message.
                                    // This allows it to test for a connection. When the connection is
                                    // unblocked this 0x00 is sent and gets put into the buffer along with
                                    // any others that were queued along the usb serial pipe line.

                                    // we discard this single 0x00 byte
                                }
                                else
                                {
                                    var decodedSize = CobsTools.CobsDecoding(packetBytes, packetBytes.Length - delimiter.Length, ref decodedBuffer);

                                    // now parse this per the HCOM protocol definition
                                    var response = Response.Parse(decodedBuffer, decodedSize);

                                    if (response is TextInformationResponse tir)
                                    {
                                        // send the message to any listeners
                                        Debug.WriteLine($"INFO> {tir.Text}");

                                        foreach (var listener in _listeners)
                                        {
                                            listener.OnInformationMessageReceived(tir.Text);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        Debug.WriteLine($"listen timeout");
                    }
                    catch (ThreadAbortException)
                    {
                        //ignoring for now until we wire cancellation ...
                        //this blocks the thread abort exception when the console app closes
                        Debug.WriteLine($"listen abort");
                    }
                    catch (InvalidOperationException)
                    {
                        // common if the port is reset/closed (e.g. mono enable/disable) - don't spew confusing info
                        Debug.WriteLine($"listen on closed port");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"listen error {ex.Message}");
                        _logger?.LogTrace(ex, "An error occurred while listening to the serial port.");
                        await Task.Delay(1000, _cts.Token);
                    }
                }
                else
                {
                    await (Task.Delay(500));
                }
            }
        }

        private bool DecodeAndProcessPacket(Memory<byte> packetBuffer, CancellationToken cancellationToken)
        {
            var decodedBuffer = ArrayPool<byte>.Shared.Rent(8192);
            var packetLength = packetBuffer.Length;
            // It's possible that we may find a series of 0x00 values in the buffer.
            // This is because when the sender is blocked (because this code isn't
            // running) it will attempt to send a single 0x00 before the full message.
            // This allows it to test for a connection. When the connection is
            // unblocked this 0x00 is sent and gets put into the buffer along with
            // any others that were queued along the usb serial pipe line.
            if (packetLength == 1)
            {
                //_logger.LogTrace("Throwing out 0x00 from buffer");
                return false;
            }

            var decodedSize = CobsTools.CobsDecoding(packetBuffer.ToArray(), packetLength, ref decodedBuffer);

            /*
            // If a message is too short it is ignored
            if (decodedSize < MeadowDeviceManager.ProtocolHeaderSize)
            {
                return false;
            }

            Debug.Assert(decodedSize <= MeadowDeviceManager.MaxAllowableMsgPacketLength);

            // Process the received packet
            ParseAndProcessReceivedPacket(decodedBuffer.AsSpan(0, decodedSize).ToArray(),
                                          cancellationToken);

            */
            ArrayPool<byte>.Shared.Return(decodedBuffer);
            return true;
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