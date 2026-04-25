using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

public interface IShardOperator
{
    public Shard[] SplitFile(StoredFileInfo fileInfo);
    // Recreation of network stored file from obtained shards
    public StoredFileInfo RecreateFile(Shard[] shards, string filePrivateKey);
    public void SaveShard(Shard shard);
    public LinkedList<Shard> LoadAllShards();
    public void DeleteShard(string filePublicKey);
    public Shard? LoadShardByName(string shardName);
    public List<string> GetShardNames();
    
    public int CalculateShardWeight();
    public bool HasShardWithPubkey(string filePrivateKey, int shardIndex=-1);
    public Shard? LoadShardByPublicKey(string publicKey);
}
