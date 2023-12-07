using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailClient;

public partial class TextCommandClient : IAsyncDisposable
{
    private readonly byte[] _buffer = new byte[1024];
    private readonly Socket _socket;
    private readonly string _host;
    private readonly ushort _port;
    public Stream? SocketStream { get; private set; }
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
        SocketStream = new BufferedStream(new NetworkStream(_socket, false));
    }
    public async Task Disconnect()
    {
        if (!Connected) return;
        await SocketStream!.DisposeAsync();
        SocketStream = null;
        await _socket.DisconnectAsync(false);
    }
    public bool Connected => _socket.Connected;
    public async Task SendMessage(ArraySegment<byte> message)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        await SocketStream!.WriteAsync(message);
        await SocketStream!.WriteAsync(StreamHelper.NewLine);
    }
    public async Task<byte[]> ReceiveMessage()
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        var received = 0;
        do
        {
            await SocketStream!.ReadExactlyAsync(_buffer, received, 1).AsTask().WaitAsync(
                new TimeSpan(0, 0, 5)
            );
        } while (_buffer[received++] != 0x0a);
        // handles dot stuffing
        if (_buffer[0] == 46 && _buffer[1] == 46)
            return _buffer[1..received];
        return _buffer[0..received];
    }
    public Task Send(ArraySegment<byte> buffer)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        return SocketStream!.WriteAsync(buffer).AsTask();
    }
    public Task ReceiveExactly(ArraySegment<byte> buffer)
    {
        if (!Connected)
            throw new ApplicationException("Client not connected!");
        return SocketStream!.ReadExactlyAsync(buffer).AsTask().WaitAsync(
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