using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using FogStorageBackend.Utils;
using Xunit.Abstractions;

namespace FogStorageTests;

public class CryptographyTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CryptographyTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("TestString1")]
    [InlineData("12341234!")]
    [InlineData("")]
    public void TestAesEncryptor(string testString)
    {
        using var aes = Aes.Create();
        var encryptedData = AesEncryptor.AesBytesEncrypt(Encoding.UTF8.GetBytes(testString), aes);
        var decryptedData = AesEncryptor.AesBytesDecrypt(encryptedData, aes.Key, aes.IV);
            
        Assert.Equal(testString, Encoding.UTF8.GetString(decryptedData));
    }
    
    [Theory]
    [InlineData("TestString1")]
    [InlineData("12341234!")]
    [InlineData("")]
    public void TestRsaEncryptor(string testString)
    {
        using var rsa = RSA.Create();
        var privateKey = Convert.ToHexString(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToHexString(rsa.ExportRSAPublicKey());
        
        _testOutputHelper.WriteLine($"Priv key: {privateKey}");
        _testOutputHelper.WriteLine($"Pub key: {publicKey}");
        _testOutputHelper.WriteLine($"Test string {testString}, {Encoding.UTF8.GetBytes(testString)}");
        
        var encryptedData = RsaEncryptor.RsaBytesEncrypt(Encoding.UTF8.GetBytes(testString), publicKey);
        var decryptedData = RsaEncryptor.RsaBytesDecrypt(encryptedData, privateKey);
        Assert.Equal(testString, Encoding.UTF8.GetString(decryptedData));
    }
}

