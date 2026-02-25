using System.Text;
using FogStorageBackend.Configuration;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FogStorageTests;

public class OperatorsTest
{
    [Theory]
    [InlineData("tempfile.txt", "AAbbCCee")]
    [InlineData("ahahahah.lol", "\f\x01\x02\xff\x05\n\r\n\r")]
    public void TestFileOperator(string filename, string fileData)
    {
        // file mock
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        string filePath = Path.Combine(tempDir, filename);
        File.WriteAllText(filePath, fileData);

        FileOperator fo = new FileOperator();
        StoredFileInfo info = fo.ReadFile(filePath);

        Assert.NotNull(info.FilePublicKey);
        Assert.NotNull(info.FileAESKey);
        Assert.NotNull(info.FilePrivateKey);
        Assert.Equal(info.FilePath, filePath);
        Assert.Equal(info.FileBytes, Encoding.UTF8.GetBytes(fileData));
        Assert.NotNull(info.FileAESIV);
    }
    
    [Theory]
    [InlineData("tempfile1.txt", "SOME FILE CONTENT!!!1111!!!", 2)]
    [InlineData("tempfile2.txt", "This is some bigger content! abacaba19", 3)]
    public void TestShardOperator(string filename, string fileContent, int shardAmount)
    {
        FileOperator fo = new FileOperator();
        Logger<ShardOperator> logger = new Logger<ShardOperator>(new LoggerFactory());
        
        string tmpFolder = Path.GetTempPath();

        ApplicationGeneralSettings appSettings = new ApplicationGeneralSettings()
        {
            ApplicationDefaultFolder = "/home/cursed/.local/share/FogStorage/",
            ShardFolderName = tmpFolder,
            DownloadFolder = tmpFolder,
        };

        // standard configuration
        StorageConfiguration storageConfiguration = new StorageConfiguration()
        {
            MaximalFileSizeKb = 65536,
            MinimalFileSizeKb = 1,
            ReplicationFactor = 2,
            ShardingFactor = shardAmount,
            FileStorageTimeout = 600,
            CheckTimeout = 60
        };
        
        string filePath = Path.Combine(tmpFolder, filename);
        File.WriteAllText(filePath, fileContent);
        StoredFileInfo info = fo.ReadFile(filePath);
        
        ShardOperator so = new ShardOperator(logger, storageConfiguration, appSettings);
        Shard[] shards = so.SplitFile(info);

        // data, that should be the same in file and each shard, stays the same
        for (var i = 0; i < shards.Length; ++i)
        {
            Assert.Equal(shards[i].FilePublicKey, info.FilePublicKey);
            Assert.Equal(shards[i].FileAESIV, info.FileAESIV);
        }

        // so.RecreateFile(shards);
    }
}