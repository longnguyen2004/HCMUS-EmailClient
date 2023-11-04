namespace EmailClient;

public class SmtpClient
{
    private readonly TextCommandClient _client;
    public SmtpClient(string host, ushort port)
    {
        _client = new(host, port);
    }
    public async Task Connect()
    {
        await _client.Connect();
        var serverInfo = await _client.ReceiveMessage();
        Console.Write(serverInfo);
        await _client.SendMessage("EHLO");
        var greeting = await _client.ReceiveMessage();
        Console.Write(greeting);
    }
}