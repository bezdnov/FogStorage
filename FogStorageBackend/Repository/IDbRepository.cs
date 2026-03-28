namespace FogStorageBackend.Repository;

public interface IDbRepository
{
    public string[] GetPrivateKeys();
    public string[] GetPublicKeys();
    public void SaveFileData(string filename, string privateKey, string publicKey);
}
