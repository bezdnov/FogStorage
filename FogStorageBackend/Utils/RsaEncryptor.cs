using System.Security.Cryptography;

namespace FogStorageBackend.Utils;

public static class RsaEncryptor
{
    public static byte[] RsaBytesEncrypt(byte[] inputBytes, string publicKey)
    {
        using var rsa = RSA.Create();
        
        var keyBytes = Convert.FromHexString(publicKey);
        rsa.ImportRSAPublicKey(keyBytes, out _);
        return rsa.Encrypt(inputBytes, RSAEncryptionPadding.OaepSHA256);
    }
    
    public static byte[] RsaBytesDecrypt(byte[] inputBytes, string privateKey)
    {
        using var rsa = RSA.Create();
        var keyBytes = Convert.FromHexString(privateKey);
        
        rsa.ImportRSAPrivateKey(keyBytes, out _);
        return rsa.Decrypt(inputBytes, RSAEncryptionPadding.OaepSHA256);
    } 
}