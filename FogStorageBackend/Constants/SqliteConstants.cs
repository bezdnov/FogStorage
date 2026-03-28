namespace FogStorageBackend.Constants;

public static class SqliteConstants
{
    public const string CreateTable = $@"
            CREATE TABLE IF NOT EXISTS FileData (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
            PrivateKey TEXT NOT NULL, 
            PublicKey TEXT NOT NULL,
            CumulativeShardSize INTEGER NOT NULL,
    );";
    
    public const string ReadTable = @"SELECT * FROM FileData;";
    public const string GetPrivateKeys = @"SELECT private_key FROM FileData;";
    public const string GetPublicKeys = @"SELECT public_key FROM FileData;";

    public const string InsertFileData =
        @"INSERT INTO FileData (PrivateKey, PublicKey, CumulativeShardSize) VALUES ($private_key , $public_key, $cumulative_shard_size);";

    public const string ConnectionString = "Data Source=files.db";
}
