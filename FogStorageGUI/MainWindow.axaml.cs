using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FogStorageBackend.Constants;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using FogStorageGUI.ViewModels;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;

namespace FogStorageGUI;

public partial class MainWindow : Window
{
    private readonly IFileOperator _fileOperator;
    private readonly IShardOperator _so;
    private readonly IDbRepository _dbRepo;
    private readonly ILogger<MainWindow> _logger;
    private readonly WebSocketsCommunicator _communicator;
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        _fileOperator = Program.AppHost.Services.GetRequiredService<IFileOperator>();
        _so = Program.AppHost.Services.GetRequiredService<IShardOperator>();
        _communicator = Program.AppHost.Services.GetRequiredService<WebSocketsCommunicator>();
        _dbRepo = Program.AppHost.Services.GetRequiredService<IDbRepository>();
        _logger = Program.AppHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger<MainWindow>();
    }

    private async void OnSaveFile(object sender, RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open File",
                AllowMultiple = false,
            });

        if (files.Count > 0)
        {
            var file = files[0];
            _logger.LogInformation("The file object that was scanned is a file");
            if (file.Path.IsFile)
            {
                var path = file.Path.LocalPath;
                var filename = file.Name;
                var storedFileInfo = _fileOperator.ReadFile(path);
                
                var shards = _so.SplitFile(storedFileInfo);
                
                var sumSize = shards.Sum(shard => shard.ShardBytes.Length);
                _dbRepo.SaveFileData(filename, storedFileInfo.FilePrivateKey, storedFileInfo.FilePublicKey, sumSize);
                
                // It's guaranteed by this moment that the connection to WebSocketsCommunicator is alive
                foreach (var shard in shards)
                    await _communicator.SendShard(shard);
            }
            else {
                _logger.LogInformation("Not a file was chosen");
            }
        }
    }

    private async void OnRestoreFile(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext;
        if (viewModel?.SelectedItem == null)
            _logger.LogWarning("ViewModel or SelectedItem is null, this must not happen");

        var filename = viewModel.SelectedItem;
        
        var publicKey = _dbRepo.GetPublicKeyByFilename(filename);
        var privateKey = _dbRepo.GetPrivateKeyByPublicKey(publicKey);
        
        var shards = await _communicator.GetShards(publicKey);
        var file = _so.RecreateFile(shards, privateKey);
        _fileOperator.WriteFile(file, filename);
    }

    private async void OnDeleteFile(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext;
        if (viewModel?.SelectedItem == null)
            _logger.LogWarning("ViewModel or SelectedItem is null, this must not happen");

        var filename = viewModel.SelectedItem;
        var publicKey = _dbRepo.GetPublicKeyByFilename(filename);
        
        await _communicator.DeleteFile(publicKey);
    }
}
