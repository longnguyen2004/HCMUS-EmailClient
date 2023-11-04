using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EmailClient;

public class TextCommandClient : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly string _host;
    private readonly ushort _port;
    private Queue<(string, TaskCompletionSource<string>)> _messageQueue = new();
    private Task? _messageProcessor;
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
        _messageProcessor = ProcessMessageQueue();
    }
    public bool Connected => _socket.Connected;
    private async Task ProcessMessageQueue()
    {
        while (_socket.Connected)
        {
            if (!_messageQueue.TryDequeue(out var item))
            {
                await Task.Yield();
                continue;
            }
            (var message, var promise) = item;
            try
            {
                await _socket.SendAsync(Encoding.ASCII.GetBytes(message + '\n'));
                promise.TrySetResult(await ReceiveMessage());
            }
            catch (Exception e)
            {
                promise.TrySetException(e);
            }
        }
    }
    public Task<string> SendMessage(string message)
    {
        TaskCompletionSource<string> task = new();
        _messageQueue.Enqueue((message, task));
        return task.Task;
    }
    public async Task<string> ReceiveMessage()
    {
        var buffer = new byte[1024];
        StringBuilder builder = new();
        try
        {
            while (true)
            {
                var receiveTask = _socket.ReceiveAsync(buffer);
                await receiveTask.WaitAsync(new TimeSpan(0, 0, 1));
                builder.Append(Encoding.ASCII.GetString(buffer, 0, receiveTask.Result));
            }
        }
        catch (TimeoutException)
        {
        }
        return builder.ToString();
    }
    public async ValueTask DisposeAsync()
    {
        await _socket.DisconnectAsync(false);
        _socket.Dispose();
        GC.SuppressFinalize(this);
    }
}