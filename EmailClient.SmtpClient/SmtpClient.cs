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
    private class SmtpCommand
    {
        private SmtpCommand(string value) { Value = value; }

        public string Value { get; private set; }

        public static SmtpCommand HELO { get; } = new("HELO");
        public static SmtpCommand EHLO { get; } = new("EHLO");
        public static SmtpCommand MAIL_FROM { get; } = new("MAIL FROM");
        public static SmtpCommand RCPT_TO { get; } = new("RCPT_TO");
        public static SmtpCommand DATA { get; } = new("DATA");
        public static SmtpCommand QUIT { get; } = new("QUIT");
        public static SmtpCommand RSET { get; } = new("RSET");

        public override string ToString() => Value;
    }
    private readonly TextCommandClient _client;
    public SmtpClient(string host, ushort port)
    {
        _client = new(host, port);
    }
    private async Task<SmtpResponse> SendCommand(SmtpCommand command, string parameter = "")
    {
        if (parameter.Length == 0)
            await _client.SendMessage(command.ToString());
        else
            await _client.SendMessage($"{command} {parameter}");
        return await ParseResponse();
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

        var greeting = await SendCommand(SmtpCommand.EHLO);
        Console.Write(greeting);
    }

}