using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using UNObot;

namespace DuplicateDetector.Services
{
    public class AIService : IDisposable
    {
        private const int Port = 8492;
        private readonly LoggerService _logger;
        private readonly Connector _connector;

        public AIService(LoggerService logger)
        {
            _logger = logger;
            _connector = new Connector (Port) {Logger = _logger};
            _connector.Message += OnMessage;
            _connector.Start();
        }

        private void OnMessage(Connector sender, string message)
        {
            _logger.Log(LogSeverity.Verbose, message);
        }

        public void Dispose()
        {
            _connector?.Dispose();
        }
    }
    
    public class Connector : IDisposable
    {
        private readonly int _port;
        private const int BufferSize = 1024;

        private readonly Socket _socket;
        private byte[] _buffer;
        private bool _active;
        private readonly ManualResetEvent _shutdown;
        
        public delegate void MessageEventHandler(Connector sender, string message);
        public event MessageEventHandler Message;

        public LoggerService Logger { private get; set; }

        public Connector() : this(27285) {}

        public Connector(int port)
        {
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _buffer = new byte[BufferSize];
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _shutdown = new ManualResetEvent(false);
        }

        /*
        
        Example Data (CSV-like):

        ,xmin,ymin,xmax,ymax,label,confidence,x_size,y_size,is_full
        0,371,486,422,584,2,0.3697565197944641,640,480,False
        
        */
        public void Start()
        {
            Task.Run(Run);
        }

        private void Run()
        {
            Logger.Log(LogSeverity.Info, $"Connecting to {_port}...");
            try
            {
                _socket.Connect("127.0.0.1", _port);
            }
            catch (SocketException e)
            {
                Logger.Log(LogSeverity.Error, "Failed to connect to server! Is it up and running?", e);
                _shutdown.Set();
                return;
            }

            _active = true;

            var byteData = Encoding.UTF8.GetBytes("HI");
            _socket.Send(byteData);
            Logger.Log(LogSeverity.Info, "Connected!");

            try
            {
                while (_active)
                {

                    if (_socket.Poll(1 * 1000 * 1000 / 60, SelectMode.SelectRead))
                    {
                        ClearBuffer(ref _buffer);
                        var dataBack = _socket.Receive(_buffer);
                        if (dataBack == 0)
                            break;
                        var message = ReadMessage(ref _buffer);
                        Message?.Invoke(this, message);
                    }

                    if (!_socket.Poll(1 * 1000 * 1000, SelectMode.SelectWrite))
                    {
                        Logger.Log(LogSeverity.Warning, "Connection failed!");
                        break;
                    }
                }

                _socket.Send(Encoding.UTF8.GetBytes("BYE"));
            }
            catch (SocketException e)
            {
                Logger.Log(LogSeverity.Critical, "Socket failed!", e);
            }

            _active = false;
            _shutdown.Set();
        }

        // Null termination!
        private static string ReadMessage(ref byte[] buffer)
        {
            var sc = new StringBuilder();
            var tempIndex = 0;

            while (tempIndex < buffer.Length)
            {
                var lastChar = (char) buffer[tempIndex];
                if (lastChar == '\0')
                    break;

                sc.Append(lastChar);
                tempIndex++;
            }

            return sc.ToString();
        }

        // If using a buffer-based implementation (raw socket)
        private static void ClearBuffer(ref byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0;
        }

        public void Dispose()
        {
            _active = false;
            _shutdown.WaitOne();
            _socket.Dispose();
            _shutdown.Dispose();
            Logger?.Log(LogSeverity.Info, "Shut down!");
        }
    }
}