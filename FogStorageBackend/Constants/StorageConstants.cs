namespace FogStorageBackend.Constants;

public static class StorageConstants
{
    public static readonly int MaximalFileSizeKb = 65536;
    public static readonly int MinimalFileSizeKb = 0;
    public static readonly int ReplicationFactor = 2;
    public static readonly int ShardingFactor = 2;
    public static readonly int FileStorageTimeout = 600; // in seconds
    public static readonly int CheckTimeout = 60; // in seconds
}