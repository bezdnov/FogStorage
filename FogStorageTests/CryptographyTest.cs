using System.Security.Cryptography;
using System.Text;
using FogStorageBackend.Utils;

namespace FogStorageTests;

public class CryptographyTest
{
    [Theory]
    [InlineData("TestString1")]
    [InlineData("12341234!")]
    public void TestAes(string testString)
    {
        
    }
}