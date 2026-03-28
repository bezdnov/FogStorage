using System.Collections;
using System.Runtime.Serialization;
using FogStorageBackend.Configuration;
using FogStorageBackend.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FogStorageBackend.Utils;
using FogStorageBackend.Constants;

namespace FogStorageBackend.Repository;

/*
 * A repository class for FogStorage
 *
 * Gives opportunity to Hosted services to work with shards: this class does all the work related to them.
 * Default shard folder is:
 * 
 */
public class ShardOperator: IShardOperator
{
    private readonly ApplicationGeneralSettings _appSettings;
    private readonly ILogger _logger;
    
    private JsonSerializerOptions _jsonSerializerOptions;
    
    public ShardOperator(ILogger<ShardOperator> logger, IOptions<ApplicationGeneralSettings> appSettings)
    {
        _appSettings = appSettings.Value;
        _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
        };
    }

    public ShardOperator(ILogger<ShardOperator> logger, ApplicationGeneralSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }
    
    // Its important that shardId's are dependent of fileIds
    // IV is one for the whole file, and its ok
    public Shard[] SplitFile(StoredFileInfo fileInfo)
    {
        _logger.LogInformation($"A file is being split");
        if (fileInfo.FileBytes.Length / 1024 < StorageConstants.MinimalFileSizeKb)
        {
            _logger.LogWarning($"Too small file; aborting");
            throw new Exception("File is too small");
        }
        
        byte[] encryptedFileBytes;
        
        using (var aes = Aes.Create())
        {
            encryptedFileBytes = AesEncryptor.AesBytesEncrypt(fileInfo.FileBytes, aes);
            fileInfo.FileAESIV = Convert.ToHexString(aes.IV);
            fileInfo.FileAESKey = Convert.ToHexString(aes.Key);
        }

        byte[] aesKeyEncrypted;
        using (var rsa = RSA.Create())
        {
            rsa.ImportRSAPublicKey(Convert.FromHexString(fileInfo.FilePublicKey), out _);
            aesKeyEncrypted = rsa.Encrypt(Convert.FromHexString(fileInfo.FileAESKey), RSAEncryptionPadding.OaepSHA256);
        }
        
        var shards = new Shard[StorageConstants.ShardingFactor];
        var splitSize = encryptedFileBytes.Length / StorageConstants.ShardingFactor;
        
        for (var i = 0; i < StorageConstants.ShardingFactor; ++i)
        {
            var fileShardBytes =
                (i == StorageConstants.ShardingFactor - 1) ?
                encryptedFileBytes[(splitSize * i)..] : 
                encryptedFileBytes[(i * splitSize)..((i + 1) * splitSize)];

            var md5Hash = MD5.HashData(fileShardBytes);

            var PoOBytes = RandomNumberGenerator.GetBytes(16);

            shards[i] = new Shard
            {
                ShardBytes = fileShardBytes,
                ShardIndex = i,
                FileAESIV = fileInfo.FileAESIV,
                FilePublicKey = fileInfo.FilePublicKey,
                FileAESKeyEncrypted = Convert.ToHexString(aesKeyEncrypted),
                MD5Checksum = Convert.ToHexString(md5Hash),
                ShardLastCheckTime = DateTime.UtcNow,
                ProofBytesUnencrypted = PoOBytes,
                ProofBytesEncrypted = RsaEncryptor.RsaBytesEncrypt(PoOBytes, fileInfo.FilePublicKey)
            };
        }

        return shards;
    }

    public StoredFileInfo RecreateFile(Shard[] shards, string filePrivateKey)
    {
        // Length validation
        if (shards.Length != StorageConstants.ShardingFactor)
            throw new ShardingMismatchException();
        
        // keys validation
        for (var i = 1; i < StorageConstants.ShardingFactor; ++i)
        {
            if (shards[i].FilePublicKey != shards[0].FilePublicKey || shards[i].FileAESKeyEncrypted != shards[0].FileAESKeyEncrypted || shards[i].FileAESIV != shards[0].FileAESIV)
                throw new ShardingMismatchException();
        }

        var size = shards.Sum(shard => shard.ShardBytes.Length);
        
        Array.Sort(shards, (a, b) => a.ShardIndex.CompareTo(b.ShardIndex));
        
        // encrypted blob recreation
        var fileDataEncrypted = new byte[size];
        var offset = 0;

        for (var i = 0; i < StorageConstants.ShardingFactor; ++i)
        {
            Buffer.BlockCopy(shards[i].ShardBytes, 0, fileDataEncrypted, offset, shards[i].ShardBytes.Length);
            offset += shards[i].ShardBytes.Length; 
        }
        
        // decryption of AES key using filePrivateKey received in arguments
        // important to note, that the order of encryption is in, firstly, encrypting the RSA key, then - the whole file
        byte[] fileAesKey;
        using (var rsa = RSA.Create())
        {
            rsa.ImportRSAPrivateKey(Convert.FromHexString(filePrivateKey), out _);
            fileAesKey = rsa.Decrypt(Convert.FromHexString(shards[0].FileAESKeyEncrypted), RSAEncryptionPadding.OaepSHA256);
            _logger.LogInformation($"Decrypted AES key: {fileAesKey}");
        }
        
        var fileData = AesEncryptor.AesBytesDecrypt(fileDataEncrypted, fileAesKey, Convert.FromHexString(shards[0].FileAESIV));
        
        StoredFileInfo fileInfo = new StoredFileInfo()
        {
            FileAESKey = Convert.ToHexString(fileAesKey),
            FilePrivateKey = filePrivateKey,
            FileAESIV = shards[0].FileAESIV,
            FileBytes = fileData,
            FilePublicKey = shards[0].FilePublicKey,
        };

        return fileInfo;
    }
    
    public void SaveShard(Shard shard)
    {
        var shardFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName);
        if (!Directory.Exists(shardFolder))
        {
            _logger.LogDebug($"Creating a new shard folder: {shardFolder}");
            Directory.CreateDirectory(shardFolder);
        }
        var shardPath = Path.Combine(shardFolder, string.Concat(CreateShardName(shard), ".shard"));
        
        if (File.Exists(shardPath))
        {
            _logger.LogWarning($"A false try on saving 2nd file: {shardPath}");
            throw new ShardExistsException();
        }
        
        // checking if any shard has the same FilePublicKey
        // seems to be unneeded, since way of creating shard files already gives opportunity
        // to discard saving of shards of the same file, but... let it be.
        var savedShards = LoadAllShards();
        foreach (var savedShard in savedShards)
        {
            if (savedShard.FilePublicKey == shard.FilePublicKey)
                throw new ShardExistsException($"Shard with public key {shard.FilePublicKey} already exists");
        }
        
        _logger.LogInformation($"Saving shard from file with pubkey (first 16) {shard.FilePublicKey.Substring(0, 16)} to {shardPath}");
        // Console.WriteLine($"Saving shard from file with pubkey {shard.FilePublicKey} to {shardPath}");
        
        // Serialization is mostly used with public properties, not with structures.
        // So I use these options to work with them

        var jsonData = JsonSerializer.Serialize(shard, _jsonSerializerOptions);
        
        File.WriteAllBytes(shardPath, Encoding.UTF8.GetBytes(jsonData));
    }

    public void DeleteShard(Shard shard)
    {
        var shardFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName);
        if (!Directory.Exists(shardFolder))
        {
            _logger.LogDebug($"Creating a new shard folder: {shardFolder}");
            Directory.CreateDirectory(shardFolder);
        }
        
        var shardPath = Path.Combine(shardFolder, CreateShardName(shard));
        File.Delete(shardPath);
    }

    public List<string> GetShardNames()
    {
        var shardFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName);
        var shardFiles = Directory.GetFiles(shardFolder);

        var shardNames = new List<string>();
        
        foreach (var file in shardFiles)
        {
            if (!file.EndsWith(".shard")) {
                _logger.LogWarning($"File {file} doesn't have '.shard' extension; ignored");
            }
            shardNames.Add(file);
        }

        return shardNames;
    }

    public Shard? LoadShardByName(string shardName)
    {
        if (!shardName.EndsWith(".shard")) {
            _logger.LogWarning($"File {shardName} doesn't have '.shard' extension; ignored");
        }
        
        var shardFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName);
        var shardPath = Path.Combine(shardFolder, shardName);
        

        Shard shard;

        try
        {
            var text = File.ReadAllText(shardPath);
            shard = JsonSerializer.Deserialize<Shard>(text);
        }
        catch (SerializationException ex)
        {
            _logger.LogWarning($"Couldn't load deserealize {shardName}");
            return null;
        }
        return shard;
    }
    
    // load all existing shards from standard folder
    public LinkedList<Shard> LoadAllShards()
    {
        var shardNames = GetShardNames();
        
        LinkedList<Shard> shards = new LinkedList<Shard>();
        foreach (var file in shardNames)
        {
            if (!file.EndsWith(".shard")) {
                _logger.LogWarning($"File {file} doesn't have '.shard' extension; ignored");
            }
            
            var shard = LoadShardByName(file);
            if (shard != null)
            {
                shards.AddLast(shard);
            }
        }

        return shards;
    }
    
    // Standard way to create shard name. Only one shard of 1 file is used in real world
    private static string CreateShardName(Shard shard)
    {
        return string.Concat("shard-", shard.FilePublicKey.AsSpan(0, 16), "-", Convert.ToString(shard.ShardIndex));
    }

    public int CalculateShardWeight() => LoadAllShards().Sum(shard => shard.ShardBytes.Length);
    
    public bool HasShardWithPubkey(string filePrivateKey)
    {
        _logger.LogDebug($"Checking if shard with public key (first 10 bytes) {filePrivateKey.AsSpan(0, 20)} exists");
        foreach (var shard in LoadAllShards())
        {
            Console.WriteLine(shard.FilePublicKey);
            if (filePrivateKey == shard.FilePublicKey)
                return true;
        }

        return false;
    }
}

