using FogStorageBackend.Constants;

namespace FogStorageBackend.REST;
using RestSharp;

public class WebHandler
{
    public WebHandler()
    {
        
    }

    // Test function, will be deleted later
    public void GetHello()
    {
        var client = new RestClient(WebConstants.ServerAddress);
        var request = new RestRequest("", Method.Get);

        RestResponse response = client.Execute(request);

        if (response.IsSuccessful)
        {
            Console.WriteLine(response.Content);
        }
        else
        {
            Console.WriteLine("Request failed:");
            Console.WriteLine(response.ErrorMessage);
        }
    }

    private void SaveShardRequest()
    {
        
    }

    public void SaveFileRequest()
    {
        
    }

    public void DeleteFileRequest()
    {
        
    }
    
    public void LoadShardsRequest()
    {
        
    }
    
    // should be parametrized: must be a bool value in case we want to duplicate shard in case of only 1 is available
    public void CheckFileAvailability()
    {
        
    }
}
