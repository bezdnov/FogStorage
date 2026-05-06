namespace FogStorageBackend.Configuration;

// This class is a set of user-redactable rules: download and shard folders, IP address of server
// which handles user requests
public class ApplicationGeneralSettings
{
    public required string ApplicationDefaultFolder { get; init; }
    public required string ShardFolderName { get; init; }
    public required string DownloadFolder { get; init; }
    public required string DbFolderName { get; init; }
}