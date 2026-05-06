using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FogStorageGUI.ViewModels;

public class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly IDbRepository _dbRepository;
    private readonly WebSocketsCommunicator _webSockets;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel()
    {
        _dbRepository = Program.AppHost.Services.GetRequiredService<IDbRepository>();
        _webSockets = Program.AppHost.Services.GetRequiredService<WebSocketsCommunicator>();
        _logger = Program.AppHost.Services.GetRequiredService<ILogger<MainWindowViewModel>>();

        _webSockets.ConnectionChanged += (sender, args) =>
        {
            OnPropertyChanged(nameof(CanSaveFile));
            OnPropertyChanged(nameof(CanRestoreOrDelete));
        };

        _webSockets.FileRestoredOrDeleted += (sender, args) =>
        {
            OnPropertyChanged(nameof(CanRestoreOrDelete));
        };
    }

    private string _selectedItem = string.Empty;
    

    public string SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnPropertyChanged(nameof(CanRestoreOrDelete));
        }
    }
    

    public string[] Files => _dbRepository.GetFilenames();

    public bool CanSaveFile
    {
        get
        {
            return _webSockets.IsConnected;
        }
    }

    public bool CanRestoreOrDelete
    {
        get
        {
            return _webSockets.IsConnected && SelectedItem != string.Empty;
        }
    }
}