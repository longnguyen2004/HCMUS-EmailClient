using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EmailClient.Gui;
public partial class Configuration: ObservableObject
{
    public partial class Login: ObservableObject, ICloneable {
        [ObservableProperty]
        private string email = string.Empty;
        [ObservableProperty]
        private string password = string.Empty;
        [ObservableProperty]
        private string smtpHost = string.Empty;
        [ObservableProperty]
        private ushort smtpPort;
        [ObservableProperty]
        private string pop3Host = string.Empty;
        [ObservableProperty]
        private ushort pop3Port;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public enum FilterType {
        From,
        Subject,
        Content,
        Spam
    }
    public class Filter {
        public FilterType Type { get; set; } = FilterType.From;
        public List<string> Keywords { get; set; } = new();
        public string Folder { get; set; } = "Folder";
    }
    [ObservableProperty]
    private Login general = new();
    public ObservableCollection<Filter> Filters { get; set; } = new();

    private static JsonSerializerOptions serializerOptions = new() {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };
    public static Configuration? Load(Stream stream)
    {
        return JsonSerializer.Deserialize<Configuration>(stream, serializerOptions);
    }
    public static ValueTask<Configuration?> LoadAsync(Stream stream)
    {
        return JsonSerializer.DeserializeAsync<Configuration>(stream, serializerOptions);
    }
    public static void Save(Stream stream, Configuration config)
    {
        JsonSerializer.Serialize(stream, config, serializerOptions);
        stream.Flush();
    }
    public static async Task SaveAsync(Stream stream, Configuration config)
    {
        await JsonSerializer.SerializeAsync(stream, config, serializerOptions);
        await stream.FlushAsync();
    }
}
