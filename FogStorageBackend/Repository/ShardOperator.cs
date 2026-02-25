using FogStorageBackend.Configuration;
using FogStorageBackend.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;

namespace FogStorageBackend.Repository;

/*
 * A repository class for FogStorage
 *
 * Gives opportunity to Hosted services to work with file data on disk
 * 
 */
public class ShardOperator: IShardOperator
{
    private StorageConfiguration _options;
    private ApplicationGeneralSettings _appSettings;
    private ILogger _logger;
    public ShardOperator(ILogger<ShardOperator> logger, IOptions<StorageConfiguration> options, IOptions<ApplicationGeneralSettings> appSettings)
    {
        _options = options.Value;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public ShardOperator(ILogger<ShardOperator> logger, StorageConfiguration options,
        ApplicationGeneralSettings appSettings)
    {
        _options = options;
        _logger = logger;
        _appSettings = appSettings;
    }

    // Its important that shardId's are dependent of fileIds
    // IV is one for the whole file, and its ok
    public Shard[] SplitFile(StoredFileInfo fileInfo)
    {
        _logger.LogInformation($"A file {fileInfo.FilePath} is being split");
        
        byte[] encryptedFileBytes;

        using (var aes = Aes.Create())
        {
            encryptedFileBytes = AesBytesEncrypt(fileInfo.FileBytes, aes);
        }
        var shards = new Shard[_options.ShardingFactor];
        var splitSize = encryptedFileBytes.Length / _options.ShardingFactor;
        
        for (var i = 0; i < _options.ShardingFactor; ++i)
        {
            byte[] fileShardBytes;

            if (i == _options.ShardingFactor - 1)
                fileShardBytes = encryptedFileBytes[(splitSize * i)..];
            else 
                fileShardBytes = encryptedFileBytes[(i * splitSize)..((i + 1) * splitSize)];
                
            byte[] aesKeyEncrypted;
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromHexString(fileInfo.FilePublicKey), out _);
                aesKeyEncrypted = rsa.Encrypt(Convert.FromHexString(fileInfo.FileAESKey), RSAEncryptionPadding.OaepSHA256);
            }

            byte[] md5Hash;
            
            using (var md5 = MD5.Create())
            {
                md5Hash = md5.ComputeHash(fileShardBytes);
            }
            
            shards[i] = new Shard
            {
                ShardBytes = fileShardBytes,
                ShardIndex = i,
                FileAESIV = fileInfo.FileAESIV,
                FilePublicKey = fileInfo.FilePublicKey,
                FileAESKeyEncrypted = Convert.ToHexString(aesKeyEncrypted),
                MD5Checksum = Convert.ToHexString(md5Hash),
            };
        }

        return shards;
    }

    // Recreation of file is quite a risky thing
    // What can go wrong: incorrect amount of shards, incorrect fileIds
    // Decryption error
    public StoredFileInfo? RecreateFile(Shard[] shards, string filePrivateKey)
    {
        // Length validation
        if (shards.Length != _options.ShardingFactor)
            throw new Exception();
        
        // keys validation
        for (var i = 1; i < _options.ShardingFactor; ++i)
        {
            if (shards[i].FilePublicKey != shards[0].FilePublicKey || shards[i].FileAESKeyEncrypted != shards[0].FileAESKeyEncrypted || shards[i].FileAESIV != shards[0].FileAESIV)
                throw new Exception();
        }

        var size = shards.Sum(shard => shard.ShardBytes.Length);
        
        Array.Sort(shards, (a, b) => b.ShardIndex.CompareTo(a.ShardIndex));
        
        
        // encrypted blob recreation
        var fileDataEncrypted = new byte[size];

        var offset = 0;

        for (var i = 0; i < _options.ShardingFactor; ++i)
        {
            Buffer.BlockCopy(shards[i].ShardBytes, 0, fileDataEncrypted, offset, shards[i].ShardBytes.Length);
            offset += shards[i].ShardBytes.Length; 
        }

        byte[] fileAesKey;
        // decryption of AES key using filePrivateKey received in arguments...
        using (var rsa = RSA.Create())
        {
            rsa.ImportRSAPrivateKey(Convert.FromHexString(filePrivateKey), out _);

            fileAesKey = rsa.Decrypt(Convert.FromHexString(shards[0].FileAESKeyEncrypted), RSAEncryptionPadding.OaepSHA256);
            _logger.LogInformation($"Decrypted AES key: ${fileAesKey}");
        }
        
        // decryption of file using AES key...
        byte[] fileData = AesBytesDecrypt(fileDataEncrypted, fileAesKey, Convert.FromHexString(shards[0].FileAESIV));
        
        // returning the file structure
        StoredFileInfo fileInfo = new StoredFileInfo()
        {
            FileAESKey = Convert.ToHexString(fileAesKey),
            FilePrivateKey = filePrivateKey,
            FileAESIV = shards[0].FileAESIV,
            FileBytes = fileData,
            FilePublicKey = shards[0].FilePublicKey,
            FilePath = _appSettings.DownloadFolder
        };

        return fileInfo;
    }
    
    public void SaveShard(Shard shard)
    {
        var shardPath = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName,
            CreateShardName(shard));
        
        _logger.LogInformation($"Saving shard from file {shard.FilePublicKey} to {shardPath}");

        var bytes = System.Text.Json.JsonSerializer.Serialize(shard);
        File.WriteAllBytes(shardPath, Encoding.UTF8.GetBytes(bytes));
    }

    public void DeleteShard(Shard shard)
    {
        var shardPath = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName,
            CreateShardName(shard));
        File.Delete(shardPath);
    }
    
    // load all existing shards from standard folder
    public Shard[] LoadShards() 
    {
        return null;
    }
    
    // Standard way to create shard name
    private static string CreateShardName(Shard shard)
    {
        return Path.Combine("shard-", shard.FilePublicKey);
    }
    
    private static byte[] AesBytesEncrypt(byte[] data, Aes aesAlg)
    {
        using (ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    { 
                        sw.Write(data);
                    }
                }
                return ms.ToArray();
            }
        }
    }
    
    private static byte[] AesBytesDecrypt(byte[] cipherText, byte[] Key, byte[] IV)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            using (ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
            {
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (BinaryReader sr = new BinaryReader(cs))
                        {
                            return sr.ReadBytes((int)cs.Length);
                        }
                    }
                }
            }
        }
    }
}
