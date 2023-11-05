using System.Runtime.CompilerServices;
using System.Text;

namespace EmailClient;

public class Pop3Client
{
    private readonly TextCommandClient _client;

    public Pop3Client(string host, ushort port)
    {
        _client = new(host, port);
    }

    public async Task<Pop3Response> SendMessage(Pop3Command command, string message)
    {
        await _client.SendMessage(command + message);
        var response = await ParseResponse(command.Multiline);
        return response;
    }

    public class Pop3Response 
    {
        public bool status {get; set;}
        public List<string> messages {get;} = new();
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(status + " ");
            for (var i = 0; i < messages.Count; ++i)
                builder.AppendLine(messages[i]);

            return builder.ToString();
        }
    }

    public class Pop3Command
    {
        private Pop3Command(string value, bool multiline) { Value = value; Multiline = multiline; }
        public string Value { get; private set; }
        public bool Multiline { get; set; }
        public static Pop3Command CAPA => new("CAPA", false);
        public static Pop3Command UIDL => new("UIDL", true);
        public static Pop3Command USER => new("USER ", false);
        public static Pop3Command PASS => new("PASS ", false);
        public static Pop3Command STAT => new("STAT", false);
        public static Pop3Command LIST => new("LIST", true);
        public static Pop3Command RETR => new("RETR ", true);
        public static Pop3Command DELE => new("DELE ", false);
        public static Pop3Command NOOP => new("NOOP", false);
        public static Pop3Command RSET => new("RSET", false);
        public static Pop3Command QUIT => new("QUIT", false);
        public override string ToString() => Value;
    }


    private async Task<Pop3Response> ParseResponse(bool multiline = false)
    {
        Pop3Response response = new();
        string line = await _client.ReceiveMessage();
        response.status = line.StartsWith("+OK");

        if (multiline)
            while (line != ".")
            {
                response.messages.Add(line);
                line = await _client.ReceiveMessage();
            }
        else
            response.messages.Add(line.Replace(response.status ? "+OK " : "-ERR ", ""));
        return response;

    }

    public async Task Connect()
    {
        await _client.Connect();
        var serverInfo = await ParseResponse();
        Console.Write(serverInfo);

        var capa = await SendMessage(Pop3Command.CAPA, "");
        Console.Write(capa);

        var user = await SendMessage(Pop3Command.USER, "minhnhat@hcmus.edu.vn");
        Console.Write(user);

        var pass = await SendMessage(Pop3Command.PASS, "pass");
        Console.Write(pass);

        var stat = await SendMessage(Pop3Command.STAT, "");
        Console.Write(stat);

        var list = await SendMessage(Pop3Command.LIST, "");
        Console.Write(list);

        var uidl = await SendMessage(Pop3Command.UIDL, "");
        Console.Write(uidl);
    }
}