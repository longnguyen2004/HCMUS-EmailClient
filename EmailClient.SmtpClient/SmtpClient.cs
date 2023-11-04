using System.Text;

namespace EmailClient;

public class SmtpClient
{
    private class SmtpResponse
    {
        public int Code { get; set; }
        public List<string> Messages { get; } = new();
        public override string ToString()
        {
            StringBuilder builder = new();
            for (var i = 0; i < Messages.Count; ++i)
            {
                builder.Append(Code);
                builder.Append(i == Messages.Count - 1 ? ' ' : '-');
                builder.AppendLine(Messages[i]);
            }
            return builder.ToString();
        }
    }
    private readonly TextCommandClient _client;
    public SmtpClient(string host, ushort port)
    {
        _client = new(host, port);
    }
    private async Task<SmtpResponse> ParseResponse()
    {
        SmtpResponse response = new();
        string line;
        do
        {
            line = await _client.ReceiveMessage();
            response.Code = int.Parse(line.AsSpan(0, 3));
            response.Messages.Add(line[4..]);
        } while (line[3] != ' ');
        return response;
    }
    public async Task Connect()
    {
        await _client.Connect();
        var serverInfo = await ParseResponse();
        Console.Write(serverInfo);

        await _client.SendMessage("EHLO");
        var greeting = await ParseResponse();
        Console.Write(greeting);
    }
    
}