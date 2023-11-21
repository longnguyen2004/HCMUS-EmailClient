using System;
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
    [ObservableProperty]
    private Login general = new();
}