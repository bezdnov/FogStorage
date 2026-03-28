using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

public interface IFileOperator
{
    public StoredFileInfo ReadFile(string filePath);
    public StoredFileInfo ReadFile(Uri filePath);
    public void WriteFile(StoredFileInfo fileInfo, string filePath);
    
}