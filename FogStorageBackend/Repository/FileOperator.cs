using System.Security.Cryptography;
using FogStorageBackend.Configuration;
using FogStorageBackend.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FogStorageBackend.Repository;

/*
 * FileOperator
 * A class which does operations to a file on disk
 * File is stored unencrypted in RAM, and encryption is done during sharding, decryption - during restoring
 * The only cryptography done here is generation of RSA keypair
 */
public class FileOperator: IFileOperator
{
    private readonly ILogger<FileOperator> _logger;
    private readonly ApplicationGeneralSettings _appSettings;

    public FileOperator(ILogger<FileOperator> logger, IOptions<ApplicationGeneralSettings> appSettings)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
    }
    
    public StoredFileInfo ReadFile(string filePath)
    {
        _logger.LogDebug($"Reading a file by path: {filePath}");
        var fileInfo = new StoredFileInfo();
        using (var sr = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            fileInfo.FileBytes = sr.ReadBytes((int)sr.BaseStream.Length);
        }
        
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

    public StoredFileInfo ReadFile(Uri pathUri)
    {
        return ReadFile(pathUri.ToString());
    }
    
    public void WriteFile(StoredFileInfo fileInfo, string fileName)
    {
        _logger.LogDebug($"Writing a file: {fileName}");
        var filePath = CreateFilePath(fileName);
        
        using (BinaryWriter bw = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            bw.Write(fileInfo.FileBytes);
        }
    }

    private string CreateFilePath(string fileName)
    {
        Thread.Sleep(15000);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _appSettings.DownloadFolder, fileName);
    }
}