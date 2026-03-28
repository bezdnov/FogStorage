using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;

namespace FogStorageGUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnSaveFile(object sender, RoutedEventArgs e)
    {
        var fo = Program.AppHost.Services.GetRequiredService<IFileOperator>();
        var so = Program.AppHost.Services.GetRequiredService<IShardOperator>();
        var logging = Program.AppHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger<MainWindow>();
        
        var files = await this.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open File",
                AllowMultiple = false,
            });

        if (files.Count > 0)
        {
            var file = files[0];
            logging.LogInformation("The file object that was scanned is a file;     ");
            if (file.Path.IsFile)
            {
                var path = file.Path.LocalPath;
                var storedFileInfo = fo.ReadFile(path);
                
                var shards = so.SplitFile(storedFileInfo);
            }

            // It should be connected to FileOperator created standard way

        }
    }

    private async void OnRestoreFile(object sender, RoutedEventArgs e)
    {
        var fo = Program.AppHost.Services.GetRequiredService<IFileOperator>();
        var so = Program.AppHost.Services.GetRequiredService<IShardOperator>();
        var logging = Program.AppHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger<MainWindow>();
        
        // getting priv key
        var privateKey = "alskd";
        
        // 
    }

    private async void OnConfigureSettings(object sender, RoutedEventArgs e)
    {
        
    }
}