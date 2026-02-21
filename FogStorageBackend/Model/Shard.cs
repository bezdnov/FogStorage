using System.Reflection.Metadata;

namespace FogStorageBackend.Model;

/*
 * Model class for Shard - a stored part of file.
 * De facto, a block of encrypted file data
 * 
 */
public struct Shard
{
    public Guid ShardGuid;   // a unique shard ID, generated based on file ID
    public Guid FileGuid;      // shard is linked to a file
    public int ShardIndex;   // a number of shard, specific to each file
    public byte[] ShardBytes;
}
