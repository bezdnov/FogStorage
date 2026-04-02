namespace FogStorageBackend.Constants;

public static class StorageConstants
{
    public static readonly int MaximalFileSizeKb = 65536;
    public static readonly int MinimalFileSizeKb = 128;
    public static readonly int ReplicationFactor = 2;
    public static readonly int ShardingFactor = 2;
    public static readonly int FileStorageTimeout = 600; // in seconds; amount of time we let the file be stored for without being checked by peers outside
    public static readonly int CheckTimeout = 60; // in seconds; checks files saved in the Fog
    public static readonly int LocalCheckTimeout = 60; // in seconds; check of file 
    
}