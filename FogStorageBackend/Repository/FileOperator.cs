using System.Security.Cryptography;
using FogStorageBackend.Model;

namespace FogStorageBackend.Repository;

/*
 * FileOperator
 * A class which does operations to a file on disk
 * File is stored unencrypted in RAM, and encryption is done during sharding, decryption - during restoring
 */
public class FileOperator: IFileOperator
{
    public StoredFileInfo ReadFile(string filePath)
    {
        StoredFileInfo fileInfo = new StoredFileInfo();
        using (var sr = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            fileInfo.FileBytes = sr.ReadBytes((int)sr.BaseStream.Length);
        }
        fileInfo.FilePath = filePath;
        
        using (var rsa = RSA.Create())
        {
            var publicKey = Convert.ToHexString(rsa.ExportRSAPublicKey());
            var privateKey = Convert.ToHexString(rsa.ExportRSAPrivateKey());
            
            fileInfo.FilePublicKey = publicKey;
            fileInfo.FilePrivateKey = privateKey;

            using (var aes = Aes.Create())
            {
                fileInfo.FileAESKey = Convert.ToHexString(aes.Key);
                fileInfo.FileAESIV = Convert.ToHexString(aes.IV);
            }
        }

        return fileInfo;
    }

    public void WriteFile(StoredFileInfo fileInfo)
    {
        using (BinaryWriter bw = new BinaryWriter(File.Open(fileInfo.FilePath, FileMode.Create)))
        {
            bw.Write(fileInfo.FileBytes);
        }
    }
}