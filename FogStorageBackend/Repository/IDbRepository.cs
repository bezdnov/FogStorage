namespace FogStorageBackend.Repository;

public interface IDbRepository
{
    public string[] GetPrivateKeys();
    public string[] GetPublicKeys();
    public string GetPrivateKeyByPublicKey(string publicKey);
    public string GetPublicKeyByFilename(string filename);
    public string[] GetFilenames();
    public int GetFileSizeSummary();
    public void SaveFileData(string filename, string privateKey, string publicKey, int cumulativeShardSize);
}
