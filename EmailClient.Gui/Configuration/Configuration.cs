using System;
using System.Collections.Generic;
using System.Text.Json;
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
        public string Keyword { get; set; } = string.Empty;
        public string Folder { get; set; } = "Folder";
    }
    [ObservableProperty]
    private Login general = new();
    public ICollection<Filter> Filters { get; } = new List<Filter>();
}
