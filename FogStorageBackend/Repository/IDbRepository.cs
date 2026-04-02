namespace FogStorageBackend.Repository;

public interface IDbRepository
{
    public string[] GetPrivateKeys();
    public string[] GetPublicKeys();
    public string GetPrivateKeyByPublicKey(string publicKey);
    public void SaveFileData(string filename, string privateKey, string publicKey);
}
