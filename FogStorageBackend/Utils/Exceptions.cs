namespace FogStorageBackend.Utils;

public class ShardingMismatchException : Exception
{
    public ShardingMismatchException(string message) : base(message)
    {
    }

    public ShardingMismatchException() : base()
    {
        
    }
}

public class ShardExistsException : Exception
{
    public ShardExistsException(string message) : base(message)
    {
    }

    public ShardExistsException() : base()
    {
        
    }
}