using System.Reflection.Metadata;

namespace FogStorageBackend.Model;

/*
 * Model class
 * Stores information on a saved file
 * Its important to mention, that information in shards is stored in encrypted way, and FileInfo contains
 * unencrypted file data
 */
public struct StoredFileInfo
{
    public string FilePrivateKey;
    public string FilePublicKey;
    public string FileAESKey;

    public string FileAESIV;
    // File content. Stored in RAM, which doesn't seem cool
    public byte[] FileBytes;
}
