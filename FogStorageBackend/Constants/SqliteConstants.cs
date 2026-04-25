namespace FogStorageBackend.Constants;

public static class SqliteConstants
{
    public const string CreateTable = $@"
            CREATE TABLE IF NOT EXISTS FileData (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Filename TEXT NOT NULL,
            PrivateKey TEXT NOT NULL, 
            PublicKey TEXT NOT NULL,
            FileSize INTEGER NOT NULL
    );";
    
    public const string ReadTable = @"SELECT * FROM FileData;";
    public const string GetPrivateKeys = @"SELECT PrivateKey FROM FileData;";
    public const string GetPublicKeys = @"SELECT PublicKey FROM FileData;";
    public const string GetPrivateKeyByPublicKey = @"SELECT PrivateKey FROM FileData WHERE PublicKey = @publicKey;";
    public const string GetPublicKeyByFilename = @"SELECT PublicKey FROM FileData WHERE Filename = @filename;";
    public const string GetFilenames = @"SELECT Filename FROM FileData;";
    public const string GetFileSizeSummary = @"SELECT COALESCE(SUM(FileSize), 0) FROM FileData;";

    public const string InsertFileData = @"INSERT INTO FileData (Filename, PrivateKey, PublicKey, FileSize)
      VALUES (@filename, @privateKey, @publicKey, @fileSize);";

    public const string DbName = "files.db";
}
