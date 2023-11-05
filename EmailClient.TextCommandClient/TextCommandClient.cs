using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailClient;

public partial class TextCommandClient : IAsyncDisposable
{
    private readonly static byte[] _newline = { 0x0d, 0x0a };
    private readonly Socket _socket;
    private NetworkStream? _netStream;
    private StreamReader? _reader;
    private readonly string _host;
    private readonly ushort _port;
    public TextCommandClient(string host, ushort port)
    {
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            throw new ArgumentException("Host is not valid");
        _host = host;
        _port = port;
        _socket = new(SocketType.Stream, ProtocolType.Tcp);
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
        _netStream = new(_socket, false);
        _reader = new(_netStream);
    }
    public async Task Disconnect()
    {
        _reader?.Dispose();
        _netStream?.Dispose();
        await _socket.DisconnectAsync(false);
        _reader = null;
        _netStream = null;
    }
    public bool Connected => _socket.Connected;
    public async Task SendMessage(string message)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        await _socket.SendAsync(Encoding.ASCII.GetBytes(message));
        await _socket.SendAsync(_newline);
    }
    public async Task<string> ReceiveMessage()
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        var readTask = _reader!.ReadLineAsync();
        await readTask.WaitAsync(new TimeSpan(0, 0, 5));
        return readTask.Result;
    }
    public async ValueTask DisposeAsync()
    {
        await Disconnect();
        _socket.Dispose();
        GC.SuppressFinalize(this);
    }
}