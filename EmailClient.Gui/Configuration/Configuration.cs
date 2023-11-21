using System;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EmailClient.Gui;
public partial class Configuration: ObservableObject
{
    public partial class Login: ObservableObject, ICloneable {
        [ObservableProperty]
        private string? email;
        [ObservableProperty]
        private string? password;
        [ObservableProperty]
        private string? smtpHost;
        [ObservableProperty]
        private short smtpPort;
        [ObservableProperty]
        private string? pop3Host;
        [ObservableProperty]
        private short pop3Port;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    [ObservableProperty]
    private Login general = new();
}