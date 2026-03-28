using System.Reflection.Metadata;

namespace FogStorageBackend.Model;

/*
 * Model class for Shard - a stored part of file.
 * De facto, a block of encrypted file data
 */
public class Shard
{
    public string FileAESKeyEncrypted {get; set;}
    public string FilePublicKey {get; set;}
    public string FileAESIV {get; set;}
    public int ShardIndex {get; set;}   // a number of shards, specific to each file
    public byte[] ShardBytes {get; set;}
    // These 2 fields are needed to holder to check if the one who wants the file is its owner
    public byte[] ProofBytesUnencrypted {get; set;}  // (PoO - proof of ownership)
                                        // a small number of bytes, using which owner can proof his ownership. This is
                                        // needed to speed up the check process
    public byte[] ProofBytesEncrypted {get; set;}
    public DateTime ShardLastCheckTime {get; set;}  // time after which shard can be deleted
    
    public string MD5Checksum {get; set;}  // ALERT!!! nothing stops holder from redacting a file and replacing checksum with something else.
}
