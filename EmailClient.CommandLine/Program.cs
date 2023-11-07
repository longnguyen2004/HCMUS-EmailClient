// See https://aka.ms/new-console-template for more information
using EmailClient;

Console.WriteLine("Hello, World!");

using (FileStream stream = new("./test.msg", FileMode.Open, FileAccess.Read))
using (StreamReader reader = new(stream))
{
    MimeParser parser = new(reader);
    var parsed = await parser.Parse();
}
Console.ReadLine();
