// See https://aka.ms/new-console-template for more information
using EmailClient;

Console.WriteLine("Hello, World!");

SmtpClient client = new("127.0.0.1", 2500);
await client.Connect();
