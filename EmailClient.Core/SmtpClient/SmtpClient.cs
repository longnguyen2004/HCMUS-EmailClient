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

        public static SmtpCommand HELO { get; } = new("HELO {0}");
        public static SmtpCommand EHLO { get; } = new("EHLO {0}");
        public static SmtpCommand MAIL_FROM { get; } = new("MAIL FROM:<{0}>");
        public static SmtpCommand RCPT_TO { get; } = new("RCPT TO:<{0}>");
        public static SmtpCommand DATA { get; } = new("DATA");
        public static SmtpCommand QUIT { get; } = new("QUIT");
        public static SmtpCommand RSET { get; } = new("RSET");

        public override string ToString() => Value;
    }
    private readonly TextCommandClient _client;
    public bool Connected => _client.Connected;
    public SmtpClient(string host, ushort port)
    {
        _client = new(host, port);
    }
    private async Task<SmtpResponse> SendCommand(SmtpCommand command, string parameter = "")
    {
        await _client.SendMessage(
            Encoding.ASCII.GetBytes(string.Format(command.Value, parameter))
        );
        return await ParseResponse();
    }
    private async Task<SmtpResponse> ParseResponse()
    {
        SmtpResponse response = new();
        string line;
        do
        {
            line = Encoding.ASCII.GetString(await _client.ReceiveMessage()).ReplaceLineEndings("");
            response.Code = int.Parse(line.AsSpan(0, 3));
            response.Messages.Add(line[4..]);
        } while (line[3] != ' ');
        return response;
    }
    public async Task Connect()
    {
        await _client.Connect();
        var serverInfo = await ParseResponse();
        var greeting = await SendCommand(SmtpCommand.EHLO);
    }
    public async Task Disconnect()
    {
        var goodbye = await SendCommand(SmtpCommand.QUIT);
        await _client.Disconnect();
    }
    public async Task SendEmail(Email email)
    {
        var fromRes = await SendCommand(SmtpCommand.MAIL_FROM, email.From);
        foreach (var recipient in email.GetRecipients())
        {
            var toRes = await SendCommand(SmtpCommand.RCPT_TO, recipient);
        }
        
    }
}