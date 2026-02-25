using System.Reflection.Metadata;

namespace FogStorageBackend.Model;

/*
 * Model class for Shard - a stored part of file.
 * De facto, a block of encrypted file data
 * 
 */
public struct Shard
{
    public string FileAESKeyEncrypted;
    public string FilePublicKey;
    public string FileAESIV;
    public int ShardIndex;   // a number of shards, specific to each file
    public byte[] ShardBytes;
    public byte[] PoOBytesUnencrypted;  // (PoO - proof of ownership)
                                        // a small number of bytes, using which owner can proof his ownership. This is
                                        // needed to speed up the check process
    public byte[] PoOBytesEncrypted;
    public int ShardTimeout;  // time after which shard can be deleted
    public string MD5Checksum;  // ALERT!!! nothing stops holder from redacting a file and replacing checksum with something else.
}
