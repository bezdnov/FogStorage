using System.Reflection.Metadata;
using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

/*
 * A repository class for FogStorage
 *
 * Gives opportunity to Hosted services to work with file data
 * 
 */
public class ShardOperator: IShardOperator
{
    // Its important that shardId's are dependent of fileIds
    public Shard[] SplitFile(StoredFileInfo fileInfo)
    {
        Shard[] shards = new Shard[2]; // replace!

        int splitSize = fileInfo.FileBytes.Length / 2; // replace!

        for (var i = 0; i < 2 - 1; ++i)
        {
            shards[i] = new Shard
            {
                FileGuid = fileInfo.FileGuid,
                ShardBytes = fileInfo.FileBytes[(i * splitSize)..((i + 1) * splitSize)]
            };
        }
        
        // last shard, which can be of bigger size
        shards[2 - 1] = new Shard
        {
            ShardBytes = fileInfo.FileBytes[(splitSize * (2 - 1))..]
        };

        return shards;
    }

    public StoredFileInfo RecreateFile(Shard[] shards)
    {
        // StoredFileInfo fileInfo();
        
        // TODO: check shard correctness (fileId's should be the same)
        throw new NotImplementedException();
    }
}
