namespace FogStorageBackend.Configuration;

// This class is a set of user-redactable rules: download and shard folders, IP address of server
// which handles user requests
public class ApplicationGeneralSettings
{
    public string ApplicationDefaultFolder { get; set; }
    public string ShardFolderName { get; set; }
    public string DownloadFolder { get; set; }
}