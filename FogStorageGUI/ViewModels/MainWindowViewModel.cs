using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.DependencyInjection;

namespace FogStorageGUI.ViewModels;

public partial class MainWindowViewModel: ObservableObject
{
    public ObservableCollection<string> Files { get; } = new(Program.AppHost.Services.GetRequiredService<IDbRepository>().GetFilenames());
    
    [ObservableProperty]
    private string? _selectedItem;
    [ObservableProperty]
    private bool _webSocketsWork = Program.AppHost.Services.GetRequiredService<WebSocketsCommunicator>().IsConnected;
}