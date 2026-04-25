using System.Runtime.InteropServices;
using FogStorageBackend.Configuration;
using FogStorageBackend.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.Repository;


/*
 * DbRepository.cs
 * This class is needed to operate with SQLite database, which is used to store information on RSA key pairs
 * of each file stored in the Fog
 */
public class DbRepository : IDbRepository
{
    private readonly ILogger<DbRepository> _logger;
    // creation of db in case of its absence + connecting to it
    public DbRepository(ILogger<DbRepository> logger)
    {
        _logger = logger;

        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var createTableCmd = connection.CreateCommand();
        createTableCmd.CommandText = SqliteConstants.CreateTable;
        createTableCmd.ExecuteNonQuery();
            
        _logger.LogDebug("Table created or checked {connectionString}", CreateConnectionString());
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
        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var getPrivateKeyCmd = connection.CreateCommand();
        getPrivateKeyCmd.CommandText = SqliteConstants.GetPrivateKeyByPublicKey;
            
        getPrivateKeyCmd.Parameters.AddWithValue("@publicKey", publicKey);

        using var reader = getPrivateKeyCmd.ExecuteReader();
        while (reader.Read())
        {
            var privateKey = reader.GetString(0);
            _logger.LogDebug($"Read private key {privateKey}");
                    
            return privateKey;
        }

        return string.Empty;
    }

    public string[] GetFilenames()
    {
        var filenamesList = new LinkedList<string>();

        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var getFilenamesCmd = connection.CreateCommand();
        using (var reader = getFilenamesCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                filenamesList.AddLast(reader.GetString(0));
            }
        }

        return filenamesList.ToArray();
    }

    public int GetFileSizeSummary()
    {
        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var getFileSizeCmd = connection.CreateCommand();
        getFileSizeCmd.CommandText = SqliteConstants.GetFileSizeSummary;
        using var reader = getFileSizeCmd.ExecuteReader();
        while (reader.Read())
        {
            var sum = reader.GetInt32(0);
            _logger.LogDebug($"Read file size {sum}");
            return sum;
        }

        return -1;
    }

    public void SaveFileData(string filename, string privateKey, string publicKey, int fileSize)
    {
        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var insertFileDataCmd = connection.CreateCommand();
        insertFileDataCmd.CommandText = SqliteConstants.InsertFileData;
            
        insertFileDataCmd.Parameters.AddWithValue("@filename", filename);
        insertFileDataCmd.Parameters.AddWithValue("@privateKey", privateKey);
        insertFileDataCmd.Parameters.AddWithValue("@publicKey", publicKey);
        insertFileDataCmd.Parameters.AddWithValue("@fileSize", fileSize);
            
        var res = insertFileDataCmd.ExecuteNonQuery();
            
        _logger.LogDebug($"Insertion returned {res}");
    }
    
    // Here must be a bug: if there are 2 same filenames, but different files, it will break
    public string GetPublicKeyByFilename(string filename)
    {
        using var connection = new SqliteConnection(CreateConnectionString());
        connection.Open();
        var getPublicKeyCmd = connection.CreateCommand();
        getPublicKeyCmd.CommandText = SqliteConstants.GetPublicKeyByFilename;
            
        getPublicKeyCmd.Parameters.AddWithValue("@filename", filename);

        using var reader = getPublicKeyCmd.ExecuteReader();
        while (reader.Read())
        {
            var publicKey = reader.GetString(0);
            _logger.LogDebug($"Read public key {publicKey}");
                    
            return publicKey;
        }

        return string.Empty;
    }

    private static string CreateConnectionString()
    {
        var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folderPath = Path.Combine(homeFolder, ".local/share/FogStorage/Db");
        var dbPath = Path.Combine(folderPath, SqliteConstants.DbName);

        return $"Data Source={dbPath}";
    }
}