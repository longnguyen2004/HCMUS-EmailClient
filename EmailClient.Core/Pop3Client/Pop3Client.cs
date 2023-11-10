using System.Text;
using System.Text.RegularExpressions;

namespace EmailClient;

public class Pop3Exception : Exception
{
    private readonly string _message;
    public Pop3Exception(string message)
    {
        _message = message;
    }
    public override string Message => _message;
}

public partial class Pop3Client
{
    [GeneratedRegex("^(\\+OK|-ERR) ?(.*?)$")]
    private static partial Regex ResponseStatusRegex();
    private readonly TextCommandClient _client;
    public bool Connected => _client.Connected;
    public Dictionary<string, string?> Capabilities { get; } = new();
    public Pop3Client(string host, ushort port)
    {
        _client = new(host, port);
    }

    private async Task<Pop3Response> SendCommand(Pop3Command command, string parameter = "")
    {
        if (parameter.Length == 0)
            await _client.SendMessage(Encoding.ASCII.GetBytes(command.ToString()));
        else
            await _client.SendMessage(Encoding.ASCII.GetBytes($"{command} {parameter}"));
        return await ParseResponse(command.Multiline);
    }

    public class Pop3Response
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> AdditionalLines { get; } = new();
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(Success ? "+OK" : "-ERR");
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
        string line = Encoding.ASCII.GetString(
            await _client.ReceiveMessage()
        ).ReplaceLineEndings("");
        Console.WriteLine(line);
        var match = ResponseStatusRegex().Match(line);
        if (!match.Success)
            throw new ApplicationException($"Expecting +OK or -ERR, got {line} (fix the parser!!!)");
        response.Success = match.Groups[1].Value == "+OK";
        response.Message = match.Groups[2].Value;

        if (multiline)
        {
            while ((
                line = Encoding.ASCII.GetString(
                    await _client.ReceiveMessage()
                ).ReplaceLineEndings("")
            ) != ".")
                response.AdditionalLines.Add(line);
        }
        return response;
    }

    public async Task Connect()
    {
        await _client.Connect();
        try
        {
            var serverInfo = await ParseResponse();
            if (!serverInfo.Success)
                throw new Pop3Exception(
                    $"Error occurred while connecting to server: {serverInfo.Message}"
                );
            var capa = await SendCommand(Pop3Command.CAPA);
            if (!capa.Success)
                throw new Pop3Exception(
                    $"Error occurred while getting server capabilities: {serverInfo.Message}"
                );
            foreach (var line in capa.AdditionalLines)
            {
                var split = line.Split(" ");
                if (split.Length == 1)
                    Capabilities[split[0]] = null;
                else if (split.Length == 1)
                    Capabilities[split[0]] = split[1];
            }
            if (!Capabilities.ContainsKey("UIDL"))
                throw new Pop3Exception(
                    $"Server doesn't support UIDL capability"
                );
        }
        catch (Exception)
        {
            await Disconnect();
            throw;
        }
    }

    public async Task Login(string user, string password)
    {
        var userResponse = await SendCommand(Pop3Command.USER, user);
        if (!userResponse.Success)
            throw new Pop3Exception($"Error while logging in: {userResponse.Message}");
        var passResponse = await SendCommand(Pop3Command.PASS, password);
        if (!passResponse.Success)
            throw new Pop3Exception($"Error while logging in: {passResponse.Message}");
    }

    public async Task<List<string>> GetListing()
    {
        var response = await SendCommand(Pop3Command.UIDL);
        if (!response.Success)
            throw new Pop3Exception($"Failed to retrieve message list: {response.Message}");
        List<string> uids = new();
        foreach (var line in response.AdditionalLines)
        {
            var match = Regex.Match(line, "^(?<id>\\d+) (?<uid>.*)$");
            uids.Add(match.Groups["uid"].Value);
        }
        return uids;
    }

    public async Task<byte[]> GetMessage(int id)
    {
        var response = await SendCommand(Pop3Command.RETR, id.ToString());
        if (!response.Success)
            throw new Pop3Exception($"Failed to get message with index {id}: {response.Message}");
        MemoryStream stream = new();
        while (true)
        {
            byte[] line = await _client.ReceiveMessage();
            if (line.AsSpan().SequenceEqual(".\r\n"u8))
                break;
            await stream.WriteAsync(line);
        }
        return stream.ToArray();
    }

    public async Task Disconnect()
    {
        await SendCommand(Pop3Command.QUIT);
        await _client.Disconnect();
        Capabilities.Clear();
    }

}