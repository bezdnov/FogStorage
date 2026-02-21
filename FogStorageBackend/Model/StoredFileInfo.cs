using System.Reflection.Metadata;

namespace FogStorageBackend.Model;

/*
 * Model class
 * Stores information on a saved file
 */
public struct StoredFileInfo
{
    public string FilePath;
    public Guid FileGuid;
    public string FilePrivateKey;
    public byte[] FileBytes;
}
