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
        
        Logger<FileOperator> logger = new Logger<FileOperator>(new LoggerFactory());
        
        ApplicationGeneralSettings appSettings = new ApplicationGeneralSettings()
        {
            ApplicationDefaultFolder = "/home/cursed_nerd/.local/share/FogStorage/",
            ShardFolderName = tempDir,
            DownloadFolder = tempDir,
            DbFolderName = tempDir,
        };

        var options = Options.Create(appSettings);

        FileOperator fo = new FileOperator(logger, options);
        StoredFileInfo info = fo.ReadFile(filePath);

        Assert.NotNull(info.FilePublicKey);
        Assert.NotNull(info.FileAESKey);
        Assert.NotNull(info.FilePrivateKey);
        Assert.Equal(info.FileBytes, Encoding.UTF8.GetBytes(fileData));
        Assert.NotNull(info.FileAESIV);
    }
    
    [Theory]
    [InlineData("tempfile1.txt", "SOME FILE CONTENT!!!1111!!!")]
    [InlineData("tempfile2.txt", "This is some bigger content! abacaba19")]
    public void TestShardOperator(string filename, string fileContent)
    {
        var tmpFolder = Path.GetTempPath();
        ApplicationGeneralSettings appSettings = new ApplicationGeneralSettings()
        {
            ApplicationDefaultFolder = "/home/cursed/.local/share/FogStorage/",
            ShardFolderName = tmpFolder,
            DownloadFolder = tmpFolder,
            DbFolderName = tmpFolder,
        };
        Logger<ShardOperator> logger1 = new Logger<ShardOperator>(new LoggerFactory());
        Logger<FileOperator> logger2 = new Logger<FileOperator>(new LoggerFactory());
        
        var options = Options.Create(appSettings);
        
        FileOperator fo = new FileOperator(logger2, options);
        
        string filePath = Path.Combine(tmpFolder, filename);
        File.WriteAllText(filePath, fileContent);
        StoredFileInfo info = fo.ReadFile(filePath);
        
        ShardOperator so = new ShardOperator(logger1, options);
        Shard[] shards = so.SplitFile(info);

        // data, that should be the same in file and each shard, stays the same
        foreach (var shard in shards)
        {
            Assert.Equal(shard.FilePublicKey, info.FilePublicKey);
            // Assert.Equal(shards[i].FileAESIV, info.FileAESIV);
        }

        var file = so.RecreateFile(shards, info.FilePrivateKey);
        Assert.Equal(file.FilePublicKey, shards[0].FilePublicKey);
        Assert.Equal(Convert.ToString(file.FileBytes), fileContent);
    }
}