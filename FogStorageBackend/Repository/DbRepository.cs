using FogStorageBackend.Configuration;
using FogStorageBackend.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.Repository;


/*
 * DbRepository.cs
 * This class is needed to operate with SQLite database, which is used to store information on RSA keypairs
 * of each file stored in the Fog
 */
public class DbRepository : IDbRepository
{
    private ILogger<DbRepository> _logger;
    // creation of db in case of its absence + connecting to it
    public DbRepository(ILogger<DbRepository> logger)
    {
        _logger = logger;

        using (var connection = new SqliteConnection(CreateConnectionString()))
        {
            connection.Open();
            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = SqliteConstants.CreateTable;
            createTableCmd.ExecuteNonQuery();
            
            _logger.LogDebug("Table created or checked {connectionString}", CreateConnectionString());
        }
    } 
    
    public string[] GetPrivateKeys()
    {
        var keys = new LinkedList<string>();
        
        using (var connection = new SqliteConnection(CreateConnectionString()))
        {
            connection.Open();
            var getPrivateKeysCmd = connection.CreateCommand();
            getPrivateKeysCmd.CommandText = SqliteConstants.GetPrivateKeys;

            using (var reader = getPrivateKeysCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var privateKey = reader.GetString(0);
                    
                    keys.AddLast(privateKey);
                    _logger.LogDebug($"Read key {privateKey}");
                }
            }
        }
        
        return keys.ToArray();
    }

    public string[] GetPublicKeys()
    {
        var keys = new LinkedList<string>();
        
        using (var connection = new SqliteConnection(CreateConnectionString()))
        {
            connection.Open();
            var getPrivateKeysCmd = connection.CreateCommand();
            getPrivateKeysCmd.CommandText = SqliteConstants.GetPublicKeys;

            using (var reader = getPrivateKeysCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var publicKey = reader.GetString(0);
                    
                    keys.AddLast(publicKey);
                    _logger.LogDebug($"Read key {publicKey}");
                }
            }
        }
        
        return keys.ToArray();
    }

    public string GetPrivateKeyByPublicKey(string publicKey)
    {
        using (var connection = new SqliteConnection(CreateConnectionString()))
        {
            connection.Open();
            var getPrivateKeyCmd = connection.CreateCommand();
            getPrivateKeyCmd.CommandText = SqliteConstants.GetPrivateKeyByPublicKey;

            using (var reader = getPrivateKeyCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var privateKey = reader.GetString(0);
                    _logger.LogDebug($"Read private key {privateKey}");
                    
                    return privateKey;
                }
            }
        }
        return string.Empty;
    }

    public void SaveFileData(string filename, string privateKey, string publicKey)
    {
        using (var connection = new SqliteConnection(CreateConnectionString()))
        {
            connection.Open();
            var insertFileDataCmd = connection.CreateCommand();
            insertFileDataCmd.CommandText = SqliteConstants.InsertFileData;
            
            var res = insertFileDataCmd.ExecuteNonQuery();
            
            _logger.LogDebug($"Insertion returned {res}");
        }
    }

    private static string CreateConnectionString()
    {
        var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folderPath = Path.Combine(homeFolder, ".local/share/FogStorage/Db");
        var dbPath = Path.Combine(folderPath, SqliteConstants.DbName);

        return $"Data Source={dbPath}";
    }
}