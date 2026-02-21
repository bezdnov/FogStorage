using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

public interface IShardOperator
{
    // Disk stored file => a list of Shards that are then will be sent to the network
    public Shard[] SplitFile(StoredFileInfo fileInfo);
    // Recreation of network stored file from obtained shards
    public StoredFileInfo RecreateFile(Shard[] shards);

}
