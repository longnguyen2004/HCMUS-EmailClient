using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

public partial class Pop3Client
{  
    [GeneratedRegex("^(\\+OK|-ERR) ?(.*?)$")]
    private static partial Regex ResponseStatusRegex();
    private readonly TextCommandClient _client;
    public bool Connected => _client.Connected;
    public Pop3Client(string host, ushort port)
    {
        _client = new(host, port);
    }

    private async Task<Pop3Response> SendCommand(Pop3Command command, string parameter = "")
    {
        if (parameter.Length == 0)
            await _client.SendMessage(command.ToString());
        else
            await _client.SendMessage($"{command} {parameter}");
        return await ParseResponse(command.Multiline);
    }

    public class Pop3Response 
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> AdditionalLines { get; } = new();
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(Status ? "+OK" : "-ERR");
            if (Message != string.Empty)
            {
                builder.Append(' ');
                builder.Append(Message);
            }
            builder.AppendLine();
            foreach (var line in AdditionalLines)
                builder.AppendLine(line);
            return builder.ToString();
        }
    }

    public class Pop3Command
    {
        private Pop3Command(string value, bool multiline) { Value = value; Multiline = multiline; }
        public string Value { get; private set; }
        public bool Multiline { get; private set; }
        public static Pop3Command CAPA => new("CAPA", true);
        public static Pop3Command UIDL => new("UIDL", true);
        public static Pop3Command USER => new("USER", false);
        public static Pop3Command PASS => new("PASS", false);
        public static Pop3Command STAT => new("STAT", false);
        public static Pop3Command LIST => new("LIST", true);
        public static Pop3Command RETR => new("RETR", true);
        public static Pop3Command DELE => new("DELE", false);
        public static Pop3Command NOOP => new("NOOP", false);
        public static Pop3Command RSET => new("RSET", false);
        public static Pop3Command QUIT => new("QUIT", false);
        public override string ToString() => Value;
    }
    private async Task<Pop3Response> ParseResponse(bool multiline = false)
    {
        Pop3Response response = new();
        string line = await _client.ReceiveMessage();
        var match = ResponseStatusRegex().Match(line);
        if (!match.Success)
            throw new ApplicationException($"Expecting +OK or -ERR, got {line} (fix the parser!!!)");
        response.Status = match.Groups[1].Value == "+OK";
        response.Message = match.Groups[2].Value;

        if (multiline)
        {
            while (true)
            {
                line = await _client.ReceiveMessage();
                if (line == ".") break;
                if (line.StartsWith('.')) line = line[1..];
                response.AdditionalLines.Add(line);
            }
        }
        return response;

    }

    public async Task Connect()
    {
        await _client.Connect();
        var serverInfo = await ParseResponse();
        Console.Write(serverInfo);

        var capa = await SendCommand(Pop3Command.CAPA, "");
        Console.Write(capa);

        var user = await SendCommand(Pop3Command.USER, "minhnhat@hcmus.edu.vn");
        Console.Write(user);

        var pass = await SendCommand(Pop3Command.PASS, "pass");
        Console.Write(pass);

        var stat = await SendCommand(Pop3Command.STAT, "");
        Console.Write(stat);

        var list = await SendCommand(Pop3Command.LIST, "");
        Console.Write(list);

        var uidl = await SendCommand(Pop3Command.UIDL, "");
        Console.Write(uidl);
    }
}