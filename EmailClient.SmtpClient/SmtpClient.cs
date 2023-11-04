namespace EmailClient;

public class SmtpClient: TextCommandClient
{
    public SmtpClient(string host, ushort port):
        base(host, port)
    {

    }
    public override async Task Connect()
    {
        await base.Connect();
        var serverInfo = await ReceiveMessage();
        var greeting = await SendMessage("EHLO");
    }
}