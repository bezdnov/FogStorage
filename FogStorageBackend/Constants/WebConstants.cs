namespace FogStorageBackend.Constants;

public static class WebConstants
{
    public static readonly string ServerAddress = Environment.GetEnvironmentVariable("SERVER_ADDRESS") ?? "localhost";
    
}
