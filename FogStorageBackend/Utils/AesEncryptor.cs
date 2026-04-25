using System.Security.Cryptography;

namespace FogStorageBackend.Utils;

public static class AesEncryptor
{
    public static byte[] AesBytesEncrypt(byte[] data, Aes aesAlg)
    {
        using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }
    
    public static byte[] AesBytesDecrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;

        using var decryptor = aesAlg.CreateDecryptor();
        using var ms = new MemoryStream(cipherText);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();

        cs.CopyTo(result);
        return result.ToArray();
    }
}