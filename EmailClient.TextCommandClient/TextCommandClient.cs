using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailClient;

public partial class TextCommandClient : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly string _host;
    private readonly ushort _port;
    private byte[] _buffer;
    private int _remaining;
    private Queue<(string, TaskCompletionSource<string>)> _messageQueue = new();
    public TextCommandClient(string host, ushort port)
    {
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            throw new ArgumentException("Host is not valid");
        _host = host;
        _port = port;
        _socket = new(SocketType.Stream, ProtocolType.Tcp);
        _buffer = new byte[1024];
        _remaining = 0;
    }
    public async Task Connect()
    {
        if (_socket.Connected)
            await _socket.DisconnectAsync(true);
        foreach (var address in (await Dns.GetHostEntryAsync(_host)).AddressList)
        {
            try
            {
                await _socket.ConnectAsync(address, _port);
                break;
            }
            catch (SocketException)
            {
            }
        }
        if (!_socket.Connected)
            throw new ApplicationException("Unable to connect to server");
    }
    public bool Connected => _socket.Connected;
    private (bool, string) ExtractLineFromBuffer()
    {
        if (_remaining == 0)
            return (false, string.Empty);
        var pos = Array.IndexOf(_buffer, (byte)0x0a, 0, _remaining) + 1;
        // Buffer contains a non-complete line
        if (pos == 0)
        {
            string returnVal = Encoding.ASCII.GetString(_buffer, 0, _remaining);
            _remaining = 0;
            _buffer = new byte[1024];
            return (false, returnVal);
        }
        // Buffer contains at least 1 line
        else
        {
            string returnVal = Encoding.ASCII.GetString(_buffer, 0, pos);
            _remaining -= pos;
            _buffer = _buffer[pos..];
            return (true, returnVal);
        }
    }
    public async Task SendMessage(string message)
    {
        await _socket.SendAsync(Encoding.ASCII.GetBytes(message + '\n'));
    }
    public async Task<string> ReceiveMessage()
    {
        StringBuilder builder = new();
        while (true)
        {
            var (stop, line) = ExtractLineFromBuffer();
            builder.Append(line);
            if (stop)
                return builder.ToString().ReplaceLineEndings("");
            var receiveTask = _socket.ReceiveAsync(_buffer);
            await receiveTask.WaitAsync(new TimeSpan(0, 0, 5));
            _remaining += receiveTask.Result;
        }
    }
    public async ValueTask DisposeAsync()
    {
        await _socket.DisconnectAsync(false);
        _socket.Dispose();
        GC.SuppressFinalize(this);
    }
}