﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Text.Json;

namespace EmailClient.Gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Configuration GlobalConfig { get; set; }
        public string RootDir { get; private set; }
        private FileStream _configFile;
        protected override void OnStartup(StartupEventArgs e)
        {
            var exePath = Environment.ProcessPath;
            RootDir = Path.GetDirectoryName(exePath)!;

            var jsonPath = Path.Join(RootDir, "config", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
            _configFile = new(
                jsonPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite
            );

            try
            {
                GlobalConfig = JsonSerializer.Deserialize<Configuration>(_configFile)!;
            }
            catch (JsonException)
            {
                GlobalConfig = new();
                _ = SaveConfig();
            }
        }
        public async Task SaveConfig()
        {
            _configFile.SetLength(0);
            await JsonSerializer.SerializeAsync(_configFile, GlobalConfig);
            await _configFile.FlushAsync();
        }
    }
}
