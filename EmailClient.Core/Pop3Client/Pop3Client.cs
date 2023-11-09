using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

public class Pop3Exception : ApplicationException
{
    private readonly string _message;
    private readonly string? _command;
    public Pop3Exception(string message, string? command = null)
    {
        _message = message;
        _command = command;
    }

    public override string Message { get => $"Pop3Client Error Occurred While Sending {_command}: {_message}"; }
}

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
        public static Pop3Command RETR => new("RETR", false);
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
        Console.WriteLine(line);
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
        if (!serverInfo.Status)
            throw new Pop3Exception(serverInfo.Message);
    }

    public async Task Login(string user, string password)
    {
        var userResponse = await SendCommand(Pop3Command.USER, user);
        if (!userResponse.Status)
            throw new Pop3Exception(userResponse.Message, Pop3Command.USER.ToString());
        var passResponse = await SendCommand(Pop3Command.PASS, password);
        if (!passResponse.Status)
            throw new Pop3Exception(passResponse.Message, Pop3Command.PASS.ToString());
    }

    public async Task<HashSet<String>> CAPA() {
        var response = await SendCommand(Pop3Command.CAPA);
        if (!response.Status)
            throw new Pop3Exception(response.Message, Pop3Command.CAPA.ToString());
        HashSet<String> capabilities = new();
        foreach (var line in response.AdditionalLines)
            capabilities.Add(line);
        return capabilities;
    }

    public async Task<bool> IsCapable(string capability)
    {
        var capabilities = await CAPA();
        return capabilities.Contains(capability);
    }

    public async Task<HashSet<(int,String)>> UIDL()
    {
        if (!await IsCapable("UIDL"))
            throw new Pop3Exception("Server doesn't have UIDL capability");
        var response = await SendCommand(Pop3Command.UIDL);
        if (!response.Status)
            throw new Pop3Exception(response.Message, Pop3Command.UIDL.ToString());
        HashSet<(int,String)> uids = new();
        foreach (var line in response.AdditionalLines)
        {
            var match = Regex.Match(line, "^(?<id>\\d+) (?<uid>.*)$");
            uids.Add((int.Parse(match.Groups["id"].Value), match.Groups["uid"].Value));
        }
        return uids;
    }

    public async Task RETR(int id, string path) {
        var response = await SendCommand(Pop3Command.RETR, id.ToString());
        if (!response.Status)
            throw new Pop3Exception(response.Message, Pop3Command.RETR.ToString());
        int buffer_size = int.Parse(response.Message.Replace("+OK ", ""));
        byte[] buffer = new byte[buffer_size];
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
        await _client.ReceiveExactly(buffer);
        await stream.WriteAsync(buffer);
        await _client.ReceiveMessage();
    }

    public async Task Disconnect()
    {
        await SendCommand(Pop3Command.QUIT);
        await _client.Disconnect();
    }

}