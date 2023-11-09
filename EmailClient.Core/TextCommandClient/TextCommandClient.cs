using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailClient;

public partial class TextCommandClient : IAsyncDisposable
{
    private readonly static byte[] _newline = { 0x0d, 0x0a };
    private readonly byte[] _buffer = new byte[1024];
    private readonly Socket _socket;
    private Stream? _stream;
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
            await Disconnect();
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
        _stream = new BufferedStream(new NetworkStream(_socket, false));
    }
    public async Task Disconnect()
    {
        if (!Connected) return;
        await _stream!.DisposeAsync();
        _stream = null;
        await _socket.DisconnectAsync(false);
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
        var received = 0;
        do
        {
            await _stream!.ReadExactlyAsync(_buffer, received, 1).AsTask().WaitAsync(
                new TimeSpan(0, 0, 5)
            );
        } while (_buffer[received++] != 0x0a);
        return Encoding.ASCII.GetString(_buffer, 0, received).ReplaceLineEndings("");
    }
    public Task<int> Send(ArraySegment<byte> buffer)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        return _socket.SendAsync(buffer);
    }
    public Task ReceiveExactly(ArraySegment<byte> buffer)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        return _stream!.ReadExactlyAsync(buffer).AsTask().WaitAsync(
            new TimeSpan(0, 0, 5)
        );
    }
    public async ValueTask DisposeAsync()
    {
        await Disconnect();
        _socket.Dispose();
        GC.SuppressFinalize(this);
    }
}