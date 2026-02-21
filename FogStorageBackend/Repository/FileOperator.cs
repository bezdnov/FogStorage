using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

/*
 * FileOperator
 * A class which does operations to a file on disk
 */
public class FileOperator: IFileOperator
{
    public StoredFileInfo ReadFile(string filePath)
    {
        StoredFileInfo fileInfo = new StoredFileInfo();
        using (BinaryReader sr = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            fileInfo.FileBytes = sr.ReadBytes((int)sr.BaseStream.Length);
        }
        fileInfo.FileGuid = Guid.NewGuid();
        // fileInfo.FilePrivateKey = ????
        fileInfo.FilePath = filePath;

        return fileInfo;
    }

    public void WriteFile(StoredFileInfo fileInfo)
    {
        
        throw new NotImplementedException();
    }
}