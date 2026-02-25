namespace FogStorageBackend.Configuration;

// set of "hardcoded" rules, that must not be redacted  
public class StorageConfiguration
{
    public int MaximalFileSizeKb { get; set; }
    public int MinimalFileSizeKb { get; set; }
    public int ReplicationFactor { get; set; }
    public int ShardingFactor { get; set; }
    public int FileStorageTimeout { get; set; }  // in seconds
    public int CheckTimeout { get; set; }  // in seconds
}
